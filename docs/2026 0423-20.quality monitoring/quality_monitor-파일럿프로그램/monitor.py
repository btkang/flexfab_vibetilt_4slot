"""CLI: 모션 MP 검사 결과 요약.

사용 예:
  python monitor.py --result-dir "<경로>"
  python monitor.py --result-dir "<경로>" --days 30
  python monitor.py --result-dir "<경로>" --out csv --csv-path events.csv
"""
from __future__ import annotations

import argparse
from pathlib import Path

import pandas as pd

from parser import load_results, serial_pass_fail


def print_summary(df: pd.DataFrame) -> None:
    if df.empty:
        print("스캔된 측정 이벤트가 없습니다.")
        return

    serials = serial_pass_fail(df)
    total = len(serials)
    n_pass = (serials["overall"] == "PASS").sum()
    n_fail = total - n_pass
    rate = (n_fail / total * 100) if total else 0.0

    print("=" * 60)
    print(f"총 시리얼: {total} · PASS: {n_pass} · FAIL: {n_fail} · 불량률: {rate:.1f}%")
    print(f"측정 이벤트: {len(df):,} 행 (TILT {len(df[df['kind']=='TILT']):,} / VIBE {len(df[df['kind']=='VIBE']):,})")
    if df["time"].notna().any():
        t_min = df["time"].min().strftime("%Y-%m-%d %H:%M")
        t_max = df["time"].max().strftime("%Y-%m-%d %H:%M")
        print(f"기간: {t_min} ~ {t_max}")
    print("=" * 60)

    print("\n[항목별 FAIL 카운트]")
    fails = df[df["result"] != "OK"]
    if fails.empty:
        print("  (없음)")
    else:
        grp = fails.groupby(["kind", "axis", "item"]).size().reset_index(name="fail_count")
        print(grp.to_string(index=False))

    print("\n[TILT 각도별 오차(value) 요약]")
    tilt = df[df["kind"] == "TILT"]
    if tilt.empty:
        print("  (없음)")
    else:
        stat = (
            tilt.groupby(["axis", "angle"])["value"]
            .agg(["count", "median", "std", "min", "max"])
            .round(4)
        )
        print(stat.to_string())

    print("\n[VIBE 축별 1G 값 요약]")
    vibe = df[df["kind"] == "VIBE"]
    if vibe.empty:
        print("  (없음)")
    else:
        stat = (
            vibe.groupby("axis")["value"]
            .agg(["count", "median", "std", "min", "max"])
            .round(4)
        )
        print(stat.to_string())


def main() -> None:
    ap = argparse.ArgumentParser(description="모션 MP Result JSON 집계")
    ap.add_argument("--result-dir", required=True, help="Result/모션지그검사(RS-485)_MP 경로")
    ap.add_argument("--days", type=int, default=None, help="최근 N일만")
    ap.add_argument("--out", choices=["console", "csv"], default="console")
    ap.add_argument("--csv-path", default="events.csv", help="--out csv 일 때 저장 경로")
    args = ap.parse_args()

    path = Path(args.result_dir)
    if not path.exists():
        raise SystemExit(f"경로 없음: {path}")

    df = load_results(path, days=args.days)

    if args.out == "csv":
        df.to_csv(args.csv_path, index=False, encoding="utf-8-sig")
        print(f"CSV 저장: {args.csv_path} ({len(df):,} 행)")
        return

    print_summary(df)


if __name__ == "__main__":
    main()
