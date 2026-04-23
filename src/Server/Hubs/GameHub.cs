using Microsoft.AspNetCore.SignalR;
using TowerFluffy.Application.Common.Networking;
using TowerFluffy.Domain.Simulation;

namespace TowerFluffy.Server.Hubs;

public class GameHub : Hub<IGameClient>, IGameHub
{
    private static readonly Dictionary<string, HashSet<string>> _games = new();
    private static readonly Dictionary<string, bool> _gameStarted = new();
    private static readonly Dictionary<string, bool> _readyPlayers = new();

    public async Task JoinGame(string gameId)
    {
        if (string.IsNullOrEmpty(gameId))
        {
            Console.WriteLine("JoinGame called with null or empty gameId");
            return;
        }

        Console.WriteLine($"Player {Context.ConnectionId} joining game {gameId}");
        await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
        
        lock (_games)
        {
            if (!_games.ContainsKey(gameId))
            {
                _games[gameId] = new HashSet<string>();
                _gameStarted[gameId] = false;
            }
            _games[gameId].Add(Context.ConnectionId);
        }

        await Clients.Group(gameId).ReceiveChat("Server", $"{Context.ConnectionId} joined the game {gameId}");
        await NotifyGameListChanged();
    }

    public async Task GetActiveGames()
    {
        List<GameInfoDto> games;
        lock (_games)
        {
            games = _games.Select(g => new GameInfoDto(g.Key, g.Value.Count, _gameStarted.GetValueOrDefault(g.Key))).ToList();
        }
        await Clients.Caller.ReceiveGameList(games);
    }

    private async Task NotifyGameListChanged()
    {
        // Optionnel : on pourrait broadcaster la liste à tout le monde
        // mais pour l'instant on laisse le client demander
    }

    public async Task SetReady(bool isReady)
    {
        _readyPlayers[Context.ConnectionId] = isReady;

        // Note: Cette logique devrait être par jeu, mais pour l'instant on garde simple
        // Idéalement on récupère le gameId depuis le Context ou un tracking
        
        await Clients.Others.ReceiveOpponentReady(isReady);

        var readyCount = _readyPlayers.Values.Count(v => v);
        
        if (readyCount >= 2)
        {
            var seed = new Random().Next();
            var startTime = DateTime.UtcNow.Ticks;
            await Clients.All.ReceiveGameStarted(seed, startTime);
            
            // Marquer le jeu par défaut comme démarré (MVP)
            lock (_games) { if (_games.ContainsKey("default-game")) _gameStarted["default-game"] = true; }
        }
    }

    public async Task SendPlayerAction(PlayerAction action)
    {
        await Clients.Others.ReceivePlayerAction(action);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _readyPlayers.Remove(Context.ConnectionId);
        
        lock (_games)
        {
            foreach (var game in _games.Values)
            {
                game.Remove(Context.ConnectionId);
            }
            // Nettoyage des jeux vides
            var emptyGames = _games.Where(g => g.Value.Count == 0).Select(g => g.Key).ToList();
            foreach (var key in emptyGames) 
            {
                _games.Remove(key);
                _gameStarted.Remove(key);
            }
        }

        await base.OnDisconnectedAsync(exception);
        await NotifyGameListChanged();
    }

    public async Task SendChat(string message)
    {
        await Clients.All.ReceiveChat(Context.ConnectionId, message);
    }
}
