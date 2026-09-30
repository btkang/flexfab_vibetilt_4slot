# Copilot Instructions

이 파일은 호스트 프로젝트 고유 규칙과 StagePilot 규칙을 함께 담는다.

## Host Project Rules

호스트 프로젝트 전용 규칙은 이 섹션에 작성한다.

## StagePilot
<!-- STAGEPILOT:BEGIN -->
# SDLC Governance Instructions

이 워크스페이스의 AI 에이전트는 Agile 중심 반복형 SDLC를 따른다.
모든 작업은 3-phase 구조와 그 안의 단위별 게이트를 기준으로 수행하며, 임의 단계 스킵은 허용되지 않는다(긴급 핫픽스 예외는 별도 규칙 적용).

## Lifecycle Phases

1. Requirements
2. Delivery
3. Release & Feedback

## Governing Units

1. Discovery: 문제 정의 단위
2. REQ: 승인 가능한 요구사항 단위
3. Batch: 함께 계획, 설계, 구현, 검증하는 delivery 단위
4. Release: 함께 배포하고 운영 환류를 기록하는 단위

## Bootstrap Baseline Path

- 프로젝트 시작 시 baseline 문서와 active index는 `bootstrap-baseline`으로 먼저 초기화한다.
- `bootstrap-baseline`은 3-phase lifecycle 밖의 bootstrap 단계이며 별도 governance unit가 아니다.
- 첫 real Discovery는 baseline 초기화 이후 실제 제품/서비스 변경 주제로 시작한다.

## Default Flow

1. Bootstrap: `bootstrap-baseline` (baseline 문서와 active index가 비어 있을 때만)
2. Requirements: `new-discovery` -> `confirm-discovery` -> `draft-req` -> `confirm-req` -> `suggest-batch-reqs` -> `draft-batch`
3. Delivery: `draft-batch-planning` -> `draft-batch-design` -> `run-batch-implementation` -> `draft-batch-verification` -> `confirm-batch-verification` -> `confirm-req-implemented`
4. Release & Feedback: `draft-release` -> `confirm-release` -> `capture-release-feedback`

## Lightweight Change Path

- 단일 저위험 `Approved` REQ이고 구조/인터페이스/런타임 흐름 영향이 없으면 `minor-change` fast path를 사용할 수 있다.
- 이 경로는 `suggest-batch-reqs`를 생략하고 `draft-batch`로 바로 `batch-lite` batch를 만든다.
- `batch-lite`는 planning부터 시작하고, design은 필요할 때만 생성한다.
- release는 `docs-only`, `tooling`, `app-service` profile 중 하나를 선택해 검증 강도를 조정한다.

## Change Management Extension

- 기존 REQ 본문을 수정할 때는 `change-req`를 사용해 Change Log 근거를 먼저 남긴다.
- release feedback의 후속 입력은 `Discovery Input`, `REQ Input`, `Change Request Input`으로 분리해 기록한다.
- 다음 반복 후보만 먼저 정리하려면 `suggest-next-discovery`를 사용하고, 실제 문서 생성은 `new-discovery`로 이어 간다.

## Stage Mapping

- Discovery는 Requirements phase 안에서 유지한다.
- Planning, Design, Implementation, Verification은 Batch 내부 stage로 다룬다.
- Release와 Operations는 Release & Feedback phase 내부에서 다룬다.
- `review-*` stage command는 더 이상 active flow에 포함하지 않고 대응 `confirm-*` 절차에 흡수한다.
- prompt 기반 stage command는 active 경로에서 사용하지 않고 `.stage-pilot/skills/` 아래 skill entrypoint만 사용한다.

## Global Rules

1. 증거 기반: 요구사항, 가정, 리스크, 결정 근거를 문서에 남긴다.
2. 게이트 준수: 각 phase와 단위의 DoR(진입 조건)과 DoD(완료 조건)을 충족해야 다음 단계로 이동한다.
3. 테스트 우선 검증: 구현 결과는 테스트 또는 검증 근거 없이 완료로 간주하지 않는다.
4. 변경 영향도 기록: 코드/문서/운영 영향 범위를 명시한다.
5. 불확실성 우선 해소: 차단 이슈가 있으면 임의 추정 대신 질문 또는 추가 탐색을 수행한다.
6. 운영 환류: 운영 단계 인사이트는 다음 Discovery의 입력으로 반드시 반영한다.

## Hotfix Exception Path

긴급 핫픽스는 Requirements/Delivery 설계를 경량화할 수 있다. 단, 아래는 필수다.

1. 최소 요구사항 정의(증상, 영향, 성공 조건)
2. 최소 검증(재현 테스트 또는 핵심 시나리오 확인)
3. 배포 전 롤백 계획
4. 배포 후 Postmortem 작성 및 다음 스프린트 환류
<!-- STAGEPILOT:END -->
