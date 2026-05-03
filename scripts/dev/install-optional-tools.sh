#!/usr/bin/env bash
set -eu

if [ "$(uname -s)" != "Linux" ]; then
  printf '%s\n' "This optional tool installer supports Linux only."
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
    printf '%s\n' "Install optional tools manually from their official documentation."
    exit 1
    ;;
esac

mode="install"
if [ "${1:-}" = "--print-only" ]; then
  mode="print"
fi

kubernetes_minor="${KUBERNETES_MINOR:-v1.35}"

print_commands() {
  cat <<EOF
sudo apt-get update
sudo apt-get install -y ca-certificates curl gnupg lsb-release apt-transport-https gpg

# .NET SDK 10.0.
sudo apt-get install -y dotnet-sdk-10.0

# Azure CLI apt repository.
sudo install -m 0755 -d /etc/apt/keyrings
curl -sLS https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor | sudo tee /etc/apt/keyrings/microsoft.gpg >/dev/null
sudo chmod go+r /etc/apt/keyrings/microsoft.gpg
AZ_DIST=\$(lsb_release -cs)
cat <<AZURE_EOF | sudo tee /etc/apt/sources.list.d/azure-cli.sources >/dev/null
Types: deb
URIs: https://packages.microsoft.com/repos/azure-cli/
Suites: \${AZ_DIST}
Components: main
Architectures: \$(dpkg --print-architecture)
Signed-by: /etc/apt/keyrings/microsoft.gpg
AZURE_EOF

# kubectl apt repository. Override with KUBERNETES_MINOR=v1.xx when needed.
curl -fsSL https://pkgs.k8s.io/core:/stable:/${kubernetes_minor}/deb/Release.key | sudo gpg --dearmor -o /etc/apt/keyrings/kubernetes-apt-keyring.gpg
sudo chmod 0644 /etc/apt/keyrings/kubernetes-apt-keyring.gpg
echo 'deb [signed-by=/etc/apt/keyrings/kubernetes-apt-keyring.gpg] https://pkgs.k8s.io/core:/stable:/${kubernetes_minor}/deb/ /' | sudo tee /etc/apt/sources.list.d/kubernetes.list >/dev/null
sudo chmod 0644 /etc/apt/sources.list.d/kubernetes.list

# Helm apt repository.
curl -fsSL https://packages.buildkite.com/helm-linux/helm-debian/gpgkey | gpg --dearmor | sudo tee /usr/share/keyrings/helm.gpg >/dev/null
echo "deb [signed-by=/usr/share/keyrings/helm.gpg] https://packages.buildkite.com/helm-linux/helm-debian/any/ any main" | sudo tee /etc/apt/sources.list.d/helm-stable-debian.list >/dev/null

sudo apt-get update
sudo apt-get install -y azure-cli kubectl helm

# Dapr CLI.
curl -fsSL https://raw.githubusercontent.com/dapr/cli/master/install/install.sh | /bin/bash
EOF
}

verify_tool() {
  tool="$1"
  if command -v "$tool" >/dev/null 2>&1; then
    path=$(command -v "$tool")
    version=$("$tool" --version 2>/dev/null | head -n 1 || true)
    if [ -n "$version" ]; then
      printf 'ok       %-8s %s (%s)\n' "$tool" "$path" "$version"
    else
      printf 'ok       %-8s %s\n' "$tool" "$path"
    fi
  else
    printf 'missing  %-8s\n' "$tool"
    return 1
  fi
}

if [ "$mode" = "print" ]; then
  print_commands
  exit 0
fi

printf '%s\n' "This script installs optional local tools for this repository:"
printf '%s\n' "- dotnet SDK 10.0"
printf '%s\n' "- kubectl from Kubernetes ${kubernetes_minor} apt repo"
printf '%s\n' "- Helm from the Helm/Buildkite apt repo"
printf '%s\n' "- Dapr CLI from the official Dapr install script"
printf '%s\n' "- Azure CLI from the Microsoft Azure CLI apt repo"
printf '%s\n' ""
printf '%s\n' "It may use sudo, configure apt repositories, and download packages."
printf '%s\n' "It will not run az login, dapr init, create cloud resources, or write secrets."
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
sudo apt-get install -y ca-certificates curl gnupg lsb-release apt-transport-https gpg

sudo apt-get install -y dotnet-sdk-10.0

sudo install -m 0755 -d /etc/apt/keyrings
curl -sLS https://packages.microsoft.com/keys/microsoft.asc |
  gpg --dearmor |
  sudo tee /etc/apt/keyrings/microsoft.gpg >/dev/null
sudo chmod go+r /etc/apt/keyrings/microsoft.gpg

az_dist=$(lsb_release -cs)
cat <<AZURE_EOF | sudo tee /etc/apt/sources.list.d/azure-cli.sources >/dev/null
Types: deb
URIs: https://packages.microsoft.com/repos/azure-cli/
Suites: ${az_dist}
Components: main
Architectures: $(dpkg --print-architecture)
Signed-by: /etc/apt/keyrings/microsoft.gpg
AZURE_EOF

curl -fsSL "https://pkgs.k8s.io/core:/stable:/${kubernetes_minor}/deb/Release.key" |
  sudo gpg --dearmor -o /etc/apt/keyrings/kubernetes-apt-keyring.gpg
sudo chmod 0644 /etc/apt/keyrings/kubernetes-apt-keyring.gpg
printf 'deb [signed-by=/etc/apt/keyrings/kubernetes-apt-keyring.gpg] https://pkgs.k8s.io/core:/stable:/%s/deb/ /\n' "$kubernetes_minor" |
  sudo tee /etc/apt/sources.list.d/kubernetes.list >/dev/null
sudo chmod 0644 /etc/apt/sources.list.d/kubernetes.list

curl -fsSL https://packages.buildkite.com/helm-linux/helm-debian/gpgkey |
  gpg --dearmor |
  sudo tee /usr/share/keyrings/helm.gpg >/dev/null
printf '%s\n' "deb [signed-by=/usr/share/keyrings/helm.gpg] https://packages.buildkite.com/helm-linux/helm-debian/any/ any main" |
  sudo tee /etc/apt/sources.list.d/helm-stable-debian.list >/dev/null

sudo apt-get update
sudo apt-get install -y azure-cli kubectl helm

if ! command -v dapr >/dev/null 2>&1; then
  curl -fsSL https://raw.githubusercontent.com/dapr/cli/master/install/install.sh | /bin/bash
fi

printf '\n%s\n' "Verification:"
missing=0
for tool in dotnet kubectl helm dapr az; do
  if ! verify_tool "$tool"; then
    missing=1
  fi
done

if [ "$missing" -ne 0 ]; then
  printf '\n%s\n' "One or more optional tools are still missing from PATH."
  exit 1
fi

cat <<'EOF'

Optional tools are installed.

Manual follow-up when needed:
- Run `az login` only when Azure access is required.
- Run `dapr init` only when the local Dapr runtime is required.
- Run `make check-tools` to verify repository tool status.
EOF
