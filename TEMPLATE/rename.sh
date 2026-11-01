#!/bin/sh
# Turns this template into a new inklib: ./rename.sh <prefix> <Name>
#   prefix: the C prefix, lower case (e.g. pz). Name: the C# name (e.g. Pz).
# Replaces tpl / TPL / Tpl everywhere, in file contents and in file names.
# (No English word contains "tpl", so a plain replace is safe.) Run once, in a fresh copy.
set -eu
[ $# -eq 2 ] || { echo "usage: $0 <prefix> <Name>"; exit 1; }
p=$1; N=$2
P=$(echo "$p" | tr '[:lower:]' '[:upper:]')
echo "$p" | grep -Eq '^[a-z][a-z0-9]*$' || { echo "prefix: lower case letters and digits"; exit 1; }
echo "$N" | grep -Eq '^[A-Z][A-Za-z0-9]*$' || { echo "Name: a C# identifier starting with a capital"; exit 1; }
cd "$(dirname "$0")"
rm -rf native/build src/*/bin src/*/obj tests/*/bin tests/*/obj

find . -type f ! -path './.git/*' ! -name rename.sh | while read -r f; do
    sed -i.bak -e "s/tpl/${p}/g; s/TPL/${P}/g; s/Tpl/${N}/g" "$f"
    rm -f "$f.bak"
done
find . -depth \( -name '*tpl*' -o -name '*Tpl*' \) ! -path './.git/*' | while read -r f; do
    mv "$f" "$(dirname "$f")/$(basename "$f" | sed "s/tpl/${p}/g; s/Tpl/${N}/g")"
done
echo "Renamed to ${p}_ / ${N}. Next: README.md, Units.cs, Tuning.cs, Character.cs, the header enums, Docs/."
