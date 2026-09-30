# flexfab VibeTilt 4-Slot Project Structure

- Baseline Mode: observed
- Baseline Seed: .stage-pilot/bootstrap/baseline.yaml
- Status: draft
- Owner: (미정 — 사용자 결정)
- Last Updated (KST): 2026-09-30 10:55
- Source Discovery / Batch: bootstrap-baseline (2026-09-30, 저장소 관찰)

## Project Summary
- 진동기울기센서(VL10 RS-232 / VL20 RS-485) 양산 검사 프로그램 — 모션 지그 4슬롯(X1·X2·Y1·Y2) 대응, 워크스페이스 JSON으로 검사 항목과 장비 라이브러리를 런타임 로딩하는 flexfab 기반 Windows 데스크톱 앱

## Primary Domain
- 제조 양산 검사 (센서 보드 EOL 테스트, 모션 지그)

## Tech Stack
- C# / .NET 8 (net8.0-windows), WinForms
- Newtonsoft.Json — 워크스페이스·결과 JSON
- System.IO.Ports (UART), TCP (EMIO 모터·IO)
- MongoDB.Driver — 결과 업로드 (config.ini `MongoDBUpload`로 끔)
- xUnit — `tools/ff_tests` 단위 테스트
- Python 3 — 워크스페이스 생성·SIL 보조 스크립트

## Primary Runtime
- Type: other (Windows 데스크톱 GUI, WinForms)
- Planned Entry Points:
- `flexfab.exe` (flexfab_mini, `Cantops.FlexFab.Mini.App`) :: 메인 검사 GUI
- 워크스페이스 JSON (`output/{Debug|Release}/net8.0-windows/*_workspace_*.json`) :: 검사 항목·장비·4슬롯 배치 정의
- `tools/` :: 단위 테스트, SIL 시뮬레이터, 워크스페이스 생성, 메인폼 GitLab 기록

## Purpose
- This document is the current approved repository and package structure baseline.
- It is a cross-cutting reference document for Discovery, REQ, Batch Design, Batch Verification, and implementation updates.

## Scope
- Covers the top-level repository layout and package/module boundaries.
- Does not duplicate detailed business rules, acceptance criteria, or implementation-only function signatures.

## Top-Level Structure

```text
flexfab_VibeTilt_4slot_/
├── flexfab.sln                 # 솔루션 (구조 변경 금지)
├── Directory.Build.props       # 공통 출력 경로·제품 버전(ff_vibetilt 번호)
├── flexfab_mini/               # 메인툴 flexfab.exe (호스트)
│   ├── FlexfabForm.cs          #   실행 루프·UI·저장
│   ├── FlexfabForm.Slot4.cs    #   4슬롯 (판정·pending·시리얼 4칸)
│   ├── FlexfabForm.Report.cs   #   성적서·검사이력·FAIL 저장
│   ├── ReportXlsxWriter.cs     #   성적서 xlsx 인쇄본
│   └── CHANGELOG.md
├── flexfab_interfaces/         # IModule·IComm·IProcess (공용)
├── ff_common/                  # Uart·Tcp·Ctsp (공용)
├── ff_vibetilt/                # VibeTilt·MotionJig 검사 모듈 (프로젝트 전용)
├── ff_dpm/ ff_portremote/ ff_pim/ ff_e84a/ Colorimeter/   # 타 모델 라이브러리
├── output/{Debug,Release}/net8.0-windows/   # 실행 폴더 — 워크스페이스 JSON만 추적
├── tools/
│   ├── ff_tests/               # xUnit (솔루션 밖)
│   ├── ff_simulator/           # SIL 가짜 장비 (솔루션 밖, Debug 전용)
│   ├── gen_workspace_4slot.py
│   └── sync_flexfab_mini.ps1   # 메인폼 → GitLab flexfab_mini
├── docs/                       # 빌드·배포 절차 + stage-pilot 문서
├── .stage-pilot/               # SDLC 프레임워크 (subtree)
├── .claude/ .github/ .agents/  # stage-pilot 명령·스킬
├── CLAUDE.md                   # 작업 규칙
└── RELEASE_NOTE_ff_vl.txt
```

## Package / Module Responsibilities

- `flexfab_mini/` (flexfab.exe)
  - 워크스페이스 로드, 라이브러리 런타임 로딩(`Assembly.LoadFrom`), 검사 실행 루프, 시리얼 입력·채번, 판정, 결과 JSON·성적서·이력 저장, UI
  - 4슬롯 경로(`slot_layout=4`)와 2슬롯 경로를 함께 가짐 — 4슬롯 전용 로직은 `Slot4Run` 분기 안
  - 모델별 브랜치로 GitLab `flexfab_mini`에 기록 (`vibetilt-4slot`)
- `flexfab_interfaces/`
  - 호스트·모듈 공통 계약. 공용 — 수정 금지
- `ff_common/`
  - 공용 통신 라이브러리(Uart·Tcp·Ctsp). 송수신 명령 하드코딩 금지(JSON에 작성). 공용 — 수정 금지(요청 시 사용자 확인)
- `ff_vibetilt/`
  - `VibeTilt`: 보드 명령(FW·UID·OFFSET·APPCFG·RCONF·REFTEMP) / `MotionJig`: 모터·경사계·영점·기울기·진동 측정
  - 모델 특화 검사 로직은 여기에만
- `output/`
  - 실행 폴더. 빌드 산출물은 추적 안 함, 워크스페이스 JSON(TEST_/MP_ × 232/485 × 2슬롯/4슬롯)만 추적
- `tools/`
  - 솔루션 밖 보조 도구. 시뮬레이터·SIM 워크스페이스는 배포본에 넣지 않음

## Dependency Rules

- `flexfab_mini` → `flexfab_interfaces` (컴파일), 검사 모듈·통신 라이브러리는 워크스페이스로 **런타임 로딩** (컴파일 의존 없음)
- `ff_vibetilt` → `flexfab_interfaces` (계약), 통신은 `IComm`으로만 (구체 클래스 형변환 없음)
- 공용 라이브러리(`ff_common`, `flexfab_interfaces`)는 프로젝트 특화 로직을 갖지 않는다
- 솔루션 구조·모듈 경계·public API·IProcess 시그니처 변경 금지 (CLAUDE.md 절대 규칙)

## Shared Boundaries

- 워크스페이스 JSON — 코드와 한몸, 같은 커밋, Debug·Release 동시 수정
- `flexfab_mini` 공유 함수(시리얼 롤백·UI 차단 등)는 2슬롯 경로에도 적용됨 — 2슬롯 해당 여부를 CHANGELOG에 표시
- 결과 파일 형식(결과 JSON·성적서·이력)은 품질모니터링 등 외부 소비자와 공유

## Current Gaps / Planned Changes

- 2슬롯·4슬롯 통합: 최종적으로 4슬롯 프로그램 하나로 두 지그 운용 (2슬롯 저장소는 버그 수정만)
- 병렬 측정(단계 2·3) 미착수 — 현재 슬롯별 순차 실행(단계1)
- E2E 자동 실행기(SIL 화면 구동)는 저장소 밖 — 재사용하려면 `tools/`로 이동 필요

## Update Triggers

- Update this document when package boundaries change.
- Update this document when new top-level runtime or domain modules are added.
- Update this document when a batch changes the approved structure baseline.

## Change Log

- 2026-09-30 bootstrap-baseline 초안 (저장소 관찰, 0.2.2.6 / 메인툴 1.8.1.8 기준)
