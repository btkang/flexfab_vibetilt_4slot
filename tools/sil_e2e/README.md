# tools/sil_e2e — SIL 화면 포함 자동 실행 (E2E·밤샘)

실물 지그 없이 **실제 flexfab 프로그램 전체**(화면·판정·저장·pending)를 가짜 장비로 돌린다.
가짜 장비는 `tools/ff_simulator`, 이 폴더는 그 위에서 **작업자 대신 버튼을 누르고 판정하는 구동기**다.

- 원리·도입 방법: `D:\03.DEVELOPE-RC-GIT\SIL_참고키트_VibeTilt4슬롯_R1_261001\참고문서\REF_SIL테스트_도입가이드_20260929_v01.md`
- 레드마인: #4901 (SIL 결과 기록)
- **SIL 통과 ≠ 실기 통과.** 배선·포트·실제 잡음·타이밍·펌웨어 특이 동작은 실기 절차서로

## 구성

| 파일 | 역할 |
|---|---|
| `simui/` | E2E 구동기 (WinForms). flexfab `MainForm`을 리플렉션으로 띄우고 Start → 시리얼 창 입력 → 팝업 응답 → 사이클별 판정·메모리 기록 |
| `setup_run.py` | Debug 출력 → 격리 실행 폴더 복사, simui 빌드·복사, config.ini(DB 업로드 끔) |
| `gen_cycles.py` | 사이클 목록 생성 (25종 1블록: 정상·고장·pending 이상·저장 실패·작업자 제외·샘플 부족·재완성) |
| `scenarios/*.json` | 고장 주입 규칙 (`sim_scenario.json`으로 복사되어 쓰임) |
| `scan_result.py` | 실행 후 결과 폴더 검사 (잘못된 PASS·중복·보관 경로) |
| `gen_cycles_2slot.py` | 2슬롯 회귀 사이클 16개 (4슬롯 프로그램으로 2슬롯 운용 확인) |
| `compare_runs.py` | **두 프로그램 비교** — 같은 사이클을 기준(양산 배포본)과 신규에 돌린 리포트를 사이클별 비교 (팝업·pending·화면 결과·결과 파일) |
| `rm_watch.py`, `rm.py` | 밤샘 중 2시간마다 레드마인 #4901 자동 요약 (키는 `~/.claude/redmine.env`) |

## 순서

```powershell
# 0) 빌드 (레포 루트)
dotnet build flexfab.sln
dotnet build tools/ff_simulator

# 1) SIM 워크스페이스 생성 — 양산(MP)과 같은 항목의 TEST에서 만든다. 설정이 바뀌면 다시 실행
python tools/ff_simulator/gen_sim_workspace.py

# 2) 실행 폴더 준비 (기본 작업폴더 = 레포 옆 ../sil_work)
python tools/sil_e2e/setup_run.py runA SIM_workspace_motion_232_VL10_4slot.json

# 3) 사이클 목록 (232, 시리얼 VL1-90001부터, 1블록=25사이클)
python tools/sil_e2e/gen_cycles.py 232 90000 1 ../sil_work/cycles_A.txt

# 4) 실행 (마감 시각까지 반복, 생략 시 목록 끝까지)
cd ../sil_work/runA
./simui.exe ../report_A.txt ../cycles_A.txt "2026-10-02 08:20"

# 5) 결과
python tools/sil_e2e/scan_result.py ../sil_work/runA
```

- 4슬롯 1사이클 약 1.5~2.5분, 25사이클 약 45분~1시간. 밤샘은 232·485를 순서대로(마감 시각을 나눠서)
- 실행 중 PC 절전은 simui가 막는다(화면 꺼짐은 괜찮음, 로그오프·종료는 멈춤)
- 2슬롯: `SIM_workspace_motion_232_VL10.json` 사용, 사이클의 X1·Y1 칸만 씀 (시리얼 창 2칸 자동 인식)

## 양산본과 비교 (회귀)

```powershell
# 기준 = 양산 배포본 복사본에 ff_simulator.dll·SIM 워크스페이스·simui 넣고, 신규 = setup_run.py 로 준비
./simui.exe ../report_B.txt ../cyc.txt     # 기준 실행 폴더에서
./simui.exe ../report_N.txt ../cyc.txt     # 신규 실행 폴더에서
python tools/sil_e2e/compare_runs.py report_B.txt report_N.txt <기준 실행폴더> <신규 실행폴더>
```
- 의도한 변경(문구 추가 등)만 차이로 나와야 한다

## 사이클 파일 형식

```
이름|X1|X2|Y1|Y2|고장시나리오(없으면 -)|예아니오(Y/N)|준비동작(;구분)|기대(&구분)
N01_04_X1고장|VL1-90007|VL1-90008|VL1-90005|VL1-90006|scen_x1offset.json|Y||X1슬롯 FAIL(FAIL #4)&Y1슬롯 완성&pending=1
```

**준비동작** (사이클 전에 실행 폴더를 조작)

| 동작 | 뜻 |
|---|---|
| `clean` | pending·serial 파일 삭제 |
| `pend:<레인>:<시리얼>:<ok\|wsbad\|noresult\|old25h\|noserial>` | 대기 기록 직접 생성 (정상/워크스페이스 불일치/결과 없음/25시간 경과/시리얼 없음) |
| `legacy:<레인>` / `inuse:<레인>:<시리얼>` | 구 형식 pending / 잔존 `.inuse` |
| `tmpdir:<레인>` / `rmtmpdir:<레인>` | pending 저장 실패 유도(같은 이름 폴더) / 해제 |
| `jsondir:<시리얼>` / `rmjsondir:<시리얼>` | 결과 JSON 저장 실패 유도 / 해제 |
| `repeat:<N>` / `fc:<0\|1>` | 반복 횟수 / fail_continue |
| `skiprow:<i>` / `unskip` | 작업자 항목 제외(번호셀 클릭) / 해제 |
| `stop:<초>` | 시작 후 N초에 STOP |

**기대** (팝업 문구 기준, 모두 맞아야 `CHECK OK`)

| 형식 | 뜻 |
|---|---|
| `문구` | 팝업·결과 문구에 포함 |
| `!문구` | 포함되면 안 됨 |
| `pending=N` | 종료 후 `pending_*.json` 수 |
| `nopopup` | 팝업이 하나도 없어야 함 |

## 고장 주입 (`scenarios/`)

```json
{"rules":[{"target":"uart_232","match":"<OFFSET","action":"fail","times":1}]}
```
- `target`: 라이브러리 id(`uart_232`, `uart_232_y`, `uart_gyro` …), `match`: 명령 앞부분
- `action`: `fail`(판정코드 1) · `timeout`(무응답) · `garbage` · `exception` · `stream_offset`(스트림 값 틀기, `mode`·`value`) · `stream_every`(N개 중 1개만 보냄 → 샘플 부족)
- `times`: 적용 횟수, `skip`: 처음 N번은 건너뜀 (같은 명령이 여러 항목에서 쓰일 때)

## 리포트 읽기

- `===== CYCLE 이름` → 준비동작 → 팝업 → `소요` → `CHECK OK / MISMATCH` → `[MEM]` → `GRID`(화면 결과 표)
- 불일치는 먼저 **테스트 입력 문제인지**(예: 485에 `VL1-` 시리얼 → 접두사 팝업) 확인 후 프로그램 결함 판단
- `[MEM]` Private가 계속 늘면 누수 의심 (0.2.4.8에서 화면 로그 상한으로 해결한 사례)

## 이력

- 2026-09-29 작성(scratchpad), 2026-09-30 밤샘 1차 387사이클, 10-01 2차 446사이클
- 2026-10-01 레포 이전, 2슬롯 시리얼 창 지원, SIM을 MP 항목 기준으로 재생성
