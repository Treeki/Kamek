#!/bin/bash
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
