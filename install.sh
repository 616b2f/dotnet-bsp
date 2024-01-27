#!/bin/bash

if [[ $# -eq 0 ]]; then
    echo "usage: $0 <project directory>"
    exit 1
fi

if [ ! -d "$1" ]; then
    echo "directory $1 does not exist."
    exit 1
fi

BSP_SERVER=$PWD/bin/dotnet-bsp.dll
mkdir -p "$1/.bsp"
cat > "$1.bsp/dotnet-bsp.json" << EOF
{
  "name": "dotnet-bsp",
  "languages": ["csharp"],
  "version": "0.0.1",
  "bspVersion": "2.1.1",
  "argv": [
    "dotnet",
    "exec",
    "$BSP_SERVER",
    "--logLevel=Debug",
    "--extensionLogDirectory",
    "."
  ]
}
EOF