# -*- coding: utf-8 -*-
"""
2슬롯 모션 워크스페이스 → 4슬롯(X1·X2·Y1·Y2) 워크스페이스 생성기
PLAN_4슬롯_메인폼_병렬측정_구현계획_20260918_v01 §2 기준.

사용: python tools/gen_workspace_4slot.py
 - output/Debug, output/Release 각 경로의 MP_/TEST_ 232/485 원본에서 *_4slot.json 생성
 - 원본(2슬롯) 파일은 수정하지 않음
 - 재실행 시 *_4slot.json 덮어씀 (손편집한 값은 사라지므로, 손편집 후에는 재실행 금지)
"""
import copy
import io
import json
import os
import re
import sys
from collections import OrderedDict

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PATHS = ["output/Debug/net8.0-windows", "output/Release/net8.0-windows"]
SOURCES = [
    ("MP_workspace_motion_232_VL10.json", "232"),
    ("TEST_workspace_motion_232_VL10.json", "232"),
    ("MP_workspace_motion_485_VL20.json", "485"),
    ("TEST_workspace_motion_485_VL20.json", "485"),
]

# 항목 번호 → slot_group (PLAN §2.3)
SLOT_GROUP = {
    "#0": "common", "#3": "common", "#10": "common",
    "#1-X": "X", "#2": "X", "#4": "X", "#5": "X", "#5-1": "X",
    "#8": "Y", "#9": "Y", "#9-1": "Y", "#11": "Y", "#11-1": "Y", "#12": "Y",
    "#13": "Y", "#14": "Y", "#15": "Y", "#16": "Y",
    "#6": "all", "#7": "all",
}
JUDGE_FROM = {"#14": "#6", "#15": "#7"}
# R1 합의 순서 (X 준비 → Y 준비 → 동시측정 → Y 마무리)
# #2(UID X)·#5(SAVE X)·#5-1(정지검사)은 TEST/MP에 따라 있거나 없음 → 있는 것만 이 순서로
ORDER = ["#0", "#1-X", "#2", "#3", "#4", "#5", "#5-1",
         "#8", "#9", "#9-1", "#11", "#11-1", "#12",
         "#6", "#7", "#10", "#13", "#14", "#15", "#16"]
OPTIONAL = {"#2", "#5", "#5-1"}


def proc_no(p):
    m = re.match(r"(#[\w\-]+)\s", p.get("name", ""))
    return m.group(1) if m else None


def insert_after(od, after_key, new_key, new_val):
    items = list(od.items())
    out = OrderedDict()
    for k, v in items:
        out[k] = v
        if k == after_key:
            out[new_key] = new_val
    return out


def make_uart(base, slot, bus):
    u = copy.deepcopy(base)
    u["id"] = f"uart_{bus}_{slot.lower()}"
    cfg = OrderedDict()
    cfg["port"] = "COM0"
    cfg["_port_comment"] = f"TODO 4슬롯 지그 배선 확정 후 실제 COM 기입 ({slot} 슬롯). serial 채우면 [자동(FTDI)]로 매칭"
    cfg["serial"] = ""
    cfg["_serial_comment"] = f"{bus} {slot} 컨버터 FTDI 고유시리얼 — 슬롯↔포트 고정 기준. TODO 실측 기입"
    for k, v in base["config"].items():
        if k in ("port", "serial") or k.startswith("_"):
            continue
        cfg[k] = v
    u["config"] = cfg
    return u


def convert(d, bus):
    d = copy.deepcopy(d)
    ux, uy = f"uart_{bus}", f"uart_{bus}_y"
    ux2, uy2 = f"uart_{bus}_x2", f"uart_{bus}_y2"

    # ── libraries: X2, Y2 uart 추가 ──
    libs = d["libraries"]
    base_x = next(l for l in libs if l["id"] == ux)
    base_y = next(l for l in libs if l["id"] == uy)
    new_libs = []
    for l in libs:
        new_libs.append(l)
        if l["id"] == ux:
            new_libs.append(make_uart(base_x, "X2", bus))
        elif l["id"] == uy:
            new_libs.append(make_uart(base_y, "Y2", bus))
    d["libraries"] = new_libs

    # ── 최상위: slot_layout / slot_uarts / clear_log_on_run_start ──
    d = insert_after(d, "_현재_VIBE", "slot_layout", "4")
    d = insert_after(d, "slot_layout", "_slot_layout_comment",
                     "4=X1·X2·Y1·Y2 밀어내기 2레인(X1→Y1, X2→Y2), 사이클당 2대 완성. 2 또는 없음=기존 2슬롯. 호스트가 결과열·시리얼팝업·pending 개수 결정")
    d = insert_after(d, "_slot_layout_comment", "slot_uarts",
                     OrderedDict([("X1", ux), ("X2", ux2), ("Y1", uy), ("Y2", uy2)]))
    d = insert_after(d, "slot_uarts", "_slot_uarts_comment",
                     "슬롯→uart 라이브러리 id. X1/Y1은 기존 id 유지(호환). 호스트가 slot_group=X/Y 항목의 uart_id를 이 표로 치환해 슬롯별 호출")
    d = insert_after(d, "_slot_uarts_comment", "clear_log_on_run_start", 0)
    d = insert_after(d, "clear_log_on_run_start", "_clear_log_on_run_start_comment",
                     "1=시작 시 화면 로그 지움(한 사이클분만 표시). 로그 파일 Log/{proj}/YYYY-MM-DD.log는 계속 append. 기본 0=기존 동작")
    d["_작업자_변경가능"] = d["_작업자_변경가능"] + " | clear_log_on_run_start"

    # ── project ──
    prj = d["projects"][0]
    prj["id"] = prj["id"] + "-4slot"
    prj["name"] = prj["name"] + "_4slot"
    d["active_project"] = prj["id"]   # 실행 시 프로젝트 매칭 (id 변경에 맞춤)
    dep = list(prj.get("library_depencency", []))
    for k, v in ((ux, ux2), (uy, uy2)):
        if k in dep and v not in dep:
            dep.insert(dep.index(k) + 1, v)
    prj["library_depencency"] = dep
    prj["_proc_order"] = ("4슬롯 밀어내기 2레인. 순서(R1 합의): X준비(#0 #1-X #3 #4 #5-1, X1‖X2) → Y준비(#8 #9 #9-1 #11 #11-1 #12, Y1‖Y2) "
                          "→ 동시측정(#6 #7: 모터 1회 이동 → X1·X2·Y1·Y2 동시 수신) → #10 → #13 → #14/#15(#6/#7 측정값 판정만) → #16")
    prj["_slot_group_comment"] = ("slot_group: common=모터·경사계 공통 1회 | X=X1‖X2 동시 호출 | Y=Y1‖Y2 동시 호출 | all=1회 호출, 모듈이 4보드 동시 수신. "
                                  "호스트가 X/Y 항목의 param을 슬롯별로 복제(uart_id·__serial 치환)해 호출. 없으면 2슬롯 기존 동작")

    # ── procs: slot_group 부여 + 순서 재배치 ──
    procs = prj["procs"]
    by_no = OrderedDict()
    for p in procs:
        no = proc_no(p)
        if no is None:
            raise SystemExit(f"항목 번호 파싱 실패: {p.get('name')}")
        by_no[no] = p
    missing = [n for n in ORDER if n not in by_no and n not in OPTIONAL]
    extra = [n for n in by_no if n not in ORDER]
    if missing or extra:
        raise SystemExit(f"항목 불일치 missing={missing} extra={extra}")
    order = [n for n in ORDER if n in by_no]

    for no, p in by_no.items():
        param = p.setdefault("param", OrderedDict())
        new = OrderedDict()
        new["slot_group"] = SLOT_GROUP[no]
        if no in ("#6", "#7"):
            new["_slot_all_comment"] = ("동시측정: 모터 1회 이동·수렴 → 경사계 g 1회 → X1·X2·Y1·Y2 동시 수신·판정. "
                                        "X슬롯 기준 g, Y슬롯 기준 -g(setdir6/setdir5 부호 반대, V1 실측 확인). "
                                        "Y슬롯 측정값은 #14/#15 판정용으로 저장. __slot_uarts 없으면 기존 단일보드 동작")
        if no in JUDGE_FROM:
            new["judge_from"] = JUDGE_FROM[no]
            new["_judge_from_comment"] = (f"{JUDGE_FROM[no]} 동시측정에서 저장한 Y슬롯 값으로 판정만 수행(모터 이동 없음). "
                                          "저장값 없으면(중단·재검 등) 기존처럼 직접 이동·측정. 판정 기준 동일")
        for k, v in param.items():
            new[k] = v
        p["param"] = new

    prj["procs"] = [by_no[n] for n in order]
    return d


def main():
    for path in PATHS:
        for fname, bus in SOURCES:
            src = os.path.join(ROOT, path, fname)
            if not os.path.exists(src):
                print("skip (없음):", src)
                continue
            raw = io.open(src, "r", encoding="utf-8-sig").read()
            d = json.loads(raw, object_pairs_hook=OrderedDict)
            out = convert(d, bus)
            dst = os.path.join(ROOT, path, fname.replace(".json", "_4slot.json"))
            text = json.dumps(out, ensure_ascii=False, indent=4)
            io.open(dst, "w", encoding="utf-8", newline="\r\n").write(text + "\n")
            print("wrote:", os.path.relpath(dst, ROOT))


if __name__ == "__main__":
    sys.exit(main())
