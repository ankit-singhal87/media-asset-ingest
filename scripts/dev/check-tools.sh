#!/usr/bin/env bash
set -u

required_tools="
bash
git
make
curl
jq
node
npm
docker
"

optional_tools="
dotnet
kubectl
helm
dapr
az
"

missing=0
optional_missing=0

check_tool() {
  tool="$1"
  label="$2"

  if command -v "$tool" >/dev/null 2>&1; then
    path=$(command -v "$tool")
    version=$("$tool" --version 2>/dev/null | head -n 1 || true)
    if [ -n "$version" ]; then
      printf 'ok       %-8s %-8s %s (%s)\n' "$label" "$tool" "$path" "$version"
    else
      printf 'ok       %-8s %-8s %s\n' "$label" "$tool" "$path"
    fi
  else
    printf 'missing  %-8s %-8s\n' "$label" "$tool"
    return 1
  fi
}

printf '%s\n' "Checking required development tools..."

for tool in $required_tools; do
  if ! check_tool "$tool" "required"; then
    missing=1
  fi
done

if [ "$missing" -ne 0 ]; then
  printf '\n%s\n' "One or more tools are missing."
  printf '%s\n' "Run: make install-tools"
  exit 1
fi

printf '\n%s\n' "Checking optional host tools..."

for tool in $optional_tools; do
  if ! check_tool "$tool" "optional"; then
    optional_missing=1
  fi
done

if [ "$optional_missing" -ne 0 ]; then
  printf '\n%s\n' "Optional tools are missing. This is acceptable for Docker-first workflows."
  printf '%s\n' "Run: make print-install-tools for manual guidance."
fi

printf '\n%s\n' "Required development tools are available."
