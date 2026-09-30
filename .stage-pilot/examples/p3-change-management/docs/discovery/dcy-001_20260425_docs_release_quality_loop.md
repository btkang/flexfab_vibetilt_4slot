# Discovery

- 상태: confirmed
- 문서 ID: dcy-001_20260425_docs_release_quality_loop
- 이슈명: Docs release quality loop
- 작성 시각(KST): 2026-04-25 10:00
- 마지막 갱신 시각(KST): 2026-04-30 13:00
- 대체됨: 없음
- 후속 Discovery 참조: 없음
- 생성된 REQ 참조: req-001

# 1. 계획 상태 요약

- 연계 문서 현황:
  - 관련 점수 파일: 미생성
  - 관련 TODO/메모: 없음
- 해석:
  - docs-only release에서도 문서 검증 품질이 release 판단 근거에 직접 연결돼야 한다.
  - 운영 피드백으로 checklist 강화를 반복적으로 흡수할 수 있어야 한다.

# 2. 요구사항 판정 결과

- 판정: 신규
- 근거
  - 기존 release checklist는 docs-only profile 세분화 기준이 부족했다.
  - follow-up feedback를 기존 REQ와 연결할 필요가 있었다.

# 3. 문제점의 요약

- 현재 상태
  - docs release는 수동 검토 메모에 의존한다.
  - broken-link 점검과 release note 점검이 acceptance criteria로 명확히 드러나지 않는다.
- 기대 상태
  - release checklist가 명시적 acceptance criteria와 evidence로 연결된다.
  - 운영 피드백이 후속 REQ 또는 Discovery로 자연스럽게 이어진다.
- 이해관계자
  - maintainer
  - reviewer
  - docs consumer
- 제약사항
  - 문서 패키지는 app-service 운영 지표를 사용하지 않는다.
  - doctor 검증과 release 문서만으로도 추적 가능해야 한다.

## 주요 사용자 시나리오

- maintainer가 docs-only release 전 broken-link와 release note 상태를 확인한다.
- reviewer가 release feedback를 보고 기존 REQ 변경인지 신규 반복인지 판단한다.

# 4. 이번에 정의할 변경

- docs release checklist를 REQ와 release evidence에 연결한다.
- feedback handoff에서 후속 작업 분류 기준을 남긴다.

## 영향 범위

- 코드 영향:
  - 없음
- 문서 영향:
  - docs/srs/Documentation/req-001_docs_release_checklist.md
  - docs/releases/rel-001_20260428_docs_release_hardening.md
- 운영 영향:
  - docs-only release review 절차 강화

# 5. 요구사항 목록

## 기능 요구사항
- FR-1: docs-only release는 broken-link 검증을 acceptance criteria로 포함해야 한다.
- FR-2: release feedback는 후속 Discovery/REQ/change-req 입력으로 분리되어야 한다.

## 비기능 요구사항
- NFR-1: 변경 이력은 Change Log와 release feedback 양쪽에서 추적 가능해야 한다.

## 범위 경계
- In Scope
  - docs release checklist
  - feedback handoff structure
- Out of Scope
  - app-service runtime health check
  - 외부 배포 자동화

# 6. 리스크/가정 목록

## 리스크
- R-1 (Medium): checklist 강화 시 기존 구현 증거가 부족해질 수 있다.
  - 완화 방안: change-req로 상태 되돌림과 재검증 계획을 남긴다.
- R-2 (Low): feedback 분류가 모호하면 follow-up 경로가 섞일 수 있다.
  - 완화 방안: suggest-next-discovery에서 분류 근거를 명시한다.

## 가정
- A-1: docs-only release는 doctor와 수동 리뷰 결과로 검증 가능하다.
- A-2: feedback handoff는 release 문서가 source of truth다.

# 7. 초기 성공 기준

- S-1: docs checklist REQ가 release feedback까지 추적된다.
- S-2: feedback에서 change-req와 new-discovery가 구분된다.

## 측정 방식 및 데이터 출처

- S-1 측정 방식: doctor traceability 확인 / 데이터 출처: docs/srs, docs/releases
- S-2 측정 방식: feedback handoff review / 데이터 출처: release 문서

# 8. REQ로 넘기기 전 확인 체크

- [x] 범위(In Scope/Out of Scope) 확정
  - 기준: docs checklist와 feedback 구조만 포함
- [x] 성공 지표의 측정 방식/데이터 출처 확정
  - 기준: doctor와 release 문서로 추적 가능
- [x] High 영향 리스크 우선순위 및 해소 책임자 지정
  - 기준: maintainer가 후속 변경 관리 수행
- [x] 모든 오픈 질문(OQ) `Open` 상태 없음 확인
  - 기준: open question 없음
- [x] Freeze 확정 담당자(CONFIRMED_BY) 지정
  - 기준: maintainer confirmed

# 9. 파일 처리 결과

- 처리 결과: 생성
- 생성 경로: docs/discovery/dcy-001_20260425_docs_release_quality_loop.md
- 참조 문서:
  - docs/srs/Documentation/req-001_docs_release_checklist.md
  - docs/releases/rel-001_20260428_docs_release_hardening.md

# 10. 사용자 결정 필요 항목 요약

## DECIDE
- 없음

## CONFIRM
- 없음

## DATA
- 없음

# 11. Discovery Freeze

## 섹션 참조
- 범위: [# 5. 요구사항 목록 > 범위 경계](#5-요구사항-목록)
- 리스크/오픈 질문: [# 6. 리스크/가정 목록](#6-리스크가정-목록)
- 성공 기준: [# 7. 초기 성공 기준](#7-초기-성공-기준)
- 인계 전 확인: [# 8. REQ로 넘기기 전 확인 체크](#8-req로-넘기기-전-확인-체크)
- 사용자 결정 필요: [# 10. 사용자 결정 필요 항목 요약](#10-사용자-결정-필요-항목-요약)

## Freeze 플래그
- Handoff Decision: REQ Drafting 진행 가능
- Handoff Rationale: docs release quality loop를 REQ와 release feedback에 연결할 준비가 됨
- Ready for REQ Drafting: true
- Confirmed By: maintainer
- Confirmed At (KST): 2026-04-25 11:00