using System.Threading.Tasks;

namespace TowerFluffy.Application.Common.Networking;

public interface IGameHub
{
    Task JoinGame(string gameId);
    Task SetReady(bool isReady);
    Task SendPlayerAction(PlayerAction action);
    Task SendChat(string message);
    Task GetActiveGames();
}
