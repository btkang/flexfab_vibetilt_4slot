# 0. 문서 상태
이 문서의 현재 상태와 식별 정보를 기록한다. 문서 추적과 승인 흐름에 필요한 최소 메타데이터를 적는다.

- 상태: {{DOC_STATUS:draft|review|confirmed}}
- 문서 ID: {{DISCOVERY_ID:dcy-<3자리>_<YYYYMMDD>_<topic-slug>}}
- 이슈명: {{ISSUE_NAME:짧은 한글 또는 영문 이슈명}}
- 작성 시각(KST): {{CREATED_AT_KST:TBD}}
- 마지막 갱신 시각(KST): {{UPDATED_AT_KST:TBD}}
- 대체됨: {{SUPERSEDED_BY_DISCOVERY:없음 또는 docs/discovery/<DISCOVERY_ID>.md}}
- 후속 Discovery 참조: {{FOLLOW_UP_DISCOVERY_REF:없음 또는 docs/discovery/<DISCOVERY_ID>.md}}
- 생성된 REQ 참조: 없음

# 1. 계획 상태 요약
현재 Discovery가 어떤 배경에서 작성되고 있는지, 연계 문서가 있는지, 지금 시점의 해석이 무엇인지 짧게 요약한다.

- 연계 문서 현황:
	- 관련 점수 파일: {{RELATED_SCORE_FILE:미생성 또는 경로}}
	- 관련 TODO/메모: {{TODO_FILE:경로 또는 없음}}
- 해석:
	- {{SUMMARY_LINE_1:현재 검토/작성 배경 요약}}
	- {{SUMMARY_LINE_2:이번 Discovery에서 판단해야 할 핵심 요약}}

# 2. 요구사항 판정 결과
이번 요청이 기존 요구사항과 비교해 신규인지, 유사한지, 중복인지 판단하고 그 근거를 남긴다.

- 판정: {{REQUIREMENT_CLASSIFICATION:완전 중복|유사|신규}}
- 근거
	- {{CLASSIFICATION_REASON_1}}
	- {{CLASSIFICATION_REASON_2}}

> **판정이 `완전 중복`인 경우**: 이하 섹션 작성을 중단하고 `# 9. 파일 처리 결과`에 중복 판정 사유와 참조 문서 경로를 기록한 뒤 문서를 닫는다. REQ 초안 작성으로 넘기지 않는다.

# 3. 문제점의 요약
문제의 현재 상태, 기대 상태, 관련 이해관계자, 제약사항을 정리한다. Discovery의 핵심 맥락을 가장 먼저 이해할 수 있어야 한다.

- 현재 상태
	- {{CURRENT_STATE_1}}
	- {{CURRENT_STATE_2}}
- 기대 상태
	- {{TARGET_STATE_1}}
	- {{TARGET_STATE_2}}
- 이해관계자
	- {{STAKEHOLDER_1:예. 문서 작성자(AI agent)}}
	- {{STAKEHOLDER_2:예. 리뷰어/승인자}}
	- {{STAKEHOLDER_3:예. 운영 담당자}}
- 제약사항
	- {{CONSTRAINT_1}}
	- {{CONSTRAINT_2}}

## 주요 사용자 시나리오
사용자 또는 운영자가 실제로 어떤 상황에서 이 변경을 필요로 하는지 대표 시나리오를 적는다.

- {{USER_SCENARIO_1}}
- {{USER_SCENARIO_2}}
- {{USER_SCENARIO_3}}

# 4. 이번에 정의할 변경
이번 Discovery에서 다루는 핵심 변경 항목을 목록으로 적는다. 기능, 구조, 규칙 변경을 함께 포함해도 된다.

- {{CHANGE_ITEM_1}}
- {{CHANGE_ITEM_2:필요 없으면 삭제}}
- {{CHANGE_ITEM_3:필요 없으면 삭제}}

## 영향 범위
변경이 미칠 코드/문서/운영 범위를 명시해 REQ 정제와 batch planning에서 추정 누락을 줄인다.

- 코드 영향:
	- {{CODE_IMPACT_1:예. docs/discovery/index.md 갱신 로직}}
	- {{CODE_IMPACT_2:필요 없으면 삭제}}
- 문서 영향:
	- {{DOC_IMPACT_1}}
	- {{DOC_IMPACT_2:필요 없으면 삭제}}
- 운영 영향:
	- {{OPS_IMPACT_1:예. 배포/운영 절차 변경 없음}}
	- {{OPS_IMPACT_2:필요 없으면 삭제}}

# 5. 요구사항 목록
실제로 구현과 검증으로 이어질 요구사항을 기능/비기능으로 나누어 정리하고, 범위 경계를 명확히 적는다.

## 기능 요구사항
- FR-1: {{FR_1}}
- FR-2: {{FR_2}}
- FR-3: {{FR_3:필요 없으면 삭제}}
- FR-4: {{FR_4:필요 없으면 삭제}}

## 비기능 요구사항
성능, 운영성, 일관성, 자동화 안정성 등 기능 외 요구사항을 적는다.

- NFR-1: {{NFR_1}}
- NFR-2: {{NFR_2}}
- NFR-3: {{NFR_3:필요 없으면 삭제}}

## 범위 경계
이번 반복에서 반드시 포함할 범위와 명시적으로 제외할 범위를 구분한다.

- In Scope
	- {{IN_SCOPE_1}}
	- {{IN_SCOPE_2}}
- Out of Scope
	- {{OUT_OF_SCOPE_1}}
	- {{OUT_OF_SCOPE_2}}

# 6. 리스크/가정 목록
불확실성과 전제 조건을 분리해서 적는다. 리스크는 영향과 완화 방안을, 가정은 현재 판단의 전제를 기록한다.

## 리스크
- R-1 ({{RISK_LEVEL_1:High|Medium|Low}}): {{RISK_1}}
	- 완화 방안: {{MITIGATION_1}}
- R-2 ({{RISK_LEVEL_2:High|Medium|Low}}): {{RISK_2}}
	- 완화 방안: {{MITIGATION_2}}
- R-3 ({{RISK_LEVEL_3:High|Medium|Low}}): {{RISK_3:필요 없으면 삭제}}
	- 완화 방안: {{MITIGATION_3:필요 없으면 삭제}}

## 가정
아직 확정되지 않았지만 현재 문서 작성의 기반이 되는 전제를 적는다.

- A-1: {{ASSUMPTION_1}}
- A-2: {{ASSUMPTION_2}}
- A-3: {{ASSUMPTION_3:필요 없으면 삭제}}

## 오픈 질문
답을 아직 모르는 항목을 적는다. 추정으로 덮지 말고 REQ 초안 작성 이전에 확인할 질문으로 남긴다.

- OQ-1: {{OPEN_QUESTION_1}}
	- 상태: {{OPEN_QUESTION_STATUS_1:Open|Answered|Deferred|Dropped}}
	- 처리 방안: {{OPEN_QUESTION_ACTION_1:누가 어떤 방식으로 결정/확인할지}}
	- 종료 조건: {{OPEN_QUESTION_EXIT_1:언제 이 질문을 Answered/Deferred/Dropped로 처리할지}}
- OQ-2: {{OPEN_QUESTION_2}}
	- 상태: {{OPEN_QUESTION_STATUS_2:Open|Answered|Deferred|Dropped}}
	- 처리 방안: {{OPEN_QUESTION_ACTION_2:누가 어떤 방식으로 결정/확인할지}}
	- 종료 조건: {{OPEN_QUESTION_EXIT_2:언제 이 질문을 Answered/Deferred/Dropped로 처리할지}}
- OQ-3: {{OPEN_QUESTION_3:필요 없으면 삭제}}
	- 상태: {{OPEN_QUESTION_STATUS_3:Open|Answered|Deferred|Dropped}}
	- 처리 방안: {{OPEN_QUESTION_ACTION_3:누가 어떤 방식으로 결정/확인할지}}
	- 종료 조건: {{OPEN_QUESTION_EXIT_3:언제 이 질문을 Answered/Deferred/Dropped로 처리할지}}

### 오픈 질문 처리 기준
- `Open`: 아직 답이 없고 다음 단계 진행에 영향이 있는 상태다. 문서에 유지한다.
- `Answered`: 답이 확정된 상태다. 관련 본문/요구사항에 반영한 뒤 오픈 질문 목록에서는 제거하거나 resolved 기록으로 이동한다.
- `Deferred`: 지금 단계에서 결정하지 않고 다음 단계 또는 후속 이슈로 넘기는 상태다. 넘길 대상과 이유를 반드시 적는다.
- `Dropped`: 더 이상 유효하지 않은 질문이다. 삭제 사유를 짧게 남기고 목록에서 제거한다.
- 처리 원칙 1: 질문만 남기지 말고 반드시 `상태`, `처리 방안`, `종료 조건`을 함께 적는다.
- 처리 원칙 2: 답이 나온 질문은 본문 반영 없이 단순 삭제하지 않는다. 반영 위치를 먼저 갱신한다.
- 처리 원칙 3: High 영향 질문은 `Deferred` 처리 후 처리 방안에 REQ 인계 추적 방식을 명시한다.

# 7. 초기 성공 기준
이 변경이 성공했다고 판단할 기준을 측정 가능하게 정의한다. 가능하면 데이터 출처까지 연결한다.

- S-1: {{SUCCESS_METRIC_1}}
- S-2: {{SUCCESS_METRIC_2}}
- S-3: {{SUCCESS_METRIC_3:필요 없으면 삭제}}

## 측정 방식 및 데이터 출처
각 성공 기준을 어떻게 측정할지와 어떤 데이터나 문서를 근거로 쓸지 적는다.

- S-1 측정 방식: {{MEASURE_METHOD_1}} / 데이터 출처: {{MEASURE_SOURCE_1}}
- S-2 측정 방식: {{MEASURE_METHOD_2}} / 데이터 출처: {{MEASURE_SOURCE_2}}
- S-3 측정 방식: {{MEASURE_METHOD_3:필요 없으면 삭제}} / 데이터 출처: {{MEASURE_SOURCE_3:필요 없으면 삭제}}

# 8. REQ로 넘기기 전 확인 체크
REQ 초안 작성 단계로 넘기기 전에 무엇이 확정되어야 하는지 확인한다. 미완료 항목은 후속 결정이 필요함을 뜻한다.

- [ ] 범위(In Scope/Out of Scope) 확정
	- 기준: {{CHECK_SCOPE_CRITERIA}}
- [ ] 성공 지표의 측정 방식/데이터 출처 확정
	- 기준: {{CHECK_METRICS_CRITERIA}}
- [ ] High 영향 리스크 우선순위 및 해소 책임자 지정
	- 기준: {{CHECK_RISK_OWNER_CRITERIA}}
- [ ] 모든 오픈 질문(OQ) `Open` 상태 없음 확인
	- 기준: OQ 목록에 `Open` 상태 항목이 0개이거나, 잔여 항목 전부 `Deferred` 처리 완료
- [ ] Freeze 확정 담당자(CONFIRMED_BY) 지정
	- 기준: {{CHECK_CONFIRMEDBY_CRITERIA}}

# 9. 파일 처리 결과
이 Discovery 문서 생성/갱신 결과와 참조 문서를 기록한다. 이후 추적과 리뷰 시 근거로 사용한다.

- 처리 결과: {{FILE_RESULT:생성|갱신|미생성}}
- 생성 경로: {{OUTPUT_PATH}}
- 참조 문서:
	- {{REFERENCE_1}}
	- {{REFERENCE_2}}
	- {{REFERENCE_3:필요 없으면 삭제}}

# 10. 사용자 결정 필요 항목 요약
사용자나 승인자가 결정해야 하는 항목만 모아 정리한다. 결정, 확인, 데이터 요청을 구분해서 적는다.

## DECIDE
- {{DECIDE_ITEM_1}}
- {{DECIDE_ITEM_2:필요 없으면 삭제}}

## CONFIRM
- {{CONFIRM_ITEM_1}}
- {{CONFIRM_ITEM_2:필요 없으면 삭제}}

## DATA
- {{DATA_ITEM_1}}
- {{DATA_ITEM_2:필요 없으면 삭제}}

# 11. Discovery Freeze
REQ 초안 작성 인계 시점의 확정 플래그와 섹션 참조만 기록한다. 내용 재작성 없이 원본 섹션을 링크로 대신한다.

## 섹션 참조
- 범위: [# 5. 요구사항 목록 > 범위 경계](#5-요구사항-목록)
- 리스크/오픈 질문: [# 6. 리스크/가정 목록](#6-리스크가정-목록)
- 성공 기준: [# 7. 초기 성공 기준](#7-초기-성공-기준)
- 인계 전 확인: [# 8. REQ로 넘기기 전 확인 체크](#8-req로-넘기기-전-확인-체크)
- 사용자 결정 필요: [# 10. 사용자 결정 필요 항목 요약](#10-사용자-결정-필요-항목-요약)

## Freeze 플래그
- Handoff Decision: {{FREEZE_HANDOFF_DECISION:REQ Drafting 진행 가능|보류}}
- Handoff Rationale: {{FREEZE_HANDOFF_RATIONALE:진행 가능 또는 보류 판단 근거 1-2줄}}
- Ready for REQ Drafting: {{READY_FOR_REQ_DRAFTING:false}}
- Confirmed By: {{CONFIRMED_BY:TBD}}
- Confirmed At (KST): {{CONFIRMED_AT_KST:TBD}}

# 12. 작성 가이드
템플릿 사용 시의 기본 규칙을 적는다. 실제 문서를 만들 때는 이 가이드를 기준으로 플레이스홀더를 치환한다.

- 이 파일은 템플릿이다. 실제 문서 작성 시 `{{...}}` 플레이스홀더를 구체 값으로 교체한다.
- 필요 없는 항목은 삭제하고, 부족한 항목은 같은 형식으로 추가한다.
- Open Questions는 가정으로 대체하지 말고 그대로 유지한다.
- `대체됨`은 현재 Discovery가 더 이상 기준 문서가 아닐 때 대체 Discovery 경로를 기록하고, 해당 사항이 없으면 `없음`으로 둔다.
- `후속 Discovery 참조`는 범위를 이어받는 별도 Discovery가 생성된 경우 그 Discovery 경로를 기록하고, 없으면 `없음`으로 둔다.
- Discovery Freeze는 문서 하단에 반드시 유지한다.