#!/usr/bin/env bash
# Downloads Unity 2021.3 reference assemblies from NuGet into .refs/ (not committed).
# They are only read for type information by the compiler; nothing in them is run.
set -euo pipefail
HERE="$(cd "$(dirname "$0")" && pwd)"
REFS="$HERE/.refs"
mkdir -p "$REFS/modules" "$REFS/ui" "$REFS/nunit"
curl -sSL -o "$REFS/modules.nupkg" https://api.nuget.org/v3-flatcontainer/unityengine.modules/2021.3.33/unityengine.modules.2021.3.33.nupkg
curl -sSL -o "$REFS/ui.nupkg" https://api.nuget.org/v3-flatcontainer/unity3d.unityengine.ui/2020.3.21/unity3d.unityengine.ui.2020.3.21.nupkg
(cd "$REFS/modules" && python3 -I -m zipfile -e ../modules.nupkg .)
(cd "$REFS/ui" && python3 -I -m zipfile -e ../ui.nupkg .)
curl -sSL -o "$REFS/nunit.nupkg" https://api.nuget.org/v3-flatcontainer/nunit/3.5.0/nunit.3.5.0.nupkg
(cd "$REFS/nunit" && python3 -I -m zipfile -e ../nunit.nupkg .)
echo "Unity reference assemblies ready in $REFS"
