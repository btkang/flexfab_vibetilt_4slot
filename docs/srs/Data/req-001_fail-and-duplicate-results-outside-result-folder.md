# req-001: FAIL·재완성 이전 결과를 품질모니터링 수집 경로(Result/) 밖에 저장

- Status: Implemented
- Type: Data
- Priority: High
- Owner: 강병택

## Intent
- 품질모니터링은 상위 `Result/` 폴더 전체를 `*.json` 재귀 수집한다. 4슬롯이 `Result/` 아래에 FAIL 결과와 재완성 이전 결과(PASS_DUPLICATE)를 두면 시리얼 판정·수율이 왜곡되므로, 이 기록을 수집 경로 밖으로 옮겨 결과 폴더에는 완성 결과만 남긴다. (출처: dcy-001 K2 결정, 레드마인 #4900)

## Requirement
- 4슬롯 FAIL 결과 JSON은 `Result_보관/{프로젝트}/{yyyy-MM-dd}/FAIL/{sn}_{X|Y}_{yyyyMMdd_HHmmss}[_n].json`에 저장한다.
- 같은 날 같은 시리얼 재완성 시 기존 완성 결과는 `Result_보관/{프로젝트}/{yyyy-MM-dd}/PASS_DUPLICATE/{sn}_{yyyyMMdd_HHmmss}[_n].json`으로 옮긴다.
- `Result/{프로젝트}/` 아래에는 완성 결과 `{yyyy-MM-dd}/{sn}.json`과 성적서(`성적서/*.csv|xlsx`)·검사이력(`이력/*.csv`)만 둔다 (JSON은 완성 결과만).
- 검사이력 CSV `결과파일` 열에 FAIL 결과는 작업 폴더 기준 상대경로 `Result_보관/…`로 적는다.
- 결과 라벨 우클릭 메뉴 "FAIL 결과 폴더 열기 (오늘)"는 새 경로를 연다.
- 2슬롯 경로(`slot_layout`≠4)의 결과 저장은 바꾸지 않는다.

## Acceptance Criteria
1. Given 4슬롯 사이클에서 슬롯 FAIL이 나면, When 사이클이 정상 종료되면, Then FAIL JSON이 `Result_보관/{프로젝트}/{날짜}/FAIL/`에 생기고 `Result/` 아래에는 FAIL JSON이 없다.
2. Given 같은 날 이미 완성된 시리얼, When 다시 완성되면, Then 이전 결과가 `Result_보관/{프로젝트}/{날짜}/PASS_DUPLICATE/`로 옮겨지고 `Result/{프로젝트}/{날짜}/{sn}.json`은 새 결과 1개만 있다.
3. Given 재완성 중 새 결과 저장이 실패하면, When 복구가 동작하면, Then 옮긴 이전 결과가 `Result/{프로젝트}/{날짜}/{sn}.json`으로 되돌아온다 (기존 동작 유지).
4. Given SIL로 완성·FAIL·재완성이 섞인 사이클을 돌린 뒤, When `Result/` 아래 `*.json`을 재귀 스캔하면, Then 모든 파일이 완성 결과(`pass_fail: PASS`)이고 시리얼당 1개다.
5. Given 2슬롯 워크스페이스, When 사이클을 돌리면, Then 결과 저장 위치·파일명이 변경 전과 같다.

## Impacted Area
- Docs: `docs/interface-contract.md` (검사 결과 파일), `docs/data-model.md` (persistence 규칙)
- Modules: `flexfab_mini/FlexfabForm.Report.cs` (`ArchiveRootDir` 신설, `PreservePassDuplicate`, `SaveFailJsonSlot`, 우클릭 메뉴)
- Tests: `tools/ff_tests/ReportTests.cs` PASS_DUPLICATE 이관 테스트 기대 경로 변경, SIL 사이클 검증

## Notes
- 보관 폴더 이름 `Result_보관`은 추천안 적용(사용자 "계속 진행해", 2026-09-30). 품질모니터링 `--result-dir`가 상위 `Result`를 가리키므로 형제 폴더면 수집되지 않는다.
- 4슬롯은 제조 미배포라 기존 데이터 이관은 없다.
- Priority·Owner는 1인 레포 추천 기본값 — 변경 시 `change-req`.
- NFR(2슬롯 무변경)은 AC-5로 흡수 (별도 REQ 승격 안 함).

## Change Log

### CHG-20260930-01
- Date: 2026-09-30
- Author: Claude (AI 3), 승인 강병택

#### Change Summary
- 최초 작성 (dcy-001 FR-2·FR-3) 및 Approved 전환

#### Intent
- 4슬롯 배포 전 품질모니터링 집계 왜곡 차단

#### Acceptance Criteria Delta
- Added:
  - AC-1~AC-5
- Updated:
  - 없음
- Removed:
  - 없음

#### Impacted Area Delta
- Docs:
  - interface-contract, data-model
- Modules:
  - FlexfabForm.Report.cs
- Tests:
  - ReportTests.cs, SIL

#### Delivery Trace Delta
- Batches:
  - bat-001 (예정)
- Releases:
  - none

#### Revalidation Impact
- Reverification Needed: no
- Existing Implementation Invalidated: no
- Status Recommendation: keep-current
- Follow-up Action: bat-001에서 구현·검증

#### Validation Plan
- 단위 테스트 전체 통과 + SIL 사이클(완성·FAIL·재완성) 후 `Result/` JSON 스캔

### CHG-20260930-02
- Date: 2026-09-30
- Author: Claude (AI 3), 승인 강병택

#### Change Summary
- Approved -> Implemented (bat-001 검증 완료)

#### Intent
- 모든 Acceptance Criteria에 근거 확보 (docs/batches/bat-001_20260930_fail-archive-path/verification.md)

#### Acceptance Criteria Delta
- Added:
  - 없음
- Updated:
  - 없음
- Removed:
  - 없음

#### Impacted Area Delta
- Docs:
  - 없음
- Modules:
  - 없음
- Tests:
  - 없음

#### Delivery Trace Delta
- Batches:
  - bat-001
- Releases:
  - 미정 (0.2.3.7 내부 개발본)

#### Revalidation Impact
- Reverification Needed: no
- Existing Implementation Invalidated: no
- Status Recommendation: keep-current
- Follow-up Action: 없음. 잔여: 실기(HIL) 미검증

#### Validation Plan
- 완료 — 단위 106, SIL 5사이클, 품질모니터링 파서 확인
