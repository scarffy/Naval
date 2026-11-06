#!/usr/bin/env bash
# Runs the edit-mode tests on Mono, without Unity. Needs mono-mcs and mono-runtime, and the
# reference assemblies that Tools/compile-check/check.sh downloads (run it first).
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
HERE="$ROOT/Tools/test-runner"
NUNIT="$ROOT/Tools/compile-check/.refs/nunit/lib/net45/nunit.framework.dll"
OUT="$(mktemp -d)"
trap 'rm -rf "$OUT"' EXIT
mapfile -d '' core < <(find "$ROOT/Assets/_NavalTradeMinigame/Scripts/Core" -name '*.cs' -print0)
mapfile -d '' tests < <(find "$ROOT/Assets/_NavalTradeMinigame/Tests/EditMode" -name '*.cs' -print0)
mcs -nowarn:1591 -target:library -out:"$OUT/Naval.Core.dll" "${core[@]}"
mcs -target:library -out:"$OUT/Naval.Tests.EditMode.dll" -r:"$OUT/Naval.Core.dll" -r:"$NUNIT" "$HERE/UnityEngineStub.cs" "${tests[@]}"
mcs -out:"$OUT/Runner.exe" -r:"$NUNIT" "$HERE/Runner.cs"
cp "$NUNIT" "$OUT/"
NAVAL_ASSETS="$ROOT/Assets" mono "$OUT/Runner.exe" "$OUT/Naval.Tests.EditMode.dll"
