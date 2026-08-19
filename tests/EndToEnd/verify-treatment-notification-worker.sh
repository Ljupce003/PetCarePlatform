#!/usr/bin/env bash

set -euo pipefail

keycloak_url="${KEYCLOAK_URL:-http://localhost:8080}"
gateway_url="${GATEWAY_URL:-http://localhost:7000}"
owner_id="d8ee7d11-6e26-40e0-a62c-a0a950402ba6"
pet_id="bd591fac-4b2c-47bb-8d57-a2f317a42fc2"

for dependency in curl jq; do
  if ! command -v "$dependency" >/dev/null 2>&1; then
    echo "Required command is missing: $dependency" >&2
    exit 1
  fi
done

token_response=$(curl --fail-with-body --silent --show-error --max-time 15 \
  -X POST "$keycloak_url/realms/petcare/protocol/openid-connect/token" \
  -H 'Content-Type: application/x-www-form-urlencoded' \
  --data-urlencode 'grant_type=password' \
  --data-urlencode 'client_id=petcare-demo' \
  --data-urlencode 'username=admin1' \
  --data-urlencode 'password=Admin123!')
access_token=$(jq -er '.access_token' <<<"$token_response")

source_event_id="runtime:worker:$(date -u +%Y%m%dT%H%M%SZ)"
scheduled_for_utc=$(date -u +%Y-%m-%dT%H:%M:%SZ)
payload=$(jq -n \
  --arg ownerId "$owner_id" \
  --arg petId "$pet_id" \
  --arg scheduledForUtc "$scheduled_for_utc" \
  --arg sourceEventId "$source_event_id" \
  '{
    ownerId: $ownerId,
    petId: $petId,
    type: "FollowUpReminder",
    title: "Worker runtime verification",
    message: "This notification verifies console delivery and the Sent transition.",
    scheduledForUtc: $scheduledForUtc,
    sourceEventId: $sourceEventId
  }')

created=$(curl --fail-with-body --silent --show-error --max-time 15 \
  -X POST "$gateway_url/treatment/api/notifications" \
  -H "Authorization: Bearer $access_token" \
  -H 'Content-Type: application/json' \
  --data "$payload")
notification_id=$(jq -er '.id' <<<"$created")

for _ in {1..20}; do
  history=$(curl --fail-with-body --silent --show-error --max-time 15 \
    "$gateway_url/treatment/api/notifications/owner/$owner_id" \
    -H "Authorization: Bearer $access_token")
  status=$(jq -r --arg id "$notification_id" \
    '.[] | select(.id == $id) | .status' <<<"$history")

  if [[ "$status" == "Sent" ]]; then
    echo "PASS: Notification worker console-delivered $notification_id and persisted Sent status"
    exit 0
  fi

  if [[ "$status" == "Failed" ]]; then
    echo "Notification $notification_id was marked Failed" >&2
    exit 1
  fi

  sleep 2
done

echo "Notification $notification_id did not reach Sent status within 40 seconds" >&2
exit 1
