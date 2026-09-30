#!/usr/bin/env bash

set -euo pipefail

usage() {
  cat <<'EOF'
Usage: install.sh [--dry-run] [HOST_ROOT] [POLICY]

source/ 아래의 모든 파일을 호스트 저장소 루트로 복사한다.
  source/.github/...  -> HOST_ROOT/.github/...
  source/.claude/...  -> HOST_ROOT/.claude/...

단, source/instruction.md는 도구 중립 단일 원본으로 다음 두 복사본으로 fan-out 한다.
  -> HOST_ROOT/.github/copilot-instructions.md  (Copilot용, verbatim 복사)
  -> HOST_ROOT/CLAUDE.md 의 StagePilot 마커 블록  (Claude용)
(instruction.md 자체는 루트로 그대로 복사하지 않는다.)

또한 .stage-pilot/skills를 .github/skills 및 .agents/skills로 동기화한다.
  .stage-pilot/skills/... -> HOST_ROOT/.github/skills/...
  .stage-pilot/skills/... -> HOST_ROOT/.agents/skills/...

Options:
  --dry-run   변경 없이 복사/생성 계획만 출력한다.
  -h, --help  도움말을 출력한다.

Arguments:
  HOST_ROOT   대상 호스트 저장소 루트 (default: 현재 디렉터리)
  POLICY      기존 파일 충돌 정책: replace|preserve|fail
              (default: STAGEPILOT_CONFLICT_POLICY 또는 preserve)
EOF
}

log() {
  printf '[install] %s\n' "$*"
}

fail() {
  printf '[install] ERROR: %s\n' "$*" >&2
  exit 1
}

# pwd -P로 물리 경로를 잡아 심링크(.stage-pilot/* 등)를 실제 위치로 해석한다.
# 그렇지 않으면 SOURCE_DIR가 심링크가 되어 `find`가 내려가지 못한다.
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -P)"
PACKAGE_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd -P)"
SOURCE_DIR="${PACKAGE_ROOT}/source"
SKILLS_SOURCE_DIR="${PACKAGE_ROOT}/skills"

DRY_RUN=0
ARGS=()
while [ "$#" -gt 0 ]; do
  case "$1" in
    --dry-run)
      DRY_RUN=1
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
POLICY="${ARGS[1]:-${STAGEPILOT_CONFLICT_POLICY:-preserve}}"
TARGET_CLAUDE="${HOST_ROOT}/CLAUDE.md"
INSTRUCTION_SOURCE="${SOURCE_DIR}/instruction.md"
STAGEPILOT_BEGIN="<!-- STAGEPILOT:BEGIN -->"
STAGEPILOT_END="<!-- STAGEPILOT:END -->"

case "${POLICY}" in
  replace|preserve|fail)
    ;;
  *)
    fail "Invalid conflict policy '${POLICY}'. Allowed: replace|preserve|fail"
    ;;
esac

[ -d "${SOURCE_DIR}" ] || fail "Source directory not found: ${SOURCE_DIR}"
[ -d "${HOST_ROOT}" ] || fail "Host root not found: ${HOST_ROOT}"

copied=0
skipped=0
conflicted=0
failed=0

# source/<rel> 파일 하나를 HOST_ROOT/<rel>로 정책에 맞춰 복사한다.
copy_one() {
  local src_file="$1"
  local rel_path="$2"
  local dst_file="${HOST_ROOT}/${rel_path}"
  local dst_dir
  dst_dir="$(dirname "${dst_file}")"

  if [ ! -e "${dst_file}" ]; then
    if [ "${DRY_RUN}" -eq 1 ]; then
      log "[dry-run] copy: ${rel_path}"
    else
      mkdir -p "${dst_dir}"
      cp -a "${src_file}" "${dst_file}"
    fi
    copied=$((copied + 1))
    return
  fi

  if cmp -s "${src_file}" "${dst_file}"; then
    skipped=$((skipped + 1))
    return
  fi

  conflicted=$((conflicted + 1))
  case "${POLICY}" in
    replace)
      if [ "${DRY_RUN}" -eq 1 ]; then
        log "[dry-run] replace: ${rel_path}"
      else
        mkdir -p "${dst_dir}"
        cp -a "${src_file}" "${dst_file}"
      fi
      copied=$((copied + 1))
      ;;
    preserve)
      log "preserve existing: ${rel_path}"
      skipped=$((skipped + 1))
      ;;
    fail)
      printf '[install] CONFLICT: %s\n' "${rel_path}" >&2
      failed=$((failed + 1))
      ;;
  esac
}

# 1) source/ 전체를 호스트 루트로 복사 (.github, .claude 등 구조 보존)
#    단, instruction.md는 단일 원본이므로 루트로 직접 복사하지 않고 아래에서 fan-out 한다.
while IFS= read -r -d '' src_file; do
  rel_path="${src_file#${SOURCE_DIR}/}"
  if [ "${rel_path}" = "instruction.md" ]; then
    continue
  fi
  copy_one "${src_file}" "${rel_path}"
done < <(find "${SOURCE_DIR}" -type f -print0 | sort -z)

# 1-b) instruction.md -> .github/copilot-instructions.md (Copilot 매직 파일명)로 fan-out
if [ -f "${INSTRUCTION_SOURCE}" ]; then
  copy_one "${INSTRUCTION_SOURCE}" ".github/copilot-instructions.md"
fi

# 1-c) .stage-pilot/skills -> .github/skills, .agents/skills 동기화 (심볼릭 링크 미사용)
if [ -d "${SKILLS_SOURCE_DIR}" ]; then
  while IFS= read -r -d '' src_file; do
    rel_path="${src_file#${SKILLS_SOURCE_DIR}/}"
    copy_one "${src_file}" ".github/skills/${rel_path}"
    copy_one "${src_file}" ".agents/skills/${rel_path}"
  done < <(find "${SKILLS_SOURCE_DIR}" -type f -print0 | sort -z)
else
  log "skills source not found: ${SKILLS_SOURCE_DIR}"
fi

# 2) CLAUDE.md: instruction.md를 단일 원본으로 StagePilot 블록 생성/갱신.
#    호스트가 직접 추가한 CLAUDE.md 내용은 마커 밖에 그대로 보존한다.
generate_claude_md() {
  [ -f "${INSTRUCTION_SOURCE}" ] || { log "instruction.md 없음; CLAUDE.md 생략"; return 0; }

  local payload output
  payload="$(mktemp)"
  output="$(mktemp)"

  # 원본에 이미 마커가 있으면 그 블록만, 없으면 파일 전체를 payload로 사용한다.
  if grep -Fq "${STAGEPILOT_BEGIN}" "${INSTRUCTION_SOURCE}" && grep -Fq "${STAGEPILOT_END}" "${INSTRUCTION_SOURCE}"; then
    awk -v b="${STAGEPILOT_BEGIN}" -v e="${STAGEPILOT_END}" '
      $0 == b { f=1; next }
      $0 == e { f=0; exit }
      f { print }
    ' "${INSTRUCTION_SOURCE}" > "${payload}"
  else
    cat "${INSTRUCTION_SOURCE}" > "${payload}"
  fi

  if [ ! -e "${TARGET_CLAUDE}" ]; then
    {
      printf '%s\n' "${STAGEPILOT_BEGIN}"
      cat "${payload}"
      printf '%s\n' "${STAGEPILOT_END}"
    } > "${output}"
  elif grep -Fq "${STAGEPILOT_BEGIN}" "${TARGET_CLAUDE}" && grep -Fq "${STAGEPILOT_END}" "${TARGET_CLAUDE}"; then
    awk -v b="${STAGEPILOT_BEGIN}" -v e="${STAGEPILOT_END}" -v p="${payload}" '
      $0 == b { print; while ((getline line < p) > 0) print line; f=1; next }
      $0 == e { f=0; print; next }
      !f { print }
    ' "${TARGET_CLAUDE}" > "${output}"
  else
    cat "${TARGET_CLAUDE}" > "${output}"
    {
      printf '\n## StagePilot\n'
      printf '%s\n' "${STAGEPILOT_BEGIN}"
      cat "${payload}"
      printf '%s\n' "${STAGEPILOT_END}"
    } >> "${output}"
  fi

  if [ "${DRY_RUN}" -eq 1 ]; then
    if command -v diff >/dev/null 2>&1 && [ -e "${TARGET_CLAUDE}" ]; then
      log "[dry-run] CLAUDE.md diff:"
      diff -u "${TARGET_CLAUDE}" "${output}" || true
    else
      log "[dry-run] create/update: CLAUDE.md"
    fi
  else
    cp -a "${output}" "${TARGET_CLAUDE}"
    log "CLAUDE.md StagePilot 블록 갱신"
  fi

  rm -f "${payload}" "${output}"
}

generate_claude_md

log "host_root=${HOST_ROOT}"
log "source=${SOURCE_DIR}"
log "policy=${POLICY}"
log "copied=${copied} skipped=${skipped} conflicted=${conflicted}"

if [ "${failed}" -ne 0 ]; then
  if [ "${DRY_RUN}" -eq 1 ]; then
    fail "[dry-run] policy=fail에서 충돌이 발생합니다."
  fi
  fail "policy=fail 충돌로 설치를 중단합니다."
fi

if [ "${DRY_RUN}" -eq 1 ]; then
  log "[dry-run] 디렉터리 골격 생성 단계 생략. 변경된 파일 없음."
  exit 0
fi

# 3) 디렉터리 골격 생성 (docs/discovery|srs|batches|releases 등)
BOOTSTRAP_SCRIPT="${PACKAGE_ROOT}/tools/bootstrap.sh"
[ -f "${BOOTSTRAP_SCRIPT}" ] || fail "Bootstrap script not found: ${BOOTSTRAP_SCRIPT}"

/bin/bash "${BOOTSTRAP_SCRIPT}" "${HOST_ROOT}"
log "Install completed successfully."
