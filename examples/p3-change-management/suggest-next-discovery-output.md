# Sample Output: suggest-next-discovery rel-001

- Input Release: `rel-001`
- Release Status: `feedback-captured`
- Basis:
  - `Discovery Input`: `release feedback triage automation for maintainers`
  - `REQ Input`: `release note checklist for docs-only profile`
  - `Change Request Input`: `req-001 docs release checklist needs stricter broken-link acceptance criteria`

## CANDIDATE-1

- Classification: `change-req`
- Target: `req-001`
- Value: High
- Risk: Medium
- Effort: Low
- Reason:
  - 피드백이 기존 REQ acceptance criteria 강화로 닫힌다.
  - 기존 batch/release trace를 유지한 채 REQ 상태를 재검증 대상으로 되돌릴 수 있다.
- Next Command:
  - `/change-req req-001`

## CANDIDATE-2

- Classification: `new-discovery`
- Target: `release feedback triage automation for maintainers`
- Value: Medium
- Risk: Low
- Effort: Medium
- Reason:
  - release feedback를 다음 반복 후보로 자동 분류하는 기능은 기존 REQ 수정만으로 닫히지 않는다.
  - 독립 Discovery로 추적하는 편이 범위와 성공 기준을 명확히 유지한다.
- Next Command:
  - `/new-discovery release feedback triage automation for maintainers`

## CANDIDATE-3

- Classification: `no-action`
- Target: `release note checklist for docs-only profile`
- Value: Low
- Risk: Low
- Effort: Low
- Reason:
  - 현재 `req-001` 변경에 흡수 가능하므로 별도 반복으로 분리하지 않는다.