# bat-002 Planning

- Status: confirmed
- Batch: bat-002
- Profile: batch-lite

## Included REQ
- REQ-003 화면 로그창 줄 수 상한 (Non-Functional, Medium)

## Delivery Plan
1. `ApplySlotLayout`에서 `log_max_lines` 읽기(없으면 20000, 0=무제한)
2. `AddLogMessage`에서 추가 후 `TrimLogLines()` — 상한 초과 시 오래된 줄을 상한의 10%만큼 `BeginUpdate`/`EndUpdate` 안에서 일괄 삭제
3. 버전 0.2.4.8 / 메인툴 1.8.3.10 (261001), CHANGELOG·릴리즈노트
4. 빌드·단위 테스트 → SIL 25사이클(TEST_LOG) [MEM] 비교 → 커밋 → GitLab 메인폼 기록 → 레드마인

## Dependencies
- Internal: 없음
- External: 없음

## Design Gate
- Design Required Before Implementation: no
- Reason: 메서드 1개 추가, 화면 표시만 변경

## Milestones
- M1 구현·검증 (2026-10-01)

## Risks
- 화면에서 오래된 로그가 사라짐 — 파일 로그로 확인 (CHANGELOG 명시)
