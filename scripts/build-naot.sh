#!/bin/bash

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

usage() {
  echo "Usage: $(basename "$0") [OPTIONS]"
  echo ""
  echo "Native AOT publish for ClassicUO (bootstrap + client)."
  echo ""
  echo "ClassicUO.exe subsystem (Bootstrap only; Windows):"
  echo "  --winexe     WinExe — no extra console window (default)"
  echo "  --console    Exe   — show console (stdout / alloc console)"
  echo "  --exe        same as --console"
  echo ""
  echo "Console output during build (dotnet --verbosity):"
  echo "  (default)     minimal  — short log"
  echo "  --quiet, -q  quiet    — almost no output"
  echo "  --verbose    normal   — more detail"
  echo "  --diag       diagnostic — full trace (very large)"
  echo ""
  echo "  -h, --help   show this help"
}

VERBOSITY="minimal"
BOOTSTRAP_OUTPUT_TYPE="WinExe"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --winexe)
      BOOTSTRAP_OUTPUT_TYPE="WinExe"
      shift
      ;;
    --console|--exe)
      BOOTSTRAP_OUTPUT_TYPE="Exe"
      shift
      ;;
    --quiet|-q)
      VERBOSITY="quiet"
      shift
      ;;
    --verbose)
      VERBOSITY="normal"
      shift
      ;;
    --diag)
      VERBOSITY="diagnostic"
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown option: $1" >&2
      usage >&2
      exit 1
      ;;
  esac
done

# Ensure vswhere.exe is in PATH (required for NativeAOT linker on Windows)
export PATH="$PATH:/c/Program Files (x86)/Microsoft Visual Studio/Installer"

# Define paths and project details
bootstrap_project="$REPO_ROOT/src/ClassicUO.Bootstrap/src/ClassicUO.Bootstrap.csproj"
client_project="$REPO_ROOT/src/ClassicUO.Client"
output_directory="$REPO_ROOT/bin/dist"
target=""

# Determine the platform
platform=$(uname -s)

# Build for the appropriate platform
case $platform in
  Linux)
    # Add Linux-specific build commands here
    target="linux-x64"
    ;;
  Darwin)
    # Add macOS-specific build commands here
   target="osx-x64"
    ;;
  MINGW* | CYGWIN*)
    # Add Windows-specific build commands here
    target="win-x64"
    ;;
  *)
    echo "Unsupported platform: $platform"
    exit 1
    ;;
esac

echo "dotnet publish verbosity: $VERBOSITY"
echo "Bootstrap OutputType (ClassicUO.exe): $BOOTSTRAP_OUTPUT_TYPE"

dotnet publish "$bootstrap_project" -c Release -o "$output_directory" -p:OutputType="$BOOTSTRAP_OUTPUT_TYPE" --verbosity "$VERBOSITY"
dotnet publish "$client_project" -c Release -p:NativeLib=Shared -p:OutputType=Library -r $target -o "$output_directory" --verbosity "$VERBOSITY"