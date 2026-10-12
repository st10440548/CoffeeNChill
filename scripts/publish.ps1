param([Parameter(Mandatory=$true)][string]$DockerHubUsername)
$ErrorActionPreference = "Stop"
function Check { if ($LASTEXITCODE -ne 0) { throw "Docker command failed" } }
docker login; Check
docker build -t "$DockerHubUsername/coffeenchill-functions:v2.0" .; Check
docker push "$DockerHubUsername/coffeenchill-functions:v2.0"; Check
docker pull mcr.microsoft.com/azure-storage/azurite:3.33.0; Check
docker tag mcr.microsoft.com/azure-storage/azurite:3.33.0 "$DockerHubUsername/coffeenchill-azurite:v1.0"; Check
docker push "$DockerHubUsername/coffeenchill-azurite:v1.0"; Check
