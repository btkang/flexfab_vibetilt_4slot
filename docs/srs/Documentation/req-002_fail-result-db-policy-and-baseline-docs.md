# req-002: FAIL 결과 DB 미업로드 정책과 결과 저장 규칙을 기준 문서에 명시

- Status: Implemented
- Type: Documentation
- Priority: High
- Owner: 강병택

## Intent
- K1 결정(FAIL 결과는 양산 DB `ctsm.product`에 올리지 않음)과 req-001의 저장 경로를 기준 문서에 남겨, 이후 변경(FAIL 업로드 재검토, 품질모니터링 개편)이 같은 기준에서 출발하게 한다. (출처: dcy-001 K1 결정·FR-1·FR-4, 레드마인 #4900)

## Requirement
- `docs/interface-contract.md` "검사 결과 파일"에 완성 결과(`Result/`)와 보관 기록(`Result_보관/` FAIL·PASS_DUPLICATE)의 경로·소비자·DB 업로드 여부를 적는다.
- `docs/data-model.md`에 FAIL·PASS_DUPLICATE 기록의 source of truth와 "DB 업로드 안 함" 정책, 품질모니터링 수집 범위를 적는다.
- 두 문서의 Change Log에 dcy-001·req-001·req-002를 출처로 기록한다.

## Acceptance Criteria
1. Given 기준 문서, When "FAIL 결과를 DB에 올리는가"를 찾으면, Then `interface-contract.md`와 `data-model.md` 모두 "올리지 않음(dcy-001 K1)"으로 답한다.
2. Given 기준 문서, When 결과 파일 경로를 찾으면, Then `Result/`(완성·성적서·이력)와 `Result_보관/`(FAIL·PASS_DUPLICATE) 구분과 품질모니터링은 `Result/`만 수집한다는 규칙이 적혀 있다.
3. Given 두 문서의 Change Log, When 확인하면, Then dcy-001·req-001·req-002 출처가 있다.

## Impacted Area
- Docs: `docs/interface-contract.md`, `docs/data-model.md`
- Modules: 없음 (K1은 현행 유지)
- Tests: 없음 (문서 검토)

## Notes
- K1 재검토(별도 컬렉션 업로드)는 후속 Discovery 후보 (dcy-001 OQ-1·OQ-3 Deferred).
- Priority·Owner는 1인 레포 추천 기본값.

## Change Log

### CHG-20260930-01
- Date: 2026-09-30
- Author: Claude (AI 3), 승인 강병택

#### Change Summary
- 최초 작성 (dcy-001 FR-1·FR-4) 및 Approved 전환

#### Intent
- 결정 사항을 기준 문서에 고정

#### Acceptance Criteria Delta
- Added:
  - AC-1~AC-3
- Updated:
  - 없음
- Removed:
  - 없음

#### Impacted Area Delta
- Docs:
  - interface-contract, data-model
- Modules:
  - 없음
- Tests:
  - 없음

#### Delivery Trace Delta
- Batches:
  - bat-001 (예정)
- Releases:
  - none

#### Revalidation Impact
- Reverification Needed: no
- Existing Implementation Invalidated: no
- Status Recommendation: keep-current
- Follow-up Action: bat-001에서 문서 반영

#### Validation Plan
- 문서 검토 (AC 1~3 항목 존재 확인)

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
