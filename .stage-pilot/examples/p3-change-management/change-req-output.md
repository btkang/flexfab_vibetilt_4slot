# Sample Output: change-req req-001

- Target REQ: `docs/srs/Documentation/req-001_docs_release_checklist.md`
- New Change Entry: `CHG-2026-04-30-02` represented as `CHG-20260430-02`
- Change Summary:
  - docs-only release profile에서도 broken-link 검증을 필수 acceptance criteria로 승격
  - delivery trace에 기존 batch/release 영향과 재검증 필요 여부를 추가
- Impacted Delivery Trace:
  - Batches: `bat-001`
  - Releases: `rel-001`
- Revalidation Impact:
  - Reverification Needed: `yes`
  - Existing Implementation Invalidated: `yes`
  - Status Recommendation: `revert-to-approved`
- Result:
  - REQ status changed from `Implemented` to `Approved`
  - `docs/srs/index.md` register status changed to `Approved`
  - `Recent Change Log Summary` updated with the new change entry

## Rationale

이 변경은 기존 release evidence만으로는 새 acceptance criteria를 충족했다고 볼 수 없으므로 `Implemented` 상태를 유지할 수 없다. 따라서 REQ는 `Approved`로 되돌아가고, 후속 batch verification을 다시 거쳐야 한다.