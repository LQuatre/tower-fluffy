import subprocess
import sys
import os
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[1]
UI_PROJECT = REPO_ROOT / "src" / "UI.Desktop" / "TowerFluffy.UI.Desktop.csproj"

def run(command, cwd=REPO_ROOT):
    print(f"+ {' '.join(command)}")
    subprocess.run(command, cwd=cwd, check=True)

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
        import shutil
        shutil.rmtree(publish_dir)

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

    print("\n✅ OPÉRATION TERMINÉE !")
    print("Les nouveaux builds sont disponibles dans le dossier 'publish/'.")
    print("Votre code est à jour sur GitHub.")

if __name__ == "__main__":
    main()
