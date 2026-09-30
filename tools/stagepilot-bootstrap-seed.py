#!/usr/bin/env python3

from __future__ import annotations

import argparse
from dataclasses import dataclass
from datetime import datetime, timedelta, timezone
from pathlib import Path
import re
import sys

try:
    from zoneinfo import ZoneInfo
except ImportError:  # pragma: no cover
    ZoneInfo = None


RUNTIME_CHOICES = (
    "cli",
    "api-service",
    "web-app",
    "worker",
    "library",
    "mixed",
    "other",
)

INTERFACE_TYPE_CHOICES = (
    "http-api",
    "cli",
    "event",
    "file",
    "internal-service",
    "other",
)

UPPERCASE_TOKENS = {
    "api": "API",
    "http": "HTTP",
    "cli": "CLI",
    "id": "ID",
    "io": "I/O",
    "json": "JSON",
    "sql": "SQL",
    "ui": "UI",
    "ux": "UX",
    "url": "URL",
}

STORAGE_BACKEND_HINTS = (
    (("postgresql", "postgres", "psql"), "PostgreSQL"),
    (("mysql", "mariadb"), "MySQL"),
    (("sqlite",), "SQLite"),
    (("mongodb", "mongo"), "MongoDB"),
    (("dynamodb",), "DynamoDB"),
    (("redis",), "Redis"),
    (("s3", "blob storage", "object storage"), "Object Storage"),
)


@dataclass(frozen=True)
class Entrypoint:
    name: str
    purpose: str


@dataclass(frozen=True)
class TopLevelArea:
    path: str
    responsibility: str


@dataclass(frozen=True)
class InterfaceContractHint:
    name: str
    interface_type: str
    actors: str
    purpose: str


@dataclass(frozen=True)
class EntityHint:
    name: str
    purpose: str


@dataclass(frozen=True)
class CompatibilityRuleHint:
    interface_name: str
    rule: str


@dataclass(frozen=True)
class InterfaceDetailHint:
    interface_name: str
    value: str


@dataclass(frozen=True)
class SeedAnswers:
    repository_name: str
    display_name: str
    summary: str
    primary_domain: str
    tech_stack: list[str]
    primary_runtime: str
    primary_entrypoints: list[Entrypoint]
    planned_top_level_areas: list[TopLevelArea]
    interface_contracts: list[InterfaceContractHint]
    interface_inputs: list[InterfaceDetailHint]
    interface_outputs: list[InterfaceDetailHint]
    interface_errors: list[InterfaceDetailHint]
    core_entities: list[EntityHint]
    persistence_backend: str
    compatibility_rules: list[CompatibilityRuleHint]
    known_unknowns: list[str]
    capture_mode: str


def fail(message: str) -> int:
    print(f"[bootstrap-seed] ERROR: {message}", file=sys.stderr)
    return 1


def log(message: str) -> None:
    print(f"[bootstrap-seed] {message}")


def default_display_name(repository_name: str) -> str:
    words = re.sub(r"[-_]+", " ", repository_name.strip()).split()
    if not words:
      return "Project"
    return " ".join(word.capitalize() for word in words)


def kst_timestamp() -> str:
    if ZoneInfo is not None:
        current = datetime.now(ZoneInfo("Asia/Seoul"))
    else:  # pragma: no cover
        current = datetime.now(timezone(timedelta(hours=9)))
    return current.strftime("%Y-%m-%d %H:%M")


def yaml_scalar(value: str) -> str:
    escaped = value.replace("'", "''")
    return f"'{escaped}'"


def parse_csv_list(raw_value: str) -> list[str]:
    return [item.strip() for item in raw_value.split(",") if item.strip()]


def parse_pair(raw_value: str, label: str) -> tuple[str, str]:
    if "::" not in raw_value:
        raise ValueError(f"{label} entries must use 'name :: purpose' format: {raw_value}")

    left, right = raw_value.split("::", 1)
    left = left.strip()
    right = right.strip()
    if not left or not right:
        raise ValueError(f"{label} entries must include values on both sides of '::': {raw_value}")
    return left, right


def parse_parts(raw_value: str, label: str, expected_parts: int) -> tuple[str, ...]:
    parts = tuple(part.strip() for part in raw_value.split("::"))
    if len(parts) != expected_parts or any(not part for part in parts):
        raise ValueError(
            f"{label} entries must use exactly {expected_parts} parts separated by '::': {raw_value}"
        )
    return parts


def prompt_value(question: str, *, default: str | None = None, choices: tuple[str, ...] | None = None) -> str:
    while True:
        choice_hint = f" ({', '.join(choices)})" if choices else ""
        default_hint = f" [{default}]" if default else ""
        raw_value = input(f"{question}{choice_hint}{default_hint}: ").strip()

        if not raw_value and default is not None:
            return default
        if not raw_value:
            print("Value is required.")
            continue
        if choices and raw_value not in choices:
            print(f"Choose one of: {', '.join(choices)}")
            continue
        return raw_value


def prompt_lines(question: str, *, example: str, required: bool = False, max_items: int | None = None) -> list[str]:
    print(question)
    print(f"Format: {example}")
    print("Press Enter on an empty line to finish.")

    values: list[str] = []
    while True:
        raw_value = input("> ").strip()
        if not raw_value:
            if required and not values:
                print("At least one line is required.")
                continue
            break

        values.append(raw_value)
        if max_items is not None and len(values) >= max_items:
            break

    return values


def title_case_phrase(text: str) -> str:
    tokens = [token for token in re.split(r"[\s/_-]+", text.strip()) if token]
    formatted: list[str] = []
    for token in tokens:
        lowered = token.lower()
        if lowered in UPPERCASE_TOKENS:
            formatted.append(UPPERCASE_TOKENS[lowered])
        else:
            formatted.append(token.capitalize())
    return " ".join(formatted)


def compact_label(text: str, *, max_words: int = 5) -> str:
    cleaned = re.sub(r"[^A-Za-z0-9\s/_-]+", " ", text).strip()
    label = title_case_phrase(cleaned)
    words = label.split()
    if len(words) > max_words:
        return " ".join(words[:max_words])
    return label


def infer_interface_type(primary_runtime: str, entrypoint_name: str, index: int) -> str:
    lowered = entrypoint_name.lower()
    if primary_runtime in {"api-service", "web-app"} and index == 0:
        return "http-api"
    if primary_runtime == "library":
        return "internal-service"
    if primary_runtime in {"cli", "worker"}:
        return "cli"
    if any(token in lowered for token in ("uvicorn", "gunicorn", ":app")):
        return "http-api"
    if lowered.startswith(("python ", "python3 ", "node ")) or " -m " in lowered:
        return "cli"
    return "other"


def default_interface_actors(interface_type: str) -> str:
    actor_map = {
        "http-api": "clients and downstream services",
        "cli": "operators and automation",
        "event": "publishers and subscribers",
        "file": "file producers and consumers",
        "internal-service": "internal services",
        "other": "project operators and integrators",
    }
    return actor_map[interface_type]


def default_interface_stability(interface_type: str, capture_mode: str) -> str:
    if interface_type == "cli":
        return "internal"
    if capture_mode == "declared":
        return "experimental"
    return "internal"


def derive_interface_name(entrypoint: Entrypoint, interface_type: str, index: int) -> str:
    purpose_label = compact_label(entrypoint.purpose)
    if interface_type == "http-api":
        return "Primary HTTP API" if index == 0 else f"{purpose_label} API"
    if interface_type == "cli":
        return f"{purpose_label} Command"
    if interface_type == "event":
        return f"{purpose_label} Event"
    if interface_type == "file":
        return f"{purpose_label} File Contract"
    if interface_type == "internal-service":
        return f"{purpose_label} Service"
    return f"{purpose_label} Interface"


def build_interface_hints(answers: SeedAnswers) -> list[InterfaceContractHint]:
    hints = list(answers.interface_contracts[:2])
    for index, entrypoint in enumerate(answers.primary_entrypoints):
        if len(hints) >= 2:
            break
        interface_type = infer_interface_type(answers.primary_runtime, entrypoint.name, index)
        hints.append(
            InterfaceContractHint(
                name=derive_interface_name(entrypoint, interface_type, index),
                interface_type=interface_type,
                actors=default_interface_actors(interface_type),
                purpose=entrypoint.purpose,
            )
        )

    while len(hints) < 2:
        index = len(hints)
        fallback_type = "http-api" if index == 0 and answers.primary_runtime in {"api-service", "web-app"} else "internal-service"
        hints.append(
            InterfaceContractHint(
                name="Primary HTTP API" if fallback_type == "http-api" else "Internal Service Interface",
                interface_type=fallback_type,
                actors=default_interface_actors(fallback_type),
                purpose=f"support the primary {answers.primary_domain} workflow",
            )
        )
    return hints[:2]


def default_entity_name(primary_domain: str, index: int) -> str:
    domain_label = compact_label(primary_domain, max_words=4) or "Domain"
    if index == 0:
        return f"{domain_label} Record"
    return f"{domain_label} Snapshot"


def default_entity_purpose(primary_domain: str, index: int) -> str:
    if index == 0:
        return f"represent a core business record in the {primary_domain} domain"
    return f"represent a derived or supporting artifact in the {primary_domain} domain"


def build_entity_hints(answers: SeedAnswers) -> list[EntityHint]:
    hints = list(answers.core_entities[:2])
    while len(hints) < 2:
        index = len(hints)
        hints.append(
            EntityHint(
                name=default_entity_name(answers.primary_domain, index),
                purpose=default_entity_purpose(answers.primary_domain, index),
            )
        )
    return hints[:2]


def detect_storage_backend(tech_stack: list[str]) -> str:
    lowered_stack = [item.lower() for item in tech_stack]
    for keywords, label in STORAGE_BACKEND_HINTS:
        if any(keyword in item for item in lowered_stack for keyword in keywords):
            return label
    return "application data store"


def normalize_storage_backend(raw_value: str) -> str:
    lowered_value = raw_value.strip().lower()
    if not lowered_value:
        return "application data store"
    for keywords, label in STORAGE_BACKEND_HINTS:
        if any(keyword in lowered_value for keyword in keywords):
            return label
    return raw_value.strip()


def derive_interface_inputs(contract: InterfaceContractHint) -> list[str]:
    if contract.interface_type == "http-api":
        return [f"service request handled by {contract.name} :: {contract.purpose}"]
    if contract.interface_type == "cli":
        return [f"command invocation :: {contract.purpose}"]
    if contract.interface_type == "event":
        return [f"event publication or subscription :: {contract.purpose}"]
    if contract.interface_type == "file":
        return [f"file input or output trigger :: {contract.purpose}"]
    return [f"interface invocation :: {contract.purpose}"]


def derive_interface_outputs(contract: InterfaceContractHint) -> list[str]:
    if contract.interface_type == "http-api":
        return [f"structured service response :: result of {contract.purpose}"]
    if contract.interface_type == "cli":
        return [f"console output or generated artifact :: result of {contract.purpose}"]
    if contract.interface_type == "event":
        return [f"published event payload :: result of {contract.purpose}"]
    if contract.interface_type == "file":
        return [f"generated or updated file artifact :: result of {contract.purpose}"]
    return [f"observable interface result :: outcome of {contract.purpose}"]


def derive_interface_errors(contract: InterfaceContractHint) -> list[str]:
    if contract.interface_type == "http-api":
        return ["4xx/5xx response :: the request could not be completed successfully"]
    if contract.interface_type == "cli":
        return ["non-zero exit code :: the command failed for operators or automation"]
    return ["reported interface failure :: the contract could not be completed successfully"]


def derive_interface_compatibility(contract: InterfaceContractHint) -> list[str]:
    if contract.interface_type == "cli":
        return ["arguments and exit-code semantics should remain stable for automation callers"]
    return ["payload shape and externally visible behavior should remain backward compatible for stable consumers"]


def resolve_interface_compatibility_rules(
    answers: SeedAnswers,
    contract: InterfaceContractHint,
) -> list[str]:
    shared_rules = [
        rule.rule for rule in answers.compatibility_rules if rule.interface_name.strip() == "*"
    ]
    named_rules = [
        rule.rule
        for rule in answers.compatibility_rules
        if rule.interface_name.strip().casefold() == contract.name.casefold()
    ]
    if shared_rules or named_rules:
        return shared_rules + named_rules
    return derive_interface_compatibility(contract)


def resolve_interface_detail_hints(
    detail_hints: list[InterfaceDetailHint],
    contract: InterfaceContractHint,
) -> list[str]:
    return [
        hint.value
        for hint in detail_hints
        if hint.interface_name.strip().casefold() == contract.name.casefold()
    ]


def derive_interface_shared_constraints(answers: SeedAnswers, contracts: list[InterfaceContractHint]) -> list[str]:
    constraints: list[str] = []
    if any(contract.interface_type == "http-api" for contract in contracts):
        constraints.append("request validation should happen before persistence or response rendering")
    if any(contract.interface_type == "cli" for contract in contracts):
        constraints.append("automation-facing commands should communicate failure through stable exit semantics")
    if not constraints:
        constraints.append("externally visible interfaces should document compatibility expectations before release")
    return constraints[:2]


def derive_interface_current_gaps(answers: SeedAnswers, contracts: list[InterfaceContractHint]) -> list[str]:
    if answers.known_unknowns:
        return answers.known_unknowns[:2]
    gaps: list[str] = []
    if any(contract.interface_type == "http-api" for contract in contracts):
        gaps.append("authentication and API versioning strategy are not defined yet")
    if any(contract.interface_type == "cli" for contract in contracts):
        gaps.append("automation caller expectations are not fully documented yet")
    return gaps[:2] or ["consumer-specific compatibility expectations are not defined yet"]


def derive_entity_source(storage_backend: str, index: int) -> str:
    if storage_backend == "PostgreSQL":
        return "PostgreSQL primary tables" if index == 0 else "PostgreSQL derived tables"
    if storage_backend == "MySQL":
        return "MySQL primary tables" if index == 0 else "MySQL derived tables"
    if storage_backend == "SQLite":
        return "SQLite primary tables" if index == 0 else "SQLite derived tables"
    if storage_backend == "MongoDB":
        return "MongoDB primary collections" if index == 0 else "MongoDB derived collections"
    if storage_backend == "Object Storage":
        return "object storage manifests" if index == 0 else "object storage derived artifacts"
    return "primary application records" if index == 0 else "derived application records"


def derive_entity_lifecycle(index: int) -> str:
    if index == 0:
        return "created -> updated -> archived"
    return "derived -> published -> superseded"


def derive_entity_fields(index: int, entity: EntityHint, source_entity: EntityHint | None = None) -> list[str]:
    if index == 0:
        return [
            "id :: string or uuid :: stable identifier for the entity",
            "status :: enum :: current lifecycle state for the entity",
            "created_at :: timestamp :: when the entity was first recorded",
        ]
    source_name = source_entity.name if source_entity is not None else "the primary entity"
    return [
        "id :: string or uuid :: stable identifier for the derived entity",
        f"source_id :: string or uuid :: reference to {source_name}",
        "updated_at :: timestamp :: when the derived state was last recomputed or refreshed",
    ]


def derive_entity_relationships(index: int, entity: EntityHint, other_entity: EntityHint) -> list[str]:
    if index == 0:
        return [f"{other_entity.name} :: derived from or associated with {entity.name}"]
    return [f"{other_entity.name} :: source record or upstream entity for {entity.name}"]


def derive_entity_state_rules(index: int, entity: EntityHint, other_entity: EntityHint) -> list[str]:
    if index == 0:
        return [
            "only one current active record should be treated as authoritative per business key",
            "archived records should remain available for audit or replay needs when retained",
        ]
    return [
        f"{entity.name} should not exist without a corresponding {other_entity.name}",
        f"{entity.name} should be refreshed whenever the authoritative {other_entity.name} changes",
    ]


def derive_consistency_rules(entities: list[EntityHint]) -> list[str]:
    return [
        "stable identifiers must remain consistent across related entities",
        f"derived records must not outlive their source relationship to {entities[0].name}",
    ]


def derive_persistence_notes(storage_backend: str) -> list[str]:
    return [
        f"{storage_backend} is expected to store the current cross-cutting records and derived artifacts",
        "migration, indexing, and retention strategy should be updated when schema assumptions change",
    ]


def derive_model_current_gaps(answers: SeedAnswers) -> list[str]:
    if answers.known_unknowns:
        return answers.known_unknowns[:2]
    return [
        "exact schema and indexing strategy are not defined yet",
        "retention and archival policy are not defined yet",
    ]


def collect_required_args(args: argparse.Namespace) -> tuple[str, str, list[str], str, list[str]]:
    missing: list[str] = []
    if not args.project_summary:
        missing.append("--project-summary")
    if not args.primary_domain:
        missing.append("--primary-domain")
    if not args.tech_stack:
        missing.append("--tech-stack")
    if not args.primary_runtime:
        missing.append("--primary-runtime")
    if not args.primary_entrypoint:
        missing.append("--primary-entrypoint")
    if missing:
        raise ValueError("Missing required arguments for --non-interactive: " + ", ".join(missing))

    tech_stack = parse_csv_list(args.tech_stack)
    if not tech_stack:
        raise ValueError("--tech-stack must include at least one comma-separated item")

    return (
        args.project_summary,
        args.primary_domain,
        tech_stack,
        args.primary_runtime,
        args.primary_entrypoint,
    )


def collect_answers(args: argparse.Namespace, host_root: Path) -> SeedAnswers:
    repository_name = args.repository_name or host_root.resolve().name or "project"
    display_name = args.display_name or default_display_name(repository_name)

    if args.non_interactive:
        summary, primary_domain, tech_stack, primary_runtime, primary_entrypoints = collect_required_args(args)
        top_level_area_values = args.top_level_area
        interface_contract_values = args.interface_contract
        interface_input_values = args.interface_input
        interface_output_values = args.interface_output
        interface_error_values = args.interface_error
        core_entity_values = args.core_entity
        persistence_backend = normalize_storage_backend(args.persistence_backend) if args.persistence_backend else detect_storage_backend(tech_stack)
        compatibility_rule_values = args.compatibility_rule
        known_unknowns = args.known_unknown
    else:
        print("[bootstrap-seed] Answer the minimum bootstrap questions for baseline.yaml.")
        print(f"[bootstrap-seed] Host root: {host_root}")
        display_name = prompt_value("Display name", default=display_name)
        summary = args.project_summary or prompt_value("project-summary | 이 프로젝트를 한 문장으로 설명하면 무엇인가?")
        primary_domain = args.primary_domain or prompt_value("primary-domain | 이 프로젝트의 주요 도메인은 무엇인가?")
        tech_stack = parse_csv_list(args.tech_stack) if args.tech_stack else parse_csv_list(
            prompt_value("tech-stack | 계획 중인 주 언어, 프레임워크, 핵심 인프라는 무엇인가? (comma-separated)")
        )
        primary_runtime = args.primary_runtime or prompt_value(
            "primary-runtime | 계획 중인 주 실행 형태는 무엇인가?",
            choices=RUNTIME_CHOICES,
        )
        primary_entrypoints = args.primary_entrypoint or prompt_lines(
            "primary-entrypoints | 대표 진입점 1~3개를 적어 달라.",
            example="name :: purpose",
            required=True,
            max_items=3,
        )
        top_level_area_values = args.top_level_area or prompt_lines(
            "planned-top-level-areas | 이미 알고 있는 top-level 경로가 있으면 적어 달라.",
            example="path :: responsibility",
        )
        interface_contract_values = args.interface_contract or prompt_lines(
            "interface-contracts | 대표 인터페이스 1~2개가 있으면 적어 달라.",
            example="name :: type :: actors :: purpose",
            max_items=2,
        )
        interface_input_values = args.interface_input or prompt_lines(
            "interface-inputs | 각 인터페이스의 대표 input을 적어 달라.",
            example="interface-name :: input contract or invocation note",
        )
        interface_output_values = args.interface_output or prompt_lines(
            "interface-outputs | 각 인터페이스의 대표 output을 적어 달라.",
            example="interface-name :: output contract or artifact",
        )
        interface_error_values = args.interface_error or prompt_lines(
            "interface-errors | 각 인터페이스의 대표 error를 적어 달라.",
            example="interface-name :: error code or failure behavior",
        )
        core_entity_values = args.core_entity or prompt_lines(
            "core-entities | 핵심 엔티티 1~2개가 있으면 적어 달라.",
            example="name :: purpose",
            max_items=2,
        )
        persistence_backend = normalize_storage_backend(args.persistence_backend) if args.persistence_backend else prompt_value(
            "persistence-backend | 주 persistence backend가 있으면 적어 달라.",
            default=detect_storage_backend(tech_stack),
        )
        compatibility_rule_values = args.compatibility_rule or prompt_lines(
            "compatibility-rules | 인터페이스 호환성 규칙이 있으면 적어 달라.",
            example="interface-name :: rule 또는 * :: rule",
        )
        known_unknowns = args.known_unknown or prompt_lines(
            "known-unknowns | 아직 미정인 핵심 항목이 있으면 적어 달라.",
            example="one unresolved item per line",
        )

    if primary_runtime not in RUNTIME_CHOICES:
        raise ValueError(f"primary runtime must be one of: {', '.join(RUNTIME_CHOICES)}")
    if not tech_stack:
        raise ValueError("tech-stack must include at least one item")
    if not primary_entrypoints:
        raise ValueError("at least one primary-entrypoint is required")
    if len(primary_entrypoints) > 3:
        raise ValueError("primary-entrypoint supports at most 3 values")
    if len(interface_contract_values) > 2:
        raise ValueError("interface-contract supports at most 2 values")
    if len(core_entity_values) > 2:
        raise ValueError("core-entity supports at most 2 values")

    entrypoints = [Entrypoint(*parse_pair(value, "primary-entrypoint")) for value in primary_entrypoints]
    top_level_areas = [TopLevelArea(*parse_pair(value, "top-level-area")) for value in top_level_area_values]
    interface_contracts = [
        InterfaceContractHint(*parse_parts(value, "interface-contract", 4)) for value in interface_contract_values
    ]
    for contract in interface_contracts:
        if contract.interface_type not in INTERFACE_TYPE_CHOICES:
            raise ValueError(
                "interface-contract type must be one of: " + ", ".join(INTERFACE_TYPE_CHOICES)
            )
    interface_inputs = [InterfaceDetailHint(*parse_pair(value, "interface-input")) for value in interface_input_values]
    interface_outputs = [InterfaceDetailHint(*parse_pair(value, "interface-output")) for value in interface_output_values]
    interface_errors = [InterfaceDetailHint(*parse_pair(value, "interface-error")) for value in interface_error_values]
    core_entities = [EntityHint(*parse_parts(value, "core-entity", 2)) for value in core_entity_values]
    compatibility_rules = [
        CompatibilityRuleHint(*parse_pair(value, "compatibility-rule")) for value in compatibility_rule_values
    ]

    return SeedAnswers(
        repository_name=repository_name,
        display_name=display_name,
        summary=summary,
        primary_domain=primary_domain,
        tech_stack=tech_stack,
        primary_runtime=primary_runtime,
        primary_entrypoints=entrypoints,
        planned_top_level_areas=top_level_areas,
        interface_contracts=interface_contracts,
        interface_inputs=interface_inputs,
        interface_outputs=interface_outputs,
        interface_errors=interface_errors,
        core_entities=core_entities,
        persistence_backend=persistence_backend,
        compatibility_rules=compatibility_rules,
        known_unknowns=known_unknowns,
        capture_mode=args.capture_mode or "declared",
    )


def render_seed(answers: SeedAnswers) -> str:
    updated_from = {
        "declared": "user-answer",
        "observed": "repo-observation",
        "mixed": "mixed",
    }[answers.capture_mode]
    interface_contracts = build_interface_hints(answers)
    entities = build_entity_hints(answers)
    storage_backend = answers.persistence_backend
    interface_shared_constraints = derive_interface_shared_constraints(answers, interface_contracts)
    interface_current_gaps = derive_interface_current_gaps(answers, interface_contracts)
    consistency_rules = derive_consistency_rules(entities)
    persistence_notes = derive_persistence_notes(storage_backend)
    model_current_gaps = derive_model_current_gaps(answers)

    lines = [
        "schema_version: 1",
        f"capture_mode: {answers.capture_mode}",
        "",
        "project:",
        f"  repository_name: {yaml_scalar(answers.repository_name)}",
        f"  display_name: {yaml_scalar(answers.display_name)}",
        f"  summary: {yaml_scalar(answers.summary)}",
        f"  primary_domain: {yaml_scalar(answers.primary_domain)}",
        "",
        "stack:",
        "  tech_stack:",
    ]

    for item in answers.tech_stack:
        lines.append(f"    - {yaml_scalar(item)}")

    lines.extend(
        [
            "",
            "runtime:",
            f"  primary_runtime: {answers.primary_runtime}",
            "  primary_entrypoints:",
        ]
    )
    for entrypoint in answers.primary_entrypoints:
        lines.append(f"    - name: {yaml_scalar(entrypoint.name)}")
        lines.append(f"      purpose: {yaml_scalar(entrypoint.purpose)}")

    lines.extend(["", "interfaces:", "  contracts:"])
    for contract in interface_contracts:
        resolved_inputs = resolve_interface_detail_hints(answers.interface_inputs, contract)
        resolved_outputs = resolve_interface_detail_hints(answers.interface_outputs, contract)
        resolved_errors = resolve_interface_detail_hints(answers.interface_errors, contract)
        lines.append(f"    - name: {yaml_scalar(contract.name)}")
        lines.append(f"      type: {yaml_scalar(contract.interface_type)}")
        lines.append(f"      actors: {yaml_scalar(contract.actors)}")
        lines.append(f"      purpose: {yaml_scalar(contract.purpose)}")
        lines.append(
            f"      stability: {yaml_scalar(default_interface_stability(contract.interface_type, answers.capture_mode))}"
        )
        lines.append("      inputs:")
        for item in resolved_inputs or derive_interface_inputs(contract):
            lines.append(f"        - {yaml_scalar(item)}")
        lines.append("      outputs:")
        for item in resolved_outputs or derive_interface_outputs(contract):
            lines.append(f"        - {yaml_scalar(item)}")
        lines.append("      errors:")
        for item in resolved_errors or derive_interface_errors(contract):
            lines.append(f"        - {yaml_scalar(item)}")
        lines.append("      compatibility_rules:")
        for item in resolve_interface_compatibility_rules(answers, contract):
            lines.append(f"        - {yaml_scalar(item)}")
    lines.append("  shared_constraints:")
    for item in interface_shared_constraints:
        lines.append(f"    - {yaml_scalar(item)}")
    lines.append("  current_gaps:")
    for item in interface_current_gaps:
        lines.append(f"    - {yaml_scalar(item)}")

    lines.extend(["", "structure:"])
    if answers.planned_top_level_areas:
        lines.append("  planned_top_level_areas:")
        for area in answers.planned_top_level_areas:
            lines.append(f"    - path: {yaml_scalar(area.path)}")
            lines.append(f"      responsibility: {yaml_scalar(area.responsibility)}")
    else:
        lines.append("  planned_top_level_areas: []")

    lines.extend(["", "data_model:", f"  persistence_backend: {yaml_scalar(storage_backend)}", "  entities:"])
    for index, entity in enumerate(entities):
        other_entity = entities[1 - index]
        source_entity = entities[0] if index == 1 else None
        lines.append(f"    - name: {yaml_scalar(entity.name)}")
        lines.append(f"      purpose: {yaml_scalar(entity.purpose)}")
        lines.append(f"      source_of_truth: {yaml_scalar(derive_entity_source(storage_backend, index))}")
        lines.append(f"      lifecycle: {yaml_scalar(derive_entity_lifecycle(index))}")
        lines.append("      key_fields:")
        for item in derive_entity_fields(index, entity, source_entity):
            lines.append(f"        - {yaml_scalar(item)}")
        lines.append("      relationships:")
        for item in derive_entity_relationships(index, entity, other_entity):
            lines.append(f"        - {yaml_scalar(item)}")
        lines.append("      state_rules:")
        for item in derive_entity_state_rules(index, entity, other_entity):
            lines.append(f"        - {yaml_scalar(item)}")
    lines.append("  consistency_rules:")
    for item in consistency_rules:
        lines.append(f"    - {yaml_scalar(item)}")
    lines.append("  persistence_notes:")
    for item in persistence_notes:
        lines.append(f"    - {yaml_scalar(item)}")
    lines.append("  current_gaps:")
    for item in model_current_gaps:
        lines.append(f"    - {yaml_scalar(item)}")

    lines.extend(["", "notes:"])
    if answers.known_unknowns:
        lines.append("  known_unknowns:")
        for item in answers.known_unknowns:
            lines.append(f"    - {yaml_scalar(item)}")
    else:
        lines.append("  known_unknowns: []")

    lines.extend(
        [
            "",
            "metadata:",
            f"  captured_at_kst: {yaml_scalar(kst_timestamp())}",
            "  source: stagepilot-bootstrap-seed.py",
            f"  updated_from: {updated_from}",
            "",
        ]
    )
    return "\n".join(lines)


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Create .stage-pilot/bootstrap/baseline.yaml from StagePilot bootstrap answers.",
    )
    parser.add_argument("host_root", nargs="?", default=".", help="Host repository root. Defaults to the current directory.")
    parser.add_argument("--output", help="Explicit output path. Defaults to HOST_ROOT/.stage-pilot/bootstrap/baseline.yaml.")
    parser.add_argument("--force", action="store_true", help="Overwrite an existing seed file.")
    parser.add_argument("--dry-run", action="store_true", help="Print the generated seed to stdout without writing a file.")
    parser.add_argument("--non-interactive", action="store_true", help="Require explicit CLI answers instead of interactive prompts.")
    parser.add_argument("--capture-mode", choices=("declared", "observed", "mixed"), help="Seed capture mode.")
    parser.add_argument("--repository-name", help="Repository name stored in the seed. Defaults to the host root folder name.")
    parser.add_argument("--display-name", help="Display name stored in the seed. Defaults to a title-cased repository name.")
    parser.add_argument("--project-summary", help="One-sentence project summary.")
    parser.add_argument("--primary-domain", help="Primary domain noun phrase.")
    parser.add_argument("--tech-stack", help="Comma-separated technology list.")
    parser.add_argument("--primary-runtime", choices=RUNTIME_CHOICES, help="Primary runtime type.")
    parser.add_argument(
        "--primary-entrypoint",
        action="append",
        default=[],
        help="Primary entrypoint line in 'name :: purpose' format. Repeat up to 3 times.",
    )
    parser.add_argument(
        "--top-level-area",
        action="append",
        default=[],
        help="Optional top-level area in 'path :: responsibility' format.",
    )
    parser.add_argument(
        "--interface-contract",
        action="append",
        default=[],
        help="Optional interface contract in 'name :: type :: actors :: purpose' format. Repeat up to 2 times.",
    )
    parser.add_argument(
        "--interface-input",
        action="append",
        default=[],
        help="Optional interface input in 'interface-name :: input contract or invocation note' format.",
    )
    parser.add_argument(
        "--interface-output",
        action="append",
        default=[],
        help="Optional interface output in 'interface-name :: output contract or artifact' format.",
    )
    parser.add_argument(
        "--interface-error",
        action="append",
        default=[],
        help="Optional interface error in 'interface-name :: error code or failure behavior' format.",
    )
    parser.add_argument(
        "--core-entity",
        action="append",
        default=[],
        help="Optional core entity in 'name :: purpose' format. Repeat up to 2 times.",
    )
    parser.add_argument("--persistence-backend", help="Optional primary persistence backend or storage system.")
    parser.add_argument(
        "--compatibility-rule",
        action="append",
        default=[],
        help="Optional compatibility rule in 'interface-name :: rule' format. Use '* :: rule' for a shared rule.",
    )
    parser.add_argument(
        "--known-unknown",
        action="append",
        default=[],
        help="Optional unresolved bootstrap note. Repeat for multiple items.",
    )
    return parser


def main() -> int:
    parser = build_parser()
    args = parser.parse_args()

    host_root = Path(args.host_root).resolve()
    if not host_root.exists() or not host_root.is_dir():
        return fail(f"host root not found: {host_root}")

    output_path = Path(args.output).resolve() if args.output else host_root / ".stage-pilot/bootstrap/baseline.yaml"

    try:
        answers = collect_answers(args, host_root)
        content = render_seed(answers)
    except ValueError as error:
        return fail(str(error))
    except EOFError:
        return fail("input ended before the questionnaire was completed")

    if args.dry_run:
        print(content, end="")
        return 0

    if output_path.exists() and not args.force:
        return fail(f"seed file already exists: {output_path}. Use --force to overwrite.")

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(content, encoding="utf-8")

    log(f"wrote {output_path}")
    log("Next: run /bootstrap-baseline in Copilot Chat to render baseline docs from the seed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())