# bat-001: FAIL·재완성 기록 보관 경로 분리와 DB 정책 문서화

- Status: release-candidate
- Profile: standard
- Source Discovery: dcy-001_20260930_fail-result-mongo-and-quality-monitor
- Included REQ: REQ-001, REQ-002
- Excluded REQ: 없음

## Summary
- 4슬롯 FAIL 결과와 재완성 이전 결과를 품질모니터링 수집 경로(`Result/`) 밖 `Result_보관/`으로 옮기고(REQ-001), FAIL 결과 DB 미업로드 정책과 저장 규칙을 기준 문서에 고정한다(REQ-002). 레드마인 #4900.

## Scope
- In Scope:
  - `FlexfabForm.Report.cs` 보관 경로·메뉴, 단위 테스트 기대 경로, 기준 문서 2개
- Out of Scope:
  - 품질모니터링(2슬롯 레포) 수정, FAIL DB 업로드, 성적서·이력 경로

## Documents
- Planning: [planning.md](./planning.md)
- Design: [design.md](./design.md)
- Implementation: [implementation.md](./implementation.md)
- Verification: [verification.md](./verification.md)

## Notes
- 파일 경로 계약(interface-contract "검사 결과 파일") 변경이 있어 batch-lite가 아닌 standard
- 사용자 "중간에 끊지 말고 계속 진행" 지시로 단계를 연속 진행, 결정은 추천안 적용 후 기록
