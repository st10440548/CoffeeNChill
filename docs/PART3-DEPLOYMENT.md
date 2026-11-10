# CoffeeNChill Part 3 — gateway deployment and verification

Development branch: `feature/part3-secure-gateway`. Do not merge into `main` before verification.

## Architecture

Public client -> ASP.NET Core gateway (:5000) -> private Azure Functions service -> Azurite Tables/Queues. The gateway also accesses the `Users` and `UserEmailIndex` Azure Tables. Azure Files requires a real Azure Storage account; Azurite does not implement Azure File Shares. The Functions HTTP service is only exposed inside the Docker network.

## Prerequisites

- Docker Engine + Docker Compose on a Linux VPS (or Docker Desktop for local checks)
- Public Docker Hub images `<username>/coffeenchill-azurite:v1.0`, `<username>/coffeenchill-functions:part3`, and `<username>/coffeenchill-gateway:part3`
- An open TCP port 5000 on the VPS, preferably HTTPS through a reverse proxy in a real deployment
- A real Azure Files connection string for document upload/list/download/delete tests

## Build and publish images

On a computer with Docker:

```bash
docker build --platform linux/amd64 -t YOUR_USERNAME/coffeenchill-functions:part3 -f Dockerfile .
docker build --platform linux/amd64 -t YOUR_USERNAME/coffeenchill-gateway:part3 -f Gateway/Dockerfile .
docker push YOUR_USERNAME/coffeenchill-functions:part3
docker push YOUR_USERNAME/coffeenchill-gateway:part3
```

Alternatively, configure GitHub repository secrets `DOCKERHUB_USERNAME` and `DOCKERHUB_TOKEN`, then run the **Publish Part 3 Docker images** workflow manually. This does **not** deploy to the VPS. Publish Azurite v1.0 from the previous Part 2 workflow if not already available.

## VPS configuration

```bash
git clone https://github.com/st10440548/CoffeeNChill.git
cd CoffeeNChill
git checkout feature/part3-secure-gateway
cp .env.example .env
openssl rand -hex 32
```

Edit `.env` with `DOCKERHUB_USERNAME`, a newly generated `JWT_SECRET` (64 hex characters), `BOOTSTRAP_ADMIN_EMAIL`, and a strong `BOOTSTRAP_ADMIN_PASSWORD` (12+ characters). Optional: `AZURE_FILES_CONNECTION_STRING` for real Azure Files. Never commit the .env file.

```bash
docker compose config
docker compose pull
docker compose up -d
docker compose ps
docker compose logs --tail=100 gateway functions
curl -i http://localhost:5000/health
```

Swagger: `http://YOUR_VPS_PUBLIC_IP:5000/swagger`. This example uses HTTP for coursework demonstration; use TLS for real credentials and production traffic. Do not expose real admin credentials or JWTs in a public video.

After successfully creating and logging in as the administrator, **remove the two BOOTSTRAP_ADMIN_* variables from .env** and recreate the gateway container (`docker compose up -d --force-recreate gateway`). Initial admin bootstrap should not remain enabled indefinitely.

## Role and endpoint matrix

| Endpoint | Anonymous | Customer | Staff | Admin |
| --- | --- | --- | --- | --- |
| GET /api/menu | yes | yes | yes | yes |
| POST /api/orders/queue | yes | yes | yes | yes |
| POST /api/auth/register | yes | yes | yes | yes |
| POST /api/auth/login | yes | yes | yes | yes |
| POST/PUT/DELETE /api/menu | no | no | yes | yes |
| PUT /api/orders/status | no | no | yes | yes |
| GET/POST/DELETE /api/documents | no | no | yes | yes |
| POST /api/users/staff | no | no | no | yes |

`GET /api/orders/{orderDate}/{orderId}` is public in this implementation. Restrict it if customer order details are sensitive.

## Postman verification

Import `docs/CoffeeNChill-Part3.postman_collection.json`. Set `baseUrl` to the VPS or local gateway URL. Set unique customer/staff emails and passwords, and admin credentials matching bootstrap values. Set `orderDate` to the UTC date portion of the queued order timestamp. Run requests in folder order. The `customerToken`, `staffToken`, and `adminToken` variables are populated by login tests. Repeating registration or menu creation may return 409.

Verify anonymous read/queue (200/202), customer registration (201), invalid login (401), Admin/Staff access (200/201/204), anonymous write denial (401), Customer write denial (403), and Staff-only order updates. Document requests may return 503 until Azure Files is configured. Status updates may return 404 until the queue worker persists the order, or 409 if the order has already advanced beyond the requested status.

## Evidence checklist for coursework

- Screenshot of VPS Docker Compose services running
- Swagger showing XML-described routes and bearer authorization
- Registration/login with JWT and role claims
- Customer request denied with 403 for staff-only route
- Staff/Admin authorized menu update and order status
- Azure Table `Users` with hashed (not plaintext) passwords
- Postman collection test results
- Public Swagger URL, GitHub URL, and new Part 3 YouTube video URL in README

## Outstanding environmental verification

No live VPS, Docker daemon, real Azure Files account, Docker Hub publishing, or end-to-end Postman run has been performed by this document. Confirm all of these before claiming completion.
