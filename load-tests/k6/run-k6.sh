#!/usr/bin/env sh
set -eu

profile="${1:-smoke}"
base_url="${BASE_URL:-http://localhost:5235}"
case "$profile" in ratelimit|smoke|load|mixed|stress|spike|soak) ;; *) echo "Unknown profile: $profile" >&2; exit 2;; esac
case "$base_url" in http://localhost:*|http://127.0.0.1:*|http://api:*|http://host.docker.internal:*) ;; *) [ "${ALLOW_EXTERNAL_TARGET:-}" = "true" ] || { echo "Refusing non-local target: $base_url" >&2; exit 2; };; esac

root=$(CDPATH= cd -- "$(dirname -- "$0")/../.." && pwd)
mkdir -p "$root/load-tests/reports"
if [ "$profile" = "ratelimit" ]; then
  stamp=$(date -u +%Y%m%d-%H%M%S)
  exec k6 run -e BASE_URL="$base_url" --summary-export "$root/load-tests/reports/ratelimit-$stamp-summary.json" \
    --out "json=$root/load-tests/reports/ratelimit-$stamp-samples.json" "$root/load-tests/k6/rate-limit-check.js"
fi
seed_file="${SEED_FILE:-$(find "$root/load-tests/output" -name 'aqarios_seed_*.json' -type f -print | sort | tail -n 1)}"
[ -n "$seed_file" ] || { echo "No seed output found. Run seed_aqarios.py first." >&2; exit 2; }
stamp=$(date -u +%Y%m%d-%H%M%S)
exec k6 run -e PROFILE="$profile" -e BASE_URL="$base_url" -e SEED_FILE="$seed_file" \
  -e SUMMARY_FILE="$root/load-tests/reports/$profile-$stamp-summary.json" \
  --out "json=$root/load-tests/reports/$profile-$stamp-samples.json" "$root/load-tests/k6/aqarios-test.js"
