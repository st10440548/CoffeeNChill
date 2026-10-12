# CoffeeNChill — CLDV6212 Parts 1 and 2

C# .NET 8 isolated Azure Functions for a campus canteen. Part 1 stores menu items in Azure Tables and staff documents in Azure Files. Part 2 immediately accepts orders with HTTP 202, queues them in `order-processing-queue`, and processes them with a background queue trigger. Each order is stored in `Orders` with its UTC order date as PartitionKey and OrderId as RowKey. Status progresses Received → Preparing → Ready → Collected, with timestamps retained for evidence.

## Start here

This source package has been statically checked, but **has not been compiled or run** in its generation environment (no .NET SDK or Docker was available). Build it and run the Postman collection before submitting. Do not submit the ZIP: submit your GitHub repository link.

Install Docker Desktop and Postman. Run commands from this folder. Docker builds the .NET app, so the .NET SDK is optional on your host. A local Functions run additionally needs .NET 8 SDK and Azure Functions Core Tools v4.

1. Copy `.env.example` to `.env` and replace `yourdockerhubusername` with your actual username.
2. Make your two Docker Hub repositories public: `coffeenchill-functions` and `coffeenchill-azurite`.
3. Build and publish the images with either:

```powershell
./scripts/publish.ps1 -DockerHubUsername YOUR_USERNAME
```

```bash
export DOCKERHUB_USERNAME=YOUR_USERNAME
bash scripts/publish.sh
```

4. Start the published images:

```bash
docker compose pull
docker compose up -d
docker compose logs -f functions
```

Compose contains `image:` references to your Docker Hub images and no `build:` entries, meeting the Part 2 marking rule. It uses a Docker network endpoint for Azurite; `UseDevelopmentStorage=true` would point at the Functions container itself and would fail here.

5. Import `docs/CoffeeNChill.postman_collection.json`. Select **Core - menu and asynchronous orders**, run its requests in order using Collection Runner, with **1000 ms delay**. The collection creates unique IDs, tests menu CRUD and queues an order, then polls until Collected. Check runner results: all final assertions must pass. Use functions logs and Azure Storage Explorer to show intermediate states and the Orders table. Polling can return 404 briefly before the queue worker creates the order.
6. Add actual team names/contributions and your unlisted YouTube demo link below. Push all source to GitHub and submit the repository URL.

## Azure Files limitation in the assignment

Azurite supports Blob, Queue and Table services, **not Azure File Shares**. The brief also swaps some port labels; actual ports are Blob 10000, Queue 10001, Table 10002. This implementation uses those real ports.

The staff document functions use the real Azure Files SDK and `staff-docs` share. Set `AZURE_FILES_CONNECTION_STRING` in `.env` to a real Azure Storage account connection string to use them, then recreate the Functions container with `docker compose up -d --force-recreate functions`. Without this setting, the document endpoints return a clear HTTP 503 instead of pretending a Blob container is a File Share. Part 2 menu/order functionality works entirely locally without Azure Files. Ask your lecturer how they want the incompatible Part 1 emulator/file-share requirement handled if a real storage account is unavailable.

For document tests, select `docs/sample-staff-document.txt` in the upload request's file field, configure Postman's working directory if necessary, and run the Documents folder. Never commit real credentials, `.env`, or real-storage local.settings.json.

## Part 1 standalone Docker execution (no Compose)

Build a Part 1-labelled image if Part 1 evidence is also required:

```bash
docker build -t YOUR_USERNAME/coffeenchill-functions:v1.0 .
docker push YOUR_USERNAME/coffeenchill-functions:v1.0
docker network create coffeenchill-net
docker run -d --name azurite --network coffeenchill-net -p 127.0.0.1:10000:10000 -p 127.0.0.1:10001:10001 -p 127.0.0.1:10002:10002 YOUR_USERNAME/coffeenchill-azurite:v1.0 azurite --blobHost 0.0.0.0 --queueHost 0.0.0.0 --tableHost 0.0.0.0 --skipApiVersionCheck
```

Copy `.env.standalone.example` to `.env.standalone`. Set `AzureFilesConnectionString` there if testing documents; keep this file out of Git. Run:

```bash
docker run -d --name coffeenchill-functions --network coffeenchill-net -p 127.0.0.1:7071:80 --env-file .env.standalone YOUR_USERNAME/coffeenchill-functions:v1.0
```

The v1.0 command packages the same combined source; it does not manufacture a historical Part 1 implementation. Stop/remove the standalone containers before starting Compose, since they bind the same ports.

## Local development

Start Azurite separately; copy `local.settings.json.example` to `local.settings.json` (the supplied local settings also work). Run `dotnet build` and `func start`. `UseDevelopmentStorage=true` is appropriate when both host processes run on your computer. Do not combine that shortcut with separate containers.

## Endpoints

| Method | Route | Purpose |
|---|---|---|
| POST | /api/menu | Add menu item |
| GET | /api/menu | List menu |
| GET | /api/menu/category/{category} | Filter category |
| PUT | /api/menu/{category}/{id} | Update price / availability |
| DELETE | /api/menu/{category}/{id} | Delete menu item |
| POST | /api/documents/upload | Multipart file field to Azure Files |
| GET | /api/documents | File names, sizes, modification dates |
| GET | /api/documents/download/{fileName} | Download file |
| POST | /api/orders/queue | Enqueue order, return 202 |
| GET | /api/orders/{orderDate}/{orderId} | Inspect persisted status |

Sample order matches the supplied specification. The collection supplies a current ISO-8601 timestamp automatically:

```json
{"OrderId":"ORD-2026-8801","CustomerName":"Jane Smith","SelectedItemSKUs":["COF-001","PAS-104"],"TotalPrice":65.0,"OrderTimestamp":"2026-10-12T10:15:00Z"}
```

Menu JSON uses `Category`, `Id`, `Name`, `Description`, `Price`, `IsAvailable`. Category/Id map to Table PartitionKey/RowKey. SelectedItemSKUs are stored as JSON text because Table Storage cannot store string arrays directly. Prices use Double, as allowed by the brief. This simulation accepts supplied order totals and SKUs; it does not recalculate totals or check inventory.

## Reliability and simulation boundaries

Queue producer and trigger agree on Base64 encoding. The trigger resumes an existing matching order from its stored state on retry, and uses ETags to avoid overwriting concurrent changes. Duplicate messages can be delivered; this is at-least-once processing, not exactly-once delivery. Reusing an ID/date with different order details is rejected by the worker and eventually routed to `order-processing-queue-poison` after five failed attempts. Check logs and the poison queue if an order stalls.

Each transition waits three seconds by default. The automatic Collected transition is explicitly a simulation required by the brief; it is not confirmation of a real customer collection. Status timestamps let tests prove all stages occurred even if a GET misses a short-lived intermediate stage. HTTP functions are anonymous and ports are bound to localhost for this local coursework demonstration. JWT/API gateway/VPS work belongs to Part 3.

## Part 2 changelog

- Added POST order producer and Base64 JSON queue messages.
- Added ProcessOrderQueue background trigger with status transitions and retry handling.
- Added Orders table and GET status endpoint for demonstrations and tests.
- Added Docker Compose pulling published v2.0 Functions and v1.0 Azurite images.
- Updated Postman collection, setup guide and demo checklist.

## Team contributions — replace before submission

| Member / student number | Actual work completed | Relevant commits |
|---|---|---|
| ADD REAL MEMBER | ADD VERIFIED CONTRIBUTIONS | ADD COMMIT LINKS |

Each student must make at least five meaningful commits per part using their own GitHub account. Record actual contributions; do not invent authors, dates or empty commits. See `docs/SUBMISSION.md` for a task breakdown and video checklist.



Unlisted YouTube URL: ****

## References

- https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide
- https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite
- https://learn.microsoft.com/en-us/azure/storage/common/storage-connect-azurite
