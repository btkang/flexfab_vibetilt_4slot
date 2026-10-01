"""SIL E2E 실행 폴더 준비 — Debug 출력을 격리 복사본으로 만들고 E2E 구동기(simui)를 넣는다.

사용: python tools/sil_e2e/setup_run.py <실행이름> <SIM 워크스페이스 파일명> [작업폴더]
  예: python tools/sil_e2e/setup_run.py runA SIM_workspace_motion_232_VL10_4slot.json
  작업폴더 기본값: 레포 옆 ../sil_work  (실행 폴더·리포트·고장 시나리오가 모이는 곳, 레포 밖)

하는 일
  1. output/Debug/net8.0-windows → <작업폴더>/<실행이름> 복사 (Result·Log·pending·sim_scenario 제외, 기존 폴더는 지움)
  2. simui 빌드(없으면) 후 실행 폴더에 복사
  3. Config/config.ini: LastWorkspace=<SIM 워크스페이스>, MongoDBUpload=False, Inspector=SIM검사
  4. scenarios/*.json → <작업폴더>/ (simui가 실행 폴더의 상위에서 고장 시나리오를 찾음)
그다음: <작업폴더>/<실행이름>/simui.exe <리포트.txt> <cycles.txt> [마감 'YYYY-MM-DD HH:MM']
"""
import glob
import os
import re
import shutil
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", ".."))
SRC = os.path.join(REPO, "output", "Debug", "net8.0-windows")
UI_PROJ = os.path.join(HERE, "simui")
UI_OUT = os.path.join(UI_PROJ, "bin", "out")


def lp(p):
    """Windows 260자 경로 제한 회피."""
    p = os.path.abspath(p)
    return p if p.startswith("\\\\?\\") or os.name != "nt" else "\\\\?\\" + p


def main():
    if len(sys.argv) < 3:
        print(__doc__)
        sys.exit(1)
    runname, ws = sys.argv[1], sys.argv[2]
    work = os.path.abspath(sys.argv[3] if len(sys.argv) > 3 else os.path.join(REPO, "..", "sil_work"))
    os.makedirs(work, exist_ok=True)
    if not os.path.exists(os.path.join(SRC, ws)):
        sys.exit(f"SIM 워크스페이스 없음: {ws} — 먼저 python tools/ff_simulator/gen_sim_workspace.py")
    if not os.path.exists(os.path.join(SRC, "ff_simulator.dll")):
        sys.exit("ff_simulator.dll 없음 — 먼저 dotnet build tools/ff_simulator")

    run = os.path.join(work, runname)
    if os.path.exists(run):
        shutil.rmtree(lp(run))

    def ignore(d, names):
        skip = set(shutil.ignore_patterns("Result", "Result_보관", "Log", "pending_*", "sim_scenario.json",
                                          "workspace_backup")(d, names))
        if os.path.basename(d) == "runtimes":
            skip |= {n for n in names if not n.startswith("win")}
        return skip

    shutil.copytree(SRC, run, ignore=ignore)

    if not os.path.exists(os.path.join(UI_OUT, "simui.exe")):
        subprocess.run(["dotnet", "build", UI_PROJ, "-c", "Release", "-o", UI_OUT], check=True)
    for f in glob.glob(os.path.join(UI_OUT, "simui*")):
        shutil.copy(f, run)

    ini = os.path.join(run, "Config", "config.ini")
    os.makedirs(os.path.dirname(ini), exist_ok=True)
    s = open(ini, encoding="utf-8-sig").read() if os.path.exists(ini) else "[General]\n"
    for key, val in (("LastWorkspace", ws), ("MongoDBUpload", "False"), ("Inspector", "SIM검사")):
        if re.search(rf"(?m)^{key}=", s):
            s = re.sub(rf"(?m)^{key}=.*$", f"{key}={val}", s)
        else:
            s = s.replace("[General]", f"[General]\n{key}={val}", 1)
    open(ini, "w", encoding="utf-8").write(s)

    for f in glob.glob(os.path.join(HERE, "scenarios", "*.json")):
        shutil.copy(f, work)
    print("ready", run)


if __name__ == "__main__":
    main()
