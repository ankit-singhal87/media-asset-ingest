#!/usr/bin/env bash
set -eu

solution="MediaIngest.sln"
sdk_image="${DOTNET_SDK_IMAGE:-mcr.microsoft.com/dotnet/sdk:10.0}"
repo_cache_dir="${DOTNET_REPO_CACHE_DIR:-.cache/dotnet}"

foundation_tests="tests/MediaIngest.Foundation.Tests/MediaIngest.Foundation.Tests.csproj"
contracts_tests="tests/MediaIngest.Contracts.Tests/MediaIngest.Contracts.Tests.csproj"
watcher_tests="tests/MediaIngest.Worker.Watcher.Tests/MediaIngest.Worker.Watcher.Tests.csproj"
persistence_tests="tests/MediaIngest.Persistence.Tests/MediaIngest.Persistence.Tests.csproj"
outbox_tests="tests/MediaIngest.Worker.Outbox.Tests/MediaIngest.Worker.Outbox.Tests.csproj"
workflow_tests="tests/MediaIngest.Workflow.Tests/MediaIngest.Workflow.Tests.csproj"
observability_tests="tests/MediaIngest.Observability.Tests/MediaIngest.Observability.Tests.csproj"

all_test_projects="$foundation_tests $contracts_tests $watcher_tests $persistence_tests $outbox_tests $workflow_tests $observability_tests"
test_projects=""
scope="selected"

append_project() {
  if [ -z "$test_projects" ]; then
    test_projects="$1"
  else
    test_projects="$test_projects $1"
  fi
}

if [ "$#" -eq 0 ] || [ "${1:-}" = "all" ]; then
  test_projects="$all_test_projects"
  scope="all"
else
  for target in "$@"; do
    case "$target" in
      foundation)
        append_project "$foundation_tests"
        ;;
      contracts)
        append_project "$contracts_tests"
        ;;
      watcher)
        append_project "$watcher_tests"
        ;;
      persistence)
        append_project "$persistence_tests"
        ;;
      outbox)
        append_project "$outbox_tests"
        ;;
      workflow)
        append_project "$workflow_tests"
        ;;
      observability)
        append_project "$observability_tests"
        ;;
      *)
        printf 'Unknown .NET test target: %s\n' "$target"
        printf '%s\n' "Supported targets: all foundation contracts watcher persistence outbox workflow observability"
        exit 2
        ;;
    esac
  done
fi

export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=true
export DOTNET_NOLOGO=1

if command -v dotnet >/dev/null 2>&1; then
  if [ "$scope" = "all" ]; then
    dotnet restore "$solution"
    dotnet build "$solution" --no-restore
  else
    for test_project in $test_projects; do
      dotnet restore "$test_project"
      dotnet build "$test_project" --no-restore
    done
  fi
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

mkdir -p "$repo_cache_dir/cli-home" "$repo_cache_dir/nuget-packages"

if [ "$scope" = "all" ]; then
  dotnet_command="dotnet restore '$solution' && dotnet build '$solution' --no-restore && for test_project in $test_projects; do dotnet run --project \"\$test_project\" --no-restore; done"
else
  dotnet_command="for test_project in $test_projects; do dotnet restore \"\$test_project\" && dotnet build \"\$test_project\" --no-restore && dotnet run --project \"\$test_project\" --no-restore; done"
fi

docker run --rm \
  -u "$(id -u):$(id -g)" \
  -e DOTNET_CLI_HOME=/workspace/"$repo_cache_dir"/cli-home \
  -e DOTNET_CLI_TELEMETRY_OPTOUT=1 \
  -e DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=true \
  -e DOTNET_NOLOGO=1 \
  -e HOME=/workspace/"$repo_cache_dir"/cli-home \
  -e NUGET_PACKAGES=/workspace/"$repo_cache_dir"/nuget-packages \
  -v "$(pwd):/workspace" \
  -w /workspace \
  "$sdk_image" \
  sh -c "$dotnet_command"
