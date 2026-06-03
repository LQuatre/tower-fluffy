using Microsoft.AspNetCore.SignalR;
using TowerFluffy.Application.Common.Networking;
using TowerFluffy.Domain.Engine;
using TowerFluffy.Domain.Combat;
using System.Collections.Concurrent;

namespace TowerFluffy.Server.Hubs;

public class GameHub : Hub<IGameClient>, IGameHub
{
    // Mapping: GameId -> HashSet of ConnectionIds
    private static readonly ConcurrentDictionary<string, HashSet<string>> _games = new();
    // Mapping: GameId -> GameStarted Status
    private static readonly ConcurrentDictionary<string, bool> _gameStarted = new();
    // Mapping: ConnectionId -> IsReady
    private static readonly ConcurrentDictionary<string, bool> _playerReady = new();
    // Mapping: ConnectionId -> GameId
    private static readonly ConcurrentDictionary<string, string> _playerToGame = new();
    // Mapping: ConnectionId -> Role (1 = Attacker, 2 = Defender)
    private static readonly ConcurrentDictionary<string, int> _playerRoles = new();
    // Mapping: GameId -> BalancingSettingsJson
    private static readonly ConcurrentDictionary<string, string?> _gameBalancingSettings = new();

    public async Task JoinGame(string gameId, int requestedRole)
    {
        if (string.IsNullOrEmpty(gameId)) return;

        Console.WriteLine($"[JOIN] Player {Context.ConnectionId} -> Game {gameId}");
        
        // Quitter l'ancienne game si nécessaire
        if (_playerToGame.TryRemove(Context.ConnectionId, out var oldGameId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, oldGameId);
            if (_games.TryGetValue(oldGameId, out var players))
            {
                lock(players) players.Remove(Context.ConnectionId);
            }
        }

        _playerToGame[Context.ConnectionId] = gameId;
        await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
        
        var gamePlayers = _games.GetOrAdd(gameId, _ => new HashSet<string>());
        int assignedRole;
        lock(gamePlayers)
        {
            if (gamePlayers.Count == 0)
            {
                assignedRole = requestedRole == 0 ? 2 : requestedRole;
            }
            else
            {
                var firstPlayer = gamePlayers.First();
                var firstRole = _playerRoles.GetValueOrDefault(firstPlayer, 2);
                assignedRole = firstRole == 1 ? 2 : 1;
            }
            gamePlayers.Add(Context.ConnectionId);
            _playerRoles[Context.ConnectionId] = assignedRole;
        }
        _gameStarted.TryAdd(gameId, false);

        await Clients.Caller.ReceiveRole(assignedRole);
        await Clients.Group(gameId).ReceiveChat("SERVEUR", $"Un nouveau pilote a rejoint la salle {gameId}");
        await NotifyGameListChanged();
    }

    public async Task LeaveGame()
    {
        if (_playerToGame.TryGetValue(Context.ConnectionId, out var gameId))
        {
            await CloseRoom(gameId);
        }
    }

    private async Task CloseRoom(string gameId)
    {
        if (_games.TryRemove(gameId, out var players))
        {
            _gameStarted.TryRemove(gameId, out _);
            _gameBalancingSettings.TryRemove(gameId, out _);
            lock(players)
            {
                foreach(var p in players)
                {
                    _playerToGame.TryRemove(p, out _);
                    _playerReady.TryRemove(p, out _);
                    _playerRoles.TryRemove(p, out _);
                    Groups.RemoveFromGroupAsync(p, gameId);
                }
            }
            await Clients.Group(gameId).ReceiveRoomClosed();
            await NotifyGameListChanged();
        }
    }

    public async Task GetActiveGames()
    {
        var games = _games.Select(g => new GameInfoDto(g.Key, g.Value.Count, _gameStarted.GetValueOrDefault(g.Key, false))).ToList();
        await Clients.Caller.ReceiveGameList(games);
    }

    private async Task NotifyGameListChanged()
    {
        var games = _games.Select(g => new GameInfoDto(g.Key, g.Value.Count, _gameStarted.GetValueOrDefault(g.Key, false))).ToList();
        await Clients.All.ReceiveGameList(games);
    }

    public async Task SetReady(bool isReady, string? balancingSettingsJson)
    {
        _playerReady[Context.ConnectionId] = isReady;

        if (!_playerToGame.TryGetValue(Context.ConnectionId, out var gameId)) return;

        if (isReady && !string.IsNullOrEmpty(balancingSettingsJson))
        {
            if (_games.TryGetValue(gameId, out var roomPlayers))
            {
                string? firstPlayer;
                lock(roomPlayers) firstPlayer = roomPlayers.FirstOrDefault();
                if (firstPlayer == Context.ConnectionId)
                {
                    _gameBalancingSettings[gameId] = balancingSettingsJson;
                }
            }
        }

        // Notifier uniquement les autres joueurs de la MEME salle
        await Clients.GroupExcept(gameId, Context.ConnectionId).ReceiveOpponentReady(isReady);

        // Vérifier si tout le monde est prêt dans CETTE salle
        if (_games.TryGetValue(gameId, out var players))
        {
            bool allReady;
            lock(players)
            {
                allReady = players.Count >= 2 && players.All(p => _playerReady.GetValueOrDefault(p, false));
            }

            if (allReady && !_gameStarted.GetValueOrDefault(gameId, false))
            {
                _gameStarted[gameId] = true;
                var seed = new Random().Next();
                var startTime = DateTime.UtcNow.Ticks;
                var settingsJson = _gameBalancingSettings.GetValueOrDefault(gameId);
                
                Console.WriteLine($"[START] Lancement du combat dans la salle {gameId}");
                await Clients.Group(gameId).ReceiveGameStarted(seed, startTime, settingsJson);
            }
        }
    }

    public async Task UpdateBalancingSettings(string balancingSettingsJson)
    {
        if (!_playerToGame.TryGetValue(Context.ConnectionId, out var gameId)) return;

        if (_games.TryGetValue(gameId, out var players))
        {
            string? firstPlayer;
            lock(players) firstPlayer = players.FirstOrDefault();
            if (firstPlayer == Context.ConnectionId)
            {
                _gameBalancingSettings[gameId] = balancingSettingsJson;
            }
        }

        await Clients.GroupExcept(gameId, Context.ConnectionId).ReceiveBalancingSettingsUpdate(balancingSettingsJson);
    }

    public async Task SendPlayerAction(PlayerAction action)
    {
        if (_playerToGame.TryGetValue(Context.ConnectionId, out var gameId))
        {
            await Clients.GroupExcept(gameId, Context.ConnectionId).ReceivePlayerAction(action);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_playerToGame.TryGetValue(Context.ConnectionId, out var gameId))
        {
            await CloseRoom(gameId);
        }
        else
        {
            _playerReady.TryRemove(Context.ConnectionId, out _);
            _playerRoles.TryRemove(Context.ConnectionId, out _);
        }

        await base.OnDisconnectedAsync(exception);
        await NotifyGameListChanged();
    }

    public async Task SendChat(string message)
    {
        if (_playerToGame.TryGetValue(Context.ConnectionId, out var gameId))
        {
            await Clients.Group(gameId).ReceiveChat(Context.ConnectionId, message);
        }
    }
}
