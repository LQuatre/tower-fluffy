import subprocess
import sys
import os
import shutil
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[1]
UI_PROJECT = REPO_ROOT / "src" / "UI.Desktop" / "TowerFluffy.UI.Desktop.csproj"

def run(command, cwd=REPO_ROOT):
    print(f"+ {' '.join(command)}")
    subprocess.run(command, cwd=cwd, check=True)

def create_launchers(publish_base):
    # --- Raccourcis à la RACINE du dossier publish ---
    
    # Windows
    with open(publish_base / "Lancer_Jeu_Windows.bat", "w", encoding="utf-8") as f:
        f.write("@echo off\nstart \"\" \"Windows\\TowerFluffy.UI.Desktop.exe\"\n")
    
    # Linux
    with open(publish_base / "Lancer_Jeu_Linux.sh", "w", encoding="utf-8") as f:
        f.write("#!/bin/bash\nchmod +x ./Linux/TowerFluffy.UI.Desktop\n./Linux/TowerFluffy.UI.Desktop\n")
    
    # MacOS (Détection auto à la racine)
    with open(publish_base / "Lancer_Jeu_MacOS.sh", "w", encoding="utf-8") as f:
        f.write("#!/bin/bash\nARCH=$(uname -m)\nif [ \"$ARCH\" == \"arm64\" ]; then\n    chmod +x ./MacOS_AppleSilicon/TowerFluffy.UI.Desktop\n    ./MacOS_AppleSilicon/TowerFluffy.UI.Desktop\nelse\n    chmod +x ./MacOS_Intel/TowerFluffy.UI.Desktop\n    ./MacOS_Intel/TowerFluffy.UI.Desktop\nfi\n")

    # --- Également à l'intérieur de chaque dossier (pour plus de sécurité) ---
    win_dir = publish_base / "Windows"
    if win_dir.exists():
        with open(win_dir / "Lancer_Direct.bat", "w", encoding="utf-8") as f:
            f.write("@echo off\nstart \"\" \"TowerFluffy.UI.Desktop.exe\"\n")
    
    linux_dir = publish_base / "Linux"
    if linux_dir.exists():
        with open(linux_dir / "Lancer_Direct.sh", "w", encoding="utf-8") as f:
            f.write("#!/bin/bash\nchmod +x ./TowerFluffy.UI.Desktop\n./TowerFluffy.UI.Desktop\n")

def main():
    message = input("Message du commit (ex: 'MAJ graphismes') : ")
    if not message:
        message = "Mise à jour automatique et nouveau build"

    print("\n--- [1/3] SAUVEGARDE ET ENVOI SUR GITHUB ---")
    try:
        run(["git", "add", "."])
        run(["git", "commit", "-m", message])
        run(["git", "push", "origin", "main"])
    except Exception as e:
        print(f"Note: Git push a peut-être échoué ou rien à commit ({e})")

    print("\n--- [2/3] NETTOYAGE DES ANCIENS BUILDS ---")
    publish_dir = REPO_ROOT / "publish"
    if publish_dir.exists():
        shutil.rmtree(publish_dir)
    os.makedirs(publish_dir, exist_ok=True)

    print("\n--- [3/3] GÉNÉRATION DES BUILDS MULTI-PLATEFORMES ---")
    platforms = {
        "Windows": "win-x64",
        "Linux": "linux-x64",
        "MacOS_Intel": "osx-x64",
        "MacOS_AppleSilicon": "osx-arm64"
    }

    for folder, runtime in platforms.items():
        print(f"\nConstruction pour {folder} ({runtime})...")
        run([
            "dotnet", "publish", str(UI_PROJECT),
            "-c", "Release",
            "-r", runtime,
            "--self-contained", "true",
            "-o", f"./publish/{folder}"
        ])

    print("\n--- [FINAL] CRÉATION DES RACCOURCIS DANS /PUBLISH/ ---")
    create_launchers(publish_dir)

    print("\n✅ OPÉRATION TERMINÉE !")
    print("Les nouveaux builds et raccourcis sont dans le dossier 'publish/'.")
    print("Votre code est à jour sur GitHub.")

if __name__ == "__main__":
    main()
