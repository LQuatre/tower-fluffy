namespace TowerFluffy.Application.Common.Networking;

public enum PlayerActionKind
{
    PlaceTower,
    SendWave,
    UpgradeTower,
    MoveTower,
    SkipPreparation
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
