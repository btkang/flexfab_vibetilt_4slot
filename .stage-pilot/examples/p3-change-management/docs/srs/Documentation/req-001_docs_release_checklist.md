# req-001: Docs release checklist

- Status: Approved
- Type: Documentation
- Priority: Medium
- Owner: Docs Maintainer

## Intent
- docs-only release에서도 소비자 관점의 품질 게이트를 문서화하고 검증 근거로 남긴다.

## Requirement
- docs-only release는 release note 반영과 broken-link 검증 결과를 release evidence에 포함해야 한다.

## Acceptance Criteria
1. Given docs-only release가 준비되었을 때, When release evidence를 검토하면, Then broken-link 검증 결과가 명시되어야 한다.
2. Given release feedback가 수집되었을 때, When follow-up를 분류하면, Then Discovery/REQ/change-req 입력이 구분되어야 한다.

## Impacted Area
- Docs: docs/releases/rel-001_20260428_docs_release_hardening.md, README.md
- Modules: tools/stagepilot.sh, .stage-pilot/tools/stagepilot-doctor.py
- Tests: doctor release feedback summary validation

## Notes
- `CHG-20260430-02`로 acceptance criteria가 강화되어 기존 구현 증거는 재검증이 필요하다.

## Change Log

### CHG-20260426-01
- Date: 2026-04-26
- Author: Docs Maintainer

#### Change Summary
- initial requirement created

#### Intent
- docs-only release checklist를 requirement로 관리한다.

#### Acceptance Criteria Delta
- Added:
  - release evidence must include release note update result
- Updated:
  - none
- Removed:
  - none

#### Impacted Area Delta
- Docs:
  - release template
- Modules:
  - none
- Tests:
  - manual release review

#### Delivery Trace Delta
- Batches:
  - bat-001
- Releases:
  - rel-001

#### Revalidation Impact
- Reverification Needed: no
- Existing Implementation Invalidated: no
- Status Recommendation: keep-current
- Follow-up Action: implement through bat-001

#### Validation Plan
- verify release checklist before approval

### CHG-20260430-02
- Date: 2026-04-30
- Author: Docs Maintainer

#### Change Summary
- strengthen docs-only release criteria to require broken-link evidence and explicit feedback triage

#### Intent
- release feedback에서 드러난 품질 gap을 기존 REQ에 반영한다.

#### Acceptance Criteria Delta
- Added:
  - broken-link 검증 결과가 release evidence에 포함되어야 한다.
  - feedback handoff는 Discovery/REQ/change-req 입력을 구분해야 한다.
- Updated:
  - release note update alone is no longer sufficient for docs-only release verification.
- Removed:
  - none

#### Impacted Area Delta
- Docs:
  - docs/releases/rel-001_20260428_docs_release_hardening.md
  - README.md
- Modules:
  - .stage-pilot/tools/stagepilot-doctor.py
- Tests:
  - doctor feedback summary sample

#### Delivery Trace Delta
- Batches:
  - bat-001 needs reverification or successor batch is required
- Releases:
  - rel-001 remains historical evidence only

#### Revalidation Impact
- Reverification Needed: yes
- Existing Implementation Invalidated: yes
- Status Recommendation: revert-to-approved
- Follow-up Action: prepare a follow-up batch to reverify docs-only release criteria

#### Validation Plan
- rerun doctor on a follow-up batch and verify release feedback summary plus broken-link evidence