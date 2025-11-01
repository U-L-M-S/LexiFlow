#!/usr/bin/env bash
set -euo pipefail

API_BASE="${API_BASE:-http://api:80}"
FRONTEND_BASE="${FRONTEND_BASE:-http://frontend:80}"
OCR_BASE="${OCR_BASE:-http://ocr:80}"
LEX_BASE="${LEX_BASE:-http://lexmock:80}"
USERNAME="${SMOKE_USERNAME:-demo}"
PASSWORD="${SMOKE_PASSWORD:-demo123!}"
TMPFILE=""

log() {
  printf '[%s] %s\n' "$(date -u +%H:%M:%S)" "$*"
}

cleanup() {
  if [[ -n "$TMPFILE" && -f "$TMPFILE" ]]; then
    rm -f "$TMPFILE"
  fi
}

trap cleanup EXIT

wait_for() {
  local name="$1"
  local url="$2"
  for attempt in $(seq 1 60); do
    if curl -fsS "$url" >/dev/null 2>&1; then
      log "${name} is ready"
      return 0
    fi
    sleep 2
  done
  log "Timed out waiting for ${name} at ${url}"
  return 1
}

log "Waiting for core services"
wait_for "api" "${API_BASE}/healthz"
wait_for "ocr" "${OCR_BASE}/healthz"
wait_for "lexmock" "${LEX_BASE}/healthz"
wait_for "frontend" "${FRONTEND_BASE}/"

log "Authenticating demo user"
LOGIN_RESPONSE=$(curl -fsS -X POST "${API_BASE}/api/auth/login" \
  -H 'Content-Type: application/json' \
  -d "{\"username\":\"${USERNAME}\",\"password\":\"${PASSWORD}\"}")
TOKEN=$(echo "$LOGIN_RESPONSE" | jq -r '.token')
if [[ -z "$TOKEN" || "$TOKEN" == "null" ]]; then
  log "Failed to obtain JWT"
  exit 1
fi

AUTH_HEADER=("-H" "Authorization: Bearer ${TOKEN}")

log "Fetching existing receipts"
RECEIPTS_JSON=$(curl -fsS "${API_BASE}/api/receipts" "${AUTH_HEADER[@]}")
RECEIPT_COUNT=$(echo "$RECEIPTS_JSON" | jq 'length')
if [[ "$RECEIPT_COUNT" -lt 3 ]]; then
  log "Expected at least 3 seeded receipts but found ${RECEIPT_COUNT}"
  exit 1
fi
log "Found ${RECEIPT_COUNT} receipts"

log "Preparing sample upload"
TMPFILE=$(mktemp /tmp/lexiflow-smoke-XXXXXX)
mv "$TMPFILE" "${TMPFILE}.png"
TMPFILE="${TMPFILE}.png"
base64 -d <<'PNGDATA' >"$TMPFILE"
iVBORw0KGgoAAAANSUhEUgAAAA8AAAAQCAYAAADJViUEAAAACXBIWXMAAAsTAAALEwEAmpwYAAAAB3RJTUUH6AkPABYXDW2kNwAAAB1pVFh0Q29tbWVudAAAAAAAQ3JlYXRlZCBmb3Igc21va2UgdGVzdADJ9CpoAAAAG0lEQVQ4jWNgGAXUBwz8T0wMAwPD/0lUhgYAw+IADnPsH2YAAAAASUVORK5CYII=
PNGDATA

log "Uploading receipt"
UPLOAD_RESPONSE=$(curl -fsS -X POST "${API_BASE}/api/upload" \
  "${AUTH_HEADER[@]}" \
  -F "file=@${TMPFILE};type=image/png")
NEW_RECEIPT_ID=$(echo "$UPLOAD_RESPONSE" | jq -r '.id')
if [[ -z "$NEW_RECEIPT_ID" || "$NEW_RECEIPT_ID" == "null" ]]; then
  log "Upload did not return a receipt id"
  exit 1
fi
log "Created receipt ${NEW_RECEIPT_ID}"

log "Booking receipt ${NEW_RECEIPT_ID}"
BOOK_RESPONSE=$(curl -fsS -X POST "${API_BASE}/api/book" \
  "${AUTH_HEADER[@]}" \
  -H 'Content-Type: application/json' \
  -d "{\"receiptId\":\"${NEW_RECEIPT_ID}\"}")
VOUCHER_ID=$(echo "$BOOK_RESPONSE" | jq -r '.voucherId')
if [[ -z "$VOUCHER_ID" || "$VOUCHER_ID" == "null" ]]; then
  log "Booking failed to return a voucherId"
  exit 1
fi
log "Booking completed with voucher ${VOUCHER_ID}"

log "SMOKE TEST PASSED"
