# Part 3 local integration verification — 2026-11-10

Environment: Docker Desktop on Apple Silicon; Functions and Gateway built for `linux/amd64`; Azurite local storage; gateway published on local port `5080` due to a port 5000 conflict.

## Verified from Terminal output

- Docker Compose configuration valid and three services started; Azurite healthy.
- Gateway `GET /health`: HTTP 200, `{"status":"ok"}`.
- Existing `scripts/part3-smoke.sh`: nine passing checks covering health, public menu, anonymous write rejection (401), customer registration (201), customer login (200), customer forbidden menu write (403), customer forbidden Staff creation (403), and invalid-password rejection (401).
- Bootstrap Admin login: HTTP 200 with JWT issued.
- Admin creates Staff: HTTP 201; new Staff login: HTTP 200.
- Admin menu CRUD: create HTTP 201; public category read HTTP 200; update HTTP 200; delete HTTP 204.

## Reproducible test commands

From the repository root, with Docker Compose running and local `.env` populated:

```bash
BASE_URL=http://localhost:5080 bash scripts/part3-smoke.sh
BASE_URL=http://localhost:5080 python3 scripts/part3-admin-staff-test.py
BASE_URL=http://localhost:5080 python3 scripts/part3-menu-crud-test.py
BASE_URL=http://localhost:5080 python3 scripts/part3-order-queue-test.py
BASE_URL=http://localhost:5080 python3 scripts/part3-staff-permissions-test.py
```

The Admin/Staff test creates a unique temporary-named Staff account that persists in the local user table. The menu test removes its own test item. All three committed Python scripts were rerun successfully from their repository paths on 2026-11-10. The Admin/Staff script passed Admin login, Staff creation and Staff login; the menu CRUD script passed create/read/update/delete; the queue script observed Received, Preparing, Ready and Collected after HTTP 202 acceptance.

## Order queue verified

- On 2026-11-10 POST /api/orders/queue returned HTTP 202; subsequent GET requests observed Received → Preparing → Ready → Collected.

## Not yet verified
- Staff permissions: verified on 2026-11-10. Staff authenticated, created/updated/deleted menu item (201/200/204) and updated an order status (200). Customer menu writes and order status updates were denied (403).
- Azure Files document operations (require a real Azure Files connection string; Azurite does not emulate Azure Files).
- Docker Hub publication and VPS deployment.

No secrets or JWT values are included in this report. Local `.env` is not committed. Keep `main` unchanged until remaining verification and review.

## Azure VM deployment verified — 2026-11-10

- Azure for Students VM `CoffeeNChill-VM`, Ubuntu 24.04 x64, South Africa North, running.
- Docker Engine 29.1.3 and Docker Compose 2.40.3 installed.
- Docker Hub images `primetimeron/coffeenchill-functions:part3` and `primetimeron/coffeenchill-gateway:part3` successfully published and pulled on VM.
- Initial Azurite image was ARM64-only; server-local Compose override selected `mcr.microsoft.com/azure-storage/azurite:latest` on `linux/amd64`.
- `sudo docker compose ps`: Azurite healthy; Functions and Gateway running.
- VM-local `GET http://localhost:5000/health`: HTTP 200 with `{\"status\":\"ok\"}`.
- From a separate Mac terminal, public `GET http://172.209.216.237:5000/health`: HTTP 200 with `{\"status\":\"ok\"}`.
- Public `GET http://172.209.216.237:5000/api/menu`: HTTP 200 with empty JSON array, expected for fresh storage.

The public port 5000 endpoint is plain HTTP and should be used for non-sensitive coursework demonstrations only; configure TLS before sending credentials or tokens over the network. Azure Files functionality and live-server RBAC/order tests remain unverified. The server-local Compose override and `.env` are not committed.
