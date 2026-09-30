# P3 Example: change-req and suggest-next-discovery

이 예시는 release feedback 이후 개선 루프를 어떻게 닫는지 보여 주는 self-contained sample workspace다.

## Scenario

1. `rel-001` 배포가 `feedback-captured` 상태로 닫혔다.
2. 운영 피드백에서 두 종류의 후속 작업이 나왔다.
   - 기존 REQ 강화: 문서 release checklist의 acceptance criteria를 더 엄격하게 바꿔야 한다.
   - 신규 반복 후보: release feedback를 기반으로 follow-up triage를 자동 추천하는 개선이 필요하다.
3. `suggest-next-discovery rel-001`는 위 두 항목을 각각 `change-req`와 `new-discovery`로 분류한다.
4. 이 예시의 active docs는 그 결과 `change-req req-001`를 이미 한 번 적용한 뒤의 상태를 보여 준다.

## Files to inspect

- `docs/releases/rel-001_20260428_docs_release_hardening.md`
  - `Feedback Handoff`에 `Discovery Input`, `REQ Input`, `Change Request Input`이 모두 있다.
- `docs/srs/Documentation/req-001_docs_release_checklist.md`
  - 두 번째 `Change Log` 항목이 `change-req` 실행 결과를 나타낸다.
  - 구현 근거가 무효화되어 상태가 `Approved`로 되돌아간 상태다.
- `suggest-next-discovery-output.md`
  - `suggest-next-discovery rel-001`의 예시 결과다.
- `change-req-output.md`
  - `change-req req-001`의 예시 결과다.

## Validation

이 예시 루트는 doctor로 독립 검증할 수 있다.

```bash
python3 .stage-pilot/tools/stagepilot-doctor.py examples/p3-change-management
```