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
    # Windows
    win_dir = publish_base / "Windows"
    if win_dir.exists():
        with open(win_dir / "Lancer_Jeu.bat", "w", encoding="utf-8") as f:
            f.write("@echo off\ncd /d \"%~dp0\"\nstart \"\" \"TowerFluffy.UI.Desktop.exe\"\n")
    
    # Linux
    linux_dir = publish_base / "Linux"
    if linux_dir.exists():
        with open(linux_dir / "Lancer_Jeu.sh", "w", encoding="utf-8", newline="\n") as f:
            f.write("#!/bin/bash\ncd \"$(dirname \"$0\")\"\nchmod +x ./TowerFluffy.UI.Desktop\n./TowerFluffy.UI.Desktop\n")
            
    # MacOS Intel
    mac_intel_dir = publish_base / "MacOS_Intel"
    if mac_intel_dir.exists():
        with open(mac_intel_dir / "Lancer_Jeu.sh", "w", encoding="utf-8", newline="\n") as f:
            f.write("#!/bin/bash\ncd \"$(dirname \"$0\")\"\nchmod +x ./TowerFluffy.UI.Desktop\n./TowerFluffy.UI.Desktop\n")
            
    # MacOS Apple Silicon
    mac_arm_dir = publish_base / "MacOS_AppleSilicon"
    if mac_arm_dir.exists():
        with open(mac_arm_dir / "Lancer_Jeu.sh", "w", encoding="utf-8", newline="\n") as f:
            f.write("#!/bin/bash\ncd \"$(dirname \"$0\")\"\nchmod +x ./TowerFluffy.UI.Desktop\n./TowerFluffy.UI.Desktop\n")

def main():
    message = input("Message du commit (ex: 'MAJ graphismes') : ")
    if not message:
        message = "Mise à jour automatique et nouveau build"

    print("\n--- [1/3] SAUVEGARDE ET ENVOI SUR GITHUB ---")
    try:
        run(["git", "add", "."])
        run(["git", "commit", "-m", message])
        
        # Détection de la branche active pour pousser sur la bonne branche
        branch = "main"
        try:
            import subprocess as sp
            res = sp.run(["git", "rev-parse", "--abbrev-ref", "HEAD"], capture_output=True, text=True, check=True)
            branch = res.stdout.strip()
        except Exception as ex:
            print(f"Erreur de détection de branche, repli sur main: {ex}")
            
        run(["git", "push", "origin", branch])
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

    print("\n--- COMPRESSION DES BUILDS EN ARCHIVES ZIP ---")
    for folder in platforms.keys():
        folder_path = publish_dir / folder
        if folder_path.exists():
            zip_name = publish_dir / f"TowerFluffy_{folder}"
            print(f"Création de l'archive {zip_name}.zip...")
            shutil.make_archive(str(zip_name), "zip", root_dir=str(folder_path))

    print("\n✅ OPÉRATION TERMINÉE !")
    print("Les nouveaux dossiers de builds et les fichiers ZIP individuels correspondants sont dans le dossier 'publish/'.")
    print("Votre code est à jour sur GitHub.")

if __name__ == "__main__":
    main()
