#!/bin/bash
cd "$(dirname "$0")"
ARCH=$(uname -m)
if [ "$ARCH" == "arm64" ]; then
    echo "Architecture Apple Silicon détectée..."
    chmod +x ../publish/MacOS_AppleSilicon/TowerFluffy.UI.Desktop
    ../publish/MacOS_AppleSilicon/TowerFluffy.UI.Desktop
else
    echo "Architecture Intel détectée..."
    chmod +x ../publish/MacOS_Intel/TowerFluffy.UI.Desktop
    ../publish/MacOS_Intel/TowerFluffy.UI.Desktop
fi
