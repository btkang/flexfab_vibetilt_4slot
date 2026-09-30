# {{PROJECT_NAME}} Runtime Flows

- Baseline Mode: {{BASELINE_MODE:declared|observed|mixed}}
- Baseline Seed: {{BASELINE_SEED_PATH:.stage-pilot/bootstrap/baseline.yaml}}
- Status: draft | confirmed
- Owner: {{OWNER_OR_TEAM}}
- Last Updated (KST): {{UPDATED_AT_KST}}
- Source Discovery / Batch: {{SOURCE_REF}}
- Primary Runtime: {{PRIMARY_RUNTIME:cli|api-service|web-app|worker|library|mixed|other}}

## Project Summary
- {{PROJECT_SUMMARY:one-sentence summary of the project}}

## Purpose
- This document is the current approved execution-flow baseline for the project.
- It is a cross-cutting reference document for Discovery, REQ, Batch Design, Batch Verification, and implementation updates.

## Scope
- Covers representative command and runtime flows.
- Does not duplicate full acceptance criteria or low-level implementation details.

## Covered Entry Points

{{PRIMARY_ENTRYPOINT_BULLETS:- <entry-point-1> :: <purpose>}}

## Shared Components

{{SHARED_COMPONENT_BULLETS:- <shared component or service 1>}}

## Flow: `{{FLOW_NAME_1:bootstrap or first runtime flow}}`

{{FLOW_STEPS_1:1. <step 1>
2. <step 2>
3. <step 3>}}

## Flow: `{{FLOW_NAME_2:secondary runtime flow or operation}}`

{{FLOW_STEPS_2:1. <step 1>
2. <step 2>
3. <step 3>}}

## Flow Constraints

{{FLOW_CONSTRAINT_BULLETS:- <flow constraint 1>}}

## Current Gaps / Planned Changes

{{CURRENT_GAPS_BULLETS:- <gap or planned flow change 1>}}

## Update Triggers

- Update this document when a command entry point changes.
- Update this document when a runtime orchestration path changes.
- Update this document when a batch changes the approved flow baseline.

## Change Log

{{CHANGE_LOG_BULLETS:- - <change log entry>}}
