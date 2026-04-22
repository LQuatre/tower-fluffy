# Game Design Document (GDD)
## Tower Defense Multiplayer : Attaque vs Défense

---

## 1. Informations générales

| Paramètre | Valeur |
| --- | --- |
| **Titre du jeu** | Tower Defense: Attaque vs Défense |
| **Genre** | Tower Defense / PvP Stratégie |
| **Type de jeu** | Multijoueur asymétrique en temps réel |
| **Plateforme** | Desktop (C# Avalonia) |
| **Cible** | Joueurs stratégiques, fans de tower defense, compétitif casual |

---

## 2. Concept global

### Vision
Un **tower defense multijoueur asymétrique** où deux joueurs s'affrontent :
- **Défenseur** : place des tours pour protéger sa base et survivre à toutes les vagues
- **Attaquant** : commande des unités pour franchir la défense et détruire la base ennemie

### Objectif du jeu
- **Défenseur gagne** : s'il survit à TOUTES les vagues (base > 0 HP)
- **Attaquant gagne** : si la base du défenseur atteint 0 HP avant la fin de la dernière vague

---

## 3. Modes de jeu

### 3.1 Mode 1v1
- 1 Défenseur vs 1 Attaquant
- Durée estimée : 10-15 minutes
- Équilibrage optimal

### 3.2 Mode 2v2
- 2 Défenseurs (coop) vs 2 Attaquants (coop)
- Partage des ressources / vision commune
- Coordination requise

---

## 4. Système de vagues

### 4.1 Vagues configurables
- Nombre de vagues **configurable** avant le match
- Progression de difficulté **linéaire ou exponentielle** (à déterminer)
- Chaque vague s'intensifie :
  - Augmentation de la **santé** des ennemis
  - Augmentation du **dégât** des ennemis
  - Augmentation du **nombre** d'ennemis
  - Introduction de **nouveaux types** d'unités

### 4.2 Temps entre les vagues
- Préparation : **30-45 secondes** entre chaque vague
- Le défenseur peut placer/améliorer des tours
- L'attaquant prépare sa composition d'unités suivante

### 4.3 Temps entre les attaques (vague)
- L'attaquant envoie ses unités progressivement ou tout en même temps (à tester)
- Les ennemis suivent les **chemins** définis sur la carte
- Les tours les attaquent en chemin

---

## 5. Système d'économie et ressources

### 5.1 Oro (Défenseur)
- **Source de revenus :**
  - Tuer un ennemi = **loot or** (proportionnel au niveau/type)
  - Revenu passif périodique (optional) : ex. 10 or par seconde
  - Bonus pour "clear" complet d'une vague

- **Utilisation :**
  - Placer une tour (coût selon type)
  - Améliorer une tour (coûts croissants)
  - Revendre une tour (récupère ~75% du prix)
  - Activations de pouvoirs spéciaux (une fois par partie)

### 5.2 Budget d'attaque (Attaquant)
- **Allocation :**
  - Budget **fixe par vague** (ex: 100 points)
  - Budget inutilisé = **report sur la vague suivante** (+25% bonus ?)
  - Bonus points si des unités survivent à la vague

- **Utilisation :**
  - Envoyer 1 unité légère (5 pts)
  - Envoyer 1 unité tank (15 pts)
  - Lancer une attaque spéciale (30 pts)

---

## 6. Types de tours (Défenseur)

Le défenseur accède à **plusieurs types de tours** avec des coûts et effets variés :

| Type | Coût | Attaque | Spécialité | Notes |
| --- | --- | --- | --- | --- |
| **Tireur rapide** | 50 | Rapide + moyen | Dégâts physiques | Bonne contre mobs légers |
| **Canon** | 100 | Lent + zone | Zone dégâts (AoE) | Parfait pour crowd control |
| **Laser** | 120 | Rapide + pénétration | Perce armure | Tir continu, chère |
| **Sniper** | 80 | Très lent + très puissant | Single target | Cible unités costauds |
| **Support** | 70 | Non | Buff/Slow aura | Ralentit ennemis, buff alliés |
| **Électrique** | 110 | Rapide + chaîne | Chaine à mobs proches | Enchainable |
| **Glace** | 90 | Rapide + slow | Ralentissement | Réduit vitesse ennemis |

### Améliorations de tour
- **Niveau 1→2→3** (croissance exponentielle du coût)
- **Options d'amélioration :**
  - +20% dégâts
  - +15% vitesse d'attaque
  - +10% portée
  - Effet spécial (critique, pénétration supplémentaire)

---

## 7. Types d'unités (Attaquant)

L'attaquant dispose d'une **variété** d'unités :

| Type | Coût | HP | Vitesse | Spécialité | Notes |
| --- | --- | --- | --- | --- | --- |
| **Grunt** | 5 | Bas | Moyen | Basique | Cheap, spam possible |
| **Archer** | 7 | Bas | Rapide | À distance | Contourne certaines tours |
| **Tank** | 15 | Très haut | Lent | Tanking dégâts | Absorbe fire des tours |
| **Mage** | 12 | Moyen | Moyen | Magique (ignoré armure) | Dégâts magiques |
| **Volant** | 10 | Bas | Très rapide | Survole obstacles | Ignore certains chemins |
| **Boss** | 30 | Extrême | Très lent | Boss unique | 1 par vague max |
| **Healer** | 8 | Bas | Moyen | Soigne alliés | Supportant |

---

## 8. Cartes

### 8.1 Structure des cartes
- **Multiples voies** (2-3 chemins distincts)
  - Attaquant choisit où envoyer ses unités
  - Défenseur doit couvrir toutes les voies

- **Zones de placement :**
  - Tours placées sur zones définies (grille ou libre ?)
  - Bases sont en zones restreintes

- **Variation :**
  - Carte "Forêt" : voies serrées
  - Carte "Tundra" : voies larges avec zones d'attente
  - Carte "Urbain" : multiples niveaux

### 8.2 Vision
- Défenseur voit **toute la carte** en temps réel
- Attaquant voit **ses unités + la route** (pas vision complète)

---

## 9. Pouvoirs spéciaux

### 9.1 Défenseur - Exemples de pouvoirs
- **Stun Grenades** : paralyse ennemis pour 3 sec
- **Bouclier Énergétique** : base gagne +50 HP temporaire
- **Turbo Towers** : toutes les tours x2 dégâts pendant 10 sec
- **Minefield** : crée zone de dégâts au sol
- **Freeze Field** : ralentit tous ennemis de 50%

### 9.2 Attaquant - Exemples de pouvoirs
- **Rush** : toutes les unités +100% vitesse temp
- **Armor Buff** : +50% HP à TOUTES les unités temp
- **Ressurection** : ramène 3 unités mortes
- **Temporal Bomb** : tour détruite à distance
- **Invisibility** : unités invisibles 5 sec

### 9.3 Utilisation
- 1 pouvoir activable **une fois par partie** (ou par vague ?)
- Coûte des ressources ou s'active sous condition
- Stratégie clée : savoir QUAND l'utiliser

---

## 10. HUD et Interface

### 10.1 Écran principal
```
┌─────────────────────────────────────────┐
│ [DÉFENSEUR] | Oro: 500 | HP Base: 100   │
│ [Vague 3/10] [████████ 80% done]        │
│                                         │
│         [CARTE PRINCIPALE]              │
│         - Tours visibles               │
│         - Unités ennemis               │
│         - Chemins clairs               │
│                                         │
├─────────────────────────────────────────┤
│ Tours disponibles:                       │
│ [Tireur 50] [Canon 100] [Laser 120]     │
│ [Sniper 80] [Support 70] [Électrique]   │
│                                         │
│ [POUVOIR SPECIAL] | [SETTINGS]          │
└─────────────────────────────────────────┘
```

### 10.2 Informations affichées
| Élément | Défenseur | Attaquant |
| --- | --- | --- |
| Ressources | Or total + par/sec | Budget vague + report |
| Santé base | Barre progressive | Cible de l'attaque |
| Vagues | Nombre restant | Composition unités |
| Pouvoirs | Actif/Cooldown | Actif/Cooldown |

### 10.3 Actions clés
- **Défenseur :** Clic sur empty → place tour (si ressources) → annuler (1x) → améliorer
- **Attaquant :** Sélectionne unité → choisit voie → confirme → envoie
- **Deux :** Activation pouvoir spécial

---

## 11. Mécanique de victoire et défaite

### 11.1 Conditions de victoire défenseur
✅ **Survie à toutes les vagues** avec base > 0 HP
- Display : "**VICTOIRE - Défense réussie !**"
- Récompense : points rank + achievements

### 11.2 Conditions de défaite défenseur
❌ **Base atteint 0 HP avant la dernière vague**
- Attaquant gagne instantanément
- Récompense pour attaquant

### 11.3 Conditions de victoire attaquant
✅ **Destruction de la base avant fin de vagues**
- Display : "**VICTOIRE - Base détruite !**"

---

## 12. Progression et matchmaking

### 12.1 Système de rang
- **ELO / Rating** basé sur victoires/défaites
- Différence de rang = bonus/malus de points
- Placement initial : 1000 ELO

### 12.2 Déblocages progressifs
- Vagues 1-3 : tours basiques uniquement
- Vagues 4-7 : tour support + améliorations
- Vagues 8+ : tous les types disponibles

### 12.3 Cosmétiques et skins
- Tours avec visual theme (neon, medieval, etc.)
- Unités avec couleurs custom
- Particules d'attaque customisables

---

## 13. Balancing et ajustements

### 13.1 Points clés d'équilibre
- Tour la **plus chère ne doit pas dominer**
- Budget attaquant vs coût tour moyen
- Vague 1 doit être **"gagnante naturelle"** pour défenseur
- Progression difficulté : **linéaire vs exponentielle** ?

### 13.2 KPIs à monitor
- Taux de victoire Défenseur : **idéal ~50%**
- Durée moyenne partie : **10-15 min**
- Usage pouvoir spécial : **>80%** (important stratégiquement ?)

---

## 14. Technologie et architecture

### 14.1 Stack technique
- **Langage** : C#
- **Framework UI** : Avalonia (desktop cross-platform)
- **Graphismes** : 2D (canvas ou sprite-based)
- **Réseau** : TCP/UDP (à déterminer) ou local

### 14.2 Architecture logique
```
┌─ Entités
│  ├─ Tower.cs
│  ├─ Unit.cs
│  ├─ Player.cs
│  └─ Map.cs
├─ Managers
│  ├─ GameManager.cs (logique globale)
│  ├─ WaveManager.cs (gestion vagues)
│  ├─ EconomyManager.cs (ressources)
│  └─ EventManager.cs (broadcast événements)
├─ Network (if multiplayer)
│  ├─ NetworkManager.cs
│  └─ Protocol.cs
└─ UI
   ├─ MainWindow.xaml
   ├─ GameBoard.xaml
   └─ HUD.xaml
```

### 14.3 Communication réseau (futur)
- Fréquence sync : **30 Hz**
- Messages : positions, actions, ressources
- Validation serveur : **anti-cheat**

---

## 15. Onboarding et tutoriel

### 15.1 Tutoriel Défenseur
1. **Leçon 1** : Placer une première tour (guide visuel)
2. **Leçon 2** : Tuer des ennemis = or
3. **Leçon 3** : Améliorer une tour
4. **Leçon 4** : Utiliser pouvoir spécial
5. **Mini-jeu** : Survive 5 vagues guidées

### 15.2 Tutoriel Attaquant
1. **Leçon 1** : Envoyer une unité
2. **Leçon 2** : Budget limité = choix
3. **Leçon 3** : Choisir voie / type unité
4. **Leçon 4** : Coordonner armée
5. **Mini-jeu** : Casser 3 tours

### 15.3 Options de confort
- **Pause** possible en solo
- **Speed multiplier** : 0.5x à 2x
- **Keybindings** customisables
- **Colorblind mode**

---

## 16. Roadmap MVP (Minimum Viable Product)

### Phase 1 : Prototype (Semaine 1-2)
- [ ] Map 1 voie simple
- [ ] Système Défenseur (place/améliore tours)
- [ ] Système Attaquant (budget, envoi unités)
- [ ] Vagues + HP système
- [ ] Win/Lose conditions

### Phase 2 : Solo + Local (Semaine 3-4)
- [ ] Mode 1v1 local (2 joueurs même PC)
- [ ] 3-4 types de tours
- [ ] 4-5 types d'unités
- [ ] Pouvoirs spéciaux basiques
- [ ] Progression vagues (difficultés)

### Phase 3 : Équilibre et Polish (Semaine 5-6)
- [ ] Balance tour vs HP unité
- [ ] Balance coûts ressources
- [ ] Effets visuels (hit, explo, spawn)
- [ ] Sounds basiques
- [ ] UI complète

### Phase 4 : Réseau (Semaine 7-8)
- [ ] Mode matchmaking 1v1 online
- [ ] Mode 2v2 (futur)
- [ ] Persistance slots joueur

---

## 17. Notes et Questions ouvertes

### À clarifier ensemble
1. **Nombre exact de vagues par défaut ?** (10, 20, custom ?)
2. **Placement libre ou sur grille ?**
3. **Attaquant choisit vague entière ou trickle ?**
4. **Respawn ennemis si tutés ? Ou vague = X unités fixe ?**
5. **Pouvoirs spéciaux : 1 par partie ou par vague ?**
6. **Seed map aléatoire ou maps prédéfinies ?**

### À tester rapidement (playtesting)
- Équilibre 1v1 après proto
- Durée idéale (trop court ? trop long ?)
- Tension narrative (pic difficulté à quelle vague ?)

---

## 18. Glossaire

| Terme | Définition |
| --- | --- |
| **Défenseur** | Joueur qui place des tours pour protéger sa base |
| **Attaquant** | Joueur qui envoie des unités pour détruire la base |
| **Vague** | Batch d'ennemis envoyés par l'attaquant |
| **Tour** | Unité défensive placée et opérée par le défenseur |
| **Oro** | Monnaie du défenseur (récolte en tuant) |
| **Budget** | Points d'attaque limités par vague (attaquant) |
| **AoE** | Area of Effect = dégâts en zone |
| **Loot** | Ressources droppées par ennemis tués |
| **ELO** | Système de ranking compétitif |

---

**Document créé :** 3 avril 2026  
**Version :** 1.0 MVP  
**Auteurs :** Dylan, Lucas
