#!/bin/sh
# Builds the C API with NativeAOT, runs the C test and the example host.
# Usage: native/build_and_test.sh [linux-x64|win-x64|osx-arm64]   (default: this machine)
# Linux needs: .NET 8 SDK, cc, clang and zlib headers. macOS needs: .NET 8 SDK, Xcode tools.
# Windows (Git Bash) needs: .NET 8 SDK, Visual Studio C++ tools, clang.
set -eu
cd "$(dirname "$0")/.."

case "$(uname -s)" in
    MINGW*|MSYS*|CYGWIN*) default_rid=win-x64 ;;
    Darwin) default_rid=osx-arm64 ;;
    *) default_rid=linux-x64 ;;
esac
rid=${1:-$default_rid}
out=native/build/$rid
cflags="-std=c99 -Wall -Wextra -Werror -Wno-unused-function -I native/include"

dotnet publish src/Tpl.Native/Tpl.Native.csproj -c Release -r "$rid" -o "$out" --nologo -v quiet

case "$rid" in
    win-*)
        # Import library from native/tpl.def (llvm-lib comes with LLVM/clang).
        llvm-lib /nologo /machine:x64 /def:native/tpl.def /out:"$out/tpl.lib"
        for prog in tests/test_tpl example/host; do
            name=$(basename "$prog")
            clang $cflags -D_CRT_SECURE_NO_WARNINGS "native/$prog.c" "$out/tpl.lib" -o "$out/$name.exe"
            "$out/$name.exe"
        done
        ;;
    osx-*)
        # NativeAOT leaves a relative install name; make it @rpath so hosts can find the dylib,
        # then re-sign (arm64 refuses to load a dylib whose signature no longer matches).
        install_name_tool -id @rpath/libtpl.dylib "$out/libtpl.dylib"
        codesign --force --sign - "$out/libtpl.dylib"
        for prog in tests/test_tpl example/host; do
            name=$(basename "$prog")
            cc $cflags "native/$prog.c" -L "$out" -ltpl -Wl,-rpath,@executable_path -o "$out/$name"
            "$out/$name"
        done
        ;;
    *)
        for prog in tests/test_tpl example/host; do
            name=$(basename "$prog")
            cc $cflags "native/$prog.c" -L "$out" -ltpl -lm -Wl,-rpath,'$ORIGIN' -o "$out/$name"
            "$out/$name"
        done
        ;;
esac
