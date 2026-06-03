using System.Collections.Generic;
using System.Threading.Tasks;
using TowerFluffy.Domain.Engine;
using TowerFluffy.Domain.Combat;

namespace TowerFluffy.Application.Common.Networking;

public interface IGameClient
{
    Task ReceiveGameState(GameState state);
    Task ReceiveCombatEvent(CombatEvent @event);
    Task ReceiveChat(string sender, string message);
    Task ReceivePlayerAction(PlayerAction action);
    Task ReceiveGameStarted(int seed, long startTimeUtc);
    Task ReceiveOpponentReady(bool isReady);
    Task ReceiveRole(int role);
    Task ReceiveGameList(List<GameInfoDto> games);
}
