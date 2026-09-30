# Software Requirement Specification Index

## 목적
- 이 문서는 프로젝트의 SRS 운영 기준과 requirement 목록을 관리하는 entry point이다.
- 개별 requirement의 상세 내용은 타입별 폴더 하위 `req-XXX_*.md` 파일에서 관리한다.

## 사용 방법
1. 새로운 requirement가 필요하면 `Next Requirement ID`를 확인하고 `docs/srs/req-template.md`를 복사해 `docs/srs/<Type>/req-XXX_<slug>.md` 파일을 생성한다.
2. 초안 작성은 `draft-req`, 승인 전환은 `confirm-req`, 변경 대응은 `change-req`, 구현 완료 전환은 `confirm-req-implemented` skill을 기준으로 수행한다.
3. 작성 또는 수정 후 누락과 모호성을 점검한다.
4. requirement 추가 또는 변경 후 `Next Requirement ID`, `Requirement Register`, `Recent Change Log Summary`를 함께 갱신한다.
5. `Proposed` requirement를 승인하려면 승인 준비 상태와 blocker를 점검한다.
6. `Approved` requirement는 구현 이슈나 작업 항목으로 연결한다.
7. 구현과 검증이 끝난 requirement는 `confirm-req-implemented` skill로 완료 전환 가능 여부를 점검한다.
8. requirement 변경 시 `change-req` skill을 기준으로 해당 REQ 파일의 `Change Log`를 먼저 갱신한 뒤 본문을 수정한다.
9. 구현 전에는 관련 REQ 파일과 최근 `Change Log`를 기준으로 변경 범위를 판단한다.

## 상태 정의
- Proposed: 초안 상태이며 구현 기준으로 확정되지 않음
- Approved: 구현 기준으로 승인됨
- Implemented: 구현과 검증이 완료됨
- Deprecated: 더 이상 유지하지 않음

## Status Transition Evidence

### Proposed -> Approved
- `Status`, `Type`, `Priority`, `Owner`, `Intent`, `Requirement`, `Acceptance Criteria`, `Impacted Area`, `Change Log`가 모두 존재해야 한다.
- `Type`은 아래 분류 도표에서 정확하게 하나로 지정되어야 한다.
- `Acceptance Criteria`는 구현 판단에 사용할 수 있을 정도로 구체적이어야 한다.
- 미해결 assumption 또는 open question이 있으면 `Notes`에 명시하고, 승인 blocker 여부를 판단해야 한다.
- 승인 전환 시 해당 REQ 파일의 `Change Log`에 status 변경 사유를 기록해야 한다.

### Approved -> Implemented
- 각 `Acceptance Criteria`에 대해 test, manual verification, 또는 구현 결과 중 최소 1개의 evidence가 있어야 한다.
- evidence는 requirement 문서 자체 또는 연결된 구현/검증 결과에서 추적 가능해야 한다.
- 미충족 `Acceptance Criteria`가 있으면 `Implemented`로 전환하지 않는다.
- 구현 완료 전환 시 잔여 리스크와 미검증 영역이 있으면 `Change Log`에 기록해야 한다.
- 상태 전환은 `confirm-req-implemented` skill을 기준으로 수행한다.

### Requirement Change Handling
- 기존 REQ 본문(`Requirement`, `Acceptance Criteria`, `Impacted Area`)을 수정하기 전에는 새 `Change Log` 항목을 만든다.
- `Change Log`에는 최소한 `Acceptance Criteria Delta`, `Impacted Area Delta`, `Delivery Trace Delta`, `Revalidation Impact`, `Validation Plan`이 포함되어야 한다.
- 변경이 기존 구현 또는 verification evidence를 무효화하면 `Implemented` requirement는 `Approved`로 되돌린다.
- 변경이 승인 전제 자체를 다시 검토하게 만들면 `Approved` 또는 `Implemented` requirement를 `Proposed`로 되돌릴 수 있다.
- 상태 되돌림이 발생하면 `Requirement Register`와 `Recent Change Log Summary`도 함께 갱신한다.

## Requirement 타입 분류 도표

| 타입 | 정의 | 예시 |
|------|------|------|
| **Configuration** | 설정 옵션, 커스터마이징 포인트 | 설정 파라미터, 환경 변수, 옵션 항목 |
| **Data** | 데이터 구조, 저장/로드, 파일 형식, 스키마 | 설정 파일 형식, DB 스키마, API 구조 |
| **Deployment** | 배포 방식, 운영 절차, 배포 환경 | 컨테이너 배포, 스택 관리, 운영 가이드 |
| **Documentation** | 문서 작성, 가이드, 매뉴얼, 안내서 | 사용자 가이드, API 문서, 튜토리얼 |
| **Exception** | 오류 처리, 예외 복구, 재시도 전략 | 오류 복구, 검증 실패 처리, 폴백 전략 |
| **Installation** | 시스템 설치, 환경 구성, 초기화 절차 | Docker 이미지 빌드, DB 마이그레이션, 사전 조건 설정 |
| **Integration** | 외부 시스템 연동, 플러그인, 확장 | 3rd party 연동, 플러그인 인터페이스 |
| **Interface** | 사용자 상호작용 방식 (UI, API, CLI, 제어방식) | CLI 명령어, API 엔드포인트, 웹 UI 기능 |
| **Migration** | 데이터 이전, 버전 업그레이드 | 데이터 마이그레이션, 버전 호환성 |
| **Non-Functional** | 성능, 보안, 확장성, 호환성, 신뢰성 | 성능 목표, 보안 정책, 확장성 |
| **Testing** | 테스트 전략, QA 요구사항, 검증 방식 | 테스트 커버리지, 회귀 테스트, 수락 기준 |

## Requirement 작성 규칙
- 각 requirement는 고유 ID를 가진다. 예: `REQ-001`
- 다음 신규 requirement 번호의 source of truth는 이 문서의 `Next Requirement ID` 섹션이다.
- 새 requirement를 추가하거나 ID 충돌이 발생할 수 있는 변경을 했으면 `Next Requirement ID`를 함께 갱신한다.
- 각 requirement는 `Type`을 명시해야 한다. 위 분류 도표 중 정확하게 하나로 선택하며, `docs/srs/<Type>/` 폴더명도 같은 문자열을 사용한다.
- 각 requirement는 `Intent`, `Requirement`, `Acceptance Criteria`, `Impacted Area`를 반드시 포함한다.
- 모호한 표현("개선한다", "적절히 처리한다") 대신 관찰 가능하고 test 가능한 문장을 사용한다.
- requirement 변경 사항은 해당 REQ 파일의 `Change Log`에 기록한다.
- requirement 변경 시 영향 받는 batch/release와 재검증 필요 여부를 `Change Log`에 남긴다.
- `Recent Change Log Summary`는 최신 5건만 유지한다.
- status 전환은 `Status Transition Evidence` 기준을 따른다.

## Next Requirement ID

- Current: `REQ-001`
- Rule: `Requirement Register`에 있는 가장 큰 REQ 번호보다 정확히 1 커야 한다.

## Requirement Register

| ID | Title | Type | Status | Priority | Owner | Link |
| --- | --- | --- | --- | --- | --- | --- |

## Recent Change Log Summary

- 없음