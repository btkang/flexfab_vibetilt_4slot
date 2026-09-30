# flexfab VibeTilt 4-Slot Runtime Flows

- Baseline Mode: observed
- Baseline Seed: .stage-pilot/bootstrap/baseline.yaml
- Status: draft
- Owner: (미정 — 사용자 결정)
- Last Updated (KST): 2026-09-30 10:55
- Source Discovery / Batch: bootstrap-baseline (2026-09-30, 저장소 관찰)
- Primary Runtime: other (Windows 데스크톱 GUI)

## Project Summary
- 진동기울기센서(VL10 RS-232 / VL20 RS-485) 양산 검사 프로그램 — 모션 지그 4슬롯(X1·X2·Y1·Y2) 대응 flexfab 기반 데스크톱 앱

## Purpose
- This document is the current approved execution-flow baseline for the project.
- It is a cross-cutting reference document for Discovery, REQ, Batch Design, Batch Verification, and implementation updates.

## Scope
- Covers representative command and runtime flows.
- Does not duplicate full acceptance criteria or low-level implementation details.
- 상세 동작 명세: 작업 폴더 `02_분석_날짜/2026 0921-4슬롯_시뮬검증/CHECK_4슬롯_동작명세_20260921_v02.md`(123항목)

## Covered Entry Points
- `flexfab.exe` :: 메인 검사 GUI
- 워크스페이스 JSON :: 검사 정의

## Shared Components
- 워크스페이스 로더 + 라이브러리 런타임 로딩 (`FlexfabForm.cs`)
- 4슬롯 판정·pending·시리얼 (`FlexfabForm.Slot4.cs`)
- 성적서·이력·FAIL 저장 (`FlexfabForm.Report.cs`)
- 검사 모듈 `VibeTilt`·`MotionJig`, 통신 `Uart`·`Tcp`

## Flow: `앱 시작 · 워크스페이스 로드`
1. `config.ini`(`Config/`) 초기화 → `LastWorkspace` 읽기
2. 워크스페이스 JSON 로드 → `libraries[]`를 `Assembly.LoadFrom` → `Activator.CreateInstance` → `SetConfig(config+log_action)`
3. `ctsp` 등 `PreRun(libraries)`
4. `slot_layout="4"` + `slot_uarts` 4개 모두 있으면 4슬롯 모드 → 그리드 결과 4열(X1·X2·Y1·Y2), 아니면 2슬롯(1열)
5. 창 타이틀 `ff_vibetilt {버전}`, 로그에 메인툴 버전·워크스페이스

## Flow: `4슬롯 검사 사이클 (정상)`
1. Start → 검사자 확인 → 4슬롯 dual 전용 확인(single 거부) → `slot_group` 허용값 검증(위반 시 Start 거부)
2. 작업자 제외 항목 기록(쓰기 항목은 자동 해제) → 시리얼 파일 줄 수 기준선(D2)
3. 시리얼 4칸 창 — X 기본값 = 채번, Y 기본값 = 레인별 유효 pending. 중복·접두사·자릿수 검사, 대기 보드 폐기 경고(C17)
4. 모든 `IComm` 연결 (하나라도 실패하면 중단)
5. 레인별 pending 처리: 잔존 `.inuse`→`.stale`, 구 형식→`.old`, 유효하면 `.inuse` 잠금, 무효·재투입·레인 교차면 `.invalid`, Y에 보드가 있는데 유효 X 기록 없으면 그 Y 슬롯 차단
6. proc 순서대로 실행 — `slot_group`: common 1회 / X → X1·X2 / Y → Y1·Y2 / all → X1·X2 (단계1: 순차). 슬롯 param에 `uart_id`·`__serial`·`__slot` 주입
7. 라운드 종료 → `SlotVerdict`(해당 셀 전부 OK일 때만 PASS, 작업자 제외 행은 판정 제외) 누적 (`repeat_all_count`)
8. `FinishSlot4`: ① 판정 확정 ② Y 완성 → X pending retmsg + Y retmsg 합쳐 결과 JSON 저장(+Mongo) ③ X 통과 → 새 pending 원자적 기록 ④ `.inuse` 정리(공통 FAIL이면 복원) ⑤ FAIL JSON·성적서·검사이력
9. 요약 팝업(`완성 n대 · X통과 n대 대기` + 슬롯별 상태), X 슬롯이 하나도 못 통과하면 시리얼 롤백(번호 소모 사이클만)
10. 모든 `IComm` Disconnect / `IProcess` End

## Flow: `4슬롯 검사 사이클 (중단)`
1. STOP 또는 공통 항목 FAIL(fail_continue 꺼짐) → `wasCanceled`
2. 공통 FAIL 중단이면 검사이력에 `FAIL(공통 항목 #n)` (FAIL JSON 없음). STOP은 기록 없음
3. 시리얼 롤백(번호 소모 사이클만) → FAIL이 있으면 FAIL 팝업
4. `CleanupSlot4Abnormal`: 새 pending 기록 안 함, `.inuse`는 Y 완성·새 X·재투입·Y FAIL이면 폐기, 아니면 복원(다음 사이클 Y 재검)

## Flow: `기울기 측정 (MotionJig RunTiltMeasure)`
1. 영점(홈 → +1250 → FineAdjust 0도) 미완료면 먼저 수행
2. 각도마다: 모터 이동 → FineAdjust 수렴(0.1125°/step) → 경사계 1회 판독
3. 시도(최대 1+3): STOP → 잔여 비우기(빈 응답까지, 최대 1.5초) → MODE 06·START → 안정 대기 → 초기 버리기(10줄) → 스트림 수집
4. 샘플 < 최소(기본 10)면 재측정 → 센서 차이 ≥10도면 즉시 FAIL → tail N개 all_in_spec 판정

## Flow Constraints
- 검사 중 UI(번호셀·헤더·더블클릭·설정·워크스페이스 열기) 차단
- STOP은 항목 경계에서만 반응
- 단독실행(항목명 더블클릭)은 별도 루프, 쓰기 항목 차단
- 모든 장비 대기는 wall-clock 타임아웃, 응답 없음 = FAIL

## Current Gaps / Planned Changes
- 병렬 측정(단계 2: #6/#7 동시, 단계 3) 미착수
- D1 레인 교차는 소프트웨어로 차단 불가 (#4902)
- 2슬롯 dual 흐름은 별도 경로(`pending_x_result.json`), 통합 예정

## Update Triggers
- Update this document when a command entry point changes.
- Update this document when a runtime orchestration path changes.
- Update this document when a batch changes the approved flow baseline.

## Change Log
- 2026-09-30 bootstrap-baseline 초안 (저장소 관찰, 0.2.2.6 / 메인툴 1.8.1.8 기준)
