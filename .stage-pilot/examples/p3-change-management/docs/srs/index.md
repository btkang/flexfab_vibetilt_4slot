# Software Requirement Specification Index

## 목적
- 이 문서는 프로젝트의 SRS 운영 기준과 requirement 목록을 관리하는 entry point이다.
- 개별 requirement의 상세 내용은 타입별 폴더 하위 `req-XXX_*.md` 파일에서 관리한다.

## 사용 방법
1. 새로운 requirement가 필요하면 `Next Requirement ID`를 확인하고 `docs/srs/req-template.md`를 복사해 `docs/srs/<Type>/req-XXX_<slug>.md` 파일을 생성한다.
2. 초안 작성은 `draft-req`, 승인 전환은 `confirm-req`, 변경 대응은 `change-req`, 구현 완료 전환은 `confirm-req-implemented` skill을 기준으로 수행한다.
3. 작성 또는 수정 후 누락과 모호성을 점검한다.
4. requirement 추가 또는 변경 후 `Next Requirement ID`, `Requirement Register`, `Recent Change Log Summary`를 함께 갱신한다.

## Next Requirement ID

- Current: `REQ-002`
- Rule: `Requirement Register`에 있는 가장 큰 REQ 번호보다 정확히 1 커야 한다.

## Requirement Register

| ID | Title | Type | Status | Priority | Owner | Link |
| --- | --- | --- | --- | --- | --- | --- |
| REQ-001 | Docs release checklist | Documentation | Approved | Medium | Docs Maintainer | [req-001](./Documentation/req-001_docs_release_checklist.md) |

## Recent Change Log Summary

- 2026-04-30 `REQ-001` `CHG-20260430-02`: broken-link acceptance criteria 강화, 상태 `Implemented -> Approved`