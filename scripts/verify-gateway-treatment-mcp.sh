#!/usr/bin/env bash

set -euo pipefail

keycloak_url="${KEYCLOAK_URL:-http://localhost:8080}"
gateway_url="${GATEWAY_URL:-http://localhost:7000}"
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
  --data-urlencode 'username=vet1' \
  --data-urlencode 'password=Vet123!')
access_token=$(jq -er '.access_token' <<<"$token_response")

owner_token_response=$(curl --fail-with-body --silent --show-error --max-time 15 \
  -X POST "$keycloak_url/realms/petcare/protocol/openid-connect/token" \
  -H 'Content-Type: application/x-www-form-urlencoded' \
  --data-urlencode 'grant_type=password' \
  --data-urlencode 'client_id=petcare-demo' \
  --data-urlencode 'username=owner1' \
  --data-urlencode 'password=Owner123!')
owner_token=$(jq -er '.access_token' <<<"$owner_token_response")

anonymous_status=$(curl --silent --output /dev/null --write-out '%{http_code}' --max-time 15 \
  "$gateway_url/treatment/api/treatments/pet/$pet_id")
if [[ "$anonymous_status" != "401" ]]; then
  echo "Expected anonymous Treatment request to return 401, received $anonymous_status" >&2
  exit 1
fi

owner_write_status=$(curl --silent --output /dev/null --write-out '%{http_code}' --max-time 15 \
  -X POST "$gateway_url/treatment/api/treatments" \
  -H "Authorization: Bearer $owner_token" \
  -H 'Content-Type: application/json' \
  --data '{}')
if [[ "$owner_write_status" != "403" ]]; then
  echo "Expected owner Treatment write to return 403, received $owner_write_status" >&2
  exit 1
fi

rest_record=$(curl --fail-with-body --silent --show-error --max-time 15 \
  -X POST "$gateway_url/treatment/api/treatments" \
  -H "Authorization: Bearer $access_token" \
  -H 'Content-Type: application/json' \
  --data '{
    "petId": "bd591fac-4b2c-47bb-8d57-a2f317a42fc2",
    "ownerId": "d8ee7d11-6e26-40e0-a62c-a0a950402ba6",
    "veterinarianId": "f793a3bd-c804-4a6b-8940-f6a80a9e4a87",
    "appointmentId": null,
    "examinedAtUtc": "2026-08-14T09:35:00Z",
    "diagnosis": "Gateway end-to-end verified",
    "treatmentPlan": "No treatment required",
    "medications": [],
    "nextControlAtUtc": null,
    "notes": "Created through API Gateway with a Keycloak veterinarian token"
  }')
jq -e --arg pet_id "$pet_id" \
  '.petId == $pet_id and .diagnosis == "Gateway end-to-end verified"' \
  <<<"$rest_record" >/dev/null

rest_history=$(curl --fail-with-body --silent --show-error --max-time 15 \
  "$gateway_url/treatment/api/treatments/pet/$pet_id" \
  -H "Authorization: Bearer $access_token")
jq -e 'any(.[]; .diagnosis == "Gateway end-to-end verified")' \
  <<<"$rest_history" >/dev/null

mcp_initialize=$(curl --fail-with-body --silent --show-error --max-time 15 \
  -X POST "$gateway_url/mcp" \
  -H "Authorization: Bearer $access_token" \
  -H 'Content-Type: application/json' \
  -H 'Accept: application/json, text/event-stream' \
  --data '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-11-25","capabilities":{},"clientInfo":{"name":"gateway-verification","version":"1.0"}}}')
mcp_initialize_json=$(sed -n 's/^data: //p' <<<"$mcp_initialize")
jq -e '.result.serverInfo.name == "PetCare MCP Server"' \
  <<<"$mcp_initialize_json" >/dev/null

mcp_call=$(curl --fail-with-body --silent --show-error --max-time 15 \
  -X POST "$gateway_url/mcp" \
  -H "Authorization: Bearer $access_token" \
  -H 'MCP-Protocol-Version: 2025-11-25' \
  -H 'Content-Type: application/json' \
  -H 'Accept: application/json, text/event-stream' \
  --data '{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"get_medical_history","arguments":{"petId":"bd591fac-4b2c-47bb-8d57-a2f317a42fc2"}}}')
mcp_call_json=$(sed -n 's/^data: //p' <<<"$mcp_call")
mcp_tool_text=$(jq -er '.result.content[0].text' <<<"$mcp_call_json")
jq -e 'any(.[]; .diagnosis == "Gateway end-to-end verified")' \
  <<<"$mcp_tool_text" >/dev/null

echo "PASS: Keycloak -> API Gateway -> Treatment Service"
echo "PASS: Gateway security returns 401 for anonymous and 403 for owner writes"
echo "PASS: Keycloak -> API Gateway -> MCP Server -> Treatment Service"
