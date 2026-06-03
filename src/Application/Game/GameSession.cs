using System;
using System.Collections.Generic;
using TowerFluffy.Domain.Match;
using TowerFluffy.Domain.Shared;
using TowerFluffy.Domain.Combat;
using TowerFluffy.Domain.Environment;
using TowerFluffy.Domain.Engine;

namespace TowerFluffy.Application.Game;

public sealed class GameSession
{
    private readonly GameConfig _config;
    private readonly Map _map;
    private MatchState _state;

    public GameSession(GameConfig config, Map map, int seed = 0)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _map = map ?? throw new ArgumentNullException(nameof(map));
        _state = MatchState.CreateNew(_config, _map, seed);
    }

    public static GameSession CreateMvp(int? seed = null) 
    {
        var finalSeed = seed ?? new Random().Next();
        var config = GameConfig.CreateMvpDefaults();
        var map = DefaultMapFactory.Create(1, finalSeed);
        return new GameSession(config, map, finalSeed);
    }

    public MatchState State => _state;

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

    public CommandResult PlaceTower(TowerType type, GridPosition position)
    {
        var result = _state.PlaceTower(type, position);
        return Apply(result);
    }

    public CommandResult MoveTower(GridPosition oldPos, GridPosition newPos)
    {
        var result = _state.MoveTower(oldPos, newPos);
        return Apply(result);
    }

    public CommandResult SellTower(GridPosition position)
    {
        var result = _state.SellTower(position);
        return Apply(result);
    }

    public CommandResult SendUnit(UnitType type)
    {
        var result = _state.SendUnit(type);
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
