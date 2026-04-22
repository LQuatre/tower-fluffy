# Plan de développement — Tower Defense (Avalonia)

## Objectif
Construire d’abord un moteur de jeu **déterministe et testable** (Domain), puis l’orchestration (Application), puis l’UI Avalonia (MVVM).

Le **MVP** vise un **1v1 local (même PC)** avec : 1 carte simple, économie, vagues, tours/unités de base. Ensuite, extension vers IA et réseau en gardant **Clean Architecture** + **TDD**.

## Découpage indicatif
- Phase 0 (1–2 jours) : cadrage + scaffolding + CI
- Phase 1 (3–5 jours) : moteur Domain (simulation + règles)
- Phase 2 (2–3 jours) : use-cases Application + DTO
- Phase 3 (5–7 jours) : UI Avalonia jouable + contenu MVP
- Phase 4 (3–5 jours) : IA
- Phase 5 (10–15 jours) : réseau

## Étapes

### 1) Cadrage MVP (décisions produit minimales)
- Fixer les valeurs par défaut pour éviter de bloquer l’implémentation :
  - 10 vagues
  - 1 voie
  - placement sur grille
  - préparation 30–45s
  - bouton "Skip"
  - pouvoirs spéciaux désactivés au tout début (réintroduits après stabilité du core)
- Définir la "Definition of Done" MVP :
  - boucle jouable complète
  - victoire/défaite
  - UX Défenseur + Attaquant
  - tests Domain/Application verts
  - CI verte

### 2) Scaffolding dépôt (bloquant)
- Créer la solution .NET et la structure Clean Architecture :
  - `src/{Domain,Application,Infrastructure,UI.Desktop}`
  - `tests/{Domain.Tests,Application.Tests,UI.Tests}`
- Choisir Avalonia **11.x** (contrainte) et une version .NET LTS (recommandé : .NET 10) ; verrouiller les versions via le tooling du repo.
- Ajouter l’outillage qualité :
  - `.editorconfig` (120 colonnes, style)
  - analyseurs (complexité/qualité)
  - convention de commits
- Ajouter :
  - `.gitignore` (.NET)
  - un README minimal (build/test/run)

### 3) Automatisation (conforme "commandes en Python")
- Ajouter des scripts Python (ex : dossier `scripts/`) pour :
  - restore / build / test
  - format / lint
  - exécuter l’app
- Mettre en place une CI GitHub Actions (build + tests) sur PR/push.

### 4) Domain : noyau de simulation (TDD, sans dépendances UI)
- Concevoir un moteur déterministe à pas de temps fixe :
  - `GameState`
  - `GameTick(TickDelta)`
  - `GameSpeed`
  - `WaveState`
- Value Objects :
  - `GridPosition`, `Health`, `Gold`, `Budget`, `Damage`, `Range`, `AttackSpeed`
- Entités/Aggregates :
  - `Tower`, `Unit`, `Base`, `Wave`, `Map` (chemin pré-déterminé via waypoints)
  - règles : placement autorisé/interdit, collisions simples
- Ports (si nécessaire) pour testabilité/déterminisme :
  - `IRandomProvider`, `ITimeProvider`, `IIdGenerator`

### 5) Domain : règles de combat et progression (TDD)
- Mouvement des unités sur chemin (sans pathfinding au début) + arrivée base ⇒ dégâts base.
- Acquisition de cible et tir : stratégie par défaut "premier sur le chemin" (extensible), cadence, portée, dégâts.
- AoE : optionnel, uniquement après stabilité single-target.
- Économie :
  - mort d’unités ⇒ loot or (défenseur)
  - budget attaquant par vague + report (règle simple)
- Conditions de victoire/défaite :
  - base HP
  - fin de dernière vague

### 6) Application : cas d’usage (orchestration, API stable pour l’UI)
- Définir des commandes/use-cases :
  - `StartGame`, `StartWave`, `SkipPreparation`, `TickGame`
  - `PlaceTower`, `UpgradeTower`, `SellTower`
  - `SendUnit` (type + voie)
  - `SetGameSpeed`
- Exposer un snapshot sérialisable côté UI : `GameStateDto` / `HudDto` (pas de types Avalonia dans Application).
- Validation + retours d’erreurs métier (ex : or insuffisant, placement invalide) explicites.

### 7) UI Avalonia (MVVM) : boucle jouable MVP
- Vue unique (MainWindow) conforme au HUD du GDD :
  - rôle
  - or/budget
  - HP base
  - vague X/Y + progression

- `GameBoard` : rendu 2D vectoriel minimal (formes) : tours, unités, chemin.
- Interactions Défenseur : choisir type de tour, placer sur grille, upgrade, vendre.
- Interactions Attaquant : choisir type d’unité + voie, envoyer ; visualiser progression.
- Contrôle vitesse (0.5x–2x) + bouton "Skip".

### 8) Contenu MVP
- Implémenter 3–4 tours (ex : Tireur, Canon AoE, Sniper, Support/Slow).
- Implémenter 4–5 unités (ex : Grunt, Archer, Tank, Mage, Healer ou Volant selon simplicité).
- Ajouter :
  - upgrades N1→N3 (coûts croissants)
  - revente (~75%)
- UI : HP ennemis, feedback de cible (si nécessaire).

### 9) Solo / IA (optionnel mais couvre US 1.3)
- Ajouter une IA simple comme adapter (hors Domain) :
  - heuristiques d’achat/placement (Défenseur)
  - composition de vague (Attaquant)
- Permettre "jouer avec/sans joueur" sans modifier le moteur : l’IA émet des commandes Application.

### 10) Réseau (phase ultérieure)
- Choisir une architecture serveur-authoritative (recommandé) + protocole :
  - messages d’inputs (commandes)
  - state snapshots
- Isoler la couche réseau dans Infrastructure.
- Garder le Domain déterministe pour faciliter la synchro.
- Ajouter tests de compat : simulation d’inputs, latence, désynchro + outils de debug (seed RNG, logs structurés).

## Vérification
1. Tests Domain (TDD) : mouvement, tir, dégâts base, économie, progression de vague, victoire/défaite.
2. Tests Application : enchaînements de use-cases + validations d’erreurs.
3. Smoke test UI : partie complète 10 vagues (vitesse 1x/2x), conditions win/lose, aucune exception.
4. CI : build + tests obligatoires sur PR.

## Décisions
- MVP = 1 carte 1 voie, placement sur grille, 10 vagues par défaut, tir auto "premier sur chemin", local 1v1 avant réseau.
- Pouvoirs spéciaux et 2v2 : hors MVP (réintroduits après stabilisation du core).

## Considérations futures
1. Data-driven balancing (stats tours/unités via fichiers) uniquement quand le core est stable.
2. Stratégies de ciblage avancées (first/last/strongest + ciblage manuel) après la boucle MVP validée.
3. Multi-voies (2–3 chemins) après validation performance/UX sur 1 voie.

## Références
- `Docs/GDD.md`
- `Docs/User Story Tower Defense.md`
- `Docs/Constitution.md`
- `.github/copilot-instructions.md`
