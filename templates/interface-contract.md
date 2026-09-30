# {{PROJECT_NAME}} Interface Contract

- Baseline Mode: {{BASELINE_MODE:declared|observed|mixed}}
- Baseline Seed: {{BASELINE_SEED_PATH:.stage-pilot/bootstrap/baseline.yaml}}
- Status: draft | confirmed
- Owner: {{OWNER_OR_TEAM}}
- Last Updated (KST): {{UPDATED_AT_KST}}
- Source Discovery / Batch: {{SOURCE_REF}}
- Primary Runtime: {{PRIMARY_RUNTIME:cli|api-service|web-app|worker|library|mixed|other}}

## Project Summary
- {{PROJECT_SUMMARY:one-sentence summary of the project}}

## Primary Domain
- {{PRIMARY_DOMAIN:short domain phrase or undecided}}

## Purpose
- This document captures the approved external and cross-boundary interface contracts for the project.
- It is a cross-cutting reference for REQ drafting, batch design, implementation, verification, and handoff.

## Scope
- Covers externally visible APIs, CLI surfaces, events, files, or batch I/O contracts.
- Does not duplicate full business requirements already owned by REQ documents.
- Does not replace low-level code references or generated API artifacts.

## Interface Summary

{{INTERFACE_SUMMARY_BULLETS:- - <interface name> :: <purpose>}}

## Interface: `{{INTERFACE_NAME_1:primary interface name}}`

- Type: {{INTERFACE_TYPE_1:http-api|cli|event|file|internal-service|other}}
- Consumers / Producers: {{INTERFACE_ACTORS_1:who calls or receives it}}
- Purpose: {{INTERFACE_PURPOSE_1:what this interface exists to do}}
- Stability: {{INTERFACE_STABILITY_1:experimental|internal|stable}}

### Inputs

{{INTERFACE_INPUTS_1:- <input field or argument> :: <meaning>}}

### Outputs

{{INTERFACE_OUTPUTS_1:- <output field or artifact> :: <meaning>}}

### Error Contract

{{INTERFACE_ERRORS_1:- <error code or condition> :: <behavior>}}

### Compatibility Rules

{{INTERFACE_COMPATIBILITY_1:- <versioning or compatibility rule>}}

## Interface: `{{INTERFACE_NAME_2:secondary interface name}}`

- Type: {{INTERFACE_TYPE_2:http-api|cli|event|file|internal-service|other}}
- Consumers / Producers: {{INTERFACE_ACTORS_2:who calls or receives it}}
- Purpose: {{INTERFACE_PURPOSE_2:what this interface exists to do}}
- Stability: {{INTERFACE_STABILITY_2:experimental|internal|stable}}

### Inputs

{{INTERFACE_INPUTS_2:- <input field or argument> :: <meaning>}}

### Outputs

{{INTERFACE_OUTPUTS_2:- <output field or artifact> :: <meaning>}}

### Error Contract

{{INTERFACE_ERRORS_2:- <error code or condition> :: <behavior>}}

### Compatibility Rules

{{INTERFACE_COMPATIBILITY_2:- <versioning or compatibility rule>}}

## Shared Constraints

{{INTERFACE_SHARED_CONSTRAINTS_BULLETS:- - <authentication, timeout, idempotency, or size constraint>}}

## Current Gaps / Planned Changes

{{INTERFACE_CURRENT_GAPS_BULLETS:- - <known contract gap or planned interface change>}}

## Update Triggers

- Update this document when an externally visible API, event, CLI, or file contract changes.
- Update this document when a batch changes compatibility, validation, or error behavior.
- Update this document when a new external consumer or producer is introduced.

## Change Log

{{CHANGE_LOG_BULLETS:- - <change log entry>}}