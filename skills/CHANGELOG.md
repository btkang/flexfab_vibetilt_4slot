# Changelog

All notable changes to the StagePilot skill pack should be recorded in this file.

## 2026-05-22

### run-batch-delivery `0.7.0`

- added new orchestration skill `run-batch-delivery`
- defined a 6-step delivery chain for an existing batch: `draft-batch-planning` -> `draft-batch-design` -> `run-batch-implementation` -> `draft-batch-verification` -> `confirm-batch-verification` -> `confirm-req-implemented`
- clarified that the skill resumes from the first incomplete delivery stage and stops at approval blockers instead of forcing progress
- clarified that release drafting remains a separate next step and is recommended, not auto-executed

### draft-req `1.0.3`

- bumped `draft-req` from `1.0.2` to `1.0.3`
- normalized the template path section to the same logical `stage-pilot/templates/...` + common resolution format used across the pack
- clarified source vs target path distinction for SRS index and REQ document generation

### bootstrap-baseline `1.0.1`

- bumped `bootstrap-baseline` from `1.0.0` to `1.0.1`
- changed template references from fixed `.stage-pilot/...` paths to logical `stage-pilot/templates/...` paths
- added the common template path resolution section (`.stage-pilot` -> `.vendor/stage-pilot` -> `~/.stage-pilot`)
- clarified source vs target path distinction for bootstrap seed, index, and baseline document generation

### new-discovery `1.0.1`

- bumped `new-discovery` from `1.0.0` to `1.0.1`
- changed template references from fixed `.stage-pilot/...` paths to logical `stage-pilot/templates/...` paths
- added the common template path resolution section (`.stage-pilot` -> `.vendor/stage-pilot` -> `~/.stage-pilot`)
- clarified source vs target path distinction for discovery document, discovery index, and baseline reference templates

### draft-release `0.8.1`

- bumped `draft-release` from `0.8.0` to `0.8.1`
- changed template references from fixed `.stage-pilot/...` paths to logical `stage-pilot/templates/...` paths
- added the common template path resolution section (`.stage-pilot` -> `.vendor/stage-pilot` -> `~/.stage-pilot`)
- clarified source vs target path distinction for release document and release index generation

### draft-batch `0.9.1`

- bumped `draft-batch` from `0.9.0` to `0.9.1`
- added `When to use` and `Do not use when` guidance to separate batch creation from recommendation, approval, downstream drafting, and SDLC routing
- clarified that `draft-batch` operates only after people have selected the REQ set to include
- replaced the fixed batch template path wording with logical `stage-pilot/templates/batches/...` paths plus explicit resolution order
- clarified that `minor-change` is an input fast path while the persisted batch profile remains `batch-lite`
- added explicit source-to-target template mapping, source discovery recording rules, `Common Pitfalls`, and a `Verification Checklist`

### suggest-batch-reqs `0.8.2`

- bumped `suggest-batch-reqs` from `0.8.0` to `0.8.2`
- added `Do not use when` guidance to separate recommendation from REQ approval, direct batch drafting, minor-change fast paths, and top-level SDLC routing
- added a dedicated `사람 선택 규칙` section so batch candidate adoption stays explicitly human-decided
- strengthened Discovery input handling so linked REQs must be explicit and approved before recommendation proceeds
- added explicit candidate-level batch profile hints for `standard` vs `batch-lite`, including required reasoning
- added `Common Pitfalls` and a `Verification Checklist`

### confirm-req `0.9.1`

- bumped `confirm-req` from `0.9.0` to `0.9.1`
- added `Do not use when` guidance to separate approval from drafting, change management, implementation confirmation, and batch planning
- made approval prerequisites explicit, including blocker vs non-blocker handling for `Notes` and open questions
- clarified that approval `Change Log` entries are appended and not written for blocked REQs
- added a local `docs/srs/index.md` preservation rule so existing repository wording/format is updated in place rather than overwritten from template wording
- added `Common Pitfalls` and a `Verification Checklist`

### draft-req `1.0.2`

- bumped `draft-req` from `1.0.1` to `1.0.2`
- added `Do not use when` guidance to separate `draft-req` from discovery review, confirmation, change-req, and batch planning stages
- made the confirmed Discovery prerequisite explicit and documented the limited draft-only exception case
- added a `Common Pitfalls` section covering confirmation, NFR splitting, baseline gaps, path confusion, index updates, and source Discovery back-references
- replaced the old validation bullets with a `Verification Checklist` for operational verification after drafting

### draft-req `1.0.1`

- bumped `draft-req` from `1.0.0` to `1.0.1`
- replaced fixed template path references with logical template source paths under `stage-pilot/templates/...`
- added explicit template path resolution rules for workspace-local, vendor/subtree, and Hermes external installs
- clarified resolution priority: workspace-local -> vendor/subtree -> Hermes external
- clarified the distinction between template source paths and generated target paths under `docs/srs/`
- added validation checks for logical-path-to-physical-path resolution and source/target path confusion

### confirm-discovery `0.9.1`

- bumped `confirm-discovery` from `0.9.0` to `0.9.1`
- applied wording cleanup (`Skill` -> `skill`) for local style consistency
- clarified that `Confirmed By` is never auto-generated or inferred by AI
- clarified when to use `review-discovery` or `new-discovery` instead of `confirm-discovery`
- added baseline cross-check guidance for `docs/project-structure.md` and `docs/runtime-flows.md`
- added explicit pitfalls and a verification checklist
- clarified how to add a missing `docs/discovery/index.md` row while preserving register format

### Metadata normalization

Applied long-term management metadata across all StagePilot skills under `~/.stage-pilot/skills`.

Changed for every skill:

- added or normalized `author: Justin Ko`
- added `license: private`
- added `metadata.hermes.tags`
- added `metadata.hermes.related_skills`
- normalized frontmatter layout for long-term reuse

### Version reclassification

Assigned workflow-maturity-based semantic versions instead of a uniform initial version.

#### Stable core (`1.0.0`)

- `bootstrap-baseline`
- `new-discovery`
- `draft-req`

#### Near-stable (initial classification)

- `confirm-discovery` → later bumped to `0.9.1`
- `confirm-req` → `0.9.0`
- `draft-batch` → `0.9.0`

#### Maturing workflow (`0.8.0`)

- `change-req`
- `confirm-batch-verification`
- `confirm-release`
- `draft-batch-design`
- `draft-batch-planning`
- `draft-batch-verification`
- `draft-release`
- `review-discovery`
- `suggest-batch-reqs`

#### Active evolution / orchestration (`0.7.0`)

- `confirm-req-implemented`
- `run-batch-delivery`
- `run-batch-implementation`
- `run-sdlc`

#### Early-support (`0.6.0`)

- `capture-release-feedback`
- `suggest-next-discovery`

### Notes

Current intent of the version scale:

- `0.6.x` — useful early-support skill, likely to evolve significantly
- `0.7.x` — execution/orchestration behavior still evolving
- `0.8.x` — maturing drafting/decision workflow
- `0.9.x` — near-stable, only moderate refinement expected
- `1.0.x` — stable core workflow skill
