# 플레이스홀더 가이드

이 파일은 active workspace 기준으로 `.github/templates/`와 `.github/skills/`에서 사용하는 플레이스홀더의 문법과 처리 원칙을 정의한다.

기본 전제는 아래와 같다.

- active SDLC 진입점은 skill-only 체계다.
- baseline 문서와 active index 초기화는 `bootstrap-baseline`이 담당하며, 이 단계는 Discovery unit를 만들지 않는다.
- `.github/prompts/`는 active 기준 문서가 아니다.
- active 산출물 경로는 `docs/discovery/`, `docs/srs/`, `docs/batches/`, `docs/releases/`다.

## 1. 문법 규칙

| 형식 | 의미 | 예시 |
| --- | --- | --- |
| `{{KEY}}` | 단순 치환 대상 | `{{DISCOVERY_ID}}` |
| `{{KEY:hint}}` | 힌트 포함 치환 대상 | `{{CONFIRMED_AT_KST:TBD}}` |
| `{{KEY:opt1\|opt2}}` | 열거형 선택 값 | `{{DOC_STATUS:draft\|confirmed}}` |

규칙:

- 최종 문서에는 `{{...}}` 문법이 남지 않아야 한다.
- 힌트 텍스트는 치환 시 함께 제거한다.
- 사람 승인 없이는 확정할 수 없는 값은 본문에 무리하게 확정하지 말고 Notes 또는 결정 필요 항목으로 남긴다.

## 2. 처리 주체

플레이스홀더는 아래 세 가지 처리 주체 중 하나에 속한다.

### A. 자동 치환

- ID, 생성 시각, 갱신 시각, 상태 기본값처럼 실행 시점에 자동으로 정해지는 값
- 예:
  - `{{DISCOVERY_ID}}`
  - `{{CONFIRMED_AT_KST}}`
  - `{{BAT_ID}}`
  - `{{REL_ID}}`

### B. AI 근거 기반 치환

- 현재 저장소 상태, 상위 문서, 이미 확정된 값으로 채울 수 있는 항목
- 예:
  - Discovery의 현재 상태 요약
  - REQ의 Impacted Area 초안
  - Batch planning의 dependency 정리
  - Release rollout checklist 초안

### C. 사람 결정 필요

- 정책 선택, 승인자 지정, 우선순위 확정, 외부 환경 정보처럼 사람 판단이 필요한 값
- 이런 항목은 무리하게 확정하지 않는다.

## 3. 경로별 플레이스홀더 원칙

### 3.0 Bootstrap baseline 템플릿

대상 경로:

- `.github/templates/bootstrap/baseline-seed.yaml`
- `.github/templates/project-structure.md`
- `.github/templates/runtime-flows.md`
- `.github/templates/discovery/index.md`
- `.github/templates/srs/index.md`
- `.github/templates/batches/index.md`
- `.github/templates/releases/index.md`

처리 원칙:

- `bootstrap-baseline`은 먼저 `.stage-pilot/bootstrap/baseline.yaml` seed를 만들거나 보강한다.
- greenfield 저장소처럼 관찰 가능한 정보가 부족하면 최소 질문 세트로 seed를 채운다.
- `bootstrap-baseline`은 baseline 문서와 index를 초기화하지만 Discovery 문서를 만들지 않는다.
- baseline 문서는 seed를 source of truth로 렌더링하고, 필요하면 저장소 관찰값으로 보강한다.
- index 문서는 빈 register 골격만 준비하고, 실제 unit 행은 이후 skill이 추가한다.

주요 키:

- `{{BASELINE_MODE}}`
- `{{BASELINE_SEED_PATH}}`
- `{{PROJECT_SUMMARY}}`
- `{{PRIMARY_DOMAIN}}`
- `{{TECH_STACK_BULLETS}}`
- `{{PRIMARY_RUNTIME}}`
- `{{PRIMARY_ENTRYPOINT_BULLETS}}`
- `interfaces.contracts[*]`
- `data_model.entities[*]`

### 3.0.1 선택 cross-cutting 템플릿

대상 경로:

- `.github/templates/interface-contract.md`
- `.github/templates/data-model.md`

처리 원칙:

- 이 문서들은 optional cross-cutting reference다.
- active unit의 source of truth를 대체하지 않고, 여러 REQ와 batch에 공통으로 반복되는 계약 정보를 모아 둔다.
- `bootstrap-baseline`은 seed의 `interfaces`와 `data_model` 섹션을 우선 읽고, 값이 없으면 `runtime`, `structure`, `notes`를 바탕으로 기본 초안을 구체적으로 유도한다.
- baseline 수준의 변경이 있을 때만 갱신하고, REQ별 acceptance criteria를 그대로 복사하지 않는다.

주요 키:

- `{{BASELINE_MODE}}`
- `{{BASELINE_SEED_PATH}}`
- `{{PROJECT_SUMMARY}}`
- `{{PRIMARY_DOMAIN}}`
- `{{PRIMARY_RUNTIME}}`
- `{{INTERFACE_NAME_1}}`
- `{{INTERFACE_PURPOSE_1}}`
- `{{INTERFACE_SUMMARY_BULLETS}}`
- `{{INTERFACE_INPUTS_1}}`
- `{{INTERFACE_OUTPUTS_1}}`
- `{{INTERFACE_ERRORS_1}}`
- `{{INTERFACE_COMPATIBILITY_1}}`
- `{{INTERFACE_SHARED_CONSTRAINTS_BULLETS}}`
- `{{INTERFACE_CURRENT_GAPS_BULLETS}}`
- `{{PERSISTENCE_BACKEND}}`
- `{{ENTITY_NAME_1}}`
- `{{ENTITY_PURPOSE_1}}`
- `{{ENTITY_SOURCE_1}}`
- `{{ENTITY_LIFECYCLE_1}}`
- `{{MODEL_SUMMARY_BULLETS}}`
- `{{ENTITY_FIELDS_1}}`
- `{{ENTITY_RELATIONSHIPS_1}}`
- `{{CONSISTENCY_RULE_BULLETS}}`
- `{{PERSISTENCE_NOTES_BULLETS}}`
- `{{MODEL_CURRENT_GAPS_BULLETS}}`

### 3.1 Discovery 템플릿

대상 경로:

- `.github/templates/discovery/discovery.md`
- `docs/discovery/*.md`

주요 키:

- `{{DISCOVERY_ID}}`
- `{{ISSUE_NAME}}`
- `{{CREATED_AT_KST}}`
- `{{UPDATED_AT_KST}}`
- `{{FREEZE_HANDOFF_DECISION}}`
- `{{READY_FOR_REQ_DRAFTING}}`
- `{{CONFIRMED_BY}}`
- `{{CONFIRMED_AT_KST}}`

처리 원칙:

- Discovery는 REQ 초안 작성으로 넘길 수 있을 때만 freeze handoff를 완료한다.
- Discovery 문서의 용어는 `Planning` 직결이 아니라 `REQ Drafting` 기준으로 유지한다.

### 3.2 SRS 템플릿

대상 경로:

- `docs/srs/req-template.md`
- `docs/srs/index.md`

주요 키:

- `REQ-XXX`
- `Status`
- `Type`
- `Priority`
- `Owner`

처리 원칙:

- REQ는 `Proposed -> Approved -> Implemented -> Deprecated` 상태를 따른다.
- `draft-req`는 초안을 만들고, `confirm-req`는 `Proposed -> Approved`, `confirm-req-implemented`는 `Approved -> Implemented` 전환을 담당한다.

### 3.3 Batch 템플릿

대상 경로:

- `.github/templates/batches/*`
- `docs/batches/*`

주요 키:

- `{{BAT_ID}}`
- `Profile`
- Included REQ 목록
- Source Discovery

처리 원칙:

- batch는 Approved REQ만 포함한다.
- batch는 `standard` 또는 `batch-lite` profile을 가진다.
- `batch-lite`는 `minor-change` fast path의 기본 delivery profile이며, 처음에는 `index.md`와 `planning.md`만 만들 수 있다.
- planning, design, implementation, verification 문서는 같은 batch 폴더 아래에서 관리한다. 단, `batch-lite`는 design/implementation/verification을 단계 진입 시점에 생성할 수 있다.
- `confirm-batch-verification` 성공 전에는 `release-candidate`로 올리지 않는다.

### 3.4 Release 템플릿

대상 경로:

- `.github/templates/releases/*`
- `docs/releases/*`

주요 키:

- `{{REL_ID}}`
- `Profile`
- `Change Request Input`
- Included Batch
- Rollout Plan
- Rollback Plan

처리 원칙:

- release는 `release-candidate` batch만 포함한다.
- release는 `docs-only`, `tooling`, `app-service` profile 중 하나를 가진다.
- feedback handoff에는 `Discovery Input`, `REQ Input`, `Change Request Input`을 구분해 기록한다.
- `confirm-release`는 release 승인, `capture-release-feedback`는 운영 환류 기록을 담당한다.

## 4. Active Skill Summary

| 목적 | Skill | 주요 입력 | 주요 출력 |
| --- | --- | --- | --- |
| Bootstrap baseline 초기화 | `.github/skills/bootstrap-baseline/SKILL.md` | 저장소 루트, baseline/index 존재 여부 | baseline 문서와 active index |
| Discovery 초안 생성 | `.github/skills/new-discovery/SKILL.md` | 이슈/요청 요약 | `docs/discovery/*.md` |
| Discovery 확정 | `.github/skills/confirm-discovery/SKILL.md` | `DISCOVERY_ID` | confirmed Discovery |
| REQ 초안 생성 | `.github/skills/draft-req/SKILL.md` | `DISCOVERY_ID` | `docs/srs/<Type>/req-*.md` |
| REQ 확정 | `.github/skills/confirm-req/SKILL.md` | `REQ-ID` | Approved REQ |
| REQ 변경 관리 | `.github/skills/change-req/SKILL.md` | `REQ-ID` | Change Log 기반 REQ 갱신 |
| REQ 구현 완료 전환 | `.github/skills/confirm-req-implemented/SKILL.md` | `REQ-ID` 또는 `BAT-ID` | Implemented REQ |
| Batch 추천 | `.github/skills/suggest-batch-reqs/SKILL.md` | `DISCOVERY_ID` 또는 REQ 목록 | batch 후보안 |
| Batch 생성 | `.github/skills/draft-batch/SKILL.md` | Approved REQ 목록 | `docs/batches/<BAT_ID>/` |
| Delivery 진행 | `.github/skills/draft-batch-planning/SKILL.md`, `.github/skills/draft-batch-design/SKILL.md`, `.github/skills/run-batch-implementation/SKILL.md`, `.github/skills/draft-batch-verification/SKILL.md`, `.github/skills/confirm-batch-verification/SKILL.md` | `BAT-ID` | batch 문서와 상태 |
| Release 진행 | `.github/skills/draft-release/SKILL.md`, `.github/skills/confirm-release/SKILL.md`, `.github/skills/capture-release-feedback/SKILL.md` | `BAT-ID` 또는 `REL-ID` | release 문서와 운영 환류 |
| 다음 Discovery 추천 | `.github/skills/suggest-next-discovery/SKILL.md` | `REL-ID` 또는 없음 | follow-up 후보안 |
| 상위 orchestration | `.github/skills/run-sdlc/SKILL.md` | Discovery/REQ/Batch/Release 식별자 | 다음 skill 라우팅 |

## 5. 금지 사항

- active 설명 문서에서 `.github/prompts/`를 기준 경로로 안내하지 않는다.
- `review-*`를 별도 active 단계로 서술하지 않는다.
- `docs/sdlc/`를 신규 active 산출물 경로로 사용하지 않는다.