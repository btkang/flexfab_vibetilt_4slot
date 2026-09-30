#!/usr/bin/env bash

set -euo pipefail

usage() {
  cat <<'EOF'
Usage: stagepilot.sh <command> [args]

Commands:
  bootstrap-seed [HOST_ROOT] [options]
      Create .stage-pilot/bootstrap/baseline.yaml from interactive answers
      or explicit CLI arguments.
  doctor [HOST_ROOT] [--strict-missing-docs] [--report <path>]
      Run the StagePilot validation script against a workspace.
EOF
}

fail() {
  printf '[stagepilot] ERROR: %s\n' "$*" >&2
  exit 1
}

run_doctor() {
  local script_dir package_root doctor_script
  script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -P)"
  package_root="$(cd "${script_dir}/.." && pwd -P)"
  doctor_script="${package_root}/tools/stagepilot-doctor.py"

  [ -f "${doctor_script}" ] || fail "Doctor script not found: ${doctor_script}"
  command -v python3 >/dev/null 2>&1 || fail "python3 is required to run doctor"

  python3 "${doctor_script}" "$@"
}

run_bootstrap_seed() {
  local script_dir package_root seed_script
  script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -P)"
  package_root="$(cd "${script_dir}/.." && pwd -P)"
  seed_script="${package_root}/tools/stagepilot-bootstrap-seed.py"

  [ -f "${seed_script}" ] || fail "Bootstrap seed helper not found: ${seed_script}"
  command -v python3 >/dev/null 2>&1 || fail "python3 is required to generate a bootstrap seed"

  python3 "${seed_script}" "$@"
}

main() {
  local command="${1:-}"
  if [ -z "${command}" ]; then
    usage
    exit 1
  fi
  shift || true

  case "${command}" in
    bootstrap-seed)
      run_bootstrap_seed "$@"
      ;;
    doctor)
      run_doctor "$@"
      ;;
    -h|--help|help)
      usage
      ;;
    *)
      usage
      fail "Unknown command: ${command}"
      ;;
  esac
}

main "$@"