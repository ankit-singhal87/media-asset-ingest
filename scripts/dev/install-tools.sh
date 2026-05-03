#!/usr/bin/env bash
set -eu

if [ "$(uname -s)" != "Linux" ]; then
  printf '%s\n' "This bootstrap script supports Linux only."
  exit 1
fi

if [ -r /etc/os-release ]; then
  . /etc/os-release
else
  printf '%s\n' "Cannot detect Linux distribution because /etc/os-release is missing."
  exit 1
fi

case "${ID:-}" in
  ubuntu|debian)
    ;;
  *)
    printf 'Unsupported distribution: %s\n' "${PRETTY_NAME:-unknown}"
    printf '%s\n' "Install the tools listed in docs/standards/tooling.md manually."
    exit 1
    ;;
esac

if [ "${1:-}" = "--print-only" ]; then
  mode="print"
else
  mode="install"
fi

print_manual_notes() {
  cat <<'EOF'

Manual follow-up may still be required:
- Docker may require adding your user to the docker group and starting a new shell.
- Azure CLI requires `az login` before cloud use.
- Dapr requires `dapr init` before local Dapr runtime use.
- kubectl requires a kubeconfig before cluster access.
- This script does not create paid Azure resources.
EOF
}

print_commands() {
  cat <<'EOF'
sudo apt-get update
sudo apt-get install -y ca-certificates curl gnupg lsb-release apt-transport-https make jq nodejs npm

# Optional .NET SDK:
# Prefer SDK containers through Makefile targets once available. Install
# locally only if you want host-native dotnet commands.

# Docker CLI installation:
# Follow Docker Engine apt repository instructions if your distro package is too old.

# Optional kubectl:
curl -LO "https://dl.k8s.io/release/$(curl -Ls https://dl.k8s.io/release/stable.txt)/bin/linux/amd64/kubectl"
sudo install -o root -g root -m 0755 kubectl /usr/local/bin/kubectl
rm -f kubectl

# Optional Helm:
curl https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-3 | bash

# Optional Dapr CLI:
curl -fsSL https://raw.githubusercontent.com/dapr/cli/master/install/install.sh | /bin/bash

# Optional Azure CLI:
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
EOF
}

if [ "$mode" = "print" ]; then
  print_commands
  print_manual_notes
  exit 0
fi

printf '%s\n' "This script installs Linux host tools for this repository."
printf '%s\n' "It may use sudo for system packages and CLI installation."
printf '%s\n' "It will not log in to Azure or create cloud resources."
printf '%s\n' "The project prefers Docker-first workflows; heavyweight SDK/CLI host installs are optional."
printf '%s' "Continue? [y/N] "
read -r answer

case "$answer" in
  y|Y|yes|YES)
    ;;
  *)
    printf '%s\n' "Installation cancelled."
    exit 0
    ;;
esac

sudo apt-get update
sudo apt-get install -y ca-certificates curl gnupg lsb-release apt-transport-https make jq nodejs npm

if ! command -v kubectl >/dev/null 2>&1; then
  curl -LO "https://dl.k8s.io/release/$(curl -Ls https://dl.k8s.io/release/stable.txt)/bin/linux/amd64/kubectl"
  sudo install -o root -g root -m 0755 kubectl /usr/local/bin/kubectl
  rm -f kubectl
fi

if ! command -v helm >/dev/null 2>&1; then
  curl https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-3 | bash
fi

if ! command -v dapr >/dev/null 2>&1; then
  curl -fsSL https://raw.githubusercontent.com/dapr/cli/master/install/install.sh | /bin/bash
fi

if ! command -v az >/dev/null 2>&1; then
  curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
fi

cat <<'EOF'

Scripted bootstrap complete for supported host tools.

If dotnet or docker are still missing, install them from their official
Linux package repositories or use the future containerized Makefile targets, then rerun:
  make check-tools
EOF

print_manual_notes
