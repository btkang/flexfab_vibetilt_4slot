# {{PROJECT_NAME}} Project Structure

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

## Tech Stack
{{TECH_STACK_BULLETS:- <planned stack item 1>
- <planned stack item 2>}}

## Primary Runtime
- Type: {{PRIMARY_RUNTIME:cli|api-service|web-app|worker|library|mixed|other}}
- Planned Entry Points:
{{PRIMARY_ENTRYPOINT_BULLETS:- <entrypoint> :: <purpose>}}

## Purpose
- This document is the current approved repository and package structure baseline.
- It is a cross-cutting reference document for Discovery, REQ, Batch Design, Batch Verification, and implementation updates.

## Scope
- Covers the top-level repository layout and package/module boundaries.
- Does not duplicate detailed business rules, acceptance criteria, or implementation-only function signatures.

## Top-Level Structure

```text
{{TOP_LEVEL_STRUCTURE_TREE:<text tree baseline>}}
```

## Package / Module Responsibilities

{{MODULE_RESPONSIBILITY_BULLETS:- `<path-or-package-1>`
  - <responsibility summary>}}

## Dependency Rules

{{DEPENDENCY_RULE_BULLETS:- <dependency rule 1>}}

## Shared Boundaries

{{SHARED_BOUNDARY_BULLETS:- <shared boundary 1>}}

## Current Gaps / Planned Changes

{{CURRENT_GAPS_BULLETS:- <gap or planned structural change 1>}}

## Update Triggers

- Update this document when package boundaries change.
- Update this document when new top-level runtime or domain modules are added.
- Update this document when a batch changes the approved structure baseline.

## Change Log

{{CHANGE_LOG_BULLETS:- - <change log entry>}}
