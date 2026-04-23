# Project Guidelines

## Priorités non négociables

- Produire un code professionnel, maintenable et lisible ; éviter le « code moche ».
- Ne jamais exposer de secrets (tokens, credentials) dans le dépôt, les logs, les exemples ou les tests.
- Appliquer la Clean Architecture (séparation Domain/Application/Infrastructure/UI) avec des dépendances orientées vers l’intérieur.
- TDD obligatoire (Red → Green → Refactor) pour toute nouvelle logique.

## Style de code

- Fonctions : 50 lignes max ; préférer plusieurs petites fonctions.
- Largeur : 120 colonnes max.
- Complexité : CCN ≤ 5 ; refactorer dès que ça dépasse.
- CRAP score ≤ 25 : réduire la complexité et renforcer les tests.
- Nommage explicite ; éviter les abréviations inutiles.
- Pas d’emojis dans le code.

## Architecture

- Garder le domaine indépendant des frameworks et des entrées/sorties (I/O).
- Utiliser des ports/adapters et l’injection de dépendances quand nécessaire.
- Favoriser du code facilement testable (petites unités, fonctions pures quand possible).

## Build & tests

- Écrire/mettre à jour les tests avant de livrer une feature (TDD).
- Quand une commande ou un outil d’automatisation est nécessaire, privilégier des scripts Python.

## Git & commits

- GitFlow : une branche par feature ; PR obligatoire.
- Interdiction de push direct sur `main` (ou `master`).
- Commits : respecter un standard (ex. Conventional Commits : `feat:`, `fix:`, `chore:`, etc.).

## Documentation

- Documenter au fil de l’eau (dossier `Docs/`).
- S’appuyer sur la documentation officielle des frameworks ; ne pas inventer d’API.
- Framework : rester compatible avec la version 11.x (pas de montée de version majeure non demandée).

## UI / DA

- Respecter la DA : thème futuriste 2D vectoriel.
- Ne pas introduire de nouvelles palettes/typos/styles hors de la DA existante sans demande explicite.