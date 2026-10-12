# Submission and demo checklist

## GitHub

Create a GitHub repository under your account. Extract the ZIP, open its CoffeeNChill folder, and use GitHub Desktop to add it as a local repository (create a repository there if needed), then publish it. Invite team members as collaborators. Each person must contribute and commit from their own account. Submit the repository URL, not the ZIP.

Before pushing, confirm `.env`, `.env.standalone`, real storage keys, bin/ and obj/ are excluded. README and scripts contain the public emulator key only.

## Meaningful work plan

Use actual incremental changes and checks. Tasks that can support meaningful contributions include implementing/refining queue input validation; adding the queue producer; implementing status processing and retry handling; configuring and verifying Compose/Docker Hub images; building/running the Postman tests; documenting observed test results and troubleshooting. The supplied source is a starting point, not proof of individual participation. Do not turn a completed project into fake commit history.

## Demo sequence (your own voice)

1. Introduce members and explain the peak-hour CoffeeNChill problem.
2. Show both public Docker Hub repositories and image tags.
3. Show Compose `image:` references and connection-string service hostname.
4. Run `docker compose up` and show both services running.
5. Explain POST /api/orders/queue: JSON becomes a queue message and HTTP 202 returns before preparation ends.
6. Run the Postman Core folder with 1000ms delay; show results.
7. Show Functions logs progressing Received, Preparing, Ready, Collected.
8. Show the Orders table in Storage Explorer with PartitionKey = UTC order date, RowKey = OrderId, and timestamps. Storage Explorer uses localhost ports: Blob 10000, Queue 10001, Table 10002.
9. Explain QueueTrigger, Base64 encoding, retries and poison queue.
10. Show GitHub history, actual contributions and README video link.

Every part requires a video showing the running app with your voice explaining the architecture and code. No AI-generated voices are permitted by the brief.

## Must pass before submission

- `docker build` completes without compilation errors.
- Public Docker Hub image pulls work.
- `docker compose config` resolves your username; `docker compose up` starts both services.
- Collection Runner Core assertions pass (including persisted transition timestamps).
- If Part 1 file evidence is required: configure real Azure Files, run document tests and demonstrate standalone `docker run`.
- README has actual member details, public image/repository links and an actual unlisted YouTube URL.

## Troubleshooting

Connection refused: check containers/logs and the Azurite hostname/ports. UseDevelopmentStorage=true is only for noncontainer host-local development.

Unauthorized file requests: check actual AzureFilesConnectionString and account key. Azurite does not implement Azure Files.

Apple Silicon: if an image platform is incompatible with the Functions base image, build using `docker build --platform linux/amd64 ...` and add `platform: linux/amd64` to the functions service. Verify by running it before recording.

Order 404 immediately after enqueue: the worker has not written it yet; rerun/poll. If it remains missing, check Functions logs and `order-processing-queue-poison`.
