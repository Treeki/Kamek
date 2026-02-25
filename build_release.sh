#!/bin/bash

if [ "$#" -eq 0 ]; then
    echo "Usage: $0 <RID>" >&2
    echo "(see here for RIDs: https://learn.microsoft.com/en-us/dotnet/core/rid-catalog#known-rids)" >&2
    exit 1
fi

ROOT="$(pwd)"
RID="$1"
rm -rf release
mkdir release
cd Kamek
dotnet restore
dotnet build -p:Configuration=Release
dotnet publish -c Release -r "$RID" --self-contained true -p:PublishSingleFile=true
cd "bin/Release/net10.0/$RID/publish"
rm -f *.pdb
cp Kamek* "$ROOT/release"
cd "$ROOT"
cp -r examples k_stdlib loader shield-fix README.md preproc_demo.cpp release
