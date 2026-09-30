# Sample Host Project Interface Contract

- Baseline Mode: declared
- Baseline Seed: .stage-pilot/bootstrap/baseline.yaml
- Status: confirmed
- Owner: Platform Team
- Last Updated (KST): 2026-04-30 16:30
- Source Discovery / Batch: bootstrap-baseline
- Primary Runtime: api-service

## Project Summary
- Python-based API service for curated market data analysis.

## Primary Domain
- financial analytics

## Purpose
- This document captures the initial interface contract baseline after StagePilot bootstrap.
- It provides a shared reference for future REQ, design, implementation, and verification work.

## Scope
- Covers the planned API and job interfaces exposed by the sample host project.
- Does not duplicate detailed acceptance criteria or generated OpenAPI/schema artifacts.

## Interface Summary

- HTTP market data API :: serve curated market data and derived analytics to clients
- Refresh job command :: update stored market data snapshots from upstream sources

## Interface: `HTTP market data API`

- Type: http-api
- Consumers / Producers: internal dashboards and downstream service clients
- Purpose: serve curated market data and derived analytics to clients
- Stability: experimental

### Inputs

- `GET /markets/{symbol}` :: request the current curated snapshot for a symbol
- `GET /analytics/{symbol}` :: request derived analytics for a symbol

### Outputs

- JSON market snapshot payload :: current normalized market data for the requested symbol
- JSON analytics payload :: derived analytics and summary metrics for the requested symbol

### Error Contract

- `404 not_found` :: symbol is not available in the curated dataset
- `503 upstream_unavailable` :: refreshed data is temporarily unavailable

### Compatibility Rules

- response payload changes should remain backward compatible within the same API version

## Interface: `Refresh job command`

- Type: cli
- Consumers / Producers: operators and scheduled automation
- Purpose: update stored market data snapshots from upstream sources
- Stability: internal

### Inputs

- `python -m src.jobs.refresh_market_data` :: trigger a refresh of curated market data snapshots

### Outputs

- refreshed local snapshot artifacts :: normalized data persisted for later API reads

### Error Contract

- non-zero exit code :: refresh failed and data should be treated as stale

### Compatibility Rules

- command arguments and exit-code semantics should remain stable for automation callers

## Shared Constraints

- all interfaces assume symbol identifiers are validated before persistence or response rendering
- refresh failures must not silently overwrite previously known-good snapshots

## Current Gaps / Planned Changes

- authentication and authorization rules are not defined yet
- API versioning strategy is still undecided

## Update Triggers

- Update this document when an externally visible API, event, CLI, or file contract changes.
- Update this document when a batch changes compatibility, validation, or error behavior.
- Update this document when a new external consumer or producer is introduced.

## Change Log

- 2026-04-30: bootstrap-baseline created the initial interface contract baseline from declared project inputs.