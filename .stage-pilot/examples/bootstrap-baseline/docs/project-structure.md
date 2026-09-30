# Sample Host Project Structure

- Baseline Mode: declared
- Baseline Seed: .stage-pilot/bootstrap/baseline.yaml
- Status: confirmed
- Owner: Platform Team
- Last Updated (KST): 2026-04-30 16:30
- Source Discovery / Batch: bootstrap-baseline

## Project Summary
- Python-based API service for curated market data analysis.

## Primary Domain
- financial analytics

## Tech Stack
- Python
- FastAPI
- PostgreSQL

## Primary Runtime
- Type: api-service
- Planned Entry Points:
- uvicorn src.main:app :: serve the HTTP API
- python -m src.jobs.refresh_market_data :: refresh curated market data snapshots

## Purpose
- This document captures the initial repository structure baseline after StagePilot bootstrap.
- It becomes a shared reference before the first real Discovery starts.

## Scope
- Covers top-level repository layout and broad module boundaries.
- Does not define feature-specific acceptance criteria or delivery units.

## Top-Level Structure

```text
.
├── .github/
├── .stage-pilot/
├── docs/
├── src/
├── scripts/
├── tests/
└── README.md
```

## Package / Module Responsibilities

- `.github/`
	- StagePilot skills, templates, and host instructions installed into the repository.
- `.stage-pilot/`
	- bootstrap declaration seed used to render the baseline documents.
- `docs/`
	- Active SDLC docs, index files, and cross-cutting baseline references.
- `src/`
	- Product and service implementation code.
- `scripts/`
	- developer and operator helper commands.
- `tests/`
	- Automated checks and regression coverage.

## Dependency Rules

- Delivery documents under `docs/` must not be treated as implementation modules.
- `src/` can evolve independently, but structure changes should update this baseline.
- `.stage-pilot/bootstrap/baseline.yaml` should be updated when the declared project identity changes before code exists.
- Tooling automation under `.stage-pilot/` must not overwrite host source code.

## Shared Boundaries

- `.stage-pilot/` provides SDLC workflow assets.
- `.stage-pilot/` stores bootstrap declarations outside active SDLC units.
- `docs/` is the source of truth for active SDLC state.
- `src/`, `scripts/`, and `tests/` remain host-project owned implementation surfaces.

## Current Gaps / Planned Changes

- The first real Discovery has not been created yet.
- Deployment target is still undecided.
- Actual domain module boundaries will be refined once the first delivery topic is defined.

## Update Triggers

- Update this document when top-level directories change.
- Update this document when module ownership boundaries change.
- Update this document when a batch changes the approved structure baseline.

## Change Log

- 2026-04-30: bootstrap-baseline created the initial repository structure baseline from `.stage-pilot/bootstrap/baseline.yaml`.