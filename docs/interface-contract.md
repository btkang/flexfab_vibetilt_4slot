# flexfab VibeTilt 4-Slot Interface Contract

- Baseline Mode: observed
- Baseline Seed: .stage-pilot/bootstrap/baseline.yaml
- Status: draft
- Owner: (미정 — 사용자 결정)
- Last Updated (KST): 2026-09-30 10:55
- Source Discovery / Batch: bootstrap-baseline (2026-09-30, 저장소 관찰)
- Primary Runtime: other (Windows 데스크톱 GUI)

## Project Summary
- 진동기울기센서(VL10 RS-232 / VL20 RS-485) 양산 검사 프로그램 — 모션 지그 4슬롯 대응 flexfab 기반 데스크톱 앱

## Primary Domain
- 제조 양산 검사 (센서 보드 EOL 테스트, 모션 지그)

## Purpose
- This document captures the approved external and cross-boundary interface contracts for the project.
- It is a cross-cutting reference for REQ drafting, batch design, implementation, verification, and handoff.

## Scope
- Covers externally visible APIs, CLI surfaces, events, files, or batch I/O contracts.
- Does not duplicate full business requirements already owned by REQ documents.
- Does not replace low-level code references or generated API artifacts.

## Interface Summary
- 워크스페이스 JSON :: 검사 항목·장비·4슬롯 배치 정의 (file)
- 검사 모듈 계약 (flexfab_interfaces) :: 호스트 ↔ 검사 모듈·통신 라이브러리 (internal-service)
- 장비 통신 프로토콜 :: 제품 보드 UART·경사계 UART·EMIO TCP (other)
- 검사 결과 파일 :: 결과 JSON·FAIL JSON·성적서·검사이력·MongoDB (file)

## Interface: `워크스페이스 JSON`
- Type: file
- Consumers / Producers: 개발자·생산기술(작성) → flexfab.exe(로드)
- Purpose: 검사 항목·판정 기준·장비 연결을 코드 수정 없이 정의
- Stability: stable

### Inputs
- `libraries[]` :: `filename`·`classname`·`id`·`config` — 런타임 로딩. `config`에 포트·주소 등
- `projects[].procs[]` :: `id`(모듈 메서드명)·`libid`·`name`·`param`(`tests[]` 명령·기대 응답, `slot_group`, 판정값)
- `slot_layout: "4"`, `slot_uarts: {X1,X2,Y1,Y2}` :: 4슬롯 활성화·슬롯별 포트
- 운영 옵션 :: `test_mode`, `fail_continue`, `serial_auto_increment`, `serial_duplicate_check`, `serial_prefix(_check)`, `repeat_all_count`, `log_level`, `clear_log_on_run_start`, `pending_max_age_h`, `report_items`, `report_serial_count`, `mongodb_uri`
- 판정 조정(선택) :: `tilt_judge_*`, `vibe_judge_*`(`_min_samples` 포함)

### Outputs
- 그리드 항목·슬롯 열 구성, 실행 순서, 판정 기준

### Error Contract
- JSON 문법 오류 :: 로드 실패 팝업, 이전 워크스페이스 유지
- `slot_group` 허용값(common/X/Y/all) 외 :: Start 거부 (하드웨어 동작 전)
- `slot_uarts` 키 누락 :: 경고 로그 + 2슬롯 폴백
- 4슬롯 + `test_mode=1` :: Start 거부

### Compatibility Rules
- 코드 변경에 JSON 변경이 따르면 같은 커밋, Debug·Release 두 경로 동시 수정
- spec 수치에는 `_` 접두어 주석 필수 (프로그램은 `_` 필드 무시)
- 키가 없으면 기존 동작 유지(하위호환) — 새 옵션은 기본값을 코드에 둔다

## Interface: `검사 모듈 계약 (flexfab_interfaces)`
- Type: internal-service
- Consumers / Producers: flexfab.exe(호출) → ff_vibetilt(VibeTilt·MotionJig), ff_common(Uart·Tcp·Ctsp)
- Purpose: 호스트와 검사 모듈·통신 라이브러리 사이 공통 계약
- Stability: stable

### Inputs
- `IModule.SetConfig(config)` / `PreRun(libraries)` :: 로드 시 설정·라이브러리 주입 (파라미터 없는 생성자 필수, `info_["name"]` 필수)
- proc 메서드 `(libraries, param, logAction[, confirmCallback])` :: 4슬롯은 슬롯 복제 param에 `uart_id`·`__serial`·`__slot`, `#0 COMM_*`에는 `__skip_uarts`
- `IComm` :: `Connect()`, `Disconnect()`, `Send({body, no_newline})`, `Recv({timeout, recv_mode})` → `{value}`

### Outputs
- proc 반환 `{success: bool, retmsg: json}` :: 셀 OK/FAIL, 결과 JSON·불량보기 원천

### Error Contract
- 모듈 예외 :: 슬롯 항목은 그 슬롯 FAIL `예외: …`·계속, 공통 항목은 4칸 FAIL + 중단(fail_continue 꺼짐)
- 라이브러리·메서드 미발견 :: 4슬롯은 전 슬롯 FAIL(계속)
- 연결 실패(`IComm` 하나라도) :: 사이클 시작 전 중단

### Compatibility Rules
- `ff_common`·`flexfab_interfaces`는 공용 — 수정 금지(요청 시 사용자 확인)
- IProcess 시그니처·모듈 경계·public API 변경 금지
- 모듈은 통신 객체를 `IComm`으로만 사용 (SIL 시뮬레이터 교체의 전제)

## Interface: `장비 통신 프로토콜`
- Type: other (UART·TCP 텍스트 프로토콜)
- Consumers / Producers: ff_vibetilt ↔ 제품 보드(VL10/VL20), 경사계(SOLAR-360), EMIO 모터·IO 컨트롤러
- Purpose: 보드 명령·측정 스트림, 지그 각도, 모터 이동
- Stability: stable (제품 FW·지그 사양에 종속)

### Inputs
- 보드 명령 `<CMD[,args]>` (485는 주소 `,1` 포함) — VER·UID·OFFSET·APPCFG·RCONF·SENTEMP·REFTEMP·MODE·START·STOP
- 경사계 `get---x`, `setdir5|6`, `setzcur`
- EMIO `\x02CMD\x03` — `PII00`, `PPC00`, `PMS…`, `PMD0{n}1ML{steps}`, `PMO011|012`

### Outputs
- 보드 응답 `[CMD,RV,…]`(RV 0 = 정상), 스트림 `[AN,x,y]`·`[GAC,x,y,z,rms]` 약 10ms 주기
- 경사계 `+000.951` 형식 각도, `OK`
- EMIO `pmd0011`, `pmo0011`, `pii…`, `pms…`

### Error Contract
- 응답 없음·RV≠0·FW 버전 불일치 :: 항목 FAIL
- 경사계 파싱 실패 :: 영점·측정 FAIL
- 스트림 샘플 부족(최소 tail 수) :: 재측정 후 FAIL

### Compatibility Rules
- 명령·기대 응답은 워크스페이스 `tests[]`에 둔다 (공용 라이브러리 하드코딩 금지)
- FW 버전 문자열은 완전 일치 (X/Y 기대값 다를 수 있음 — 485)

## Interface: `검사 결과 파일`
- Type: file (+ MongoDB)
- Consumers / Producers: flexfab.exe(생성) → 품질모니터링(`tools/quality_monitor`·QMP), 제조팀(성적서 인쇄), 개발(분석)
- Purpose: 보드별 결과·일자별 이력·성적서
- Stability: internal (4슬롯 형식은 미배포)

### Inputs
- 완성·FAIL 판정, 그리드 셀, 슬롯 시리얼

### Outputs
- `Result/{프로젝트}/{날짜}/{sn}.json` (+ `PASS_DUPLICATE/`, `FAIL/{sn}_{X|Y}_{시각}.json`)
- `Result/{프로젝트}/성적서/…csv·xlsx`, `이력/…csv`
- MongoDB `ctsm.product` (완성만, `MongoDBUpload=True`일 때)

### Error Contract
- 결과 저장 실패 :: 그 슬롯 FAIL(저장 실패), 반쪽 JSON 삭제, 다른 레인 격리
- 성적서·이력 파일 잠김 :: 재시도 후 건너뜀(덮어쓰기 금지), 판정 무영향

### Compatibility Rules
- 결과 JSON `ver` = 검사 모듈 `MODULE_VERSION`. 과거 데이터 표기는 소급 변경 안 함
- 품질모니터링은 결과 폴더를 재귀 수집 — 새 하위 폴더(FAIL·PASS_DUPLICATE) 추가 시 영향 확인(K2)

## Shared Constraints
- 판정은 로그 문자열 Contains 기반, PASS/FAIL 명확 구분
- 4슬롯 양성 증거 원칙: 슬롯의 모든 해당 셀이 OK일 때만 PASS
- 장비 대기는 wall-clock 타임아웃, 응답 없음 = FAIL (fail-closed)

## Current Gaps / Planned Changes
- K1 FAIL JSON Mongo 업로드 여부 / K2 품질모니터링 집계 영향 — 배포 전 결정
- #0 통신검사는 X1·Y1 포트만 확인, VER 판정코드 미확인
- D1 레인 교차 — Y 단계 UID 대조 등 검사 항목 변경 안건(#4902)

## Update Triggers
- Update this document when an externally visible API, event, CLI, or file contract changes.
- Update this document when a batch changes compatibility, validation, or error behavior.
- Update this document when a new external consumer or producer is introduced.

## Change Log
- 2026-09-30 bootstrap-baseline 초안 (저장소 관찰, 0.2.2.6 / 메인툴 1.8.1.8 기준)
