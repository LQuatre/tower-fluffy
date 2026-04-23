using Microsoft.AspNetCore.SignalR;
using TowerFluffy.Application.Common.Networking;
using TowerFluffy.Domain.Simulation;

namespace TowerFluffy.Server.Hubs;

public class GameHub : Hub<IGameClient>, IGameHub
{
    private static readonly Dictionary<string, bool> _readyPlayers = new();

    public async Task JoinGame(string gameId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
        await Clients.Group(gameId).ReceiveChat("Server", $"{Context.ConnectionId} joined the game {gameId}");
    }

    public async Task SetReady(bool isReady)
    {
        _readyPlayers[Context.ConnectionId] = isReady;

        // Notify other players in the lobby
        await Clients.Others.ReceiveOpponentReady(isReady);

        // Check if we have at least 2 ready players
        var readyCount = _readyPlayers.Values.Count(v => v);
        if (readyCount >= 2)
        {
            await Clients.All.ReceiveGameStarted();
        }
    }

    public async Task SendPlayerAction(PlayerAction action)
    {
        await Clients.Others.ReceivePlayerAction(action);
        await Clients.All.ReceiveChat("System", $"Player {action.PlayerId} performed {action.Kind}");
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _readyPlayers.Remove(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    public async Task SendChat(string message)
    {
        await Clients.All.ReceiveChat(Context.ConnectionId, message);
    }
}
