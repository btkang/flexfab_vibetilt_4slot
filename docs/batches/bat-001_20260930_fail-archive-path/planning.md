# bat-001 Planning

- Status: confirmed
- Batch: bat-001
- Profile: standard

## Included REQ
- REQ-001 FAIL·재완성 이전 결과를 `Result_보관/`에 저장 (Data, High)
- REQ-002 FAIL 결과 DB 미업로드 정책·저장 규칙 기준 문서 명시 (Documentation, High)

## Delivery Plan
1. `FlexfabForm.Report.cs`에 보관 루트 `ArchiveRootDir()` 추가, FAIL·PASS_DUPLICATE 저장 경로 변경, 이력 `결과파일` 열·우클릭 메뉴 갱신
2. 단위 테스트(`ReportTests`) PASS_DUPLICATE 이관 기대 경로를 보관 경로로 수정, 보관 경로 테스트 추가
3. 기준 문서 `interface-contract.md`·`data-model.md` 갱신
4. 빌드·단위 테스트 → SIL(완성·FAIL·재완성 사이클) 후 `Result/` JSON 스캔
5. 버전 0.2.3.7 (결함성 변경 아님 — 저장 규칙 변경이지만 기능 추가 없음 → c+1, d+1), CHANGELOG·릴리즈노트·레드마인

## Dependencies
- Internal: 테스트 절차서 T5-4·T5-6 기대 경로 갱신
- External: 없음 (품질모니터링 무수정)

## Design Gate
- Design Required Before Implementation: yes
- Reason: 결과 파일 경로 계약 변경 — 경로 규칙·되돌리기(Restore) 흐름을 먼저 고정

## Milestones
- M1 구현·단위 테스트 (2026-09-30)
- M2 SIL 검증·기준 문서 (2026-09-30)

## Risks
- 재완성 저장 실패 시 되돌리기(RestorePassDuplicate)가 드라이브·폴더가 다른 경로 간 이동이 되어도 동작해야 함 — 같은 드라이브(작업 폴더 하위)라 `File.Move` 유지
- 기존 사용자가 `Result/…/FAIL`을 찾을 수 있음 — 우클릭 메뉴가 새 경로를 열고 CHANGELOG에 명시
