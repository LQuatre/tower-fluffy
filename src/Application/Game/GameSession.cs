using System;
using TowerFluffy.Application.Game.Dtos;
using TowerFluffy.Domain.Match;
using TowerFluffy.Domain.Simulation;

namespace TowerFluffy.Application.Game;

public sealed class GameSession
{
    private readonly GameConfig _config;
    private readonly Map _map;
    private MatchState _state;

    public GameSession(GameConfig config, Map map)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _map = map ?? throw new ArgumentNullException(nameof(map));
        _state = MatchState.CreateNew(_config, _map);
    }

    public static GameSession CreateMvp() => new(GameConfig.CreateMvpDefaults(), DefaultMapFactory.Create());

    public GameSnapshotDto Snapshot => GameSnapshotMapper.ToDto(_state);

    public void Reset() => _state = MatchState.CreateNew(_config, _map);

    public void Tick(int ticks)
    {
        if (ticks <= 0)
        {
            return;
        }

        List<CombatEvent>? aggregatedEvents = null;

        for (var i = 0; i < ticks; i++)
        {
            _state = _state.Tick();

            var tickEvents = _state.LastCombatEvents;
            if (tickEvents.Count == 0)
            {
                continue;
            }

            aggregatedEvents ??= new List<CombatEvent>(capacity: tickEvents.Count * ticks);
            aggregatedEvents.AddRange(tickEvents);
        }

        _state = _state with
        {
            LastCombatEvents = aggregatedEvents is null
                ? Array.Empty<CombatEvent>()
                : aggregatedEvents.ToArray(),
        };
    }

    public CommandResult SkipPreparation()
    {
        var result = _state.SkipPreparation();
        return Apply(result);
    }

    public CommandResult PlaceTower(TowerTypeDto type, GridPositionDto position)
    {
        var domainType = (TowerType)type;
        var domainPosition = new GridPosition(position.X, position.Y);

        var result = _state.PlaceTower(domainType, domainPosition);
        return Apply(result);
    }

    public CommandResult MoveTower(GridPositionDto oldPos, GridPositionDto newPos)
    {
        var domainOld = new GridPosition(oldPos.X, oldPos.Y);
        var domainNew = new GridPosition(newPos.X, newPos.Y);

        var result = _state.MoveTower(domainOld, domainNew);
        return Apply(result);
    }

    public CommandResult SendUnit(UnitTypeDto type)
    {
        var domainType = (UnitType)type;
        var result = _state.SendUnit(domainType);
        return Apply(result);
    }

    private CommandResult Apply(DomainResult<MatchState> result)
    {
        if (result.IsSuccess)
        {
            _state = result.Value!;
            return CommandResult.Success();
        }

        return CommandResult.Failure(result.Error!.Value.Message);
    }
}
