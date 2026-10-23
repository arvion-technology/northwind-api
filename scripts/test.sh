#!/usr/bin/env bash
# One test entry point for the .NET project.
#
# The local machine may have only the .NET runtime (no SDK), so this script uses
# dotnet test when an 8.x SDK is present and otherwise runs the same command in
# the official SDK container. The gate calls this script, not npm.
set -euo pipefail
cd "$(dirname "$0")/.."

if command -v dotnet >/dev/null 2>&1 && dotnet --list-sdks 2>/dev/null | grep -q '^8\.'; then
  echo "dotnet SDK found; running tests locally"
  exec dotnet test Northwind.sln --configuration Release --nologo
fi

echo "No .NET 8 SDK found locally; falling back to Docker (mcr.microsoft.com/dotnet/sdk:8.0)"
command -v docker >/dev/null 2>&1 || { echo "Docker is not installed either. Install the .NET 8 SDK or Docker." >&2; exit 1; }

# The SDK image runs as root by default. Test output is chowned back to the
# calling user so the working tree stays readable and build artifacts stay
# disposable. (Running dotnet as an arbitrary non-root uid trips a NuGet mutex
# bug in this image, so root plus a chown is the reliable container path.)
exec docker run --rm \
  -v "$PWD":/app \
  -w /app \
  -e DOTNET_CLI_TELEMETRY_OPTOUT=1 \
  -e DOTNET_NOLOGO=1 \
  -e HOST_UID="$(id -u)" \
  -e HOST_GID="$(id -g)" \
  mcr.microsoft.com/dotnet/sdk:8.0 \
  bash -c 'dotnet test Northwind.sln --configuration Release --nologo; status=$?; chown -R "$HOST_UID:$HOST_GID" /app/src /app/test 2>/dev/null || true; exit $status'
