3#!/usr/bin/env bash
set -euo pipefail

# Basic usage notes
if [[ "${1:-""}" == "--help" ]]; then
  cat <<'USAGE'
WSL bootstrap helper for AutodocX.

Flags:
  --skip-azure   Skip Azure CLI installation (default: install)
  --skip-bicep   Skip az bicep install (default: install when Azure CLI present)

Environment overrides:
  PKG_LIST   Space-separated apt packages to install (defaults baked in)
USAGE
  exit 0
fi

INSTALL_AZ=1
INSTALL_BICEP=1
while [[ $# -gt 0 ]]; do
  case "$1" in
    --skip-azure) INSTALL_AZ=0 ;;
    --skip-bicep) INSTALL_BICEP=0 ;;
  esac
  shift
done

packages=${PKG_LIST:-"python3.10 python3.10-venv python3.10-dev build-essential pkg-config \
  graphviz graphviz-dev fonts-dejavu mkdocs git curl libxml2-dev libxslt1-dev"}

echo "[setup-wsl] Updating apt cache..."
sudo apt-get update -y

echo "[setup-wsl] Installing core packages..."
sudo apt-get install -y ${packages}

if [[ ${INSTALL_AZ} -eq 1 ]]; then
  if ! command -v az >/dev/null 2>&1; then
    echo "[setup-wsl] Installing Azure CLI..."
    curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
  else
    echo "[setup-wsl] Azure CLI already present."
  fi
  if [[ ${INSTALL_BICEP} -eq 1 ]]; then
    echo "[setup-wsl] Installing/refreshing Bicep CLI via az..."
    az bicep install || echo "[setup-wsl] Failed to install Bicep via az; install manually if needed."
  fi
else
  echo "[setup-wsl] Skipping Azure CLI/Bicep installation."
fi

echo "[setup-wsl] Installing tree-sitter tooling (optional but recommended)..."
python3 -m pip install --upgrade pip
python3 -m pip install tree-sitter tree-sitter-languages || echo "[setup-wsl] Skipped tree-sitter wheel install (requires activated venv)."

cat <<'NEXT'

[setup-wsl] Done.
- Keep the repo under /home/<user>/... for best IO performance.
- Create your virtual environment: python3.10 -m venv .venv && source .venv/bin/activate
- Install project deps: pip install -e .[treesitter]
- Export secrets: echo "OPENAI_API_KEY=..." >> .env
- Optional: set AUTODOCX_SKIP_PYCLEAN=1 if you temporarily mirror the repo under /mnt/c.
NEXT
