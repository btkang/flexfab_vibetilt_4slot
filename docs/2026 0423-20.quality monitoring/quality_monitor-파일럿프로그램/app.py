"""Streamlit 대시보드: 모션 MP 품질 모니터링.

실행:
  streamlit run app.py -- --result-dir "<Result 경로>"

사이드바에서 경로를 직접 변경할 수도 있음.
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

import pandas as pd
import plotly.express as px
import plotly.graph_objects as go
import streamlit as st

from parser import load_results, serial_pass_fail
from spc import MIN_SAMPLES_FOR_LIMITS, compute_limits, cpk, detect_western_electric


def _cli_args() -> dict:
    ap = argparse.ArgumentParser(add_help=False)
    ap.add_argument("--result-dir", default="")
    known, _ = ap.parse_known_args(sys.argv[1:])
    return {"result_dir": known.result_dir}


@st.cache_data(show_spinner=False)
def _load(path_str: str) -> pd.DataFrame:
    return load_results(Path(path_str))


def _sidebar(default_dir: str) -> tuple[str, int | None, list[str]]:
    st.sidebar.header("설정")
    result_dir = st.sidebar.text_input("Result 경로", value=default_dir)
    days = st.sidebar.selectbox(
        "기간 (최근)", options=[None, 1, 7, 30, 90], index=0,
        format_func=lambda x: "전체" if x is None else f"{x}일",
    )
    if st.sidebar.button("캐시 새로고침"):
        st.cache_data.clear()
    return result_dir, days, []


def _filter_days(df: pd.DataFrame, days: int | None) -> pd.DataFrame:
    if days is None or df.empty:
        return df
    cutoff = pd.Timestamp.now(tz="UTC") - pd.Timedelta(days=days)
    return df[df["time"] >= cutoff].reset_index(drop=True)


def tab_today(df: pd.DataFrame) -> None:
    st.subheader("오늘 요약")
    if df.empty:
        st.info("데이터 없음")
        return

    today = pd.Timestamp.now(tz="UTC").normalize()
    today_df = df[df["time"] >= today]
    serials_today = serial_pass_fail(today_df) if not today_df.empty else pd.DataFrame()
    serials_all = serial_pass_fail(df)

    col1, col2, col3, col4 = st.columns(4)
    col1.metric("오늘 시리얼", len(serials_today))
    if not serials_today.empty:
        col2.metric("오늘 PASS", int((serials_today["overall"] == "PASS").sum()))
        col3.metric("오늘 FAIL", int((serials_today["overall"] == "FAIL").sum()))
    else:
        col2.metric("오늘 PASS", 0)
        col3.metric("오늘 FAIL", 0)
    col4.metric("전체 시리얼", len(serials_all))

    st.markdown("---")
    st.markdown("### 작업자별")
    st.dataframe(
        df.groupby("inspector")["sn"].nunique().rename("시리얼 수").reset_index(),
        use_container_width=True, hide_index=True,
    )

    st.markdown("### 시리얼 검색")
    q = st.text_input("시리얼 번호", placeholder="예: VL2-00001")
    if q:
        hit = df[df["sn"].str.contains(q, case=False, na=False)]
        if hit.empty:
            st.warning("일치 없음")
        else:
            st.dataframe(hit, use_container_width=True, hide_index=True)


def tab_tilt_trend(df: pd.DataFrame) -> None:
    st.subheader("TILT 추이 (오차 value 시계열)")
    tilt = df[df["kind"] == "TILT"].copy()
    if tilt.empty:
        st.info("TILT 데이터 없음")
        return

    axes = st.multiselect("축", ["X", "Y"], default=["X", "Y"])
    angles = sorted(tilt["angle"].dropna().unique().tolist())
    sel_angles = st.multiselect("각도", angles, default=angles)

    sub = tilt[tilt["axis"].isin(axes) & tilt["angle"].isin(sel_angles)].sort_values("time")
    if sub.empty:
        st.info("필터 결과 없음")
        return

    sub["label"] = sub["axis"] + " " + sub["angle"].astype(str) + "°"
    fig = px.line(
        sub, x="time", y="value", color="label", markers=True,
        labels={"value": "오차(도)", "time": "시각"},
    )
    for lim, ref in ((0.2, 20), (0.5, 45)):
        fig.add_hline(y=lim, line_dash="dot", opacity=0.3, annotation_text=f"±{lim}°(±{ref})")
        fig.add_hline(y=-lim, line_dash="dot", opacity=0.3)
    st.plotly_chart(fig, use_container_width=True)


def tab_tilt_dist(df: pd.DataFrame) -> None:
    st.subheader("TILT 분포")
    tilt = df[df["kind"] == "TILT"].copy()
    if tilt.empty:
        st.info("TILT 데이터 없음")
        return

    tilt["label"] = tilt["axis"] + " " + tilt["angle"].astype(str) + "°"
    fig_hist = px.histogram(tilt, x="value", color="label", barmode="overlay", nbins=40)
    fig_box = px.box(tilt, x="label", y="value", points="all")
    st.plotly_chart(fig_hist, use_container_width=True)
    st.plotly_chart(fig_box, use_container_width=True)

    st.markdown("### 통계 요약")
    stat = (
        tilt.groupby(["axis", "angle"])["value"]
        .agg(["count", "median", "std", "min", "max"])
        .round(4)
        .reset_index()
    )
    st.dataframe(stat, use_container_width=True, hide_index=True)


def tab_vibe(df: pd.DataFrame) -> None:
    st.subheader("VIBE 추이/분포")
    vibe = df[df["kind"] == "VIBE"].copy()
    if vibe.empty:
        st.info("VIBE 데이터 없음")
        return

    col1, col2 = st.columns(2)
    with col1:
        fig_t = px.line(
            vibe.sort_values("time"), x="time", y="value", color="axis", markers=True,
            labels={"value": "1G 측정값"},
        )
        fig_t.add_hline(y=1.01, line_dash="dot", opacity=0.3)
        fig_t.add_hline(y=0.99, line_dash="dot", opacity=0.3)
        st.plotly_chart(fig_t, use_container_width=True)
    with col2:
        fig_h = px.histogram(vibe, x="value", color="axis", barmode="overlay", nbins=30)
        st.plotly_chart(fig_h, use_container_width=True)

    st.markdown("### 축간 비교 (같은 시리얼의 X/Y/Z 묶음)")
    wide = vibe.pivot_table(index="sn", columns="axis", values="value", aggfunc="first")
    if {"X", "Y", "Z"}.issubset(wide.columns):
        wide["norm"] = (wide["X"] ** 2 + wide["Y"] ** 2 + wide["Z"] ** 2) ** 0.5
        st.dataframe(wide.round(4), use_container_width=True)
        st.caption("norm = √(X²+Y²+Z²). 이상적으로 1.0 근처.")


def tab_tilt_triplet(df: pd.DataFrame) -> None:
    st.subheader("TILT: gyro / vl / value 비교")
    tilt = df[df["kind"] == "TILT"].copy()
    if tilt.empty:
        st.info("TILT 데이터 없음")
        return

    tilt["label"] = tilt["axis"] + " " + tilt["angle"].astype(str) + "°"
    tall = tilt.melt(
        id_vars=["sn", "time", "axis", "angle", "label"],
        value_vars=["value", "gyro", "vl"],
        var_name="field", value_name="measure",
    )
    fig = px.scatter(
        tall, x="time", y="measure", color="field", facet_col="label", facet_col_wrap=2,
        height=600,
    )
    st.plotly_chart(fig, use_container_width=True)
    st.caption("value=오차, gyro=자이로 읽음값, vl=기울기센서 읽음값")


def tab_spc(df: pd.DataFrame) -> None:
    st.subheader("SPC 관리도 (X-bar + Western Electric Rules)")
    st.caption(
        f"각 그룹 PASS 이력 기준 μ±3σ를 관리한계로 사용. "
        f"그룹별 최소 {MIN_SAMPLES_FOR_LIMITS}개 이상 필요."
    )

    tilt = df[df["kind"] == "TILT"].copy()
    vibe = df[df["kind"] == "VIBE"].copy()

    if tilt.empty and vibe.empty:
        st.info("데이터 없음")
        return

    groups: list[tuple[str, pd.DataFrame, float, float]] = []
    if not tilt.empty:
        for (axis, angle), g in tilt.groupby(["axis", "angle"]):
            lsl = g["min"].dropna().iloc[0] if not g["min"].dropna().empty else None
            usl = g["max"].dropna().iloc[0] if not g["max"].dropna().empty else None
            groups.append((f"TILT {axis} {angle:.0f}°", g.sort_values("time"), lsl, usl))
    if not vibe.empty:
        for axis, g in vibe.groupby("axis"):
            lsl = g["min"].dropna().iloc[0] if not g["min"].dropna().empty else None
            usl = g["max"].dropna().iloc[0] if not g["max"].dropna().empty else None
            groups.append((f"VIBE {axis}", g.sort_values("time"), lsl, usl))

    if not groups:
        st.info("집계할 그룹이 없습니다.")
        return

    labels = [g[0] for g in groups]
    choice = st.selectbox("그룹 선택", labels)
    label, sub, lsl, usl = next(g for g in groups if g[0] == choice)

    passed = sub[sub["result"] == "OK"]
    lim = compute_limits(passed["value"])
    cpk_val = cpk(passed["value"], lsl, usl) if (lsl is not None and usl is not None) else None

    col1, col2, col3, col4, col5 = st.columns(5)
    col1.metric("샘플 수 (PASS)", lim["n"])
    col2.metric("평균 μ", f"{lim['mean']:.4f}" if lim["mean"] is not None else "-")
    col3.metric("표준편차 σ", f"{lim['std']:.4f}" if lim["std"] is not None else "-")
    col4.metric("관리한계 ±3σ", f"[{lim['lcl']:.3f}, {lim['ucl']:.3f}]" if lim["ucl"] is not None else "-")
    col5.metric("Cpk", f"{cpk_val:.2f}" if cpk_val is not None else "-")

    if lim["mean"] is None:
        st.warning(
            f"샘플 {lim['n']}개. 관리한계/Cpk 산출에 최소 {MIN_SAMPLES_FOR_LIMITS}개 필요. "
            "데이터가 더 쌓이면 자동으로 활성화됩니다."
        )
        fig = px.line(sub, x="time", y="value", markers=True, title=label)
        if lsl is not None:
            fig.add_hline(y=lsl, line_dash="dot", line_color="red", annotation_text=f"LSL={lsl}")
        if usl is not None:
            fig.add_hline(y=usl, line_dash="dot", line_color="red", annotation_text=f"USL={usl}")
        st.plotly_chart(fig, use_container_width=True)
        return

    series = sub.set_index("time")["value"].sort_index()
    rolling = series.rolling(5, min_periods=1).mean()

    fig = go.Figure()
    fig.add_trace(go.Scatter(x=series.index, y=series.values, mode="lines+markers", name="value"))
    fig.add_trace(go.Scatter(x=rolling.index, y=rolling.values, mode="lines", name="이동평균 n=5", line=dict(dash="dash")))
    fig.add_hline(y=lim["mean"], line_color="green", annotation_text=f"μ={lim['mean']:.4f}")
    fig.add_hline(y=lim["ucl"], line_color="orange", annotation_text=f"UCL={lim['ucl']:.3f}")
    fig.add_hline(y=lim["lcl"], line_color="orange", annotation_text=f"LCL={lim['lcl']:.3f}")
    if lsl is not None:
        fig.add_hline(y=lsl, line_color="red", line_dash="dot", annotation_text=f"LSL={lsl}")
    if usl is not None:
        fig.add_hline(y=usl, line_color="red", line_dash="dot", annotation_text=f"USL={usl}")
    fig.update_layout(title=label, xaxis_title="시각", yaxis_title="value", height=500)
    st.plotly_chart(fig, use_container_width=True)

    alarms = detect_western_electric(series, lim["mean"], lim["std"])
    if alarms.empty:
        st.success("관리도 알람 없음")
    else:
        st.error(f"알람 {len(alarms)}건 감지")
        st.dataframe(alarms, use_container_width=True, hide_index=True)


def tab_fails(df: pd.DataFrame) -> None:
    st.subheader("FAIL 조회")
    fails = df[df["result"] != "OK"]
    if fails.empty:
        st.success("FAIL 없음")
        return
    st.dataframe(fails, use_container_width=True, hide_index=True)


def main() -> None:
    st.set_page_config(page_title="모션 MP 품질 모니터링", layout="wide")
    st.title("모션 MP 품질 모니터링")

    cli = _cli_args()
    result_dir, days, _ = _sidebar(cli["result_dir"])

    if not result_dir:
        st.warning("사이드바에 Result 경로를 입력하거나, `--result-dir` 인자로 실행하세요.")
        return
    if not Path(result_dir).exists():
        st.error(f"경로 없음: {result_dir}")
        return

    df = _load(result_dir)
    df = _filter_days(df, days)

    if df.empty:
        st.info("해당 경로에 파싱 가능한 Result JSON이 없습니다.")
        return

    st.caption(f"총 {len(df):,} 이벤트 · 시리얼 {df['sn'].nunique()}건 · {result_dir}")

    tabs = st.tabs([
        "오늘", "TILT 추이", "TILT 분포", "VIBE", "TILT 삼중값", "관리도", "FAIL",
    ])
    with tabs[0]: tab_today(df)
    with tabs[1]: tab_tilt_trend(df)
    with tabs[2]: tab_tilt_dist(df)
    with tabs[3]: tab_vibe(df)
    with tabs[4]: tab_tilt_triplet(df)
    with tabs[5]: tab_spc(df)
    with tabs[6]: tab_fails(df)


if __name__ == "__main__":
    main()
