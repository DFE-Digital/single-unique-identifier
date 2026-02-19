#!/usr/bin/env bash

# Prereqs:
# - gh CLI installed
# - gh auth logged in with repo scope (no separate PAT needed if already logged in)
#   check: gh auth status
#   refresh if needed: gh auth refresh -s repo
#   or export GH_TOKEN=YOUR_PAT (classic PAT with repo scope)
#
# Usage:
#   ./cleanup-artifacts.sh [options]
#
# Options:
#   -o, --owner DFE-Digital
#   -r, --repo single-unique-identifier
#   -w, --workflows find-build-and-deploy.yml,singleview-build-and-deploy.yml,transfer-build-and-deploy.yml,custodians-build-and-deploy.yml
#   -d, --retention-days DAYS (omit to skip date filter)
#   -b, --target-branch BRANCH (omit to skip branch filter)
#   -g, --artifact-name-glob GLOB (omit to match all)
#   -n, --dry-run 0|1
# Note: At least one filter must be provided (--retention-days, --target-branch, or --artifact-name-glob).

# Defaults (override via CLI options below)
OWNER="DFE-Digital"
REPO="single-unique-identifier"
WORKFLOWS_CSV="find-build-and-deploy.yml,singleview-build-and-deploy.yml,transfer-build-and-deploy.yml,custodians-build-and-deploy.yml"
RETENTION_DAYS=""
TARGET_BRANCH=""
ARTIFACT_NAME_GLOB=""
DRY_RUN=1

usage() {
  cat <<'EOF'
Usage: ./cleanup-artifacts.sh [OPTION]...
Delete GitHub Actions artifacts for selected workflow runs.
At least one filter must be provided (--retention-days, --target-branch, or --artifact-name-glob).

Mandatory arguments to long options are mandatory for short options too.
  -o, --owner OWNER          repository owner (default: DFE-Digital)
  -r, --repo REPO            repository name (default: single-unique-identifier)
  -w, --workflows CSV        workflow files (comma-separated)
                             default: find-build-and-deploy.yml,singleview-build-and-deploy.yml,
                                      transfer-build-and-deploy.yml,custodians-build-and-deploy.yml
  -d, --retention-days DAYS  delete artifacts from runs older than DAYS
  -b, --target-branch BRANCH delete artifacts from runs not on BRANCH
  -g, --artifact-name-glob GLOB
                             only delete artifacts matching shell glob
  -n, --dry-run 0|1          1 = print actions, 0 = delete (default: 1)
  -h, --help                 show this help and exit

Examples:
  ./cleanup-artifacts.sh -d 30
      delete artifacts from runs older than 30 days (any branch)
  ./cleanup-artifacts.sh -b main -g "publish-*"
      delete publish-* artifacts from runs not on main
  ./cleanup-artifacts.sh -d 7 -g "find-*"
      delete find-* artifacts older than 7 days
EOF
}

while [ $# -gt 0 ]; do
  case "$1" in
    -o|--owner)
      OWNER="$2"
      shift 2
      ;;
    -r|--repo)
      REPO="$2"
      shift 2
      ;;
    -w|--workflows)
      WORKFLOWS_CSV="$2"
      shift 2
      ;;
    -d|--retention-days)
      RETENTION_DAYS="$2"
      shift 2
      ;;
    -b|--target-branch)
      TARGET_BRANCH="$2"
      shift 2
      ;;
    -g|--artifact-name-glob)
      ARTIFACT_NAME_GLOB="$2"
      shift 2
      ;;
    -n|--dry-run)
      DRY_RUN="$2"
      shift 2
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage
      exit 1
      ;;
  esac
done

if [ -n "$RETENTION_DAYS" ]; then
  if ! [[ "$RETENTION_DAYS" =~ ^[0-9]+$ ]] || [ "$RETENTION_DAYS" -le 0 ]; then
    echo "RETENTION_DAYS must be a positive integer." >&2
    exit 1
  fi
fi

if ! [[ "$DRY_RUN" =~ ^[01]$ ]]; then
  echo "DRY_RUN must be 0 or 1." >&2
  exit 1
fi

if [ -z "$RETENTION_DAYS" ] && [ -z "$TARGET_BRANCH" ] && [ -z "$ARTIFACT_NAME_GLOB" ]; then
  echo "At least one filter must be provided: --retention-days, --target-branch, or --artifact-name-glob." >&2
  exit 1
fi

IFS=',' read -r -a WORKFLOWS_RAW <<< "$WORKFLOWS_CSV"
WORKFLOWS=()
for wf in "${WORKFLOWS_RAW[@]}"; do
  wf="${wf#"${wf%%[![:space:]]*}"}"
  wf="${wf%"${wf##*[![:space:]]}"}"
  if [ -n "$wf" ]; then
    WORKFLOWS+=("$wf")
  fi
done

if [ -n "$RETENTION_DAYS" ]; then
  if command -v python3 >/dev/null 2>&1; then
    PYTHON_BIN="python3"
  elif command -v python >/dev/null 2>&1; then
    PYTHON_BIN="python"
  else
    echo "python3 (or python) is required to compute the cutoff date." >&2
    exit 1
  fi
  # Cutoff: RETENTION_DAYS ago (UTC) using python for portability
  CUTOFF="$("$PYTHON_BIN" - "$RETENTION_DAYS" <<'PY'
from datetime import datetime, timedelta, timezone
import sys

days = int(sys.argv[1])
print((datetime.now(timezone.utc) - timedelta(days=days)).date().isoformat())
PY
)"
  CUTOFF_ISO="${CUTOFF}T00:00:00Z"
fi

FILTER_CONDS=()
if [ -n "$CUTOFF_ISO" ]; then
  FILTER_CONDS+=(".created_at < \"${CUTOFF_ISO}\"")
fi
if [ -n "$TARGET_BRANCH" ]; then
  FILTER_CONDS+=(".head_branch != null and .head_branch != \"${TARGET_BRANCH}\"")
fi

if [ ${#FILTER_CONDS[@]} -eq 0 ]; then
  FILTER_JQ='.workflow_runs[] | [.id, (.display_title // .name // "unknown")] | @tsv'
else
  FILTER_JQ=".workflow_runs[] | select((${FILTER_CONDS[0]})"
  for cond in "${FILTER_CONDS[@]:1}"; do
    FILTER_JQ+=" or (${cond})"
  done
  FILTER_JQ+=") | [.id, (.display_title // .name // \"unknown\")] | @tsv"
fi

RUN_FILTER_DESC="none"
if [ ${#FILTER_CONDS[@]} -gt 0 ]; then
  RUN_FILTER_DESC="${FILTER_CONDS[0]}"
  for cond in "${FILTER_CONDS[@]:1}"; do
    RUN_FILTER_DESC+=" OR ${cond}"
  done
fi
ARTIFACT_FILTER_DESC="${ARTIFACT_NAME_GLOB:-any}"

for wf in "${WORKFLOWS[@]}"; do
  echo "== Workflow: $wf (run filter: $RUN_FILTER_DESC, artifact name filter: $ARTIFACT_FILTER_DESC) =="
  gh api --paginate \
    "repos/$OWNER/$REPO/actions/workflows/$wf/runs?per_page=100" \
    --jq "$FILTER_JQ" \
  | while IFS=$'\t' read -r run_id run_name; do
      gh api --paginate \
        "repos/$OWNER/$REPO/actions/runs/$run_id/artifacts?per_page=100" \
        --jq '.artifacts[] | "\(.id)\t\(.name)\t\(.created_at)"' \
      | while IFS=$'\t' read -r artifact_id artifact_name artifact_created_at; do
          if [ -n "$ARTIFACT_NAME_GLOB" ] && [[ "$artifact_name" != $ARTIFACT_NAME_GLOB ]]; then
            continue
          fi
          if [ "$DRY_RUN" = "1" ]; then
            echo "Would delete artifact $artifact_id ($artifact_name, created $artifact_created_at) (run $run_id: $run_name)"
          else
            gh api -X DELETE "repos/$OWNER/$REPO/actions/artifacts/$artifact_id"
            echo "Deleted artifact $artifact_id ($artifact_name, created $artifact_created_at) (run $run_id: $run_name)"
          fi
        done
    done
done
