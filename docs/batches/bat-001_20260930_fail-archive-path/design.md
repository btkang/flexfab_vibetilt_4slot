# bat-001 Design

- Status: confirmed
- Batch: bat-001

## Architecture Summary
- 결과 루트를 두 개로 나눈다: `Result/{프로젝트}/` = 완성 결과·성적서·이력(품질모니터링 수집 대상), `Result_보관/{프로젝트}/` = FAIL·PASS_DUPLICATE(수집 대상 아님). 둘 다 `Directory.GetCurrentDirectory()` 기준 형제 폴더.

## Changed Areas
- Docs: `docs/interface-contract.md`, `docs/data-model.md`
- Modules: `flexfab_mini/FlexfabForm.Report.cs`
- Interfaces: 검사 결과 파일 경로 (FAIL·PASS_DUPLICATE)

## Key Decisions
- `ArchiveRootDir() = {cwd}/Result_보관/{ResultProjFolder()}` — 프로젝트 폴더명 규칙은 `ResultRootDir`과 동일
- FAIL: `SaveFailJsonSlot`의 날짜 폴더를 `ArchiveRootDir()/{날짜}`로. `FailJsonPath`(정적, 파일명 규칙)는 그대로
- PASS_DUPLICATE: `PreservePassDuplicate`가 완성 파일의 날짜 폴더명(`{yyyy-MM-dd}`)을 읽어 `ArchiveRootDir()/{날짜}`를 `PassDuplicatePath`에 넘김. `PassDuplicatePath`(정적) 그대로
- `RestorePassDuplicate`는 옮긴 경로 → 원래 경로 `File.Move` 그대로 (같은 드라이브)
- 이력 `결과파일` 열: 완성 = `Result/{프로젝트}` 기준 상대경로(기존), FAIL = 작업 폴더 기준 `Result_보관/{프로젝트}/{날짜}/FAIL/…`
- 우클릭 메뉴 "FAIL 결과 폴더 열기 (오늘)" → `ArchiveRootDir()/{오늘}/FAIL`

## Edge Cases
- 완성 파일 경로가 예상 형식이 아니면(날짜 폴더명이 아님) 날짜 폴더명으로 오늘 날짜 사용
- 자정 경계: 완성 파일의 날짜 폴더명을 그대로 쓰므로 보관 날짜 = 원래 결과 날짜

## Architecture Impact
- 품질모니터링은 `Result/`만 재귀 수집하므로 무수정으로 완성 결과만 읽게 됨
- 2슬롯 경로는 FAIL·PASS_DUPLICATE를 쓰지 않아 무영향

## Reference Doc Update Plan
- `interface-contract.md` 검사 결과 파일 Outputs·Compatibility, `data-model.md` Persistence Notes·Consistency Rules·Current Gaps(K2 해소)
