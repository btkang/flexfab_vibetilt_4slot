# Sample Host Project Data Model

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

## Primary Persistence Backend
- PostgreSQL

## Purpose
- This document captures the initial data model baseline after StagePilot bootstrap.
- It provides a shared reference for later REQ, delivery, migration, and verification work.

## Scope
- Covers core entities, relationships, lifecycle rules, and persistence assumptions for the sample host project.
- Does not replace ORM code, migrations, or detailed acceptance criteria.

## Model Summary

- Market Snapshot :: normalized raw and curated market data for a tracked symbol
- Analytics Snapshot :: derived metrics computed from a Market Snapshot

## Entity: `Market Snapshot`

- Purpose: represent the latest curated market data for a tracked symbol
- Source of Truth: PostgreSQL `market_snapshots` table
- Lifecycle: collected -> normalized -> published -> superseded

### Key Fields

- `symbol` :: string :: tracked market identifier
- `captured_at` :: timestamp :: time the upstream data snapshot was collected
- `payload` :: json :: normalized market data stored for API responses and analytics inputs

### Relationships

- `Analytics Snapshot` :: derived from one published Market Snapshot

### State Rules

- only one published snapshot per symbol should be treated as current
- superseded snapshots remain queryable for audit or replay use cases

## Entity: `Analytics Snapshot`

- Purpose: represent derived metrics calculated from a market snapshot
- Source of Truth: PostgreSQL `analytics_snapshots` table
- Lifecycle: queued -> computed -> published -> superseded

### Key Fields

- `symbol` :: string :: tracked market identifier
- `derived_at` :: timestamp :: time analytics were computed
- `metrics` :: json :: derived indicators and summary values

### Relationships

- `Market Snapshot` :: source snapshot used to compute analytics

### State Rules

- published analytics must reference an existing published or superseded market snapshot
- analytics should be recomputed whenever a new published market snapshot replaces the current one

## Shared Consistency Rules

- `symbol` identifiers must remain consistent across snapshot and analytics entities
- analytics records must not exist without a corresponding source market snapshot

## Persistence / Integration Notes

- refresh jobs write Market Snapshot rows before recomputing Analytics Snapshot rows
- future retention and archival policies are not defined yet

## Current Gaps / Planned Changes

- exact table schema and indexing strategy are still undecided
- historical retention policy is still undecided

## Update Triggers

- Update this document when a core entity, key field, or relationship changes.
- Update this document when lifecycle or persistence behavior changes.
- Update this document when a batch introduces a migration, backfill, or breaking data assumption.

## Change Log

- 2026-04-30: bootstrap-baseline created the initial data model baseline from declared project inputs.