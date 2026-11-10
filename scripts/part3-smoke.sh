#!/usr/bin/env bash
set -euo pipefail
BASE_URL="${BASE_URL:-http://localhost:5000}"
CUSTOMER_EMAIL="${CUSTOMER_EMAIL:-smoke-$(date +%s)@example.com}"
CUSTOMER_PASSWORD="${CUSTOMER_PASSWORD:-SmokePassword12345!}"
temp_dir="$(mktemp -d)"
trap 'rm -rf "$temp_dir"' EXIT

request() {
  local expected="$1" method="$2" endpoint="$3" data="${4:-}" token="${5:-}"
  local args=(-sS -o "$temp_dir/response" -w '%{http_code}' -X "$method")
  if [[ -n "$data" ]]; then args+=(-H 'Content-Type: application/json' -d "$data"); fi
  if [[ -n "$token" ]]; then args+=(-H "Authorization: Bearer $token"); fi
  local status
  status="$(curl "${args[@]}" "$BASE_URL$endpoint")"
  if [[ "$status" != "$expected" ]]; then
    echo "FAIL $method $endpoint: expected $expected, got $status" >&2
    cat "$temp_dir/response" >&2
    exit 1
  fi
  echo "PASS $method $endpoint ($status)"
}

request 200 GET /health
request 200 GET /api/menu
request 401 POST /api/menu '{"category":"Hot Drinks","id":"smoke","name":"Coffee","description":"Test","price":10,"isAvailable":true}'
request 201 POST /api/auth/register "{\"email\":\"$CUSTOMER_EMAIL\",\"fullName\":\"Smoke Test\",\"password\":\"$CUSTOMER_PASSWORD\"}"
request 200 POST /api/auth/login "{\"email\":\"$CUSTOMER_EMAIL\",\"password\":\"$CUSTOMER_PASSWORD\"}"
if command -v python3 >/dev/null; then
  token="$(python3 -c 'import json,sys;print(json.load(sys.stdin)["token"])' < "$temp_dir/response")"
else
  echo 'Python 3 required to parse JWT response' >&2
  exit 1
fi
request 403 POST /api/menu '{"category":"Hot Drinks","id":"smoke","name":"Coffee","description":"Test","price":10,"isAvailable":true}' "$token"
request 403 POST /api/users/staff '{"email":"nope@example.com","fullName":"Denied","password":"LongEnough1234!"}' "$token"
request 401 POST /api/auth/login "{\"email\":\"$CUSTOMER_EMAIL\",\"password\":\"wrong-password\"}"
echo 'Gateway smoke checks passed.'
