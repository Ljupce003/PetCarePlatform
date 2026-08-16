import json
import os
import time
from datetime import date, timedelta
from typing import Any

import requests
import streamlit as st


KEYCLOAK_URL = os.getenv("KEYCLOAK_URL", "http://localhost:8080").rstrip("/")
GATEWAY_URL = os.getenv("GATEWAY_URL", "http://localhost:7000").rstrip("/")
CONSUL_URL = os.getenv("CONSUL_URL", "http://localhost:8500").rstrip("/")
UI_PUBLIC_URL = os.getenv("UI_PUBLIC_URL", "http://localhost:8501")

SERVICE_HEALTH = {
    "Gateway": f"{GATEWAY_URL}/health",
    "Pet Service": f"{os.getenv('PET_SERVICE_URL', 'http://localhost:5101').rstrip('/')}/health",
    "Appointment Service": f"{os.getenv('APPOINTMENT_SERVICE_URL', 'http://localhost:5102').rstrip('/')}/health",
    "Treatment Service": f"{os.getenv('TREATMENT_SERVICE_URL', 'http://localhost:5103').rstrip('/')}/health",
    "MCP Server": f"{os.getenv('MCP_SERVER_URL', 'http://localhost:7001').rstrip('/')}/health",
}

DEMO_OWNER_ID = "33333333-3333-3333-3333-333333333333"
DEMO_SECOND_OWNER_ID = "33333333-3333-3333-3333-333333333334"
DEMO_PET_ID = "44444444-4444-4444-4444-444444444444"

DEMO_USERS = {
    "Owner": {"username": "owner1", "password": "Owner123!"},
    "Veterinarian": {"username": "vet1", "password": "Vet123!"},
    "Administrator": {"username": "admin1", "password": "Admin123!"},
}

TOOL_ARGUMENTS = {
    "get_pet": {"petId": DEMO_PET_ID},
    "get_owner_pets": {"ownerId": DEMO_OWNER_ID},
    "find_available_veterinarians": {},
    "get_upcoming_appointments": {"ownerId": DEMO_OWNER_ID},
    "search_clinics": {"location": "Skopje"},
    "search_available_slots": {},
    "find_open_appointment_slots": {"date": str(date.today() + timedelta(days=1)), "location": "Skopje"},
    "get_medical_history": {"petId": DEMO_PET_ID},
    "get_vaccination_history": {"petId": DEMO_PET_ID},
    "get_next_vaccination": {"petId": DEMO_PET_ID},
}


st.set_page_config(page_title="PetCare Platform Demo", page_icon="🐾", layout="wide")
st.markdown(
    """
    <style>
      .block-container { padding-top: 1.5rem; padding-bottom: 3rem; }
      [data-testid="stMetric"] { background: #f8fafc; border: 1px solid #e2e8f0; padding: 0.8rem; border-radius: 0.75rem; }
      .petcare-note { background: #eff6ff; border-left: 4px solid #2563eb; padding: 0.8rem 1rem; border-radius: 0.35rem; }
      .petcare-path { font-family: monospace; color: #1e3a8a; }
    </style>
    """,
    unsafe_allow_html=True,
)


def parse_response(response: requests.Response) -> Any:
    try:
        return response.json()
    except ValueError:
        return response.text


def api_request(
    method: str,
    path: str,
    token: str,
    *,
    payload: dict[str, Any] | None = None,
    params: dict[str, Any] | None = None,
    timeout: int = 20,
) -> requests.Response:
    return requests.request(
        method,
        f"{GATEWAY_URL}{path}",
        headers={"Authorization": f"Bearer {token}"},
        json=payload,
        params=params,
        timeout=timeout,
    )


def login(role: str) -> str:
    user = DEMO_USERS[role]
    response = requests.post(
        f"{KEYCLOAK_URL}/realms/petcare/protocol/openid-connect/token",
        data={
            "grant_type": "password",
            "client_id": "petcare-demo",
            "username": user["username"],
            "password": user["password"],
        },
        timeout=15,
    )
    response.raise_for_status()
    token = response.json()["access_token"]
    st.session_state[f"token_{role}"] = token
    return token


def token_for(role: str) -> str:
    token = st.session_state.get(f"token_{role}")
    if token:
        return token
    return login(role)


def parse_mcp_payload(response: requests.Response) -> dict[str, Any]:
    response.raise_for_status()
    content_type = response.headers.get("Content-Type", "")
    if "text/event-stream" not in content_type:
        return response.json()

    for line in response.text.splitlines():
        if line.startswith("data: "):
            return json.loads(line[6:])
    raise ValueError("MCP returned an event stream without a JSON data event.")


def mcp_request(token: str, method: str, params: dict[str, Any] | None, request_id: int) -> dict[str, Any]:
    response = requests.post(
        f"{GATEWAY_URL}/mcp",
        headers={
            "Authorization": f"Bearer {token}",
            "MCP-Protocol-Version": "2025-11-25",
            "Content-Type": "application/json",
            "Accept": "application/json, text/event-stream",
        },
        json={"jsonrpc": "2.0", "id": request_id, "method": method, "params": params or {}},
        timeout=30,
    )
    return parse_mcp_payload(response)


def show_http_result(response: requests.Response, expected: int | None = None) -> None:
    if expected is not None and response.status_code == expected:
        st.success(f"Expected HTTP {expected} received")
    elif response.ok:
        st.success(f"HTTP {response.status_code}")
    else:
        st.error(f"HTTP {response.status_code}")
    body = parse_response(response)
    if isinstance(body, (dict, list)):
        st.json(body)
    else:
        st.code(str(body))


st.title("🐾 PetCare Platform Demo")
st.caption("One interface for the Gateway workflow, real Appointment → Pet verification, Kafka notifications, and MCP tools.")

with st.sidebar:
    st.header("Demo access")
    selected_role = st.selectbox("Keycloak role", list(DEMO_USERS))
    if st.button("Sign in with Keycloak", use_container_width=True):
        try:
            login(selected_role)
            st.success(f"Signed in as {DEMO_USERS[selected_role]['username']}")
        except requests.RequestException as exc:
            st.error(f"Keycloak login failed: {exc}")

    if st.session_state.get(f"token_{selected_role}"):
        st.success("Token ready")
    st.divider()
    st.markdown(f"**UI:** [{UI_PUBLIC_URL}]({UI_PUBLIC_URL})")
    st.caption("Demo credentials are imported by the local Keycloak realm and are not production secrets.")

overview_tab, workflow_tab, mcp_tab, requirements_tab = st.tabs(
    ["System status", "Real workflow", "MCP playground", "Requirement evidence"]
)

with overview_tab:
    st.subheader("Running components")
    if st.button("Check all services", type="primary"):
        results: list[tuple[str, bool, str]] = []
        for name, url in SERVICE_HEALTH.items():
            try:
                response = requests.get(url, timeout=8)
                results.append((name, response.ok, f"HTTP {response.status_code}"))
            except requests.RequestException as exc:
                results.append((name, False, str(exc)))

        columns = st.columns(len(results))
        for column, (name, healthy, detail) in zip(columns, results):
            column.metric(name, "Healthy" if healthy else "Unavailable", detail)

        try:
            catalog = requests.get(f"{CONSUL_URL}/v1/catalog/services", timeout=8)
            catalog.raise_for_status()
            services = catalog.json()
            st.markdown("**Consul service registry**")
            st.json(services)
            required = {"pet-service", "appointment-service"}
            if required.issubset(services):
                st.success("Pet and Appointment services are registered in Consul.")
            else:
                st.warning(f"Missing Consul registrations: {sorted(required.difference(services))}")
        except requests.RequestException as exc:
            st.error(f"Consul check failed: {exc}")
    else:
        st.info("Use the button to query live health endpoints and the Consul catalog.")

with workflow_tab:
    st.subheader("Schedule an appointment through the real distributed path")
    st.markdown(
        '<div class="petcare-note"><span class="petcare-path">UI → Keycloak → Gateway → Appointment → Consul → Pet → Kafka → Treatment</span></div>',
        unsafe_allow_html=True,
    )

    try:
        owner_token = token_for("Owner")
    except requests.RequestException as exc:
        st.error(f"Owner login failed: {exc}")
        owner_token = ""

    if owner_token and st.button("Load demo pet and open slots"):
        pets_response = api_request("GET", f"/pet/owners/{DEMO_OWNER_ID}/pets", owner_token)
        slots_response = api_request("GET", "/appointment/slots", owner_token)
        if pets_response.ok and slots_response.ok:
            st.session_state["demo_pets"] = pets_response.json()
            st.session_state["demo_slots"] = slots_response.json()
            st.success("Demo data loaded through the Gateway")
        else:
            show_http_result(pets_response)
            show_http_result(slots_response)

    pets = st.session_state.get("demo_pets", [])
    slots = st.session_state.get("demo_slots", [])
    left, right = st.columns(2)
    with left:
        st.markdown("**Pet Service data**")
        if pets:
            st.dataframe(pets, use_container_width=True, hide_index=True)
        else:
            st.caption("Load the demo data first.")
    with right:
        st.markdown("**Appointment Service open slots**")
        if slots:
            st.dataframe(slots, use_container_width=True, hide_index=True)
        else:
            st.caption("Load the demo data first.")

    selected_slot_id = None
    if slots:
        def slot_label(slot: dict[str, Any]) -> str:
            return f"{slot.get('startsAtUtc', 'unknown time')} · {slot.get('availabilitySlotId', '')}"

        selected_slot = st.selectbox("Open slot", slots, format_func=slot_label)
        selected_slot_id = selected_slot.get("availabilitySlotId")

    schedule_column, rejection_column = st.columns(2)
    with schedule_column:
        if st.button("Schedule with real ownership verification", type="primary", disabled=not selected_slot_id):
            before_response = api_request(
                "GET", f"/treatment/api/notifications/owner/{DEMO_OWNER_ID}", owner_token
            )
            known_notification_ids = {
                item.get("id")
                for item in (before_response.json() if before_response.ok else [])
            }
            response = api_request(
                "POST",
                "/appointment/appointments",
                owner_token,
                payload={
                    "petId": DEMO_PET_ID,
                    "ownerId": DEMO_OWNER_ID,
                    "availabilitySlotId": selected_slot_id,
                    "reason": "Streamlit end-to-end demonstration",
                },
            )
            show_http_result(response, expected=201)
            if response.status_code == 201:
                st.session_state["last_appointment"] = response.json()
                st.session_state["demo_slots"] = [
                    slot for slot in slots if slot.get("availabilitySlotId") != selected_slot_id
                ]
                with st.status("Waiting for Kafka notification…", expanded=True) as status:
                    found = None
                    for attempt in range(1, 9):
                        notifications = api_request(
                            "GET", f"/treatment/api/notifications/owner/{DEMO_OWNER_ID}", owner_token
                        )
                        current = notifications.json() if notifications.ok else []
                        new_notifications = [
                            item
                            for item in current
                            if item.get("id") not in known_notification_ids
                        ]
                        if new_notifications:
                            found = new_notifications
                            break
                        st.write(f"Poll {attempt}/8")
                        time.sleep(1.5)
                    if found is not None:
                        status.update(label="Kafka event consumed by Treatment Service", state="complete")
                        st.json(found)
                    else:
                        status.update(label="No notification observed within 12 seconds", state="error")

    with rejection_column:
        if st.button("Prove invalid ownership is rejected", disabled=not selected_slot_id):
            response = api_request(
                "POST",
                "/appointment/appointments",
                owner_token,
                payload={
                    "petId": DEMO_PET_ID,
                    "ownerId": DEMO_SECOND_OWNER_ID,
                    "availabilitySlotId": selected_slot_id,
                    "reason": "Expected ownership rejection",
                },
            )
            show_http_result(response, expected=403)

with mcp_tab:
    st.subheader("Test the MCP server through Streamlit")
    st.caption("Requests use Streamable HTTP through `/mcp`; the MCP server forwards the selected caller's bearer token to the owning service.")
    mcp_role = st.selectbox("Caller role", list(DEMO_USERS), key="mcp_role")

    try:
        mcp_token = token_for(mcp_role)
    except requests.RequestException as exc:
        st.error(f"Keycloak login failed: {exc}")
        mcp_token = ""

    initialize_column, list_column = st.columns(2)
    with initialize_column:
        if st.button("Initialize MCP", disabled=not mcp_token, use_container_width=True):
            try:
                result = mcp_request(
                    mcp_token,
                    "initialize",
                    {
                        "protocolVersion": "2025-11-25",
                        "capabilities": {},
                        "clientInfo": {"name": "petcare-streamlit", "version": "1.0"},
                    },
                    1,
                )
                st.session_state["mcp_initialize"] = result
                st.json(result)
            except (requests.RequestException, ValueError) as exc:
                st.error(f"MCP initialization failed: {exc}")

    with list_column:
        if st.button("List MCP tools", disabled=not mcp_token, use_container_width=True):
            try:
                result = mcp_request(mcp_token, "tools/list", {}, 2)
                st.session_state["mcp_tools"] = result.get("result", {}).get("tools", [])
                st.json(result)
            except (requests.RequestException, ValueError) as exc:
                st.error(f"MCP tool discovery failed: {exc}")

    discovered_tools = st.session_state.get("mcp_tools", [])
    tool_names = [tool["name"] for tool in discovered_tools] or list(TOOL_ARGUMENTS)
    selected_tool = st.selectbox("Tool", tool_names)
    default_arguments = TOOL_ARGUMENTS.get(selected_tool, {})
    arguments_text = st.text_area(
        "Arguments (JSON)", value=json.dumps(default_arguments, indent=2), height=150
    )

    if st.button("Call selected MCP tool", type="primary", disabled=not mcp_token):
        try:
            arguments = json.loads(arguments_text)
            result = mcp_request(
                mcp_token,
                "tools/call",
                {"name": selected_tool, "arguments": arguments},
                3,
            )
            st.json(result)
        except json.JSONDecodeError as exc:
            st.error(f"Arguments are not valid JSON: {exc}")
        except (requests.RequestException, ValueError) as exc:
            st.error(f"MCP call failed: {exc}")

with requirements_tab:
    st.subheader("How this satisfies the MCP requirement")
    requirement_columns = st.columns(4)
    requirement_columns[0].markdown("**What it offers**\n\nPet, appointment, treatment, vaccination, and notification tools discovered with `tools/list`.")
    requirement_columns[1].markdown("**Communication**\n\nStreamable HTTP to MCP; authenticated REST from MCP to the owning microservice.")
    requirement_columns[2].markdown("**How it is tested**\n\nThis Streamlit playground initializes MCP, lists tools, and calls tools with real Keycloak tokens.")
    requirement_columns[3].markdown("**Architectural role**\n\nAn AI-facing adapter that preserves service boundaries and downstream authorization.")
    st.info("The repository also includes terminal/HTTP verification and automated MCP integration tests.")
