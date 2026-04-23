# Dossier de Présentation : TowerFluffy

Ce document contient les schémas techniques et les arguments clés pour votre passage à l'oral.

---

## 1. Architecture Globale (Clean Architecture)
Nous avons séparé le projet en couches pour garantir que la logique de jeu est indépendante de l'interface graphique et du réseau.

```mermaid
graph TD
    UI[UI.Desktop - Avalonia] --> App[Application - Logic de Session]
    App --> Domain[Domain - Simulation & Entités]
    Infra[Infrastructure - SignalR Client] --> App
    Server[Server - ASP.NET Core Hub] -- SignalR --> Infra
    
    subgraph "Noyau de Jeu (Logic)"
        Domain
        App
    end
    
    subgraph "Entrées/Sorties"
        UI
        Infra
    end
```

**Argument oral :** *"Nous avons utilisé une Clean Architecture. Si demain nous voulons passer d'Avalonia à Unity ou du SignalR au WebSockets pur, le Noyau de Jeu (Domain) ne changerait pas d'une seule ligne."*

---

## 2. Communication Réseau (Groupes & Temps Réel)
Le multijoueur repose sur SignalR avec une gestion de salles (Game IDs).

```mermaid
sequenceDiagram
    participant P1 as Pilote A (Client)
    participant S as Serveur (SignalR Hub)
    participant P2 as Pilote B (Client)
    
    P1->>S: JoinGame("Room-123")
    P2->>S: JoinGame("Room-123")
    Note over S: Les joueurs sont isolés<br/>dans le groupe "Room-123"
    
    P1->>S: SetReady(true)
    S-->>P2: ReceiveOpponentReady(true)
    
    P2->>S: SetReady(true)
    Note over S: Tout le monde est prêt !
    S->>S: Générer Seed (Aléatoire)
    S-->>P1: ReceiveGameStarted(seed, time)
    S-->>P2: ReceiveGameStarted(seed, time)
```

**Argument oral :** *"La gestion des salles est sécurisée via les Groupes SignalR. Les joueurs ne reçoivent que les données tactiques de leur propre opération, ce qui optimise la bande passante."*

---

## 3. Boucle de Simulation (Tick Rate)
Pour éviter la désynchronisation, le jeu tourne à 60 Ticks par seconde.

```mermaid
graph LR
    Start(Tick Initial) --> Input[Récupérer Actions Réseau]
    Input --> Move[Déplacement des Unités]
    Move --> Combat[Calcul des Dégâts / Tours]
    Combat --> UI_Update[Mise à jour graphique]
    UI_Update --> Wait[Attendre 16ms]
    Wait --> Start
```

**Argument oral :** *"Le moteur de simulation est déterministe. Avec la même 'Seed' et le même timing, les deux joueurs voient exactement la même bataille se dérouler sur leurs écrans respectifs."*

---

## 4. Points Forts du Projet
- **Asymétrie Tactique** : Un rôle Défenseur (gestion de base) et un rôle Attaquant (stratégie d'invasion).
- **Design Immersif** : Interface "High-Tech Tactical HUD" développée avec SukiUI.
- **Robustesse** : Utilisation du protocole binaire **MessagePack** pour des échanges réseau ultra-rapides.
- **Multi-plateforme** : Code .NET 9 compatible Windows, Linux et macOS.

---

## 5. Démo : Scénario Idéal
1. **Connexion** : Montrer l'UPLINK automatique.
2. **Lobby** : Créer une salle "EXAMEN-2026".
3. **Préparation** : Choisir les rôles, cliquer sur "Prêt".
4. **Gameplay** : Poser une tour, envoyer une vague, et montrer la barre de vie qui descend en temps réel sur les deux écrans.
