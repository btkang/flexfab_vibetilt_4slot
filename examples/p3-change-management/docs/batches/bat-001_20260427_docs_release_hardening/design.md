# bat-001 Design

- Status: confirmed

## Architecture Summary
- docs-only release는 app-service health signal 대신 문서 품질 증거를 사용한다.

## Changed Areas
- release template
- feedback handoff structure

## Key Decisions
- docs-only release는 broken-link와 release note update를 핵심 evidence로 남긴다.

## Edge Cases
- release 후 feedback로 acceptance criteria가 강화되면 REQ 상태를 되돌릴 수 있다.

## Architecture Impact
- runtime-flows

## Reference Doc Update Plan
- project-structure.md: none
- runtime-flows.md: docs-only release feedback flow 반영