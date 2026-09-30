# Sample Host Project Runtime Flows

- Baseline Mode: declared
- Baseline Seed: .stage-pilot/bootstrap/baseline.yaml
- Status: confirmed
- Owner: Platform Team
- Last Updated (KST): 2026-04-30 16:30
- Source Discovery / Batch: bootstrap-baseline
- Primary Runtime: api-service

## Project Summary
- Python-based API service for curated market data analysis.

## Purpose
- This document captures the initial command and runtime flow baseline after StagePilot bootstrap.
- It gives later Discovery and Batch work a stable reference before product-specific flows exist.

## Scope
- Covers bootstrap, discovery start, and validation entry points.
- Does not define detailed feature behavior.

## Covered Entry Points

- uvicorn src.main:app :: serve the HTTP API
- python -m src.jobs.refresh_market_data :: refresh curated market data snapshots
- Copilot Chat /bootstrap-baseline :: initialize declared baseline docs

## Shared Components

- .stage-pilot/bootstrap/baseline.yaml
- `.stage-pilot/skills/`
- `docs/project-structure.md`
- `docs/*/index.md`

## Flow: `bootstrap baseline initialization`

1. StagePilot assets are installed into the host repository.
2. `/bootstrap-baseline` collects the minimum declaration set and writes `.stage-pilot/bootstrap/baseline.yaml`.
3. baseline docs and active index skeletons are rendered from that seed.
4. The repository becomes ready for the first real Discovery.

## Flow: `planned host runtime`

1. Operator starts `uvicorn src.main:app` to serve the planned HTTP API.
2. Background refresh can run with `python -m src.jobs.refresh_market_data`.
3. Detailed production flows remain subject to later Discovery and delivery work.

## Flow Constraints

- Fresh host repositories should complete baseline initialization before the first real Discovery.
- Declared runtime assumptions must stay in sync with `.stage-pilot/bootstrap/baseline.yaml` until observed implementation evidence exists.
- Structure or orchestration changes must update this document or `docs/project-structure.md`.

## Current Gaps / Planned Changes

- Product-specific runtime orchestration is not defined yet.
- Deployment target is undecided.
- Delivery and release flows will be added by later Discovery and Batch work.

## Update Triggers

- Update this document when command entry points change.
- Update this document when runtime orchestration changes.
- Update this document when a batch changes the approved flow baseline.

## Change Log

- 2026-04-30: bootstrap-baseline created the initial runtime flow baseline from `.stage-pilot/bootstrap/baseline.yaml`.