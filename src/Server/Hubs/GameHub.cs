using Microsoft.AspNetCore.SignalR;
using TowerFluffy.Application.Common.Networking;
using TowerFluffy.Domain.Simulation;
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

    public async Task JoinGame(string gameId)
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
        lock(gamePlayers)
        {
            gamePlayers.Add(Context.ConnectionId);
        }
        _gameStarted.TryAdd(gameId, false);

        await Clients.Group(gameId).ReceiveChat("SERVEUR", $"Un nouveau pilote a rejoint la salle {gameId}");
        await NotifyGameListChanged();
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

    public async Task SetReady(bool isReady)
    {
        _playerReady[Context.ConnectionId] = isReady;

        if (!_playerToGame.TryGetValue(Context.ConnectionId, out var gameId)) return;

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
                
                Console.WriteLine($"[START] Lancement du combat dans la salle {gameId}");
                await Clients.Group(gameId).ReceiveGameStarted(seed, startTime);
            }
        }
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
        _playerReady.TryRemove(Context.ConnectionId, out _);
        
        if (_playerToGame.TryRemove(Context.ConnectionId, out var gameId))
        {
            if (_games.TryGetValue(gameId, out var players))
            {
                lock(players)
                {
                    players.Remove(Context.ConnectionId);
                    if (players.Count == 0)
                    {
                        _games.TryRemove(gameId, out _);
                        _gameStarted.TryRemove(gameId, out _);
                    }
                }
            }
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
