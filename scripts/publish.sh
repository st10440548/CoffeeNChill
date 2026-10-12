#!/usr/bin/env bash
set -euo pipefail
: "${DOCKERHUB_USERNAME:?Export your Docker Hub username first}"
docker login
docker build --platform linux/amd64 -t "$DOCKERHUB_USERNAME/coffeenchill-functions:v2.0" .
docker push "$DOCKERHUB_USERNAME/coffeenchill-functions:v2.0"
docker pull mcr.microsoft.com/azure-storage/azurite:3.33.0
docker tag mcr.microsoft.com/azure-storage/azurite:3.33.0 "$DOCKERHUB_USERNAME/coffeenchill-azurite:v1.0"
docker push "$DOCKERHUB_USERNAME/coffeenchill-azurite:v1.0"
