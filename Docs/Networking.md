# Architecture Réseau & Protocole de Jeu

## Présentation
Tower Fluffy utilise **SignalR** avec le protocole binaire **MessagePack** pour assurer une communication haute performance et basse latence entre les joueurs.

## Composants Clés

### 1. GameHub (Serveur)
Situé dans `src/Server/Hubs/GameHub.cs`, il orchestre :
- La gestion des connexions et des groupes de jeu.
- Le **Lobby System** : Suivi de l'état "Prêt" des joueurs.
- La diffusion des actions de jeu (`PlayerAction`).

### 2. SignalRGameClient (Infrastructure)
Situé dans `src/Infrastructure/Networking/SignalRGameClient.cs`, il implémente l'interface `IGameHub` (port) pour :
- Établir la connexion WebSocket.
- Gérer la sérialisation/désérialisation MessagePack.
- Exposer des événements (`OnGameStarted`, `OnPlayerActionReceived`, etc.) pour le ViewModel.

## Flux Opérationnel

### Phase de Connexion (Lobby)
1. Le client appelle `JoinGame(roomName)`.
2. Le client appelle `SetReady(true)`.
3. Le serveur vérifie si 2 joueurs sont prêts via un dictionnaire statique.
4. Le serveur diffuse `ReceiveGameStarted` à tous les clients du groupe.

### Phase de Gameplay
Les actions sont envoyées via `SendAction(action)` et reçues via `ReceiveAction(action)`.
Chaque action contient :
- `Kind` (PlaceTower, SendWave, etc.)
- `X, Y` (Coordonnées)
- `Type` (ID de l'unité ou de la tour)

## Configuration
Le serveur écoute par défaut sur le port **5128**.
L'URL de connexion standard est : `http://localhost:5128/gameHub`
