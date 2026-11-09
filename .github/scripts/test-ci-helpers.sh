#!/usr/bin/env bash
set -euo pipefail
cd "$(git rev-parse --show-toplevel)"
mapfile -d '' test_files < <(find .github/actions .github/scripts -type f \
  \( -name '*.test.js' -o -name '*.spec.js' -o -name '*.test.mjs' -o -name '*.spec.mjs' \
  -o -name '*.test.cjs' -o -name '*.spec.cjs' \) -print0)
if (( ${#test_files[@]} == 0 )); then
  echo 'No GitHub action tests found.' >&2
  exit 1
fi
node --test "${test_files[@]}"
