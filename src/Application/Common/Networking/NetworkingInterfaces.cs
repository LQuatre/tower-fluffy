using TowerFluffy.Domain.Simulation;

namespace TowerFluffy.Application.Common.Networking;

public interface IGameHub
{
    Task JoinGame(string gameId);
    Task SetReady(bool isReady);
    Task SendPlayerAction(PlayerAction action);
    Task SendChat(string message);
}

public interface IGameClient
{
    Task ReceiveGameState(GameState state);
    Task ReceiveCombatEvent(CombatEvent @event);
    Task ReceiveChat(string sender, string message);
    Task ReceivePlayerAction(PlayerAction action);
    Task ReceiveGameStarted(int seed, long startTimeUtc);
    Task ReceiveOpponentReady(bool isReady);
}

public record PlayerAction(
    int PlayerId, 
    PlayerActionKind Kind, 
    int? TowerType = null, 
    int? X = null, 
    int? Y = null,
    int? UnitType = null,
    int? OldX = null,
    int? OldY = null,
    long? Tick = null);

public enum PlayerActionKind
{
    PlaceTower,
    SendWave,
    UpgradeTower,
    MoveTower,
    SkipPreparation
}
