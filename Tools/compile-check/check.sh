#!/usr/bin/env bash
# Type-checks the game's C# scripts against Unity 2021.3 reference assemblies, without Unity.
# Each assembly is compiled separately, the way Unity does, so `internal` members of one
# assembly are not visible to another (Naval.Core -> Naval.Tests.EditMode excepted, via
# InternalsVisibleTo). Order and references:
#   Naval.Core            no engine refs (proves "no Unity dependencies")
#   Naval.Tests.EditMode  Unity refs + nunit + Naval.Core (editor-only, so nothing references it)
#   Prototype             Unity refs (editor-only, no references)
#   Assembly-CSharp       everything else outside Editor folders; Unity refs + Naval.Core
#   Assembly-CSharp-Editor  Editor folders (not Plugins); Assembly-CSharp + Naval.Core + Prototype
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
refs+=("-r:$REFS/ui/lib/UnityEngine.UI.dll" "-r:$ROOT/Assets/Plugins/Demigiant/DOTween/DOTween.dll")

flags=(-langversion:experimental -nowarn:414,169,649,219,618,436 -nostdlib- -sdk:4.5)
A="$ROOT/Assets"
CORE_DIR="$A/_NavalTradeMinigame/Scripts/Core"
TESTS_DIR="$A/_NavalTradeMinigame/Tests/EditMode"
PROTO_DIR="$A/_Prototype"
# Output names must equal the "name" in each .asmdef. A file belongs to the asmdef in its
# nearest parent folder, so files under the three asmdef folders are kept out of Assembly-CSharp.
stubs_rt=("$HERE/stubs/TMPro.cs" "$HERE/stubs/EventSystems.cs")

# Naval.Core: no UnityEngine references at all (noEngineReferences).
mapfile -d '' core < <(find "$CORE_DIR" -name '*.cs' -print0)
echo "Naval.Core: ${#core[@]}"
mcs "${flags[@]}" -target:library -out:"$OUT/Naval.Core.dll" "${core[@]}"

mapfile -d '' tests < <(find "$TESTS_DIR" -name '*.cs' -print0)
echo "Naval.Tests.EditMode: ${#tests[@]}"
mcs "${flags[@]}" -target:library -out:"$OUT/Naval.Tests.EditMode.dll" "${refs[@]}" \
    "-r:$REFS/nunit/lib/net45/nunit.framework.dll" -r:"$OUT/Naval.Core.dll" "${tests[@]}"

mapfile -d '' proto < <(find "$PROTO_DIR" -name '*.cs' -print0)
echo "Prototype: ${#proto[@]}"
mcs "${flags[@]}" -target:library -out:"$OUT/Prototype.dll" "${refs[@]}" "${proto[@]}"

# SimpleCameraController uses the Input System package, which has no reference assembly here.
mapfile -d '' runtime < <(find "$A" -name '*.cs' -not -path '*/Editor/*' \
    -not -path "$CORE_DIR/*" -not -path "$TESTS_DIR/*" -not -path "$PROTO_DIR/*" \
    -not -name 'SimpleCameraController.cs' -print0)
echo "Assembly-CSharp: ${#runtime[@]}"
mcs "${flags[@]}" -target:library -out:"$OUT/Assembly-CSharp.dll" "${refs[@]}" -r:"$OUT/Naval.Core.dll" \
    "${stubs_rt[@]}" "${runtime[@]}"

mapfile -d '' editor < <(find "$A" -path '*/Editor/*' -name '*.cs' -not -path '*/Plugins/*' \
    -not -path "$CORE_DIR/*" -not -path "$TESTS_DIR/*" -not -path "$PROTO_DIR/*" -print0)
if [ "${#editor[@]}" -gt 0 ]; then
  echo "Assembly-CSharp-Editor: ${#editor[@]}"
  mcs "${flags[@]}" -target:library -out:"$OUT/Assembly-CSharp-Editor.dll" "${refs[@]}" \
      -r:"$OUT/Assembly-CSharp.dll" -r:"$OUT/Naval.Core.dll" -r:"$OUT/Prototype.dll" \
      "$HERE/stubs/TMPro.cs" "$HERE/stubs/UnityEditor.cs" "${editor[@]}"
fi
echo "OK: everything compiles"
