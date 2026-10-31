#!/bin/sh
# Copies Core's sources into the UPM package (unity/Runtime/Core/). Unity compiles them itself,
# so Unity needs no native library. Usage: unity/pack.sh, then "Add package from disk" (unity/package.json).
set -eu
cd "$(dirname "$0")/.."
rm -rf unity/Runtime/Core
mkdir -p unity/Runtime/Core
cp src/Tpl.Core/*.cs unity/Runtime/Core/
cp README.md unity/README.md
echo "packed $(ls unity/Runtime/Core | wc -l | tr -d ' ') files into unity/Runtime/Core"
