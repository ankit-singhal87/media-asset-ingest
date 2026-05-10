#!/usr/bin/env bash
set -eu

solution="MediaIngest.slnx"
sdk_image="${DOTNET_SDK_IMAGE:-mcr.microsoft.com/dotnet/sdk:10.0}"
repo_cache_dir="${DOTNET_REPO_CACHE_DIR:-.cache/dotnet}"

contracts_tests="tests/MediaIngest.Contracts.Tests/MediaIngest.Contracts.Tests.csproj"
watcher_tests="tests/MediaIngest.Worker.Watcher.Tests/MediaIngest.Worker.Watcher.Tests.csproj"
api_tests="tests/MediaIngest.Api.Tests/MediaIngest.Api.Tests.csproj"
essence_tests="tests/MediaIngest.Essence.Tests/MediaIngest.Essence.Tests.csproj"
persistence_tests="tests/MediaIngest.Persistence.Tests/MediaIngest.Persistence.Tests.csproj"
outbox_tests="tests/MediaIngest.Worker.Outbox.Tests/MediaIngest.Worker.Outbox.Tests.csproj"
workflow_tests="tests/MediaIngest.Workflow.Tests/MediaIngest.Workflow.Tests.csproj"
observability_tests="tests/MediaIngest.Observability.Tests/MediaIngest.Observability.Tests.csproj"
command_runner_tests="tests/MediaIngest.Worker.CommandRunner.Tests/MediaIngest.Worker.CommandRunner.Tests.csproj"
local_file_system_watcher_tests="tests/MediaIngest.Worker.LocalFileSystemWatcher.Tests/MediaIngest.Worker.LocalFileSystemWatcher.Tests.csproj"

all_test_projects="$contracts_tests $watcher_tests $api_tests $essence_tests $persistence_tests $outbox_tests $workflow_tests $observability_tests $command_runner_tests $local_file_system_watcher_tests"
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
      contracts)
        append_project "$contracts_tests"
        ;;
      watcher)
        append_project "$watcher_tests"
        ;;
      api)
        append_project "$api_tests"
        ;;
      essence)
        append_project "$essence_tests"
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
      command-runner)
        append_project "$command_runner_tests"
        ;;
      local-file-system-watcher)
        append_project "$local_file_system_watcher_tests"
        ;;
      *)
        printf 'Unknown .NET test target: %s\n' "$target"
        printf '%s\n' "Supported targets: all contracts watcher api essence persistence outbox workflow observability command-runner local-file-system-watcher"
        exit 2
        ;;
    esac
  done
fi

export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=true
export DOTNET_NOLOGO=1

mkdir -p "$repo_cache_dir/cli-home" "$repo_cache_dir/nuget-packages"

export DOTNET_CLI_HOME="$(pwd)/$repo_cache_dir/cli-home"
export HOME="$(pwd)/$repo_cache_dir/cli-home"
export NUGET_PACKAGES="$(pwd)/$repo_cache_dir/nuget-packages"

if command -v dotnet >/dev/null 2>&1; then
  if [ "$scope" = "all" ]; then
    dotnet restore "$solution" -m:1
    dotnet build "$solution" --no-restore -m:1
  else
    for test_project in $test_projects; do
      dotnet restore "$test_project" -m:1
      dotnet build "$test_project" --no-restore -m:1
    done
  fi
  for test_project in $test_projects; do
    dotnet run --project "$test_project" --no-build
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

if [ "$scope" = "all" ]; then
  dotnet_command="dotnet restore '$solution' -m:1 && dotnet build '$solution' --no-restore -m:1 && for test_project in $test_projects; do dotnet run --project \"\$test_project\" --no-build; done"
else
  dotnet_command="for test_project in $test_projects; do dotnet restore \"\$test_project\" -m:1 && dotnet build \"\$test_project\" --no-restore -m:1 && dotnet run --project \"\$test_project\" --no-build; done"
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
