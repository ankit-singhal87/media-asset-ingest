#!/usr/bin/env bash
set -eu

solution="MediaIngest.sln"
test_projects="tests/MediaIngest.Foundation.Tests/MediaIngest.Foundation.Tests.csproj tests/MediaIngest.Contracts.Tests/MediaIngest.Contracts.Tests.csproj tests/MediaIngest.Worker.Watcher.Tests/MediaIngest.Worker.Watcher.Tests.csproj tests/MediaIngest.Persistence.Tests/MediaIngest.Persistence.Tests.csproj tests/MediaIngest.Worker.Outbox.Tests/MediaIngest.Worker.Outbox.Tests.csproj"
sdk_image="${DOTNET_SDK_IMAGE:-mcr.microsoft.com/dotnet/sdk:10.0}"

export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=true
export DOTNET_NOLOGO=1

if command -v dotnet >/dev/null 2>&1; then
  dotnet restore "$solution"
  dotnet build "$solution" --no-restore
  for test_project in $test_projects; do
    dotnet run --project "$test_project" --no-restore
  done
  exit 0
fi

if ! command -v docker >/dev/null 2>&1; then
  printf '%s\n' "Neither dotnet nor docker is available. Install dotnet or Docker, then rerun make test-dotnet."
  exit 1
fi

if ! docker info >/dev/null 2>&1; then
  printf '%s\n' "Docker is installed, but the daemon is not reachable. Start Docker or install dotnet, then rerun make test-dotnet."
  exit 1
fi

docker run --rm \
  -u "$(id -u):$(id -g)" \
  -e DOTNET_CLI_HOME=/tmp \
  -e DOTNET_CLI_TELEMETRY_OPTOUT=1 \
  -e DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=true \
  -e DOTNET_NOLOGO=1 \
  -e HOME=/tmp \
  -v "$(pwd):/workspace" \
  -w /workspace \
  "$sdk_image" \
  sh -c "dotnet restore '$solution' && dotnet build '$solution' --no-restore && for test_project in $test_projects; do dotnet run --project \"\$test_project\" --no-restore; done"
