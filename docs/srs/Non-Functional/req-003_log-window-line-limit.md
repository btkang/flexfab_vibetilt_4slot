# req-003: 화면 로그창 줄 수 상한으로 장시간 운용 메모리 증가 차단

- Status: Implemented
- Type: Non-Functional
- Priority: Medium
- Owner: 강병택

## Intent
- 화면 로그(ListBox)가 무한 누적되어 사이클당 약 1MB씩 메모리가 늘던 문제를 막아, 며칠 무재시작 운용에서도 메모리가 일정하게 유지되게 한다. (출처: dcy-003, 레드마인 #4901 밤샘 SIL 2차)

## Requirement
- 화면 로그 줄 수가 워크스페이스 `log_max_lines`(없으면 20000, 0이면 무제한)를 넘으면 오래된 줄부터 상한의 10%만큼 한 번에 지운다.
- 파일 로그(`Log/{프로젝트}/{날짜}.log`)는 모든 줄을 그대로 남긴다.
- 2슬롯·4슬롯 모두 적용한다(전역, 화면 표시만).

## Acceptance Criteria
1. Given TEST_LOG 4슬롯 SIL, When 화면 로그가 상한을 넘는 사이클 수(≥6)를 돌리면, Then 이후 사이클에서 Private 메모리·관리 힙이 더 늘지 않는다(2차 밤샘 같은 구간 대비).
2. Given 같은 SIL, When 로그 파일 줄 수를 세면, Then 상한과 무관하게 모든 줄이 남아 있다.
3. Given 단위 테스트·SIL 판정, When 변경 후 실행하면, Then 회귀가 없다(단위 105/1, SIL CHECK 전부 OK).

## Impacted Area
- Docs: CHANGELOG, RELEASE_NOTE
- Modules: `flexfab_mini/FlexfabForm.cs` (`AddLogMessage`, `TrimLogLines`), `FlexfabForm.Slot4.cs` (`ApplySlotLayout`)
- Tests: SIL 25사이클 메모리 비교

## Notes
- 상한 20000줄 ≈ TEST_LOG 약 5사이클. 화면 "전체 로그 복사"는 화면에 남은 줄만 — 전체는 로그 파일.

## Change Log

### CHG-20261001-01
- Date: 2026-10-01
- Author: Claude (AI 3), 승인 강병택

#### Change Summary
- 최초 작성 (dcy-003 FR-1·FR-2) 및 Approved 전환 (사용자 "추천대로 진행해")

#### Intent
- 장시간 운용 안정성

#### Acceptance Criteria Delta
- Added:
  - AC-1~AC-3
- Updated:
  - 없음
- Removed:
  - 없음

#### Impacted Area Delta
- Docs:
  - CHANGELOG, RELEASE_NOTE
- Modules:
  - FlexfabForm.cs, FlexfabForm.Slot4.cs
- Tests:
  - SIL

#### Delivery Trace Delta
- Batches:
  - bat-002
- Releases:
  - none

#### Revalidation Impact
- Reverification Needed: no
- Existing Implementation Invalidated: no
- Status Recommendation: keep-current
- Follow-up Action: bat-002(batch-lite)에서 구현·검증

#### Validation Plan
- 빌드·단위 테스트, SIL 25사이클 [MEM] 비교, 로그 파일 줄 수

### CHG-20261001-02
- Date: 2026-10-01
- Author: Claude (AI 3), 승인 강병택

#### Change Summary
- Approved -> Implemented (bat-002 검증 완료)

#### Intent
- AC-1~3 근거 확보 (docs/batches/bat-002_20261001_log-line-limit/verification.md)

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
  - bat-002
- Releases:
  - 미정 (0.2.4.8 내부 개발본)

#### Revalidation Impact
- Reverification Needed: no
- Existing Implementation Invalidated: no
- Status Recommendation: keep-current
- Follow-up Action: 없음

#### Validation Plan
- 완료 — 단위 106, SIL 25사이클 메모리 비교
