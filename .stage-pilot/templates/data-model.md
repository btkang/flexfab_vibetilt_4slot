# {{PROJECT_NAME}} Data Model

- Baseline Mode: {{BASELINE_MODE:declared|observed|mixed}}
- Baseline Seed: {{BASELINE_SEED_PATH:.stage-pilot/bootstrap/baseline.yaml}}
- Status: draft | confirmed
- Owner: {{OWNER_OR_TEAM}}
- Last Updated (KST): {{UPDATED_AT_KST}}
- Source Discovery / Batch: {{SOURCE_REF}}

## Project Summary
- {{PROJECT_SUMMARY:one-sentence summary of the project}}

## Primary Domain
- {{PRIMARY_DOMAIN:short domain phrase or undecided}}

## Primary Persistence Backend
- {{PERSISTENCE_BACKEND:primary persistence backend or storage system}}

## Purpose
- This document captures the approved cross-cutting data model baseline for the project.
- It is a shared reference for REQ drafting, batch design, implementation, verification, migration planning, and handoff.

## Scope
- Covers core entities, relationships, states, persistence mappings, and shared consistency rules.
- Does not duplicate full functional acceptance criteria already owned by REQ documents.
- Does not replace ORM definitions, migration files, or generated schema artifacts.

## Model Summary

{{MODEL_SUMMARY_BULLETS:- - <entity name> :: <role in the system>}}

## Entity: `{{ENTITY_NAME_1:primary entity}}`

- Purpose: {{ENTITY_PURPOSE_1:what this entity represents}}
- Source of Truth: {{ENTITY_SOURCE_1:table, collection, file, or service}}
- Lifecycle: {{ENTITY_LIFECYCLE_1:created -> updated -> archived}}

### Key Fields

{{ENTITY_FIELDS_1:- <field name> :: <type or shape> :: <meaning>}}

### Relationships

{{ENTITY_RELATIONSHIPS_1:- <related entity> :: <relationship meaning>}}

### State Rules

{{ENTITY_STATE_RULES_1:- <state or invariant rule>}}

## Entity: `{{ENTITY_NAME_2:secondary entity}}`

- Purpose: {{ENTITY_PURPOSE_2:what this entity represents}}
- Source of Truth: {{ENTITY_SOURCE_2:table, collection, file, or service}}
- Lifecycle: {{ENTITY_LIFECYCLE_2:created -> updated -> archived}}

### Key Fields

{{ENTITY_FIELDS_2:- <field name> :: <type or shape> :: <meaning>}}

### Relationships

{{ENTITY_RELATIONSHIPS_2:- <related entity> :: <relationship meaning>}}

### State Rules

{{ENTITY_STATE_RULES_2:- <state or invariant rule>}}

## Shared Consistency Rules

{{CONSISTENCY_RULE_BULLETS:- - <uniqueness, referential, or retention rule>}}

## Persistence / Integration Notes

{{PERSISTENCE_NOTES_BULLETS:- - <mapping, indexing, migration, or synchronization note>}}

## Current Gaps / Planned Changes

{{MODEL_CURRENT_GAPS_BULLETS:- - <known model gap or planned schema change>}}

## Update Triggers

- Update this document when a core entity, key field, or relationship changes.
- Update this document when lifecycle or persistence behavior changes.
- Update this document when a batch introduces a migration, backfill, or breaking data assumption.

## Change Log

{{CHANGE_LOG_BULLETS:- - <change log entry>}}