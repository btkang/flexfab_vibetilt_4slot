"""
SIM 워크스페이스 생성 (2026-09-29, 레드마인 #4901)

TEST_workspace_motion_{232_VL10|485_VL20}_4slot.json 을 복사해 SIM_ 워크스페이스를 만든다.
바꾸는 것은 아래뿐 — 검사 항목(procs)·판정 기준·slot_layout·slot_uarts 는 원본 그대로 둬야
검증 대상이 실제 설정과 같아진다.
  - libraries: ff_common Uart/Tcp → ff_simulator UartSim/TcpSim (+ sim_* config)
  - mongodb_uri: 닿지 않는 로컬 주소 → 가짜 결과가 양산 DB(ctsm.product)에 올라가지 않음
  - projects[0].name 끝에 _SIM → Result/Log 폴더가 실제 결과와 분리

Debug 폴더에만 만든다 (시뮬 DLL 은 Debug 에만 빌드 — Release 배포본에 섞이지 않게).
실행: python tools/ff_simulator/gen_sim_workspace.py   (재실행 시 SIM_ 파일 덮어씀)
"""
import json
import os

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "output", "Debug", "net8.0-windows")
SOURCES = ["TEST_workspace_motion_232_VL10_4slot.json", "TEST_workspace_motion_485_VL20_4slot.json"]
DEAD_MONGO = "mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=300&connectTimeoutMS=300&directConnection=true"


def expected(procs, prefix, group, key, test_index=0):
    for p in procs:
        prm = p.get("param", {})
        if p["id"].startswith(prefix) and prm.get("slot_group") == group:
            tests = prm.get("tests", [])
            if len(tests) > test_index:
                return tests[test_index].get(key)
    return None


def main():
    for src in SOURCES:
        path = os.path.join(OUT, src)
        with open(path, encoding="utf-8-sig") as f:
            ws = json.load(f)
        proto = "485" if "_485_" in src else "232"
        procs = ws["projects"][0]["procs"]
        fw_x = expected(procs, "FWVER_", "X", "expected_fw_version")
        fw_y = expected(procs, "FWVER_", "Y", "expected_fw_version")
        appcfg = expected(procs, "APPCFG_" + proto, "Y", "expected_response", 1)

        for lib in ws["libraries"]:
            lid = lib["id"]
            cls = lib.get("classname", "")
            if cls == "Cantops.FlexFab.Uart":
                lib["filename"] = "ff_simulator.dll"
                lib["classname"] = "Cantops.FlexFab.UartSim"
                cfg = lib.setdefault("config", {})
                cfg["sim_id"] = lid
                if lid == "uart_gyro":
                    cfg["sim_role"] = "gyro"
                else:
                    axis = "Y" if "_y" in lid else "X"
                    cfg.update({"sim_role": "vl", "sim_proto": proto, "sim_axis": axis,
                                "sim_fw": fw_y if axis == "Y" else fw_x, "sim_appcfg": appcfg})
            elif cls == "Cantops.FlexFab.Tcp":
                lib["filename"] = "ff_simulator.dll"
                lib["classname"] = "Cantops.FlexFab.TcpSim"
                lib.setdefault("config", {})["sim_id"] = lid

        ws["mongodb_uri"] = DEAD_MONGO
        ws["_sim_comment"] = ("시뮬 검증 전용 (tools/ff_simulator/gen_sim_workspace.py 생성, 손편집 금지). "
                              f"원본 {src} — libraries 만 가짜 통신으로 교체, DB 업로드 차단, 결과 폴더 _SIM 분리. "
                              "고장 주입 = 실행 폴더 sim_scenario.json")
        name = ws["projects"][0].get("name", "")
        if not name.endswith("_SIM"):
            ws["projects"][0]["name"] = name + "_SIM"

        dst = os.path.join(OUT, src.replace("TEST_", "SIM_", 1))
        with open(dst, "w", encoding="utf-8") as f:
            json.dump(ws, f, ensure_ascii=False, indent=4)
        print(f"생성: {os.path.basename(dst)}  (fw X={fw_x} / Y={fw_y}, appcfg={appcfg})")


if __name__ == "__main__":
    main()
