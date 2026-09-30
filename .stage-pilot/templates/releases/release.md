# {{REL_ID}}: <릴리즈 제목>

- Status: draft | confirmed | released | feedback-captured
- Profile: docs-only | tooling | app-service
- Included Batch: <BAT-ID 목록>

## Scope
- Included:
  - <포함 batch 또는 REQ>
- Excluded:
  - <제외 범위>

## Rollout Plan
1. <배포 순서>
2. <배포 순서>

## Rollback Plan
- <롤백 조건과 절차>

## Verification Checklist
- <배포 전 확인>
- <배포 후 확인>

## Release Execution Evidence
- Release Status Transition Target: released

### Deployment Facts
- Executed At:
  - <배포 시작 시각>
- Completed At:
  - <배포 완료 시각>
- Environment:
  - <배포 대상 환경>
- Operator:
  - <배포 수행자>
- Revision or Artifact:
  - <커밋 SHA 또는 배포 artifact 식별자>
- Commands or Procedure:
  - <실행 명령 또는 배포 절차 요약>

### Profile-Specific Checks
- docs-only:
  - Published or Packaged Targets:
    - <문서 경로, 배포 산출물 또는 none>
  - Link or Rendering Check:
    - <링크 점검, 렌더링 확인 또는 none>
  - Navigation or Consumer Update:
    - <목차/가이드/릴리즈 노트 반영 결과 또는 none>
- tooling:
  - Install or Upgrade Smoke:
    - <설치/업데이트/부트스트랩 결과 또는 none>
  - CLI or Script Execution:
    - <명령 실행 결과 또는 none>
  - Compatibility Note:
    - <하위 호환성/운영자 영향 또는 none>
- app-service:
  - Health Endpoint:
    - <호출 결과와 확인 시각>
  - Startup Log Fields:
    - <app, event, command, run_id 확인 결과>
  - Request Log Fields:
    - <request_id 포함 여부와 확인 결과>
  - Smoke or Manual Verification:
    - <배포 후 재실행한 검증과 결과>

### Rollback Outcome
- Rollback Required:
  - <yes 또는 no>
- Rollback Trigger:
  - <발생 시 원인, 없으면 none>
- Rollback Action:
  - <수행한 조치, 없으면 none>

### Release Decision Evidence
- Released Decision:
  - <released 전환 여부>
- Rationale:
  - <released 전환 판단 근거>
- Recorded By:
  - <기록자>
- Recorded At:
  - <기록 시각>

## Feedback Handoff
- Observation Summary:
  - <운영 관찰 사실 요약 또는 없음>
- Discovery Input:
  - <후속 discovery 후보>
- REQ Input:
  - <후속 req 후보>
- Change Request Input:
  - <기존 req 변경 후보>