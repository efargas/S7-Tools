#!/bin/bash
# Helper script to activate Python virtual environment for validation scripts
# Usage: source scripts/activate-venv.sh

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
VENV_PATH="$REPO_ROOT/.venv"

if [ -f "$VENV_PATH/bin/activate" ]; then
    source "$VENV_PATH/bin/activate"
    echo "✅ Virtual environment activated: $VENV_PATH"
else
    echo "❌ Virtual environment not found at: $VENV_PATH"
    echo "Run: python3 -m venv .venv && pip install -r requirements.txt"
    exit 1
fi
