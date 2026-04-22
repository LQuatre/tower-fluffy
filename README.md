# TowerFluffy

Tower Defense asymétrique (Attaquant vs Défenseur) en C# / Avalonia.

## Prérequis

- .NET SDK 10.x (voir `global.json`)
- Python 3.x (pour les scripts `scripts/`)

## Commandes

- Restore : `python scripts/cli.py restore`
- Build   : `python scripts/cli.py build`
- Tests   : `python scripts/cli.py test`
- Run UI  : `python scripts/cli.py run-ui`

## Architecture

- `src/Domain` : règles métier + simulation déterministe (sans UI)
- `src/Application` : cas d’usage (orchestration)
- `src/Infrastructure` : adaptateurs I/O (futur)
- `src/UI.Desktop` : UI Avalonia (MVVM + ReactiveUI)

Docs : `Docs/`
