# 0. 문서 상태

- 상태: confirmed
- 문서 ID: dcy-003_20261001_log-window-memory
- 이슈명: 화면 로그창 누적으로 장시간 운용 시 메모리 증가
- 작성 시각(KST): 2026-10-01 10:10
- 마지막 갱신 시각(KST): 2026-10-01 10:10
- 대체됨: 없음
- 후속 Discovery 참조: 없음
- 생성된 REQ 참조: docs/srs/Non-Functional/req-003_log-window-line-limit.md

# 1. 계획 상태 요약

- 연계 문서 현황:
	- 레드마인 #4901 (SIL 밤샘 2차), 작업 폴더 `02_분석_날짜/2026 1001-4슬롯_SIL밤샘2차/CODE_…_20261001_v01.md` §5
- 해석:
	- 밤샘 SIL 1차(0.7MB/사이클)·2차(0.9~1.3MB/사이클)에서 메모리가 선형 증가. 크래시는 없으나 며칠 무재시작 운용 시 위험

# 2. 요구사항 판정 결과

- 판정: 신규
- 근거: 로그창 관련 REQ·Batch 없음

# 3. 문제점의 요약

- 현재 상태
	- `FlexfabForm.cs` `AddLogMessage`가 `listBox_Log.Items.Add`만 하고 지우지 않는다 (`clear_log_on_run_start=1`일 때만 사이클 시작에 비움, 기본 0)
	- 2차 밤샘 232: 203사이클 동안 로그 79만 줄(파일 50MB) → Private 37→296MB, 관리 힙 9→156MB. 줄 수·텍스트 양과 증가량이 맞음 (원인 확정)
- 기대 상태
	- 화면 로그는 최근 일정 줄 수만 유지해 메모리가 일정 수준에서 멈춘다. 파일 로그(`Log/{proj}/날짜.log`)는 전부 남는다
- 이해관계자: 응용기술팀(강병택), 제조팀(장시간 운용)
- 제약사항: 메인폼 수정 → GitLab 기록 절차, 공용 라이브러리 무변경

## 주요 사용자 시나리오
- 교대 근무로 프로그램을 며칠 재시작하지 않고 운용
- TEST_LOG로 문제 추적 중 로그가 사이클당 수천 줄 발생

# 4. 이번에 정의할 변경

- 화면 로그 최대 줄 수(`log_max_lines`, 기본 20000, 0=무제한). 넘으면 오래된 줄을 상한의 10%씩 한 번에 지움
- 전역 적용(2슬롯 포함) — 안정성 결함 수정은 2슬롯 경로에도 허용(CLAUDE.md 예외 규정). 화면 표시만 바뀌고 파일 로그·판정 무관

## 영향 범위
- 코드 영향: `flexfab_mini/FlexfabForm.cs` `AddLogMessage`·`TrimLogLines`, `FlexfabForm.Slot4.cs` `ApplySlotLayout`(설정 읽기)
- 문서 영향: CHANGELOG, 릴리즈노트
- 운영 영향: 화면 "전체 로그 복사"는 최근 2만 줄까지만 — 전체는 로그 파일

# 5. 요구사항 목록

## 기능 요구사항
- FR-1: 화면 로그 줄 수가 `log_max_lines`를 넘으면 오래된 줄부터 지운다 (기본 20000, 0이면 무제한)
- FR-2: 파일 로그는 기존대로 모든 줄을 남긴다

## 비기능 요구사항
- NFR-1: 장시간 SIL에서 메모리가 상한 도달 후 더 늘지 않는다
- NFR-2: 로그 추가 성능 저하 없음 (지우기는 10% 단위 일괄)

## 범위 경계
- In Scope: 화면 로그 상한, 설정 키
- Out of Scope: 로그 파일 회전·압축, 그리드·불량보기 누적(측정상 원인 아님)

# 6. 리스크/가정 목록

## 리스크
- R-1 (Low): 작업자가 오래된 화면 로그를 못 봄 — 파일 로그로 확인, CHANGELOG에 명시

## 가정
- A-1: TEST_LOG 사이클당 약 3,900줄, MP_LOG는 더 적음 → 2만 줄 = TEST_LOG 약 5사이클, 메모리 상한 약 10MB 이하

## 오픈 질문
- 없음

# 7. 초기 성공 기준

- S-1: SIL 25사이클(TEST_LOG)에서 화면 로그 상한 도달 후 메모리(Private·관리 힙)가 평탄
- S-2: 단위 테스트·SIL 판정 회귀 없음

## 측정 방식 및 데이터 출처
- simui `[MEM]` 로그를 2차 밤샘 같은 구간과 비교

# 8. REQ로 넘기기 전 확인 체크

- [x] 범위 확정 — 사용자 "추천대로 진행해"(2026-10-01, 추천 3번 "로그창 줄 수 제한, batch-lite")
- [x] 성공 지표 확정
- [x] High 리스크 없음
- [x] Open OQ 0
- [x] Confirmed By 지정

# 9. 파일 처리 결과

- 처리 결과: 생성
- 생성 경로: docs/discovery/dcy-003_20261001_log-window-memory.md
- 참조 문서: 레드마인 #4901, `flexfab_mini/FlexfabForm.cs` `AddLogMessage`

# 10. 사용자 결정 필요 항목 요약

## DECIDE
- 없음 (기본값 20000은 추천값 — 바꾸려면 워크스페이스 `log_max_lines`)

## CONFIRM
- 없음

## DATA
- 없음

# 11. Discovery Freeze

## Freeze 플래그
- Handoff Decision: REQ Drafting 진행 가능
- Handoff Rationale: 원인 확정, 국소 변경, 결정 항목 없음
- Ready for REQ Drafting: true
- Confirmed By: 강병택 (기본 승인자, 사용자 "추천대로 진행해" 2026-10-01)
- Confirmed At (KST): 2026-10-01 10:10
