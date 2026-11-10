# CoffeeNChill Part 3 — implementation handoff

Branch: `feature/part3-secure-gateway`; draft PR #1. `main` is unchanged.

## Implemented in source
- Separate .NET 8 ASP.NET Core gateway with Swagger, XML documentation, JWT bearer validation
- `Users` Azure Table profiles and `UserEmailIndex` email reservations; Identity PBKDF2 password hashing
- Public Customer registration, login, opt-in Admin bootstrap, Admin-only Staff creation
- Role-protected menu writes, order status updates, staff document operations
- Public menu read, queue order submission and order status read
- Functions order status update with ETag optimistic concurrency and queue-worker coordination
- Azure Files document deletion, as well as existing upload/list/download
- Docker Compose gateway on port 5000, private Functions network, gateway Dockerfile
- GitHub Actions build validation and separate manual/tag Docker Hub publishing
- Part 3 Postman collection, gateway smoke script, deployment guide, README pointers

## Must verify before merging or submitting
1. Latest GitHub Actions build, JSON, Compose and smoke script syntax checks are green.
2. End-to-end gateway tests with Azurite: register, login, role-based 401/403, menu CRUD, order queue/status, staff creation.
3. Real Azure Files integration for staff documents; unavailable under Azurite.
4. Docker Hub images published and VPS Compose pulls/starts successfully.
5. Public Swagger URL, VPS firewall, TLS for real credentials, and Part 3 demonstration video.
6. Verify Admin bootstrap variables removed after account creation.
7. Add actual public VPS URL and Part 3 video URL to README.

## Known limitations / hardening
- Email reservations in `UserEmailIndex` are intentionally retained if profile creation fails; manual cleanup may be needed.
- Public order status contains customer information. Consider requiring an order-specific access token for a real service.
- JWTs are valid until expiration; no token revocation.
- The gateway forwards requests to an unauthenticated Functions backend on a private Docker network. Never publish that backend directly.
- Gateway public Swagger and HTTP port are for coursework demonstration, not hardened Internet production.
- Tests are not yet confirmed end-to-end; a successful compilation is insufficient.
