#!/usr/bin/env bash
set -euo pipefail

# Simple smoke test script for TheThroneOfGames API
# Usage:
#   BASE_URL=http://localhost:5000 ./scripts/smoke-test.sh

BASE_URL="${BASE_URL:-http://localhost:5000}"
RETRIES="${RETRIES:-30}"
SLEEP="${SLEEP:-2}"

echo "[smoke-test] BASE_URL=$BASE_URL RETRIES=$RETRIES SLEEP=$SLEEP"

check() {
  curl -sS -o /dev/null -w "%{http_code}" "$1" || echo "000"
}

wait_for() {
  local url="$1"
  for i in $(seq 1 $RETRIES); do
    code=$(check "$url")
    if [ "$code" = "200" ]; then
      echo "[smoke-test] OK $url"
      return 0
    fi
    echo "[smoke-test] Waiting for $url -> $code ($i/$RETRIES)"
    sleep $SLEEP
  done
  echo "[smoke-test] FAILED waiting for $url"
  return 1
}

wait_for "$BASE_URL/health"
wait_for "$BASE_URL/health/ready"

echo "[smoke-test] Performing sample API requests"
HTTP_CODE=$(curl -sS -o /dev/null -w "%{http_code}" "$BASE_URL/") || HTTP_CODE=000
echo "[smoke-test] GET / -> $HTTP_CODE"

echo "[smoke-test] Completed"
exit 0
