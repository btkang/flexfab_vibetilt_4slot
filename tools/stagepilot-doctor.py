#!/usr/bin/env python3

from __future__ import annotations

import argparse
from datetime import datetime
import re
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable


PLACEHOLDER_PATTERN = re.compile(r"\{\{[^{}]+\}\}")
LINK_PATTERN = re.compile(r"\[[^\]]+\]\(([^)]+)\)")
REQ_ID_PATTERN = re.compile(r"req-(\d+)", re.IGNORECASE)
BATCH_ID_PATTERN = re.compile(r"bat-(\d+)", re.IGNORECASE)
DISCOVERY_ID_PATTERN = re.compile(r"dcy-(\d+)", re.IGNORECASE)
RELEASE_ID_PATTERN = re.compile(r"rel-(\d+)", re.IGNORECASE)
STATUS_PATTERN = re.compile(r"^-\s*(?:Status|상태):\s*(.+?)\s*$", re.MULTILINE)
BATCH_PROFILE_PATTERN = re.compile(r"^-\s*Profile:\s*(.+?)\s*$", re.MULTILINE)
RELEASE_PROFILE_PATTERN = re.compile(r"^-\s*Profile:\s*(.+?)\s*$", re.MULTILINE)
REQ_TYPE_LINE_PATTERN = re.compile(r"^-\s*Type:\s*(.+)$", re.MULTILINE)
INLINE_CODE_PATTERN = re.compile(r"`([^`]+)`")

BATCH_PROFILES = {"standard", "batch-lite"}
RELEASE_PROFILES = {"docs-only", "tooling", "app-service"}

FINALIZED_STATUSES = {
    "approved",
    "archived",
    "completed",
    "confirmed",
    "deprecated",
    "feedback-captured",
    "implemented",
    "in-delivery",
    "release-candidate",
    "released",
}

ACTIVE_INDEX_FILES = [
    Path("docs/discovery/index.md"),
    Path("docs/srs/index.md"),
    Path("docs/batches/index.md"),
    Path("docs/releases/index.md"),
]

BASELINE_DOC_FILES = [
    Path("docs/project-structure.md"),
    Path("docs/runtime-flows.md"),
]

OPTIONAL_CROSS_CUTTING_DOC_FILES = [
    Path("docs/interface-contract.md"),
    Path("docs/data-model.md"),
]

BOOTSTRAP_REQUIRED_FILES = ACTIVE_INDEX_FILES + BASELINE_DOC_FILES


@dataclass(frozen=True)
class Finding:
    level: str
    code: str
    path: Path
    message: str


@dataclass(frozen=True)
class TraceabilityRow:
    req_id: str
    status: str
    discovery_ids: tuple[str, ...]
    batch_ids: tuple[str, ...]
    release_ready_batch_ids: tuple[str, ...]
    release_ids: tuple[str, ...]
    flags: tuple[str, ...]


@dataclass(frozen=True)
class FeedbackHandoffRow:
    release_id: str
    status: str
    discovery_inputs: tuple[str, ...]
    req_inputs: tuple[str, ...]
    change_req_inputs: tuple[str, ...]
    flags: tuple[str, ...]


@dataclass(frozen=True)
class ChangeReqImpact:
    change_id: str
    status_recommendation: str
    reverification_needed: str
    implementation_invalidated: str


class Doctor:
    def __init__(self, workspace_root: Path, package_root: Path, strict_missing_docs: bool, report_path: Path | None = None) -> None:
        self.workspace_root = workspace_root
        self.package_root = package_root
        self.strict_missing_docs = strict_missing_docs
        self.report_path = report_path
        self.report_written = False
        self.findings: list[Finding] = []
        self.traceability_rows: list[TraceabilityRow] = []
        self.feedback_handoff_rows: list[FeedbackHandoffRow] = []

    def add(self, level: str, code: str, path: Path, message: str) -> None:
        self.findings.append(Finding(level=level, code=code, path=path, message=message))

    def run(self) -> int:
        self.check_req_type_contract()
        self.check_design_template_contract()
        self.check_markdown_links(self.package_markdown_files(), relative_to=self.package_root)

        workspace_mode = self.check_active_workspace_roots()
        if workspace_mode:
            self.check_discovery_index()
            self.check_srs_index()
            self.check_batch_index()
            self.check_release_index()
            self.build_traceability_matrix()
            self.build_feedback_handoff_summary()
            self.check_placeholders(self.workspace_markdown_files())
            self.check_markdown_links(self.workspace_markdown_files(), relative_to=self.workspace_root)

        if self.report_path is not None:
            self.write_report()
        self.print_report()
        return 1 if any(f.level == "ERROR" for f in self.findings) else 0

    def package_markdown_files(self) -> list[Path]:
        patterns = [
            Path("README.md"),
            Path("source/instruction.md"),
            Path("source/.github/instructions"),
            Path("skills"),
            Path("templates"),
        ]
        files: list[Path] = []
        for pattern in patterns:
            full_path = self.package_root / pattern
            if full_path.is_file():
                files.append(full_path)
                continue
            if full_path.is_dir():
                files.extend(sorted(full_path.rglob("*.md")))
        return files

    def workspace_markdown_files(self) -> list[Path]:
        docs_root = self.workspace_root / "docs"
        if not docs_root.exists():
            return []
        return sorted(docs_root.rglob("*.md"))

    def check_active_workspace_roots(self) -> bool:
        existing = [path for path in ACTIVE_INDEX_FILES if (self.workspace_root / path).exists()]
        missing_bootstrap_files = [path for path in BOOTSTRAP_REQUIRED_FILES if not (self.workspace_root / path).exists()]
        if not existing:
            level = "ERROR" if self.strict_missing_docs else "WARN"
            self.add(
                level,
                "missing-active-docs",
                self.workspace_root,
                "No active SDLC docs were found under docs/. Workspace-specific checks were skipped.",
            )
            self.add_bootstrap_required_hint(missing_bootstrap_files)
            return False

        for path in ACTIVE_INDEX_FILES:
            if not (self.workspace_root / path).exists():
                self.add(
                    "ERROR",
                    "missing-root-index",
                    self.workspace_root / path,
                    "Missing active index file.",
                )

        if missing_bootstrap_files:
            self.add_bootstrap_required_hint(missing_bootstrap_files)
        else:
            self.check_optional_cross_cutting_docs()
        return True

    def check_optional_cross_cutting_docs(self) -> None:
        if self.is_package_repo_self_check():
            return

        for path in OPTIONAL_CROSS_CUTTING_DOC_FILES:
            if (self.workspace_root / path).exists():
                continue
            self.add(
                "WARN",
                "missing-cross-cutting-baseline-doc",
                self.workspace_root / path,
                "Missing optional cross-cutting baseline doc. Run /bootstrap-baseline to backfill it or add the document manually if the project needs this shared contract.",
            )

    def add_bootstrap_required_hint(self, missing_files: list[Path]) -> None:
        if not missing_files or self.is_package_repo_self_check():
            return

        missing_list = ", ".join(str(path) for path in missing_files[:6])
        if len(missing_files) > 6:
            missing_list += ", ..."

        self.add(
            "INFO",
            "bootstrap-required",
            self.workspace_root,
            "Fresh host repos should run /bootstrap-baseline in Copilot Chat before the first real Discovery. "
            f"Missing bootstrap files: {missing_list}",
        )

    def is_package_repo_self_check(self) -> bool:
        if self.workspace_root.resolve() != self.package_root.resolve():
            return False

        required_paths = [
            Path("tools/install.sh"),
            Path("examples/p3-change-management"),
        ]
        return all((self.workspace_root / path).exists() for path in required_paths)

    def check_req_type_contract(self) -> None:
        skill_path = self.package_root / "skills/draft-req/SKILL.md"
        template_path = self.package_root / "templates/srs/req-template.md"
        index_path = self.package_root / "templates/srs/index.md"

        if not skill_path.exists() or not template_path.exists() or not index_path.exists():
            self.add("ERROR", "missing-req-contract-files", self.package_root, "REQ type contract files are missing.")
            return

        skill_types = self.parse_req_types_from_skill(skill_path)
        template_types = self.parse_req_types_from_template(template_path)
        index_types = self.parse_req_types_from_index(index_path)

        if not skill_types or not template_types or not index_types:
            self.add("ERROR", "unparseable-req-types", skill_path, "Could not parse REQ types from skill/template/index files.")
            return

        normalized_sets = {
            tuple(sorted(skill_types)),
            tuple(sorted(template_types)),
            tuple(sorted(index_types)),
        }
        if len(normalized_sets) != 1:
            self.add(
                "ERROR",
                "req-type-mismatch",
                skill_path,
                "REQ type taxonomy differs across draft-req skill, req-template, and srs index.",
            )

    def check_design_template_contract(self) -> None:
        skill_path = self.package_root / "skills/draft-batch-design/SKILL.md"
        template_path = self.package_root / "templates/batches/design.md"
        if not skill_path.exists() or not template_path.exists():
            self.add("ERROR", "missing-design-contract-files", self.package_root, "Batch design contract files are missing.")
            return

        required_sections = self.parse_design_required_sections(skill_path)
        template_sections = self.parse_markdown_headings(template_path)
        missing = [section for section in required_sections if section not in template_sections]
        for section in missing:
            self.add(
                "ERROR",
                "design-template-missing-section",
                template_path,
                f"Missing required section '{section}' declared by draft-batch-design.",
            )

    def check_markdown_links(self, files: Iterable[Path], relative_to: Path) -> None:
        for file_path in files:
            text = file_path.read_text(encoding="utf-8")
            for target in LINK_PATTERN.findall(text):
                target = target.strip()
                if not target or target.startswith(("http://", "https://", "mailto:")):
                    continue
                if target.startswith("#"):
                    continue
                if any(token in target for token in ("<", ">", "{{", "}}")):
                    continue
                link_target = target.split("#", 1)[0]
                resolved = (file_path.parent / link_target).resolve()
                if not resolved.exists():
                    self.add(
                        "ERROR",
                        "broken-link",
                        file_path,
                        f"Link target '{target}' does not exist.",
                    )

    def check_placeholders(self, files: Iterable[Path]) -> None:
        for file_path in files:
            text = file_path.read_text(encoding="utf-8")
            matches = sorted(set(PLACEHOLDER_PATTERN.findall(text)))
            if not matches:
                continue
            status = self.parse_status(text)
            level = "ERROR" if status in FINALIZED_STATUSES else "WARN"
            preview = ", ".join(matches[:3])
            if len(matches) > 3:
                preview += ", ..."
            self.add(level, "placeholder-found", file_path, f"Found unresolved placeholders: {preview}")

    def check_discovery_index(self) -> None:
        index_path = self.workspace_root / "docs/discovery/index.md"
        if not index_path.exists():
            return

        for row in self.parse_markdown_table(index_path, "## Discovery 목록"):
            doc_target = self.extract_link_target(row.get("문서", ""))
            doc_path = index_path.parent / doc_target if doc_target else None
            if not doc_target or not doc_path or not doc_path.exists():
                self.add("ERROR", "discovery-index-link", index_path, f"Discovery row has a missing document target: {row}")
                continue
            doc_text = doc_path.read_text(encoding="utf-8")
            doc_status = self.parse_status(doc_text)
            row_status = row.get("상태", "").strip().lower()
            if doc_status and row_status and doc_status != row_status:
                self.add("ERROR", "discovery-status-mismatch", doc_path, f"Index status '{row_status}' does not match document status '{doc_status}'.")

    def check_srs_index(self) -> None:
        index_path = self.workspace_root / "docs/srs/index.md"
        if not index_path.exists():
            return

        index_text = index_path.read_text(encoding="utf-8")
        req_files = sorted((self.workspace_root / "docs/srs").glob("*/req-*.md"))
        register = self.parse_markdown_table(index_path, "## Requirement Register")
        register_map = {self.normalize_req_id(row.get("ID", "")): row for row in register if row.get("ID")}

        max_req_number = 0
        for req_path in req_files:
            req_id = self.normalize_req_id(req_path.stem.split("_", 1)[0])
            max_req_number = max(max_req_number, self.req_id_number(req_id))
            text = req_path.read_text(encoding="utf-8")
            req_status = self.parse_status(text)
            req_type = self.parse_req_type(text)
            latest_change_req_impact = self.parse_latest_change_req_impact(text)
            expected_type = req_path.parent.name
            if req_type and expected_type and req_type != expected_type:
                self.add("ERROR", "req-folder-type-mismatch", req_path, f"REQ type '{req_type}' does not match folder '{expected_type}'.")
            self.check_req_change_rollback(req_path, req_status, latest_change_req_impact)
            if req_id not in register_map:
                self.add("ERROR", "req-missing-register-row", req_path, "REQ file is missing from Requirement Register.")
                continue
            row = register_map[req_id]
            row_status = row.get("Status", "").strip().lower()
            row_type = row.get("Type", "").strip()
            row_link = self.extract_link_target(row.get("Link", ""))
            if req_status and row_status and req_status != row_status:
                self.add("ERROR", "req-status-mismatch", req_path, f"Register status '{row_status}' does not match REQ status '{req_status}'.")
            if req_type and row_type and req_type != row_type:
                self.add("ERROR", "req-type-register-mismatch", req_path, f"Register type '{row_type}' does not match REQ type '{req_type}'.")
            if row_link:
                resolved = (index_path.parent / row_link).resolve()
                if resolved != req_path.resolve():
                    self.add("ERROR", "req-link-mismatch", req_path, f"Register link points to '{row_link}', expected '{req_path.relative_to(index_path.parent)}'.")

        for req_id, row in register_map.items():
            link_target = self.extract_link_target(row.get("Link", ""))
            if not link_target:
                self.add("ERROR", "req-register-link-missing", index_path, f"Requirement Register row for '{req_id}' is missing a link target.")
                continue
            linked_path = (index_path.parent / link_target).resolve()
            if not linked_path.exists():
                self.add("ERROR", "req-register-link-broken", index_path, f"Requirement Register link '{link_target}' does not exist.")

        next_req = self.parse_next_requirement_id(index_text)
        if next_req is not None:
            expected_next = max_req_number + 1 if max_req_number else 1
            if next_req != expected_next:
                self.add(
                    "ERROR",
                    "next-req-id-mismatch",
                    index_path,
                    f"Next Requirement ID is REQ-{next_req:03d}, expected REQ-{expected_next:03d}.",
                )

    def check_batch_index(self) -> None:
        index_path = self.workspace_root / "docs/batches/index.md"
        if not index_path.exists():
            return

        rows = self.parse_markdown_table(index_path, "## Batch Register")
        row_map = {row.get("BAT ID", "").strip().lower(): row for row in rows if row.get("BAT ID")}
        batch_dirs = sorted(path for path in (self.workspace_root / "docs/batches").glob("bat-*") if path.is_dir())
        for batch_dir in batch_dirs:
            batch_id = batch_dir.name.split("_", 1)[0].lower()
            batch_index = batch_dir / "index.md"
            if not batch_index.exists():
                self.add("ERROR", "batch-index-missing", batch_dir, "Batch folder is missing index.md.")
                continue
            batch_index_text = batch_index.read_text(encoding="utf-8")
            batch_status = self.parse_status(batch_index_text)
            batch_profile = self.parse_batch_profile(batch_index_text)
            if batch_profile and batch_profile not in BATCH_PROFILES:
                self.add(
                    "ERROR",
                    "batch-profile-invalid",
                    batch_index,
                    f"Unsupported batch profile '{batch_profile}'. Expected 'standard' or 'batch-lite'.",
                )
            for doc_name in self.required_batch_docs(batch_profile, batch_status):
                if not (batch_dir / doc_name).exists():
                    self.add("ERROR", "batch-doc-missing", batch_dir / doc_name, "Batch folder is missing a required document.")
            if batch_id not in row_map:
                self.add("ERROR", "batch-missing-register-row", batch_index, "Batch folder is missing from Batch Register.")
                continue
            row = row_map[batch_id]
            row_profile = row.get("Profile", "").strip().lower()
            row_status = row.get("Status", "").strip().lower()
            if row_profile and batch_profile and row_profile != batch_profile:
                self.add("ERROR", "batch-profile-mismatch", batch_index, f"Register profile '{row_profile}' does not match batch profile '{batch_profile}'.")
            if batch_status and row_status and batch_status != row_status:
                self.add("ERROR", "batch-status-mismatch", batch_index, f"Register status '{row_status}' does not match batch status '{batch_status}'.")

        for batch_id, row in row_map.items():
            folder_target = self.extract_link_target(row.get("Folder", "")) or row.get("Folder", "").strip()
            if not folder_target:
                self.add("ERROR", "batch-register-folder-missing", index_path, f"Batch Register row for '{batch_id}' is missing a folder path.")
                continue
            linked_path = (index_path.parent / folder_target).resolve()
            if not linked_path.exists():
                self.add("ERROR", "batch-register-folder-broken", index_path, f"Batch Register folder '{folder_target}' does not exist.")

    def check_release_index(self) -> None:
        index_path = self.workspace_root / "docs/releases/index.md"
        if not index_path.exists():
            return

        rows = self.parse_markdown_table(index_path, "## Release Register")
        row_map = {row.get("REL ID", "").strip().lower(): row for row in rows if row.get("REL ID")}
        release_files = sorted((self.workspace_root / "docs/releases").glob("rel-*.md"))
        for release_path in release_files:
            rel_id = release_path.stem.split("_", 1)[0].lower()
            if rel_id not in row_map:
                self.add("ERROR", "release-missing-register-row", release_path, "Release document is missing from Release Register.")
                continue
            row = row_map[rel_id]
            release_text = release_path.read_text(encoding="utf-8")
            release_status = self.parse_status(release_text)
            release_profile = self.parse_release_profile(release_text)
            if release_profile and release_profile not in RELEASE_PROFILES:
                self.add(
                    "ERROR",
                    "release-profile-invalid",
                    release_path,
                    f"Unsupported release profile '{release_profile}'. Expected 'docs-only', 'tooling', or 'app-service'.",
                )
            row_profile = row.get("Profile", "").strip().lower()
            row_status = row.get("Status", "").strip().lower()
            if row_profile and release_profile and row_profile != release_profile:
                self.add("ERROR", "release-profile-mismatch", release_path, f"Register profile '{row_profile}' does not match release profile '{release_profile}'.")
            if release_status and row_status and release_status != row_status:
                self.add("ERROR", "release-status-mismatch", release_path, f"Register status '{row_status}' does not match release status '{release_status}'.")

        for rel_id, row in row_map.items():
            link_target = self.extract_link_target(row.get("Link", ""))
            if not link_target:
                self.add("ERROR", "release-register-link-missing", index_path, f"Release Register row for '{rel_id}' is missing a link target.")
                continue
            linked_path = (index_path.parent / link_target).resolve()
            if not linked_path.exists():
                self.add("ERROR", "release-register-link-broken", index_path, f"Release Register link '{link_target}' does not exist.")

    def build_traceability_matrix(self) -> None:
        req_files = sorted((self.workspace_root / "docs/srs").glob("*/req-*.md"))
        if not req_files:
            return

        discovery_refs = self.parse_discovery_traceability()
        batch_refs, batch_sources, batch_statuses = self.parse_batch_traceability()
        release_refs, release_statuses, release_feedback_states = self.parse_release_traceability()

        for release_id, release_status in release_statuses.items():
            feedback_state = release_feedback_states.get(release_id, "missing")
            release_path = self.resolve_release_path(release_id)
            if release_status == "released":
                if feedback_state != "resolved":
                    self.add(
                        "WARN",
                        "release-feedback-unlinked",
                        release_path,
                        "Released release has no resolved Feedback Handoff content.",
                    )
                else:
                    self.add(
                        "WARN",
                        "release-feedback-pending",
                        release_path,
                        "Released release has Feedback Handoff content but is not yet closed with feedback-captured status.",
                    )
            elif release_status == "feedback-captured" and feedback_state != "resolved":
                self.add(
                    "ERROR",
                    "release-feedback-unresolved",
                    release_path,
                    "Release is marked feedback-captured but Feedback Handoff is still unresolved.",
                )

        rows: list[TraceabilityRow] = []
        for req_path in req_files:
            req_id = self.normalize_req_id(req_path.stem.split("_", 1)[0])
            if not req_id:
                continue

            req_text = req_path.read_text(encoding="utf-8")
            req_status = self.parse_status(req_text)
            req_batches = set(batch_refs.get(req_id, set()))
            req_discoveries = set(discovery_refs.get(req_id, set()))
            req_releases: set[str] = set()

            for batch_id in req_batches:
                req_discoveries.update(batch_sources.get(batch_id, set()))
                req_releases.update(release_refs.get(batch_id, set()))

            releasable_batches = {
                batch_id
                for batch_id in req_batches
                if batch_statuses.get(batch_id) in {"release-candidate", "released"}
            }

            flags: list[str] = []
            has_discovery = bool(req_discoveries)
            has_batch = bool(req_batches)

            if req_status == "implemented" and not has_batch:
                flags.append("implemented-without-batch")
                self.add(
                    "ERROR",
                    "implemented-req-no-batch",
                    req_path,
                    "Implemented REQ has no batch traceability.",
                )
            elif req_status in {"proposed", "approved", "implemented"} and not has_discovery and not has_batch:
                flags.append("orphan")
                self.add(
                    "WARN",
                    "orphan-req",
                    req_path,
                    "REQ has no Discovery or Batch linkage.",
                )
            elif req_status == "approved" and not has_batch:
                flags.append("approved-without-batch")
                self.add(
                    "WARN",
                    "orphan-approved-req",
                    req_path,
                    "Approved REQ is not included in any batch.",
                )
            elif req_status == "approved" and not releasable_batches:
                flags.append("approved-without-release-candidate-batch")
                self.add(
                    "WARN",
                    "approved-req-no-release-candidate-batch",
                    req_path,
                    "Approved REQ is not included in any release-candidate or released batch.",
                )

            rows.append(
                TraceabilityRow(
                    req_id=req_id,
                    status=req_status or "unknown",
                    discovery_ids=tuple(sorted(req_discoveries)),
                    batch_ids=tuple(sorted(req_batches)),
                    release_ready_batch_ids=tuple(sorted(releasable_batches)),
                    release_ids=tuple(sorted(req_releases)),
                    flags=tuple(flags),
                )
            )

        self.traceability_rows = sorted(rows, key=lambda row: self.req_id_number(row.req_id))

    def parse_discovery_traceability(self) -> dict[str, set[str]]:
        refs: dict[str, set[str]] = {}
        discovery_dir = self.workspace_root / "docs/discovery"
        if not discovery_dir.exists():
            return refs

        for discovery_path in sorted(discovery_dir.glob("dcy-*.md")):
            discovery_id = self.normalize_discovery_id(discovery_path.stem)
            if not discovery_id:
                continue
            for line in discovery_path.read_text(encoding="utf-8").splitlines():
                if "생성된 REQ 참조" not in line:
                    continue
                for req_id in self.extract_req_ids(line):
                    refs.setdefault(req_id, set()).add(discovery_id)
        return refs

    def parse_batch_traceability(self) -> tuple[dict[str, set[str]], dict[str, set[str]], dict[str, str]]:
        req_to_batches: dict[str, set[str]] = {}
        batch_to_discoveries: dict[str, set[str]] = {}
        batch_statuses: dict[str, str] = {}
        batches_dir = self.workspace_root / "docs/batches"
        if not batches_dir.exists():
            return req_to_batches, batch_to_discoveries, batch_statuses

        for batch_dir in sorted(path for path in batches_dir.glob("bat-*") if path.is_dir()):
            batch_index = batch_dir / "index.md"
            if not batch_index.exists():
                continue
            batch_id = self.normalize_batch_id(batch_dir.name)
            if not batch_id:
                continue

            batch_text = batch_index.read_text(encoding="utf-8")
            batch_statuses[batch_id] = self.parse_status(batch_text)

            for line in batch_text.splitlines():
                if "Included REQ" in line:
                    for req_id in self.extract_req_ids(line):
                        req_to_batches.setdefault(req_id, set()).add(batch_id)
                if "Source Discovery" in line:
                    for discovery_id in self.extract_discovery_ids(line):
                        batch_to_discoveries.setdefault(batch_id, set()).add(discovery_id)

        return req_to_batches, batch_to_discoveries, batch_statuses

    def parse_release_traceability(self) -> tuple[dict[str, set[str]], dict[str, str], dict[str, str]]:
        batch_to_releases: dict[str, set[str]] = {}
        release_statuses: dict[str, str] = {}
        release_feedback_states: dict[str, str] = {}
        releases_dir = self.workspace_root / "docs/releases"
        if not releases_dir.exists():
            return batch_to_releases, release_statuses, release_feedback_states

        for release_path in sorted(releases_dir.glob("rel-*.md")):
            release_id = self.normalize_release_id(release_path.stem)
            if not release_id:
                continue
            release_text = release_path.read_text(encoding="utf-8")
            release_statuses[release_id] = self.parse_status(release_text)
            release_feedback_states[release_id] = self.parse_release_feedback_state(release_text)
            for line in release_text.splitlines():
                if "Included Batch" not in line:
                    continue
                for batch_id in self.extract_batch_ids(line):
                    batch_to_releases.setdefault(batch_id, set()).add(release_id)

        return batch_to_releases, release_statuses, release_feedback_states

    def write_report(self) -> None:
        if self.report_path is None:
            return

        try:
            self.report_path.parent.mkdir(parents=True, exist_ok=True)
            self.report_path.write_text(self.render_markdown_report(), encoding="utf-8")
            self.report_written = True
        except OSError as exc:
            self.add(
                "ERROR",
                "report-write-failed",
                self.report_path,
                f"Failed to write Markdown report: {exc}",
            )

    def render_markdown_report(self) -> str:
        error_count, warn_count, info_count = self.summarize_findings()
        lines = [
            "# StagePilot Doctor Report",
            "",
            "## Metadata",
            "",
            f"- Generated At: `{datetime.now().astimezone().isoformat(timespec='seconds')}`",
            f"- Workspace Root: `{self.workspace_root}`",
            f"- Package Root: `{self.package_root}`",
            f"- Strict Missing Docs: `{str(self.strict_missing_docs).lower()}`",
        ]
        if self.report_path is not None:
            lines.append(f"- Requested Report Path: `{self.report_path}`")

        lines.extend(
            [
                "",
                "## Summary",
                "",
                f"- Errors: {error_count}",
                f"- Warnings: {warn_count}",
                f"- Info: {info_count}",
                "",
                "## Findings",
                "",
            ]
        )

        sorted_findings = self.sorted_findings()
        if not sorted_findings:
            lines.append("- None")
        else:
            for level in ("ERROR", "WARN", "INFO"):
                level_findings = [finding for finding in sorted_findings if finding.level == level]
                if not level_findings:
                    continue
                lines.append(f"### {level}")
                lines.append("")
                for finding in level_findings:
                    lines.append(
                        f"- [{finding.code}] `{self.display_path(finding.path)}`: {finding.message}"
                    )
                lines.append("")

        lines.extend(["## Traceability Matrix", ""])
        if not self.traceability_rows:
            lines.append("- No traceability rows generated.")
        else:
            lines.extend(
                [
                    "| REQ ID | Status | Discovery | Batch | Release-Ready Batch | Release | Flags |",
                    "| --- | --- | --- | --- | --- | --- | --- |",
                ]
            )
            for row in self.traceability_rows:
                discovery = ", ".join(row.discovery_ids) if row.discovery_ids else "-"
                batch = ", ".join(row.batch_ids) if row.batch_ids else "-"
                release_ready_batch = ", ".join(row.release_ready_batch_ids) if row.release_ready_batch_ids else "-"
                release = ", ".join(row.release_ids) if row.release_ids else "-"
                flags = ", ".join(row.flags) if row.flags else "-"
                lines.append(
                    f"| {row.req_id} | {row.status} | {discovery} | {batch} | {release_ready_batch} | {release} | {flags} |"
                )

        lines.extend(["", "## Feedback Loop Summary", ""])
        if not self.feedback_handoff_rows:
            lines.append("- No feedback handoff rows generated.")
        else:
            lines.extend(
                [
                    "| Release | Status | Discovery Input | REQ Input | Change Request Input | Flags |",
                    "| --- | --- | --- | --- | --- | --- |",
                ]
            )
            for row in self.feedback_handoff_rows:
                discovery_inputs = ", ".join(row.discovery_inputs) if row.discovery_inputs else "-"
                req_inputs = ", ".join(row.req_inputs) if row.req_inputs else "-"
                change_req_inputs = ", ".join(row.change_req_inputs) if row.change_req_inputs else "-"
                flags = ", ".join(row.flags) if row.flags else "-"
                lines.append(
                    f"| {row.release_id} | {row.status} | {discovery_inputs} | {req_inputs} | {change_req_inputs} | {flags} |"
                )

        return "\n".join(lines) + "\n"

    def sorted_findings(self) -> list[Finding]:
        severity_order = {"ERROR": 0, "WARN": 1, "INFO": 2}
        return sorted(
            self.findings,
            key=lambda item: (severity_order.get(item.level, 99), str(item.path), item.code),
        )

    def summarize_findings(self) -> tuple[int, int, int]:
        error_count = sum(1 for finding in self.findings if finding.level == "ERROR")
        warn_count = sum(1 for finding in self.findings if finding.level == "WARN")
        info_count = sum(1 for finding in self.findings if finding.level == "INFO")
        return error_count, warn_count, info_count

    def print_report(self) -> None:
        if not self.findings:
            print("stagepilot-doctor: no issues found")
        else:
            for finding in self.sorted_findings():
                display_path = self.display_path(finding.path)
                print(f"{finding.level} [{finding.code}] {display_path}: {finding.message}")

        if self.traceability_rows:
            print()
            print("Traceability Matrix")
            print("REQ ID | Status | Discovery | Batch | Release-Ready Batch | Release | Flags")
            print("--- | --- | --- | --- | --- | --- | ---")
            for row in self.traceability_rows:
                discovery = ", ".join(row.discovery_ids) if row.discovery_ids else "-"
                batch = ", ".join(row.batch_ids) if row.batch_ids else "-"
                release_ready_batch = ", ".join(row.release_ready_batch_ids) if row.release_ready_batch_ids else "-"
                release = ", ".join(row.release_ids) if row.release_ids else "-"
                flags = ", ".join(row.flags) if row.flags else "-"
                print(f"{row.req_id} | {row.status} | {discovery} | {batch} | {release_ready_batch} | {release} | {flags}")

        if self.feedback_handoff_rows:
            print()
            print("Feedback Loop Summary")
            print("Release | Status | Discovery Input | REQ Input | Change Request Input | Flags")
            print("--- | --- | --- | --- | --- | ---")
            for row in self.feedback_handoff_rows:
                discovery_inputs = ", ".join(row.discovery_inputs) if row.discovery_inputs else "-"
                req_inputs = ", ".join(row.req_inputs) if row.req_inputs else "-"
                change_req_inputs = ", ".join(row.change_req_inputs) if row.change_req_inputs else "-"
                flags = ", ".join(row.flags) if row.flags else "-"
                print(f"{row.release_id} | {row.status} | {discovery_inputs} | {req_inputs} | {change_req_inputs} | {flags}")

        error_count, warn_count, info_count = self.summarize_findings()
        print(f"stagepilot-doctor: {error_count} error(s), {warn_count} warning(s), {info_count} info message(s)")
        if self.report_written and self.report_path is not None:
            print(f"stagepilot-doctor: wrote Markdown report to {self.report_path}")

    def display_path(self, path: Path) -> str:
        for base in (self.workspace_root, self.package_root):
            try:
                return str(path.resolve().relative_to(base.resolve())) or "."
            except ValueError:
                continue
        return str(path)

    def resolve_release_path(self, release_id: str) -> Path:
        releases_dir = self.workspace_root / "docs/releases"
        matches = sorted(releases_dir.glob(f"{release_id}_*.md"))
        if matches:
            return matches[0]
        return releases_dir / f"{release_id}.md"

    def check_req_change_rollback(self, req_path: Path, req_status: str, latest_change_req_impact: ChangeReqImpact | None) -> None:
        if latest_change_req_impact is None:
            return

        recommendation = latest_change_req_impact.status_recommendation
        implementation_invalidated = latest_change_req_impact.implementation_invalidated == "yes"
        reverification_needed = latest_change_req_impact.reverification_needed == "yes"

        if recommendation == "revert-to-approved" and req_status == "implemented":
            self.add(
                "ERROR",
                "req-change-rollback-missing",
                req_path,
                f"Latest change '{latest_change_req_impact.change_id}' recommends reverting to Approved, but REQ status is still Implemented.",
            )
        elif recommendation == "revert-to-proposed" and req_status in {"approved", "implemented"}:
            self.add(
                "ERROR",
                "req-change-rollback-missing",
                req_path,
                f"Latest change '{latest_change_req_impact.change_id}' recommends reverting to Proposed, but REQ status is still '{req_status}'.",
            )
        elif implementation_invalidated and req_status == "implemented":
            self.add(
                "ERROR",
                "req-change-rollback-missing",
                req_path,
                f"Latest change '{latest_change_req_impact.change_id}' invalidates existing implementation, but REQ status is still Implemented.",
            )
        elif reverification_needed and req_status == "implemented":
            self.add(
                "WARN",
                "req-change-reverification-pending",
                req_path,
                f"Latest change '{latest_change_req_impact.change_id}' requires reverification while REQ remains Implemented.",
            )

    def build_feedback_handoff_summary(self) -> None:
        releases_dir = self.workspace_root / "docs/releases"
        if not releases_dir.exists():
            self.feedback_handoff_rows = []
            return

        existing_req_ids = {
            self.normalize_req_id(path.stem.split("_", 1)[0])
            for path in sorted((self.workspace_root / "docs/srs").glob("*/req-*.md"))
        }
        rows: list[FeedbackHandoffRow] = []

        for release_path in sorted(releases_dir.glob("rel-*.md")):
            release_id = self.normalize_release_id(release_path.stem)
            if not release_id:
                continue

            release_text = release_path.read_text(encoding="utf-8")
            feedback_inputs = self.parse_release_feedback_inputs(release_text)
            discovery_inputs = self.normalize_feedback_values(feedback_inputs.get("discovery", []))
            req_inputs = self.normalize_feedback_values(feedback_inputs.get("req", []))
            change_req_inputs = self.normalize_feedback_values(feedback_inputs.get("change-req", []))

            flags: list[str] = []
            if not discovery_inputs and not req_inputs and not change_req_inputs:
                flags.append("no-follow-up-input")

            referenced_change_req_ids = {
                req_id
                for value in change_req_inputs
                for req_id in self.extract_req_ids(value)
            }
            missing_change_req_ids = sorted(req_id for req_id in referenced_change_req_ids if req_id not in existing_req_ids)
            if missing_change_req_ids:
                flags.append("unknown-change-req-target")
                self.add(
                    "WARN",
                    "feedback-change-req-target-missing",
                    release_path,
                    f"Change Request Input references missing REQ IDs: {', '.join(missing_change_req_ids)}",
                )

            rows.append(
                FeedbackHandoffRow(
                    release_id=release_id,
                    status=self.parse_status(release_text) or "unknown",
                    discovery_inputs=discovery_inputs,
                    req_inputs=req_inputs,
                    change_req_inputs=change_req_inputs,
                    flags=tuple(flags),
                )
            )

        self.feedback_handoff_rows = rows

    @staticmethod
    def parse_req_types_from_skill(path: Path) -> list[str]:
        lines = path.read_text(encoding="utf-8").splitlines()
        collecting = False
        types: list[str] = []
        for line in lines:
            if "아래 폴더 중 정확히 하나를 선택한다." in line:
                collecting = True
                continue
            if collecting and line.startswith("- 새 REQ 경로"):
                break
            if collecting:
                match = INLINE_CODE_PATTERN.search(line)
                if match:
                    types.append(match.group(1))
        return types

    @staticmethod
    def parse_req_types_from_template(path: Path) -> list[str]:
        text = path.read_text(encoding="utf-8")
        match = REQ_TYPE_LINE_PATTERN.search(text)
        if not match:
            return []
        return [part.strip() for part in match.group(1).split("|") if part.strip()]

    @staticmethod
    def parse_req_types_from_index(path: Path) -> list[str]:
        rows = Doctor.parse_markdown_table(path, "## Requirement 타입 분류 도표")
        types: list[str] = []
        for row in rows:
            type_name = row.get("타입", "").strip().strip("*")
            if type_name:
                types.append(type_name)
        return types

    @staticmethod
    def parse_design_required_sections(path: Path) -> list[str]:
        lines = path.read_text(encoding="utf-8").splitlines()
        collecting = False
        sections: list[str] = []
        for line in lines:
            if "design.md에는 최소한 아래 내용을 포함해야 한다." in line:
                collecting = True
                continue
            if collecting and not line.startswith(("\t-", "  -", "- ")):
                if line.strip():
                    break
            if collecting:
                entry = line.strip().lstrip("-").strip()
                if not entry:
                    continue
                cleaned = entry.strip("`")
                if "(" in cleaned:
                    cleaned = cleaned.split("(", 1)[0].strip()
                sections.append(cleaned)
        return sections

    @staticmethod
    def parse_markdown_headings(path: Path) -> set[str]:
        headings = set()
        for line in path.read_text(encoding="utf-8").splitlines():
            if line.startswith("## "):
                headings.add(line[3:].strip())
        return headings

    @staticmethod
    def parse_markdown_table(path: Path, heading: str) -> list[dict[str, str]]:
        lines = path.read_text(encoding="utf-8").splitlines()
        start_index = None
        for index, line in enumerate(lines):
            if line.strip() == heading:
                start_index = index + 1
                break
        if start_index is None:
            return []

        table_lines: list[str] = []
        for line in lines[start_index:]:
            if line.strip().startswith("|"):
                table_lines.append(line)
                continue
            if table_lines:
                break

        if len(table_lines) < 2:
            return []

        header = [cell.strip() for cell in table_lines[0].strip().strip("|").split("|")]
        rows: list[dict[str, str]] = []
        for line in table_lines[2:]:
            cells = [cell.strip() for cell in line.strip().strip("|").split("|")]
            if len(cells) != len(header):
                continue
            rows.append(dict(zip(header, cells)))
        return rows

    @staticmethod
    def extract_link_target(cell: str) -> str | None:
        match = LINK_PATTERN.search(cell)
        if match:
            return match.group(1).split("#", 1)[0]
        return None

    @staticmethod
    def parse_status(text: str) -> str:
        match = STATUS_PATTERN.search(text)
        if not match:
            return ""
        value = match.group(1).strip().strip("`")
        return value.split("|")[0].strip().lower()

    @staticmethod
    def parse_batch_profile(text: str) -> str:
        match = BATCH_PROFILE_PATTERN.search(text)
        if not match:
            return "standard"
        value = match.group(1).strip().strip("`")
        return value.split("|")[0].strip().lower()

    @staticmethod
    def parse_release_profile(text: str) -> str:
        match = RELEASE_PROFILE_PATTERN.search(text)
        if not match:
            return "app-service"
        value = match.group(1).strip().strip("`")
        return value.split("|")[0].strip().lower()

    @staticmethod
    def required_batch_docs(profile: str, status: str) -> tuple[str, ...]:
        normalized_profile = profile or "standard"
        normalized_status = status or "draft"
        if normalized_profile == "batch-lite":
            required_docs = ["planning.md"]
            if normalized_status in {"in-delivery", "release-candidate", "released", "archived"}:
                required_docs.append("implementation.md")
            if normalized_status in {"release-candidate", "released", "archived"}:
                required_docs.append("verification.md")
            return tuple(required_docs)
        return ("planning.md", "design.md", "implementation.md", "verification.md")

    @staticmethod
    def parse_req_type(text: str) -> str:
        match = REQ_TYPE_LINE_PATTERN.search(text)
        if not match:
            return ""
        value = match.group(1).strip()
        return value.split("|")[0].strip()

    @staticmethod
    def parse_next_requirement_id(text: str) -> int | None:
        match = re.search(r"Current:\s*`REQ-(\d+)`", text)
        if not match:
            return None
        return int(match.group(1))

    @staticmethod
    def parse_release_feedback_state(text: str) -> str:
        lines = text.splitlines()
        in_section = False
        values: list[str] = []
        for line in lines:
            stripped = line.strip()
            if stripped == "## Feedback Handoff":
                in_section = True
                continue
            if in_section and stripped.startswith("## "):
                break
            if not in_section:
                continue
            if stripped.startswith("- "):
                value = stripped[2:].strip()
                if value.endswith(":"):
                    continue
                values.append(value)

        if not in_section:
            return "missing"
        if not values:
            return "missing"

        normalized_values = {value.strip().lower() for value in values}
        unresolved_tokens = (
            "<",
            ">",
            "{{",
            "}}",
            "후속 discovery 후보",
            "후속 req 후보",
            "tbd",
            "미정",
        )
        if any(any(token in value for token in unresolved_tokens) for value in normalized_values):
            return "unresolved"
        if normalized_values.issubset({"없음", "none", "n/a", "na"}):
            return "resolved"
        return "resolved"

    @staticmethod
    def parse_latest_change_req_impact(text: str) -> ChangeReqImpact | None:
        sections = re.split(r"^###\s+(CHG-\d{8}-\d+)\s*$", text, flags=re.MULTILINE)
        if len(sections) < 3:
            return None

        latest_change_id = sections[-2].strip()
        latest_change_body = sections[-1]
        status_recommendation = Doctor.parse_change_log_field(latest_change_body, "Status Recommendation")
        reverification_needed = Doctor.parse_change_log_field(latest_change_body, "Reverification Needed")
        implementation_invalidated = Doctor.parse_change_log_field(latest_change_body, "Existing Implementation Invalidated")

        if not status_recommendation and not reverification_needed and not implementation_invalidated:
            return None

        return ChangeReqImpact(
            change_id=latest_change_id,
            status_recommendation=status_recommendation,
            reverification_needed=reverification_needed,
            implementation_invalidated=implementation_invalidated,
        )

    @staticmethod
    def parse_change_log_field(text: str, field_name: str) -> str:
        pattern = rf"^-\s*{re.escape(field_name)}:\s*(.+?)\s*$"
        match = re.search(pattern, text, flags=re.MULTILINE)
        if not match:
            return ""
        return match.group(1).strip().strip("`").lower()

    @staticmethod
    def parse_release_feedback_inputs(text: str) -> dict[str, list[str]]:
        lines = text.splitlines()
        in_section = False
        current_group: str | None = None
        groups: dict[str, list[str]] = {
            "observation": [],
            "discovery": [],
            "req": [],
            "change-req": [],
        }
        group_labels = {
            "Observation Summary": "observation",
            "Discovery Input": "discovery",
            "REQ Input": "req",
            "Change Request Input": "change-req",
        }

        for line in lines:
            stripped = line.strip()
            if stripped == "## Feedback Handoff":
                in_section = True
                current_group = None
                continue
            if in_section and stripped.startswith("## "):
                break
            if not in_section or not stripped.startswith("- "):
                continue

            value = stripped[2:].strip()
            if value.endswith(":"):
                current_group = group_labels.get(value[:-1].strip())
                continue
            if current_group is not None:
                groups[current_group].append(value)

        return groups

    @staticmethod
    def normalize_feedback_values(values: Iterable[str]) -> tuple[str, ...]:
        normalized: list[str] = []
        for value in values:
            cleaned = value.strip()
            if not cleaned or cleaned.lower() in {"없음", "none", "n/a", "na"}:
                continue
            normalized.append(cleaned)
        return tuple(normalized)

    @staticmethod
    def extract_req_ids(text: str) -> set[str]:
        return {f"req-{int(match.group(1)):03d}" for match in REQ_ID_PATTERN.finditer(text)}

    @staticmethod
    def extract_batch_ids(text: str) -> set[str]:
        return {f"bat-{int(match.group(1)):03d}" for match in BATCH_ID_PATTERN.finditer(text)}

    @staticmethod
    def extract_discovery_ids(text: str) -> set[str]:
        return {f"dcy-{int(match.group(1)):03d}" for match in DISCOVERY_ID_PATTERN.finditer(text)}

    @staticmethod
    def extract_release_ids(text: str) -> set[str]:
        return {f"rel-{int(match.group(1)):03d}" for match in RELEASE_ID_PATTERN.finditer(text)}

    @staticmethod
    def normalize_req_id(raw_value: str) -> str:
        match = REQ_ID_PATTERN.search(raw_value or "")
        if not match:
            return ""
        return f"req-{int(match.group(1)):03d}"

    @staticmethod
    def normalize_batch_id(raw_value: str) -> str:
        match = BATCH_ID_PATTERN.search(raw_value or "")
        if not match:
            return ""
        return f"bat-{int(match.group(1)):03d}"

    @staticmethod
    def normalize_discovery_id(raw_value: str) -> str:
        match = DISCOVERY_ID_PATTERN.search(raw_value or "")
        if not match:
            return ""
        return f"dcy-{int(match.group(1)):03d}"

    @staticmethod
    def normalize_release_id(raw_value: str) -> str:
        match = RELEASE_ID_PATTERN.search(raw_value or "")
        if not match:
            return ""
        return f"rel-{int(match.group(1)):03d}"

    @staticmethod
    def req_id_number(req_id: str) -> int:
        match = REQ_ID_PATTERN.search(req_id)
        return int(match.group(1)) if match else 0


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Validate StagePilot package contracts and active SDLC workspace documents.")
    parser.add_argument("root", nargs="?", default=".", help="Workspace root to validate (default: current directory)")
    parser.add_argument(
        "--strict-missing-docs",
        action="store_true",
        help="Treat a missing active docs workspace as an error instead of a warning.",
    )
    parser.add_argument(
        "--report",
        help="Write a Markdown report to the given path.",
    )
    return parser


def main() -> int:
    parser = build_parser()
    args = parser.parse_args()

    workspace_root = Path(args.root).resolve()
    # tools/stagepilot-doctor.py 기준 한 단계 위가 패키지 루트.
    # (호스트에서는 .stage-pilot subtree 루트, 개발 레포에서는 저장소 루트)
    package_root = Path(__file__).resolve().parents[1]
    report_path = Path(args.report).resolve() if args.report else None

    if not workspace_root.exists():
        parser.error(f"Workspace root does not exist: {workspace_root}")

    doctor = Doctor(
        workspace_root=workspace_root,
        package_root=package_root,
        strict_missing_docs=args.strict_missing_docs,
        report_path=report_path,
    )
    return doctor.run()


if __name__ == "__main__":
    sys.exit(main())