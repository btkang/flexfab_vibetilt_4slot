#!/usr/bin/env bash

set -euo pipefail

usage() {
  cat <<'EOF'
Usage: update.sh [options] [HOST_ROOT]

Options:
  --dry-run           Show planned commands without executing them.
  --remote <name>     Git remote name to use when available (default: stage-pilot)
  --repo-url <url>    Fallback repository URL when remote is missing.
                      (default: https://github.com/genichin/stage-pilot.git)
  --branch <name>     Upstream branch to sync (default: main)
  --prefix <path>     Subtree prefix path (default: .stage-pilot)
  --overwrite         Overwrite conflicts during install (same as --policy replace).
                      (default behavior)
  --no-overwrite      Preserve host files on conflicts (same as --policy preserve).
  --policy <name>     install.sh conflict policy: replace|preserve|fail
                      (default: replace)
  --skip-install      Only update subtree. Do not run install.sh.
  -h, --help          Show this help message.

Arguments:
  HOST_ROOT           Host repository root (default: current directory)
EOF
}

log() {
  printf '[update] %s\n' "$*"
}

fail() {
  printf '[update] ERROR: %s\n' "$*" >&2
  exit 1
}

run_cmd() {
  if [ "${DRY_RUN}" -eq 1 ]; then
    log "[dry-run] $*"
  else
    "$@"
  fi
}

DRY_RUN=0
REMOTE_NAME="stage-pilot"
REPO_URL="https://github.com/genichin/stage-pilot.git"
BRANCH="main"
PREFIX=".stage-pilot"
POLICY="replace"
SKIP_INSTALL=0
ARGS=()

while [ "$#" -gt 0 ]; do
  case "$1" in
    --dry-run)
      DRY_RUN=1
      ;;
    --remote)
      [ "$#" -ge 2 ] || fail "--remote requires a value"
      REMOTE_NAME="$2"
      shift
      ;;
    --repo-url)
      [ "$#" -ge 2 ] || fail "--repo-url requires a value"
      REPO_URL="$2"
      shift
      ;;
    --branch)
      [ "$#" -ge 2 ] || fail "--branch requires a value"
      BRANCH="$2"
      shift
      ;;
    --prefix)
      [ "$#" -ge 2 ] || fail "--prefix requires a value"
      PREFIX="$2"
      shift
      ;;
    --overwrite)
      POLICY="replace"
      ;;
    --no-overwrite)
      POLICY="preserve"
      ;;
    --policy)
      [ "$#" -ge 2 ] || fail "--policy requires a value"
      POLICY="$2"
      shift
      ;;
    --skip-install)
      SKIP_INSTALL=1
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    --*)
      fail "Unknown option: $1"
      ;;
    *)
      ARGS+=("$1")
      ;;
  esac
  shift
done

HOST_ROOT="${ARGS[0]:-$(pwd)}"

case "${POLICY}" in
  replace|preserve|fail)
    ;;
  *)
    fail "Invalid --policy '${POLICY}'. Allowed: replace|preserve|fail"
    ;;
esac

[ -d "${HOST_ROOT}" ] || fail "Host root not found: ${HOST_ROOT}"

if ! git -C "${HOST_ROOT}" rev-parse --is-inside-work-tree >/dev/null 2>&1; then
  fail "Host root is not a git repository: ${HOST_ROOT}"
fi

if git -C "${HOST_ROOT}" remote get-url "${REMOTE_NAME}" >/dev/null 2>&1; then
  SOURCE_REF="${REMOTE_NAME}"
  log "Using git remote '${REMOTE_NAME}'"
else
  SOURCE_REF="${REPO_URL}"
  log "Remote '${REMOTE_NAME}' not found; using URL '${REPO_URL}'"
fi

if [ -d "${HOST_ROOT}/${PREFIX}" ]; then
  run_cmd git -C "${HOST_ROOT}" subtree pull --prefix="${PREFIX}" "${SOURCE_REF}" "${BRANCH}" --squash
else
  log "Subtree prefix not found; adding subtree at ${PREFIX}"
  run_cmd git -C "${HOST_ROOT}" subtree add --prefix="${PREFIX}" "${SOURCE_REF}" "${BRANCH}" --squash
fi

if [ "${SKIP_INSTALL}" -eq 1 ]; then
  log "Skip install requested. Update completed."
  exit 0
fi

INSTALL_SCRIPT="${HOST_ROOT}/${PREFIX}/tools/install.sh"
[ -f "${INSTALL_SCRIPT}" ] || fail "Install script not found after update: ${INSTALL_SCRIPT}"

if [ "${DRY_RUN}" -eq 1 ]; then
  run_cmd /bin/bash "${INSTALL_SCRIPT}" --dry-run "${HOST_ROOT}" "${POLICY}"
else
  /bin/bash "${INSTALL_SCRIPT}" "${HOST_ROOT}" "${POLICY}"
fi

log "Update completed successfully."
