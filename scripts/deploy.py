import subprocess
import sys
import os
import shutil
import zipfile
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[1]
UI_PROJECT = REPO_ROOT / "src" / "UI.Desktop" / "TowerFluffy.UI.Desktop.csproj"

def run(command, cwd=REPO_ROOT):
    print(f"+ {' '.join(command)}")
    subprocess.run(command, cwd=cwd, check=True)

def zip_folder(folder_to_zip, zip_filepath):
    with zipfile.ZipFile(zip_filepath, 'w', zipfile.ZIP_DEFLATED) as zipf:
        for root, dirs, files in os.walk(folder_to_zip):
            for file in files:
                filepath = Path(root) / file
                relative_path = filepath.relative_to(folder_to_zip)
                
                with open(filepath, 'rb') as f:
                    file_data = f.read()
                
                zip_info = zipfile.ZipInfo(str(relative_path).replace("\\", "/"))
                
                # Détecter si c'est un exécutable ou un script shell
                is_executable = (
                    filepath.suffix in ['.sh', '.bat'] or 
                    filepath.name == 'TowerFluffy.UI.Desktop' or 
                    filepath.name == 'TowerFluffy.UI.Desktop.exe'
                )
                
                if is_executable:
                    zip_info.external_attr = 0o100755 << 16
                else:
                    zip_info.external_attr = 0o100644 << 16
                
                zip_info.create_system = 3  # UNIX
                zipf.writestr(zip_info, file_data)

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
            
    # MacOS App Bundling (Intel et Apple Silicon)
    mac_folders = ["MacOS_Intel", "MacOS_AppleSilicon"]
    for mac_folder in mac_folders:
        mac_dir = publish_base / mac_folder
        if mac_dir.exists():
            app_dir = mac_dir / "TowerFluffy.app"
            contents_dir = app_dir / "Contents"
            macos_dir = contents_dir / "MacOS"
            
            os.makedirs(macos_dir, exist_ok=True)
            
            # Déplacer tout le contenu de mac_dir vers Contents/MacOS
            for item in os.listdir(mac_dir):
                item_path = mac_dir / item
                if item == "TowerFluffy.app":
                    continue
                shutil.move(str(item_path), str(macos_dir / item))
            
            # Créer le Info.plist
            info_plist_content = """<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleDevelopmentRegion</key>
    <string>English</string>
    <key>CFBundleExecutable</key>
    <string>TowerFluffy.UI.Desktop</string>
    <key>CFBundleIdentifier</key>
    <string>com.lucas.towerfluffy</string>
    <key>CFBundleInfoDictionaryVersion</key>
    <string>6.0</string>
    <key>CFBundleName</key>
    <string>TowerFluffy</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleShortVersionString</key>
    <string>1.0.0</string>
    <key>CFBundleSignature</key>
    <string>????</string>
    <key>CFBundleVersion</key>
    <string>1.0.0</string>
    <key>LSMinimumSystemVersion</key>
    <string>10.12</string>
    <key>NSHighResolutionCapable</key>
    <true/>
</dict>
</plist>
"""
            with open(contents_dir / "Info.plist", "w", encoding="utf-8") as f:
                f.write(info_plist_content)

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
            zip_file = publish_dir / f"TowerFluffy_{folder}.zip"
            print(f"Création de l'archive {zip_file.name} (avec préservation des permissions)...")
            zip_folder(folder_path, zip_file)

    print("\n✅ OPÉRATION TERMINÉE !")
    print("Les nouveaux dossiers de builds et les fichiers ZIP individuels correspondants sont dans le dossier 'publish/'.")
    print("Votre code est à jour sur GitHub.")

if __name__ == "__main__":
    main()
