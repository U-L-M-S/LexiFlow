#!/bin/sh
set -eu

CONFIG_DIR=/usr/share/nginx/html
TEMPLATE="$CONFIG_DIR/config.template.js"
TARGET="$CONFIG_DIR/config.js"

API_BASE="${VITE_API_BASE:-${PUBLIC_API_BASE:-http://localhost:8081}}"

if [ -f "$TEMPLATE" ]; then
  envsubst '__API_BASE__' < "$TEMPLATE" > "$TARGET"
else
  # fallback for first run where only config.js exists
  if [ -f "$TARGET" ]; then
    cp "$TARGET" "$TARGET.bak"
    sed "s|__API_BASE__|$API_BASE|g" "$TARGET.bak" > "$TARGET"
  fi
fi

echo "Serving frontend with API base: $API_BASE"

exec "$@"
