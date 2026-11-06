#!/usr/bin/env bash
# Type-checks the game's C# scripts against Unity 2021.3 reference assemblies, without Unity.
# Runtime scripts are compiled first; Editor scripts are then compiled against them.
# Catches syntax, type and API-signature errors. It does not run anything, so runtime
# behaviour still needs testing in the Unity editor. Write code in C# 7.3 (mcs's limit).
#
#   Tools/compile-check/check.sh          # needs: apt-get install mono-mcs
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
HERE="$ROOT/Tools/compile-check"
REFS="$HERE/.refs"
[ -f "$REFS/nunit/lib/net45/nunit.framework.dll" ] || "$HERE/fetch-refs.sh"
OUT="$(mktemp -d)"
trap 'rm -rf "$OUT"' EXIT

refs=()
for dll in "$REFS"/modules/lib/net45/*.dll; do refs+=("-r:$dll"); done
refs+=("-r:$REFS/ui/lib/UnityEngine.UI.dll" "-r:$ROOT/Assets/Plugins/Demigiant/DOTween/DOTween.dll"
      "-r:$REFS/nunit/lib/net45/nunit.framework.dll")

# SimpleCameraController uses the Input System package, which has no reference assembly here.
mapfile -d '' runtime < <(find "$ROOT/Assets" -name '*.cs' -not -path '*/Editor/*' \
    -not -name 'SimpleCameraController.cs' -print0)
mapfile -d '' editor < <(find "$ROOT/Assets" -path '*/Editor/*' -name '*.cs' -not -path '*/Plugins/*' -print0)

flags=(-langversion:experimental -nowarn:414,169,649,219,618 -nostdlib- -sdk:4.5)
echo "Runtime scripts: ${#runtime[@]}"
mcs "${flags[@]}" -target:library -out:"$OUT/Runtime.dll" "${refs[@]}" "$HERE/stubs/TMPro.cs" "${runtime[@]}"
if [ "${#editor[@]}" -gt 0 ]; then
  echo "Editor scripts: ${#editor[@]}"
  mcs "${flags[@]}" -target:library -out:"$OUT/Editor.dll" "${refs[@]}" -r:"$OUT/Runtime.dll" \
      "$HERE/stubs/TMPro.cs" "$HERE/stubs/UnityEditor.cs" "${editor[@]}"
fi
echo "OK: everything compiles"
