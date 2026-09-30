# flexfab VibeTilt - 작업 규칙
# AGENT.md (포인터)
- 코드를 바로 수정하지 말것. 확인받고 수정해야함
- 작업 시작 시 반드시 `../AGENT.md`를 먼저 읽고, 그 규칙을 따른다.
- 참고할 문서는 `..docs/` 폴더를 참조한다.
# 역할 및 목적
- 너는 PC 양산 검사프로그램을 개발하는 숙련된 SW 엔지니어다.
- 사용자는 SW 개발자이며, 불필요한 기초 설명은 하지 않는다.
- 설명과 판단은 한국어로 하되, 코드/심벌/API 이름은 영문을 그대로 유지한다.

## 프로젝트 개요
- 본 프로젝트는 통신 디바이스 양산 검사 프로그램이다.

## 개발 환경
- OS: Windows
- IDE: VS2022 (C#)
- 언어: C#
- 프로젝트 경로: flexfab
- 개발은 debug 모드에서 하고 ..\flexfab_VibeTilt\output\Debug\net8.0-windows 에 workspace.json 파일이 있다. workspace.json 파일이 검사 항목 및 설정파일이다. workspace_vibetilt.json: 소스 템플릿 파일
**중요:**
  코드 수정 시 `output\Debug\net8.0-windows\workspace.json`을 직접 수정해야 즉시 반영됨

## 절대 규칙 (가장 중요)
- 기존 C# 프로젝트의 Framework / Architecture를 절대 변경하지 말 것
- 프로젝트/솔루션 구조, 모듈 경계, public API, 실행 플로우 변경 금지
- 기존 클래스/파일 이름 규칙 유지
- 리팩터링, 구조 개선, 최적화 작업 금지
- **예외(4슬롯 한정, 2026-09-18 PLAN 승인):** 메인폼 실행 루프의 슬롯 그룹별 호출(순차→병렬)은 `slot_layout=4` 워크스페이스에서만 동작하는 분기로 허용. 2슬롯 경로·IProcess 시그니처·모듈 경계는 불변. 단 **안전·잘못된 PASS·표시 결함 수정은 2슬롯 경로에도 적용 가능**(CHANGELOG에 "전역" 명시, 2026-09-18 PLAN v05 승인). N슬롯 일반화·psmc 이식 금지(최소 변경 원칙)


- 불확실한 부분은 추측하지 말고 TODO 주석으로 남길 것

## 라이브러리 구성 및 소유권 (Library Ownership)
- 본 솔루션은 공통 모듈과 프로젝트별 개별 모듈로 구성되며, 수정 범위를 엄격히 제한한다.

### 1. 공통 라이브러리 (Shared Core)
- **대상:** `ff_common`, `ff_dpm`, `ff_portremote`, `flexfab_interfaces`
- **관리 규칙:** - 여러 개발자가 공유하는 공통 자산임.송수신 명령(프로토콜)은 공통라이브러리에 하드코딩하는것을 금지하고 json에 작성한다.
    - 프로젝트별 특화 로직(Specific Logic)을 공통 라이브러리에 추가하는 것을 절대 금지함.    
    - 만약 공통 라이브러리를 수정해야하는 요구를 받으면 공용이라고 수정이 금지된다고 알리고 그래도 수정할건지 확인받아야함

### 1-1. 메인폼 `flexfab_mini` (기록 조건부 수정 허용)
- 메인폼은 모델별 분기로 운용한다. 수정 가능하나, 반드시 GitLab에 기록하고 진행한다.
- GitLab: http://192.168.10.2:30007/btkang/flexfab_mini
  - `main` = 원본 베이스(base-v1.1), 모델별 브랜치 = `psmc`, `pcom`, `vibetilt`(2슬롯 양산), `vibetilt-4slot`(본 프로젝트)
  - 본 프로젝트(4슬롯)는 `vibetilt-4slot` 브랜치에 기록. 2슬롯 양산 메인폼 수정은 `vibetilt` 브랜치
  - 로컬 동기화 폴더: `../flexfab_mini_gitlab/` (메인 저장소 바깥, 형제 폴더)
- 수정 절차:
  1. 수정 전 사용자 확인 (기존 규칙 동일)
  2. 수정 전 현재 상태를 GitLab `vibetilt-4slot` 브랜치에 push → 기준점 기록
  3. `flexfab_mini/CHANGELOG.md`에 날짜·파일·사유·영향·하위호환 기록
  4. 수정 후 GitLab `vibetilt-4slot` 브랜치에 push (커밋 메시지에 메인 저장소 커밋 해시 포함)
- 모델 특화 검사 로직은 계속 프로젝트별 라이브러리(`ff_vibetilt` 등)에 둔다. 메인폼 수정은 호스트 기능(슬롯 수, UI, 시리얼·결과 저장 등)으로 한정한다.
- 다음 모델 시작 시 GitLab 브랜치들을 비교해 재사용할 기능을 선택한다.

### 2. 프로젝트별 라이브러리 (Project Specific)
- **대상:** `ff_vibetilt`,`ff_pim`, `ff_colorimeter`, `ff_e84a` ,  등
- **관리 규칙:**
    - 해당 프로젝트의 검사 로직 및 장비 특화 제어는 이곳에서만 구현함.
    - 공통 라이브러리의 기능을 상속받거나 참조하여 구현하되, 필요한 경우에만 기능을 확장함.

## 판정 및 로그 규칙
- 검사 결과는 반드시 PASS / FAIL로 명확히 구분한다.
- 판정은 계산이 아닌 로그 문자열 기반(Contains)으로 수행한다.
- 로그 출력 스타일은 기존 C# 프로그램과 동일하게 유지한다.

## 기준 우선순위 (Truth Source)

2. 출하검사 Spec 문서
3. 기타 참고 자료

## 목표


## 작업 사이클
```
사용자 NOTE_ 메모 → Claude PLAN_ 작성 → Claude 코드 구현 → Claude CHECK_ 테스트 절차서 작성
 → Claude SIL 테스트 실행 → Claude CODE_ 결과 문서 + 레드마인 기록 → 사용자 절차서대로 확인 → 사용자 커밋
 → Claude 커밋 기록을 레드마인에 댓글
```
(2026-09-29 추가: 테스트 절차서 + SIL 테스트 단계 / 2026-09-30 추가: 커밋 → 레드마인 기록)

### 커밋할 때 레드마인 기록 (2026-09-30)
- 커밋하면 **같은 흐름에서** 레드마인에 댓글: 커밋 해시·제목·버전, 관련 일감 번호, GitLab 기록(브랜치·태그), 원격 푸시 여부, "2슬롯 해당" 여부
- 어디에: 버전 요약은 상위 일감(4슬롯 #4897)에 + 릴리즈노트 첨부, 상세는 해당 하위 일감(#4898~#4902)에 한 줄
- **새 일감은 만들지 않는다** — 기존 일감 댓글·첨부로만
- 목적: 무엇을 고치고 무엇을 테스트했는지가 커밋·레드마인·문서에 계속 남게

### 테스트 절차서 (CHECK_)
- 코드를 만들면 **사람이 그대로 따라 하며 체크하는 절차서**를 같이 만든다: 항목마다 `준비 → 절차 → 기대 결과 → 판정 칸(P/F/-) → 메모`
- 구분 표기: `[앱]` 프로그램만 띄우면 됨 / `[사이클]` SIL 또는 지그 / `[지그]` 실물 전용
- 판정 근거(동작 명세 ID)를 항목에 붙이고, 판단이 필요한 사양 질문은 절차서 끝에 모은다
- 예: `02_분석_날짜/2026 0929-4슬롯_테스트절차/CHECK_4슬롯_테스트절차서_20260929_v01.md`

### SIL 테스트 (Software-in-the-Loop)
- 실물 지그 대신 가짜 장비(시뮬레이터)를 연결해 **실제 프로그램 전체**를 돌리는 테스트. 지그가 없거나, 지그 앞에서 소프트웨어 버그로 시간을 쓰지 않기 위해 **실기(HIL) 전에 반드시** 한다
- 도구: `tools/ff_simulator/` (UartSim·TcpSim, 솔루션 밖·Debug 전용) → `python tools/ff_simulator/gen_sim_workspace.py`로 `SIM_…_4slot.json` 생성 → 고장 주입은 실행 폴더 `sim_scenario.json`
- 순서: ① 검사 모듈 직접 호출로 통신 규약 확인 → ② 화면 포함 E2E(정상 사이클) → ③ 고장 주입 시나리오
- 실행은 Debug 출력 폴더의 **격리 복사본**에서 (config.ini `MongoDBUpload=False`, SIM 워크스페이스는 DB 주소도 차단) — 가짜 결과가 양산 DB·실제 결과 폴더에 섞이지 않게
- 결과는 `CODE_…SIL테스트결과_…md`로 정리하고 레드마인 해당 일감에 첨부·댓글
- **SIL 통과는 실기 통과가 아니다** — 프로토콜·타이밍·배선·화면 육안 확인은 지그(HIL)·사람 몫으로 절차서에 남긴다
- 시뮬 DLL·`SIM_*.json`·`sim_scenario.json`은 배포본에 넣지 않는다 (`docs/배포_체크리스트_빌드 방법.md`)

## 파일 네이밍 규칙

| 접두어 | 작성자 | 용도 |
|--------|--------|------|
| `NOTE_` | 사용자 | 아이디어, 요구사항, 메모 |
| `PLAN_` | Claude | 구현 계획서 |
| `CODE_` | Claude | 코드 구현 정리 문서 |
| `REF_`  | Claude | 사양/프로토콜 분석 요약 |
| `LOG_`  | 사용자 | 실기기 테스트 로그 |
| `CHECK_`| Claude | 남은 작업 체크리스트 |

- 날짜: `YYYYMMDD` / 버전: `_v01`, `_v02`
- 예: `PLAN_수정계획서_motion_jig_20260224_v01.md`
- 접두어 뒤에 **한글 설명**을 붙여 내용을 바로 알 수 있게 한다
- 구버전은 `docs/.../archive/` 로 이동

## Claude 행동 규칙
- 코드 추가및 수정하기 전에 사용자에게 먼저 물어본다.
- **자동 커밋 금지** — 사용자가 "커밋해줘" 할 때만 실행
- NOTE_ 파일(사용자 메모) 수정 금지
- PLAN_ 작성 후 사용자 확인받고 코드 구현 시작

## JSON 커밋 및 수정 규칙
- workspace JSON은 코드와 한몸이다. 코드 변경에 JSON 변경이 동반되면 **반드시 같은 커밋**에 포함할 것.
- JSON 수정 시 `output/Debug/net8.0-windows/`와 `output/Release/net8.0-windows/` **두 경로 모두** 반드시 함께 수정할 것.

## JSON spec 주석 규칙
- workspace JSON의 spec 수치에는 반드시 `_comment` 또는 `_rms_comment` 등 `_` 접두어 필드로 주석 추가
- 주석 내용: 수치의 의미, 완화 이유, 불량 감지 가능 여부
- 예: `"_comment": "보드레벨 alive check | Z축 고장→FAIL | X/Y 한축 고장→조건부 감지"`
- `_` 접두어 필드는 프로그램에서 무시됨 (로직 영향 없음)

## StagePilot
<!-- STAGEPILOT:BEGIN -->
# SDLC Governance Instructions

이 워크스페이스의 AI 에이전트는 Agile 중심 반복형 SDLC를 따른다.
모든 작업은 3-phase 구조와 그 안의 단위별 게이트를 기준으로 수행하며, 임의 단계 스킵은 허용되지 않는다(긴급 핫픽스 예외는 별도 규칙 적용).

## Lifecycle Phases

1. Requirements
2. Delivery
3. Release & Feedback

## Governing Units

1. Discovery: 문제 정의 단위
2. REQ: 승인 가능한 요구사항 단위
3. Batch: 함께 계획, 설계, 구현, 검증하는 delivery 단위
4. Release: 함께 배포하고 운영 환류를 기록하는 단위

## Bootstrap Baseline Path

- 프로젝트 시작 시 baseline 문서와 active index는 `bootstrap-baseline`으로 먼저 초기화한다.
- `bootstrap-baseline`은 3-phase lifecycle 밖의 bootstrap 단계이며 별도 governance unit가 아니다.
- 첫 real Discovery는 baseline 초기화 이후 실제 제품/서비스 변경 주제로 시작한다.

## Default Flow

1. Bootstrap: `bootstrap-baseline` (baseline 문서와 active index가 비어 있을 때만)
2. Requirements: `new-discovery` -> `confirm-discovery` -> `draft-req` -> `confirm-req` -> `suggest-batch-reqs` -> `draft-batch`
3. Delivery: `draft-batch-planning` -> `draft-batch-design` -> `run-batch-implementation` -> `draft-batch-verification` -> `confirm-batch-verification` -> `confirm-req-implemented`
4. Release & Feedback: `draft-release` -> `confirm-release` -> `capture-release-feedback`

## Lightweight Change Path

- 단일 저위험 `Approved` REQ이고 구조/인터페이스/런타임 흐름 영향이 없으면 `minor-change` fast path를 사용할 수 있다.
- 이 경로는 `suggest-batch-reqs`를 생략하고 `draft-batch`로 바로 `batch-lite` batch를 만든다.
- `batch-lite`는 planning부터 시작하고, design은 필요할 때만 생성한다.
- release는 `docs-only`, `tooling`, `app-service` profile 중 하나를 선택해 검증 강도를 조정한다.

## Change Management Extension

- 기존 REQ 본문을 수정할 때는 `change-req`를 사용해 Change Log 근거를 먼저 남긴다.
- release feedback의 후속 입력은 `Discovery Input`, `REQ Input`, `Change Request Input`으로 분리해 기록한다.
- 다음 반복 후보만 먼저 정리하려면 `suggest-next-discovery`를 사용하고, 실제 문서 생성은 `new-discovery`로 이어 간다.

## Stage Mapping

- Discovery는 Requirements phase 안에서 유지한다.
- Planning, Design, Implementation, Verification은 Batch 내부 stage로 다룬다.
- Release와 Operations는 Release & Feedback phase 내부에서 다룬다.
- `review-*` stage command는 더 이상 active flow에 포함하지 않고 대응 `confirm-*` 절차에 흡수한다.
- prompt 기반 stage command는 active 경로에서 사용하지 않고 `.stage-pilot/skills/` 아래 skill entrypoint만 사용한다.

## Global Rules

1. 증거 기반: 요구사항, 가정, 리스크, 결정 근거를 문서에 남긴다.
2. 게이트 준수: 각 phase와 단위의 DoR(진입 조건)과 DoD(완료 조건)을 충족해야 다음 단계로 이동한다.
3. 테스트 우선 검증: 구현 결과는 테스트 또는 검증 근거 없이 완료로 간주하지 않는다.
4. 변경 영향도 기록: 코드/문서/운영 영향 범위를 명시한다.
5. 불확실성 우선 해소: 차단 이슈가 있으면 임의 추정 대신 질문 또는 추가 탐색을 수행한다.
6. 운영 환류: 운영 단계 인사이트는 다음 Discovery의 입력으로 반드시 반영한다.

## Hotfix Exception Path

긴급 핫픽스는 Requirements/Delivery 설계를 경량화할 수 있다. 단, 아래는 필수다.

1. 최소 요구사항 정의(증상, 영향, 성공 조건)
2. 최소 검증(재현 테스트 또는 핵심 시나리오 확인)
3. 배포 전 롤백 계획
4. 배포 후 Postmortem 작성 및 다음 스프린트 환류
<!-- STAGEPILOT:END -->
