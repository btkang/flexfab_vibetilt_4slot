# bat-002: 화면 로그창 줄 수 상한

- Status: release-candidate
- Profile: batch-lite
- Source Discovery: dcy-003_20261001_log-window-memory
- Included REQ: REQ-003
- Excluded REQ: 없음

## Summary
- 화면 로그 무한 누적으로 인한 메모리 증가(사이클당 약 1MB)를 줄 수 상한(`log_max_lines`, 기본 20000)으로 막는다. 레드마인 #4901.

## Scope
- In Scope:
  - `FlexfabForm.cs` `TrimLogLines`, `FlexfabForm.Slot4.cs` 설정 읽기
- Out of Scope:
  - 로그 파일 회전, 워크스페이스 JSON 변경(기본값으로 동작)

## Documents
- Planning: [planning.md](./planning.md)
- Implementation: [implementation.md](./implementation.md)
- Verification: [verification.md](./verification.md)

## Notes
- minor-change fast path: 단일 Approved REQ, 국소 변경, 구조·인터페이스·흐름 영향 없음 → design 생략
