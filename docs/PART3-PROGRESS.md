# Part 3 implementation progress

Branch: `feature/part3-secure-gateway`. Do not merge into main until validated.

## Implemented (requires build and integration verification)
- Separate .NET 8 ASP.NET Core gateway, Swagger/OpenAPI and XML comments
- Azure Table `Users`, Customer registration and PBKDF2 password hashing via ASP.NET Identity
- JWT login with Name, Email and Role claims
- Role-based restrictions for menu writes and document endpoints
- Anonymous menu reading and order submission
- Gateway Dockerfile and GitHub Actions build validation

## Outstanding
- Verify staff-only order status update and queue-worker concurrency under integration tests
- Verify document deletion backend and forwarding with real Azure Files
- Robust role provisioning for initial staff/admin accounts (never through public registration)
- Race-safe uniqueness across Azure Table role partitions
- Integration tests, Postman Part 3 collection, build checks and fixes
- Verify Docker Compose gateway integration and harden deployment configuration
- Docker Hub publishing, VPS provisioning, deployment, live validation and demonstration
- Production readiness: rate limiting, error handling, upstream timeouts, request validation, safe Swagger exposure

## Configuration
Set `Jwt__Key` to a cryptographically random secret of at least 32 bytes, `AzureWebJobsStorage` to the Azure Table connection string, and `Functions__BaseUrl` to the private Functions URL.
Never commit secrets. Current code is an implementation starting point, not production-ready.
