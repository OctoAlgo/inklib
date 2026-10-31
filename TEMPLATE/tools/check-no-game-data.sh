#!/bin/sh
# Fails if a tracked file looks like game data (SPEC.md section 8). A backstop, not permission.
set -eu
cd "$(dirname "$0")/.."
bad=$(git ls-files | grep -iE '\.(iso|bin|cue|img|mdf|chd|exe|elf|dll|so|dylib|wad|pak|cnt|lvl|sna|str|xa|vag|tim|vh|vb|wav|ogg|mp3|png|jpg|tga|bmp|dds|fbx|obj|blend)$' || true)
if [ -n "$bad" ]; then
    echo "Files that look like game data or binaries:"; echo "$bad"; exit 1
fi
big=$(git ls-files -z | xargs -0 -I{} sh -c 'test -f "{}" && test $(wc -c < "{}") -gt 1048576 && echo "{}"' || true)
if [ -n "$big" ]; then
    echo "Files over 1 MB (code only in this repo):"; echo "$big"; exit 1
fi
echo "no game data found"
