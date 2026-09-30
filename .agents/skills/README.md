# StagePilot Skills

Personal StagePilot skill pack for Hermes external skills.

This directory is configured as an external skill root and contains the reusable skills, shared conventions, and cross-skill metadata for the StagePilot SDLC workflow.

## Purpose

StagePilot organizes work into a document-driven flow around:

- baseline bootstrap
- discovery
- requirements
- batch planning / design / implementation / verification
- release
- release feedback and next-discovery planning

These skills are intended to be reusable across repositories and machines, with versioned frontmatter so changes can be tracked over time.

## Installation / usage on another machine

1. Copy or clone this directory to the target machine.
2. Point Hermes `config.yaml` external skills setting at `~/.stage-pilot/skills`.
3. Reload skills:
   - `/reload-skills` in-session, or
   - restart Hermes / start a new session.

## Skill inventory

### Stable core skills

- `bootstrap-baseline` — `1.0.1`
- `new-discovery` — `1.0.1`
- `draft-req` — `1.0.3`

These are treated as the most reusable, foundational StagePilot workflow skills.

### Near-stable skills

- `confirm-discovery` — `0.9.1`
- `confirm-req` — `0.9.1`
- `draft-batch` — `0.9.1`

These are close to stable but may still get light structural refinement.

### Maturing workflow skills

- `change-req` — `0.8.0`
- `confirm-batch-verification` — `0.8.0`
- `confirm-release` — `0.8.0`
- `draft-batch-design` — `0.8.0`
- `draft-batch-planning` — `0.8.0`
- `draft-batch-verification` — `0.8.0`
- `draft-release` — `0.8.1`
- `review-discovery` — `0.8.0`
- `suggest-batch-reqs` — `0.8.2`

These are structured and usable now, but are expected to evolve as StagePilot usage expands.

### Active evolution / orchestration skills

- `confirm-req-implemented` — `0.7.0`
- `run-batch-delivery` — `0.7.0`
- `run-batch-implementation` — `0.7.0`
- `run-sdlc` — `0.7.0`

These involve orchestration or execution-heavy behavior and are expected to change more than drafting skills.

### Early-support skills

- `capture-release-feedback` — `0.6.0`
- `suggest-next-discovery` — `0.6.0`

These are useful but still relatively early in the overall workflow maturity.

## Versioning policy

Semantic versioning is used loosely to describe workflow maturity:

- `0.6.x` — useful early-support skill, likely to evolve significantly
- `0.7.x` — active execution/orchestration logic still evolving
- `0.8.x` — maturing drafting/decision workflow
- `0.9.x` — near-stable and expected to change lightly
- `1.0.x` — stable core skill suitable for broad reuse

Practical interpretation:

- patch bump (`x.y.Z`) → wording fixes, metadata tweaks, examples, minor clarifications
- minor bump (`x.Y.z`) → meaningful behavior/structure expansion without redefining the role of the skill
- major bump (`X.y.z`) → significant workflow or contract change

## Metadata conventions

Each StagePilot skill should include these frontmatter fields:

- `name`
- `description`
- `version`
- `author: Justin Ko`
- `license: private`
- `metadata.hermes.tags`
- `metadata.hermes.related_skills`

Optional but commonly retained:

- `argument-hint`
- `user-invocable`

## Recommended repository hygiene

If this directory is tracked in git, recommended companions are:

- `README.md` — this file
- `CHANGELOG.md` — top-level release notes for the skill pack
- commits for every meaningful skill change
- optional tags when you want to snapshot a known-good pack revision

## Last metadata normalization

- Date: 2026-05-22
- Author normalized to: `Justin Ko`
- Version fields reclassified by workflow maturity
- Cross-skill tags and related-skill metadata added to all StagePilot skills
