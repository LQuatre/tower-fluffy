using System;
using System.Linq;
using TowerFluffy.Application.Game.Dtos;
using TowerFluffy.Domain.Match;
using TowerFluffy.Domain.Simulation;

namespace TowerFluffy.Application.Game;

public static class GameSnapshotMapper
{
    public static GameSnapshotDto ToDto(MatchState state)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        var map = state.Map;

        var mapDto = new MapDto(
            Width: map.Grid.Width,
            Height: map.Grid.Height,
            CellSize: map.Grid.CellSize,
            BlockedCells: map.BlockedCells.Select(c => new GridPositionDto(c.X, c.Y)).ToArray(),
            Waypoints: map.Path.Waypoints.Select(w => new WorldPositionDto(w.X, w.Y)).ToArray());

        var hudDto = new HudDto(
            Tick: state.Simulation.Tick.Value,
            Level: state.CurrentLevel,
            BaseHealth: state.Simulation.BaseHealth.Value,
            DefenderGold: state.DefenderGold.Value,
            AttackerBudget: state.AttackerBudget.Value,
            WaveNumber: state.WaveNumber,
            TotalWaves: state.Config.TotalWaves,
            Phase: (MatchPhaseDto)state.Phase,
            Outcome: (MatchOutcomeDto)state.Outcome,
            PreparationTicksRemaining: state.PreparationTicksRemaining,
            WaveSendTicksRemaining: state.WaveSendTicksRemaining);

        var units = state.Simulation.Units
            .Select(u => new UnitDto(
                Id: u.Id,
                Position: ToWorldPositionDto(map.Path.GetPositionAtDistance(u.DistanceAlongPath)),
                Direction: ToWorldPositionDto(map.Path.GetDirectionAtDistance(u.DistanceAlongPath)),
                Health: u.Health.Value,
                DistanceAlongPath: u.DistanceAlongPath))
            .ToArray();

        var towers = state.Simulation.Towers
            .Select(t => new TowerDto(
                Id: t.Id,
                Type: (TowerTypeDto)t.Type,
                Cell: new GridPositionDto(t.Position.X, t.Position.Y),
                Health: t.Health.Value,
                CooldownTicksRemaining: t.CooldownTicksRemaining))
            .ToArray();

        var combatEvents = state.LastCombatEvents
            .Select(e => new CombatEventDto(
                Tick: e.Tick.Value,
                Kind: (CombatEventKindDto)e.Kind,
                SourceId: e.SourceId,
                TargetId: e.TargetId,
                From: ToWorldPositionDto(e.From),
                To: ToWorldPositionDto(e.To),
                Damage: e.Damage.Value,
                TargetDestroyed: e.TargetDestroyed))
            .ToArray();

        return new GameSnapshotDto(mapDto, hudDto, units, towers, combatEvents);
    }

    private static WorldPositionDto ToWorldPositionDto(WorldPosition position) => new(position.X, position.Y);
}
