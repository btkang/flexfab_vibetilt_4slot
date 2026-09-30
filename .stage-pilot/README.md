
# 사용방법

## 최초 등록

```bash
# 리모트 등록
git remote add stage-pilot https://github.com/genichin/stage-pilot.git
# subtree 추가
git fetch stage-pilot main
git subtree add --prefix=.stage-pilot stage-pilot main --squash
# 설치
bash .stage-pilot/tools/install.sh .
```

설치 직후에는 Copilot Chat에서 `/bootstrap-baseline`을 먼저 실행해 `docs/project-structure.md`, `docs/runtime-flows.md`, `docs/interface-contract.md`, `docs/data-model.md`, 각 active index를 초기화한다. 이 단계는 Discovery/REQ/Batch/Release 중 어느 것도 아니며, 첫 real Discovery 전에 수행하는 bootstrap 단계다.

greenfield 저장소처럼 읽을 코드나 설정이 없으면 `bootstrap-baseline`은 사용자에게 최소 질문 세트와 필요한 선택 follow-up(`interface-contracts`, `interface-inputs`, `interface-outputs`, `interface-errors`, `core-entities`, `persistence-backend`, `compatibility-rules`)를 묻고, 답변을 `.stage-pilot/bootstrap/baseline.yaml`에 저장한 뒤 baseline 문서를 렌더링한다.

seed 파일만 먼저 만들고 싶으면 helper를 사용할 수 있다.

```bash
# vendor wrapper 사용
bash .stage-pilot/tools/stagepilot.sh bootstrap-seed .

# 설치된 script 직접 사용
python3 .stage-pilot/tools/stagepilot-bootstrap-seed.py .
```

bootstrap-baseline 실행 결과 예시는 `examples/bootstrap-baseline/`에 포함돼 있다.

## 업데이트

```bash
# 원격이 등록된 경우(stage-pilot remote 우선 사용)
bash .stage-pilot/tools/update.sh .

# 충돌 파일을 보존하고 싶을 때(덮어쓰기 비활성화)
bash .stage-pilot/tools/update.sh --no-overwrite .

# 원격 없이 URL로 직접 업데이트
bash .stage-pilot/tools/update.sh --repo-url https://github.com/genichin/stage-pilot.git .

# 설치 재적용 없이 subtree만 갱신
bash .stage-pilot/tools/update.sh --skip-install .
```

## 검증

```bash
# .stage-pilot subtree에서 doctor 실행
bash .stage-pilot/tools/stagepilot.sh doctor .

# subtree의 scripts를 직접 실행
python3 .stage-pilot/tools/stagepilot-doctor.py .

# active docs가 반드시 있어야 하는 workspace라면 엄격 모드 사용
python3 .stage-pilot/tools/stagepilot-doctor.py --strict-missing-docs .

# Markdown 리포트를 파일로 저장
python3 .stage-pilot/tools/stagepilot-doctor.py --report artifacts/stagepilot-doctor.md .
```

`stagepilot-doctor`는 아래 항목을 검사한다.

- active 문서 루트 존재 여부
- index와 본문 문서의 상태 동기화
- REQ traceability matrix (`Discovery -> REQ -> Batch -> Release`)
- feedback loop summary (`Release -> Discovery/REQ/change-req input`)
- change-req 이후 상태 되돌림 누락 (`Status Recommendation`, 재검증 필요, 구현 무효화와 현재 REQ 상태의 불일치)
- orphan REQ (`Approved`인데 batch 미포함, release-candidate batch 미포함 등)
- feedback 미연결 release (`released`인데 `feedback-captured` 미전환 등)
- 남아 있는 placeholder
- 상대 markdown 링크 경로
- REQ 타입 분류 일관성
- `Next Requirement ID` 규칙
- template-skill contract 불일치
- bootstrap 완료 저장소에서 선택 cross-cutting baseline 문서(`docs/interface-contract.md`, `docs/data-model.md`) 누락

active 문서가 아직 없는 패키지 저장소나 초기 host 저장소에서는 package contract 검사만 수행하고, active docs 부재는 기본적으로 warning으로 보고한다.

fresh host 저장소에서 baseline 초기화가 아직 끝나지 않았으면 doctor는 `INFO [bootstrap-required]`로 `/bootstrap-baseline` 실행과 누락된 bootstrap 파일 목록을 함께 안내한다.

`--report`를 주면 findings와 traceability matrix를 별도 Markdown 리포트로 저장한다. 기본 동작은 stdout 출력만 유지한다.

## 예제

- `examples/bootstrap-baseline/`: fresh host 저장소에서 `/bootstrap-baseline`을 실행한 직후의 baseline 문서와 sample output
- `examples/p3-change-management/`: change-req와 suggest-next-discovery가 연결되는 P3 sample workspace

## Active 문서 루트와 Source of Truth

### 필수 루트

| 경로 | 역할 |
| --- | --- |
| `docs/discovery/index.md` | Discovery 전역 인덱스 |
| `docs/discovery/*.md` | Discovery 본문 |
| `docs/srs/index.md` | REQ 전역 인덱스와 상태 정의 |
| `docs/srs/<Type>/req-XXX_<slug>.md` | 개별 REQ 문서 |
| `docs/batches/index.md` | batch 전역 인덱스 |
| `docs/batches/<BAT_ID>/planning.md` | batch 계획 |
| `docs/batches/<BAT_ID>/design.md` | batch 설계 |
| `docs/batches/<BAT_ID>/implementation.md` | batch 구현 로그 |
| `docs/batches/<BAT_ID>/verification.md` | batch 검증 근거 |
| `docs/releases/index.md` | release 전역 인덱스 |
| `docs/releases/rel-XXX_*.md` | 개별 release 문서 |

### 선택 cross-cutting 문서

| 경로 | 역할 |
| --- | --- |
| `docs/project-structure.md` | 저장소 구조, 모듈 경계, 의존 규칙 baseline |
| `docs/runtime-flows.md` | 대표 entry point와 실행 흐름 baseline |
| `docs/interface-contract.md` | 외부 API, CLI, 이벤트, 파일 I/O 계약 baseline |
| `docs/data-model.md` | 핵심 엔티티, 관계, 상태, persistence 규칙 baseline |

이 문서들은 active unit 자체는 아니지만 여러 REQ와 batch에서 반복 참조하는 공통 기술 기준선이다.

### 4.2 명령, 템플릿, 가이드 경로

| 경로 | 역할 |
| --- | --- |
| `.stage-pilot/skills/bootstrap-baseline/SKILL.md` | 프로젝트 시작 시 baseline 문서와 active index를 초기화하는 bootstrap entrypoint |
| `.stage-pilot/skills/` | active skill entrypoint 집합 |
| `.stage-pilot/skills/run-sdlc/SKILL.md` | 현재 상태를 읽고 다음 skill을 안내하는 orchestrator |
| `.stage-pilot/templates/bootstrap/baseline-seed.yaml` | bootstrap 질문 결과를 저장하는 seed 템플릿 |
| `.stage-pilot/templates/discovery/discovery.md` | Discovery 생성 템플릿 |
| `.stage-pilot/templates/project-structure.md` | baseline 구조 문서 템플릿 |
| `.stage-pilot/templates/runtime-flows.md` | baseline 실행 흐름 문서 템플릿 |
| `.stage-pilot/templates/interface-contract.md` | 선택형 인터페이스 계약 문서 템플릿 |
| `.stage-pilot/templates/data-model.md` | 선택형 데이터 모델 문서 템플릿 |
| `.stage-pilot/instructions/placeholder-guide.md` | placeholder 치환 규칙과 active 경로 가이드 |

### 4.3 비활성 경로 정책

- prompt 기반 stage command는 active flow에 포함하지 않는다.
- legacy stage template는 active source of truth가 아니다.
- active 작업은 `docs/discovery/`, `docs/srs/`, `docs/batches/`, `docs/releases/` 기준으로만 수행한다.

## 5. 단위별 상태 모델

### 5.1 Discovery 상태

| 상태 | 의미 |
| --- | --- |
| `draft` | 초안 상태이며 REQ drafting 전 |
| `confirmed` | REQ drafting으로 넘길 수 있도록 확인 완료 |

Discovery는 구현 완료 상태를 표현하는 단위가 아니다. Discovery의 역할은 문제 정의와 handoff 준비다.

### 5.2 REQ 상태

| 상태 | 의미 |
| --- | --- |
| `Proposed` | 초안 상태이며 구현 기준으로 확정되지 않음 |
| `Approved` | 구현 기준으로 승인됨 |
| `Implemented` | 구현과 검증이 완료됨 |
| `Deprecated` | 더 이상 유지하지 않음 |

### 5.3 Batch 상태

| 상태 | 의미 |
| --- | --- |
| `draft` | batch 초안만 생성된 상태 |
| `in-delivery` | planning, design, implementation, verification 중 일부가 진행 중 |
| `release-candidate` | verification 확인이 끝나 release 입력으로 사용 가능 |
| `released` | 하나 이상의 release에 포함됨 |
| `archived` | 더 이상 active delivery 단위로 사용하지 않음 |

### 5.4 Release 상태

| 상태 | 의미 |
| --- | --- |
| `draft` | release 초안 상태 |
| `confirmed` | rollout, rollback, verification 준비가 승인됨 |
| `released` | 배포 실행이 완료됨 |
| `feedback-captured` | 운영 환류까지 기록 완료 |

## 6. 기본 운영 흐름

### 6.0 프로젝트 시작 시 baseline 초기화

새 저장소에 StagePilot을 처음 적용할 때는 Discovery를 만들기 전에 `bootstrap-baseline`을 먼저 실행한다.

1. `bootstrap-baseline`
	필요하면 최소 질문 세트(`project-summary`, `primary-domain`, `tech-stack`, `primary-runtime`, `primary-entrypoints`)와 선택 follow-up(`interface-contracts`, `core-entities`)를 묻고, 답을 `.stage-pilot/bootstrap/baseline.yaml`에 저장한 뒤 `docs/project-structure.md`, `docs/runtime-flows.md`, `docs/interface-contract.md`, `docs/data-model.md`, `docs/discovery/index.md`, `docs/srs/index.md`, `docs/batches/index.md`, `docs/releases/index.md`를 렌더링한다.
	이 단계는 bootstrap 단계이며, Discovery/REQ/Batch/Release 단위로 계산하지 않는다.
	샘플 결과는 `examples/bootstrap-baseline/bootstrap-baseline-output.md`를 참고한다.
2. `new-discovery`
	baseline 초기화가 끝난 뒤 첫 real Discovery를 실제 제품/서비스 변경 주제로 생성한다.

이미 코드와 실행 경로가 있는 기존 저장소에 StagePilot을 처음 적용할 때도 시작점은 동일하게 `bootstrap-baseline`이다. 차이는 bootstrap이 질문만으로 baseline을 만들지 않고, 현재 저장소 구조와 실행 흔적을 먼저 읽어 `observed` 또는 `mixed` baseline을 만든다는 점이다.

1. 설치 직후 `python3 .stage-pilot/tools/stagepilot-doctor.py .` 또는 `bash .stage-pilot/tools/stagepilot.sh doctor .`로 누락된 bootstrap 파일과 active docs 상태를 확인한다.
2. active SDLC 문서가 아직 없다면 `/bootstrap-baseline`을 실행해 기존 저장소 기준의 `docs/project-structure.md`, `docs/runtime-flows.md`, `docs/interface-contract.md`, `docs/data-model.md`, active index를 만든다.
3. 저장소만으로 프로젝트 정체성이나 계획 runtime을 충분히 설명할 수 없으면 `python3 .stage-pilot/tools/stagepilot-bootstrap-seed.py .` 또는 `bash .stage-pilot/tools/stagepilot.sh bootstrap-seed .`로 `.stage-pilot/bootstrap/baseline.yaml`을 먼저 만들고, 그 다음 `/bootstrap-baseline`을 실행해 baseline을 `mixed`로 보강한다.
4. bootstrap이 끝난 뒤 첫 real Discovery는 baseline 생성 자체가 아니라 현재 진행하려는 기능 변경, 운영 이슈, 기술 결정 중 하나를 주제로 `new-discovery`에서 시작한다.
5. 이미 `docs/discovery/`, `docs/srs/`, `docs/batches/`, `docs/releases/` 아래 active unit가 운영 중인 저장소라면 bootstrap을 광범위하게 다시 수행하지 말고 `run-sdlc <ID>` 또는 해당 active unit의 skill로 이어서 처리한다. 이 경우 `bootstrap-baseline`은 단순 누락 복구에만 사용한다.

### 6.1 Requirements Phase

1. `new-discovery`
	baseline 초기화 이후 사용자 요청, 운영 피드백, issue 입력을 실제 변경 주제의 Discovery 초안으로 만든다.
2. `confirm-discovery`
	Discovery를 저장소 현실과 대조하고 REQ drafting 가능 상태로 `confirmed` 전환한다.
	선행 품질 점검이 필요하면 보조 helper인 `review-discovery`로 누락, OQ 후보, 중복, 불필요 서술을 먼저 정리할 수 있다. 이 helper는 상태 전환 없이 review만 수행한다.
3. `draft-req`
	confirmed Discovery에서 승인 가능한 REQ 문서를 만든다.
4. `confirm-req`
	REQ를 `Approved`로 전환한다.
5. `change-req`
	기존 REQ 수정이 필요할 때 Change Log 기반 변경 요청을 먼저 기록하고 상태 되돌림/재검증 범위를 판정한다.
6. `suggest-batch-reqs`
	Approved REQ를 어떤 batch로 묶을지 후보를 제시한다.
	`change-req` 이후에도 delivery로 이어질 REQ만 batch 후보로 본다.
7. `draft-batch`
	선택된 REQ 묶음으로 `standard` 또는 `batch-lite` profile의 batch 폴더와 기본 문서를 생성한다.

### 6.2 Delivery Phase

1. `draft-batch-planning`
	batch 범위, 제외 범위, 의존성, 마일스톤, 리스크를 정리한다.
2. `draft-batch-design`
	batch 수준의 architecture summary, changed areas, key decisions를 정리한다. `batch-lite`에서 구조 영향이 없으면 생략할 수 있다.
3. `run-batch-implementation`
	실제 코드 변경을 수행하고 implementation 문서에 changed files, execution log, validation을 기록한다.
4. `draft-batch-verification`
	REQ acceptance criteria를 evidence에 연결한다. `batch-lite`는 planning과 implementation만으로도 검증할 수 있지만, 구조 영향이 생기면 design을 추가해야 한다.
5. `confirm-batch-verification`
	evidence가 충분하면 batch를 `release-candidate`로 전환한다.
6. `confirm-req-implemented`
	`release-candidate` 또는 `released` batch의 verification evidence를 기준으로 포함 REQ를 `Implemented`로 동기화한다.

### 6.3 Release & Feedback Phase

1. `draft-release`
	하나 이상의 `release-candidate` batch로 release 문서를 생성하고 `docs-only`, `tooling`, `app-service` 중 하나의 release profile을 선택한다.
2. `confirm-release`
	rollout, rollback, verification checklist와 profile별 검증 항목을 점검하고 release를 `confirmed`로 전환한다.
3. `capture-release-feedback`
	배포 후 운영 관찰 결과와 후속 Discovery/REQ/change-req 입력을 release 문서에 남긴다.
4. `suggest-next-discovery`
	release feedback, baseline gap, 미구현/변경 대기 REQ를 읽고 다음 반복 후보를 추천한다.

### 6.4 경량 변경 경로

아래 조건을 모두 만족하면 `minor-change` fast path를 사용할 수 있다.

- `Approved` REQ가 정확히 1개다.
- 범위가 문서, 템플릿, 스크립트, bootstrap, 설정처럼 국소적이다.
- 구조, 인터페이스, runtime flow 변경이 없다.
- 독립 release가 가능하고 사람 선택이 필요한 묶음 판단이 남아 있지 않다.

이 경로에서는 `suggest-batch-reqs`를 생략하고 바로 `draft-batch`를 실행해 `Profile: batch-lite` batch를 만든다. `batch-lite`는 최초에 `index.md`와 `planning.md`만 만들고, implementation/verification 문서는 해당 단계 진입 시 추가한다. design은 planning의 `Design Gate`가 `yes`가 될 때만 생성한다.

### 6.5 Orchestrator 사용 기준

`run-sdlc`는 현재 문서 상태를 읽고 다음에 실행할 skill을 1단계씩 안내하는 entrypoint다.

- active unit가 아직 없고 baseline 문서나 active index가 비어 있으면 `bootstrap-baseline`을 먼저 제안한다.
- Discovery 입력이면 `confirm-discovery`, `draft-req`, `suggest-batch-reqs` 중 다음 단계를 제안한다.
- REQ 입력이면 `confirm-req`, `change-req`, `suggest-batch-reqs`, `draft-batch`, `confirm-req-implemented` 중 다음 단계를 제안한다. 단일 저위험 REQ면 `minor-change -> batch-lite` 경로를 우선 제안할 수 있다.
- Batch 입력이면 `draft-batch-planning`, `draft-batch-design`, `run-batch-implementation`, `draft-batch-verification`, `confirm-batch-verification`, `confirm-req-implemented`, `draft-release` 중 다음 단계를 제안한다.
- Release 입력이면 선택된 release profile과 feedback handoff 상태를 기준으로 `confirm-release`, `capture-release-feedback`, `suggest-next-discovery`, `change-req`를 제안한다.

### 6.6 변경 관리와 개선 루프

- 기존 REQ의 `Requirement`, `Acceptance Criteria`, `Impacted Area`를 바꿀 때는 먼저 `change-req`로 새 Change Log 항목을 만든다.
- 변경이 기존 구현/검증을 무효화하면 REQ 상태를 `Implemented -> Approved` 또는 필요 시 `Proposed`로 되돌린다.
- release feedback는 `Discovery Input`, `REQ Input`, `Change Request Input`으로 나눠 기록한다.
- 다음 개선 루프의 우선순위만 먼저 정하고 싶으면 `suggest-next-discovery`를 사용한다.

## 7. Phase Gate 기준

### 7.1 Requirements 완료 기준

아래 조건을 만족해야 Delivery로 넘어간다.

- Discovery가 `confirmed` 상태다.
- 구현 대상으로 채택할 REQ가 `Approved` 상태다.
- REQ의 Acceptance Criteria가 구현 판단 가능한 수준이다.
- 사람 결정 필요 항목과 후속 backlog 항목이 분리되어 있다.
- 기존 REQ 변경이 있었다면 `change-req` 기록과 상태 동기화가 끝났다.

### 7.2 Delivery 완료 기준

아래 조건을 만족해야 Release & Feedback으로 넘어간다.

- batch planning, implementation, verification 문서가 존재한다.
- `standard` batch이거나 `batch-lite`에서 구조 영향이 생긴 경우 design 문서가 존재한다.
- batch에 포함된 REQ acceptance criteria가 evidence와 연결된다.
- verification blocker가 해소되었다.
- batch 상태가 `release-candidate`다.
- 구현과 검증이 완료된 포함 REQ는 필요 시 `Implemented` 상태로 동기화되었다.

### 7.3 Release & Feedback 완료 기준

아래 조건을 만족해야 release 단위를 닫는다.

- release 계획이 `confirmed` 상태를 통과했다.
- 배포 결과가 기록되었다.
- 운영 관찰 결과가 release 문서에 남았다.
- 후속 Discovery, REQ, change-req 입력이 정리되었다.

## 8. 사람 판단이 필요한 경계

다음 항목은 AI가 임의로 확정하지 않는다.

- 승인자 지정
- Owner 지정
- 정책 선택
- 우선순위 승인
- 외부 시스템 또는 외부 데이터 확인 결과
- `suggest-batch-reqs` 결과 중 어떤 후보를 실제 batch로 채택할지에 대한 선택

특히 `suggest-batch-reqs`는 추천안을 만드는 단계이며, 최종 batch 묶음은 사람 선택이 필요하다. 따라서 orchestrator는 이 경계에서 자동 진행하지 않고 멈춘다. 단, 단일 저위험 REQ는 `minor-change` fast path로 바로 `batch-lite`를 만들 수 있다.

## 9. 자동화 규칙

### 9.1 Discovery 자동 생성 확장 규칙

현재 StagePilot 기본 패키지에는 GitHub issue label 기반 Discovery 자동 생성 workflow/script가 포함되어 있지 않다.

호스트 저장소가 별도 workflow 또는 script를 추가해 Discovery 자동 생성을 확장할 수 있으며, 그런 자동화는 아래 규칙을 따라야 한다.

- Discovery ID는 `dcy-<3자리>_<YYYYMMDD>_<topic-slug>` 형식을 사용한다.
- 동일 issue에 대해 반복 실행해도 중복 Discovery를 만들지 않는다.
- discovery index에 문서 링크와 상태를 등록한다.
- 결과는 issue comment로 다시 보고한다.

자동화를 추가하지 않는 경우에도 Discovery 생성과 index 갱신의 source of truth는 여전히 `new-discovery` skill과 `docs/discovery/` 문서다.

### 9.2 Index 관리 규칙

- `docs/discovery/index.md`는 자동 생성 및 자동 갱신 결과를 포함할 수 있다.
- `docs/srs/index.md`, `docs/batches/index.md`, `docs/releases/index.md`는 각 confirm 또는 draft 단계에서 상태와 register를 함께 맞춘다.
- 상태 변경은 본문 문서와 index가 항상 함께 갱신되어야 한다.

## 10. 표준 작업 절차

### 10.1 신규 기능 또는 운영 이슈가 들어온 경우

1. issue 또는 요청 내용을 기준으로 `new-discovery`를 실행한다.
2. Discovery를 저장소 현실과 맞춰 보강한 뒤 `confirm-discovery`를 실행한다.
3. Discovery를 REQ로 쪼개고 `draft-req`를 수행한다.
4. 각 REQ를 `confirm-req`로 승인한다.
5. REQ가 여러 개이거나 묶음 판단이 필요하면 `suggest-batch-reqs`로 batch 후보를 받고, 사람이 묶음을 선택한다.
6. 단일 저위험 REQ면 `draft-batch`를 `minor-change -> batch-lite` 경로로 바로 실행한다.
7. 그 외 경우 선택된 묶음으로 `draft-batch`를 실행한다.
8. planning, design(필요한 경우), implementation, verification을 순서대로 수행한다.
9. `confirm-batch-verification`으로 release-candidate를 만든다.
10. `confirm-req-implemented`로 구현/검증 완료 REQ를 `Implemented`로 동기화한다.
11. `draft-release`, `confirm-release`로 배포를 준비하고 실행한다. 이때 release profile은 `docs-only`, `tooling`, `app-service` 중 하나를 선택한다.
12. `capture-release-feedback`으로 운영 환류를 남긴다.
13. 필요하면 `suggest-next-discovery`로 다음 반복 후보를 정리하거나, 기존 REQ 수정이면 `change-req`로 바로 연결한다.

### 10.2 이미 존재하는 문서를 이어서 처리하는 경우

1. 가장 가까운 식별자 하나를 선택한다.
2. `run-sdlc <ID>`로 현재 단위의 다음 skill을 확인한다.
3. 제안된 다음 skill을 실행한다.
4. 사람이 결정해야 하는 경계에 도달하면 그 결정만 먼저 확정한다.

## 11. Hotfix 예외 경로

긴급 핫픽스는 Requirements와 Delivery 설계를 경량화할 수 있다. 단, 아래 4가지는 생략하지 않는다.

1. 최소 요구사항 정의
	증상, 영향, 성공 조건을 기록한다.
2. 최소 검증
	재현 테스트 또는 핵심 시나리오 확인을 남긴다.
3. 배포 전 롤백 계획
	실패 시 복구 방법을 명시한다.
4. 배포 후 Postmortem
	운영 결과를 기록하고 다음 sprint 입력으로 환류한다.

## 12. 유지보수 규칙

이 메뉴얼을 수정해야 하는 경우는 아래와 같다.

- skill 이름이 바뀌는 경우
- active 문서 루트가 바뀌는 경우
- 상태 모델이 바뀌는 경우
- workflow 또는 자동 생성 경로가 바뀌는 경우
- phase gate 기준이 바뀌는 경우

위 항목이 바뀌면 아래 파일들도 함께 점검한다.

- `source/instruction.md` (설치 시 `.github/copilot-instructions.md`와 `CLAUDE.md`로 fan-out)
- `skills/run-sdlc/SKILL.md`
- 관련 `confirm-*`, `draft-*`, `run-*`, `capture-*` skill
- `docs/discovery/index.md`
- `docs/srs/index.md`
- `docs/batches/index.md`
- `docs/releases/index.md`

### 12.1 새 skill 추가 시 체크리스트

skill을 새로 추가할 때는 아래 두 파일을 반드시 함께 추가한다.

| 파일 | 역할 |
| --- | --- |
| `skills/<name>/SKILL.md` | skill 본문 (subtree로 `.stage-pilot/skills/`에 노출된다) |
| `source/.claude/commands/<name>.md` | Claude Code용 wrapper (내용: `Follow the instructions in \`.stage-pilot/skills/<name>/SKILL.md\` exactly.`) |

wrapper는 설치 시 `source/.claude/commands/`에서 호스트 `.claude/commands/`로 복사된다. skill을 삭제하거나 이름을 변경할 때도 두 경로를 함께 처리한다.

## 13. 빠른 점검표

### 13.1 Requirements 진입 전

- 요청 또는 운영 이슈가 명확한가
- 기존 Discovery를 갱신할지 새로 만들지 판단했는가

### 13.2 Delivery 진입 전

- 관련 REQ가 모두 `Approved`인가
- batch 범위와 제외 범위가 정리되었는가

### 13.3 Release 진입 전

- batch가 `release-candidate`인가
- rollout plan, rollback plan, verification checklist가 준비되었는가