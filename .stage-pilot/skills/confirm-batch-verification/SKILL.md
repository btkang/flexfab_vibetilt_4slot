---
name: confirm-batch-verification
description: "Use when: approving a batch verification result, running /confirm-batch-verification with a BAT ID, promoting docs/batches/<BAT_ID> to release-candidate, or updating docs/batches/index.md after verification passes."
version: 0.8.0
author: Justin Ko
license: private
argument-hint: "예: bat-001 또는 docs/batches/bat-001_20260424_scaffold"
user-invocable: true
metadata:
  hermes:
    tags: [stage-pilot, batch, verification, approval, sdlc]
    related_skills: [draft-batch-verification, confirm-req-implemented, draft-release]
---

# Purpose

This skill checks whether a batch verification document has enough evidence to release the batch and, if so, promotes the batch to `release-candidate`.

# Inputs

- `BAT-ID` 또는 batch 경로
- `docs/batches/<BAT_ID>/verification.md`
- 포함된 REQ 문서들
- `docs/batches/index.md`

# Core Rules

- verification에 미해결 blocker가 있으면 승인하지 않는다.
- verification에 미해결 blocker가 있으면 승인하지 않는다.
- 단, verification 문서에 명시적 `Human Approval Memo`가 있고 승인자, 승인 시각, 스킵/수용 범위, 잔여 리스크, 승인 근거가 모두 적혀 있으면 그 메모가 명시적으로 수용한 blocker는 residual risk로 보고 승인할 수 있다. 이 경우 승인 결과에는 사람이 수용한 예외 범위를 함께 요약한다.
- 포함된 REQ의 acceptance criteria가 evidence와 연결돼야 한다.
- `batch-lite`는 design 문서 없이도 승인할 수 있지만, planning의 `Design Gate`가 design 불필요를 명시하고 verification이 구조 영향 없음 또는 baseline 영향 없음을 확인해야 한다.
- 승인 성공 시 batch status는 `release-candidate`가 된다.

# Execution Procedure

1. batch와 verification 문서를 읽는다.
2. 필요하면 planning과 design을 함께 읽어 profile과 구조 영향 여부를 확인한다.
3. Acceptance Mapping, Evidence, Blocking Issues를 점검한다.
4. 승인 가능하면 verification 상태와 batch index 상태를 갱신한다.
5. 승인 불가면 상태는 유지하고 blocker를 보고한다.
6. verification에 `Human Approval Memo`가 있으면, 메모가 수용한 blocker와 여전히 승인 불가한 blocker를 분리해서 판단한다. 사람이 수용한 항목만 남아 있다면 verification을 `approved`로 올리고 batch를 `release-candidate`로 승격할 수 있다.

# Validation

- 승인 성공인 경우 batch index 상태가 `release-candidate`인지 확인한다.
- 승인 보류인 경우 blocker가 명시됐는지 확인한다.
- `batch-lite` 승인 성공인 경우 design 부재가 검증 근거와 모순되지 않는지 확인한다.