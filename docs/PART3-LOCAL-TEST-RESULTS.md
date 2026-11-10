# Part 3 local integration verification — 2026-11-10

Environment: Docker Desktop on Apple Silicon; Functions and Gateway built for `linux/amd64`; Azurite local storage; gateway published on local port `5080` due to a port 5000 conflict.

## Verified manually from Terminal output

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
```

The Admin/Staff test creates a unique temporary-named Staff account that persists in the local user table. The menu test removes its own test item. These new scripts were committed after equivalent manual checks passed; they have not yet been rerun from their committed paths. The order-queue test script likewise reproduces a manually passed test.

## Not yet verified

- Order queue processing and status transitions: verified on 2026-11-10. POST /api/orders/queue returned HTTP 202, followed by observed Received → Preparing → Ready → Collected states via GET /api/orders/{orderDate}/{orderId}.
- Authorized Staff menu operations and Staff order-status updates.
- Azure Files document operations (require a real Azure Files connection string; Azurite does not emulate Azure Files).
- Docker Hub publication and VPS deployment.

No secrets or JWT values are included in this report. Local `.env` is not committed. Keep `main` unchanged until remaining verification and review.
