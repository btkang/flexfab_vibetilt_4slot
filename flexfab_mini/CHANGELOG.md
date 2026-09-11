# flexfab_mini CHANGELOG

## 2026-06-12
- **FlexfabForm.cs** — `TryUploadToMongo` 진입부에 공통 게이트 추가: `MongoDBUpload=False`면 **DB 연결 시도 자체를 건너뜀**(즉시 return false, 로컬 JSON만 저장).
  - 사유: 제조팀 DB 미연결(랜선 없음) 환경에서 매 검사마다 연결 타임아웃(serverSelectionTimeoutMS=4000) 4~5초 낭비. 보드검사(`SaveLog`)는 이미 wantMongo 체크가 있었으나 **모션 밀어내기(`SaveLogDirect`)는 체크 누락**으로 설정이 안 먹혔음.
  - 방식: 경로마다 체크하지 않고 **모든 업로드 길목인 `TryUploadToMongo` 한 곳**에 게이트 → 보드/모션/미래 경로 일괄 제어, 누락 재발 방지. `SaveLog`의 기존 체크(라인 1308)는 이중 안전으로 유지.
  - 영향 프로젝트: `SaveLogDirect`(밀어내기 dual) 사용 검사 = 현재 ff_vibetilt 모션. ff_pim/colorimeter/e84a는 본 리포 독립 fork라 직접 영향 없음.
  - 하위호환: 기본값 `True` → 기존 동작 그대로. `MongoDBUpload=False` 설정 PC에서만 DB 스킵. 깨짐 없음.

## 2026-06-11
- **PortDeviceScanner.cs (신규)** — 포트 장치 스캐너 (read-only). 레지스트리로 현재 꽂힌 COM의 칩종류/FTDI 시리얼/USB 위치 조회. 의존성 0(Microsoft.Win32.Registry = WindowsDesktop 프레임워크 내장, .csproj 무변경).
- **FlexfabForm.cs** — `ShowPortChangeDialog` 개선: ①상단에 "현재 꽂힌 포트" 스캔 목록 표시 ②각 입력행에 현재 COM의 장치 정체 즉시 표시(장치관리자 왕복 제거) ③[자동(FTDI)] 버튼 — `config.serial`(FTDI) 기준으로 COM 자동 채움(드리프트/스왑 차단).
  - 기존 −/+·적용·JSON 저장 플로우, `textBoxes` 타입, public API 무변경.
  - 영향 프로젝트: 본 리포 독립 fork (ff_pim/colorimeter/e84a 직접 영향 없음). 변경은 순수 추가(read-only 표시 + 버튼)라 하위호환 깨짐 없음.
  - 동반 JSON: 485 모션 4종(MP/TEST × Debug/Release)에 `serial` 필드 추가 + stale 포트(COM11/12→COM17/4) 교정. `serial`은 uart.cs가 무시(통신 영향 0).
- **FlexfabForm.cs** — [적용] 시 `serial` 자동 동기화: 지정 COM이 FTDI면 `port`와 함께 그 칩의 시리얼도 JSON에 저장. 새 컨버터 교체 시 JSON 손편집 불필요(창에서 COM 선택→적용으로 끝).
  - 가드1: `serial` 필드가 이미 있는 라이브러리만 갱신(485 X/Y로 blast radius 한정, Prolific/타 워크스페이스 자동생성 안 함).
  - 가드2: 적용 시점 재스캔 + 변경 시 `[INFO] serial 갱신: …` 로그(조용한 덮어쓰기 방지). COM 동일·시리얼만 변경된 경우도 저장되도록 changed 트리거.

## 2026-06-10
- **FlexfabForm.cs** — Open Workspace 다이얼로그 기본 필터 `*.json` → `*workspace*.json`
  - 사유: 실행 폴더에 비워크스페이스 json 13개(deps/runtimeconfig/error_table 등) — 워크스페이스 8개만 바로 보이게. 드롭다운에 All JSON/All files 유지.
  - 영향: UI 필터만, 로딩 로직 무변경. 하위호환 깨짐 없음.
- **FlexfabForm.cs** — 시리얼 입력 다이얼로그 `MaxLength 9 → 11` (txtX/txtY 2곳)
  - 사유: 출하검사 Spec(20260602 기능검사 양식 #2 비고) — 시리얼 5자리(00001~99999) 소진 후 6자리(100000~)·7자리(1000000~) 확장 대응. `VL1-` 접두사 4자 + 7자리 = 11자.
  - 채번/증가 로직(PadLeft 방식)은 자연 롤오버(99999→100000)로 이미 지원 — 변경 없음.
  - 영향 프로젝트: VibeTilt (본 리포는 독립 fork — ff_pim/ff_colorimeter/ff_e84a 영향 없음)
  - 하위호환: 깨짐 없음 (입력 상한 확대만)
