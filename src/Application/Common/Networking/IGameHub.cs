using System.Threading.Tasks;

namespace TowerFluffy.Application.Common.Networking;

public interface IGameHub
{
    Task JoinGame(string gameId, int requestedRole);
    Task LeaveGame();
    Task SetReady(bool isReady, string? balancingSettingsJson);
    Task SendPlayerAction(PlayerAction action);
    Task SendChat(string message);
    Task GetActiveGames();
}
