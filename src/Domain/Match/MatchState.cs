using System;
using System.Linq;
using TowerFluffy.Domain.Simulation;

namespace TowerFluffy.Domain.Match;

public sealed record MatchState(
    GameConfig Config,
    Map Map,
    GameState Simulation,
    int WaveNumber,
    int CurrentLevel,
    MatchPhase Phase,
    MatchOutcome Outcome,
    Gold DefenderGold,
    Budget AttackerBudget,
    Budget CarryOverBudget,
    int PreparationTicksRemaining,
    int WaveSendTicksRemaining,
    int NextEntityId,
    IReadOnlyList<CombatEvent> LastCombatEvents)
{
    public static MatchState CreateNew(GameConfig config, Map map)
    {
        if (config is null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        if (map is null)
        {
            throw new ArgumentNullException(nameof(map));
        }

        var simulation = new GameState(
            tick: new Tick(0),
            baseHealth: config.StartingBaseHealth,
            units: Array.Empty<Unit>(),
            towers: Array.Empty<Tower>());

        return new MatchState(
            Config: config,
            Map: map,
            Simulation: simulation,
            WaveNumber: 0,
            CurrentLevel: 1,
            Phase: MatchPhase.Preparation,
            Outcome: MatchOutcome.None,
            DefenderGold: config.StartingGold,
            AttackerBudget: Budget.Zero,
            CarryOverBudget: Budget.Zero,
            PreparationTicksRemaining: config.PreparationTicks,
            WaveSendTicksRemaining: 0,
            NextEntityId: 1,
            LastCombatEvents: Array.Empty<CombatEvent>());
    }

    public MatchState Tick()
    {
        if (Outcome != MatchOutcome.None)
        {
            return this with { LastCombatEvents = Array.Empty<CombatEvent>() };
        }

        if (Phase == MatchPhase.Preparation)
        {
            return TickPreparation();
        }

        if (Phase == MatchPhase.Wave)
        {
            return TickWave();
        }

        return this with { LastCombatEvents = Array.Empty<CombatEvent>() };
    }

    public DomainResult<MatchState> SkipPreparation() => StartWave();

    public DomainResult<MatchState> StartWave()
    {
        if (Outcome != MatchOutcome.None)
        {
            return DomainResult<MatchState>.Failure("match.finished", "Le match est terminé.");
        }

        if (Phase != MatchPhase.Preparation)
        {
            return DomainResult<MatchState>.Failure("match.phase", "Impossible de démarrer la vague en dehors de la préparation.");
        }

        if (WaveNumber >= Config.TotalWaves)
        {
            return DomainResult<MatchState>.Failure("match.waves", "Plus de vagues restantes.");
        }

        var nextWave = WaveNumber + 1;
        var waveBudget = Config.GetWaveBudget(nextWave);

        var started = this with
        {
            WaveNumber = nextWave,
            Phase = MatchPhase.Wave,
            PreparationTicksRemaining = 0,
            WaveSendTicksRemaining = Config.WaveSendWindowTicks,
            AttackerBudget = CarryOverBudget.Add(waveBudget),
            CarryOverBudget = Budget.Zero,
            LastCombatEvents = Array.Empty<CombatEvent>(),
        };

        return DomainResult<MatchState>.Success(started);
    }

    public DomainResult<MatchState> PlaceTower(TowerType type, GridPosition position)
    {
        if (Outcome != MatchOutcome.None)
        {
            return DomainResult<MatchState>.Failure("match.finished", "Le match est terminé.");
        }

        if (Phase != MatchPhase.Preparation)
        {
            return DomainResult<MatchState>.Failure("tower.phase", "Les tours ne peuvent être placées que pendant la préparation.");
        }

        if (!Map.IsBuildable(position))
        {
            return DomainResult<MatchState>.Failure("tower.placement", "Impossible de placer une tour sur cette cellule.");
        }

        if (Simulation.Towers.Any(t => t.Position == position))
        {
            return DomainResult<MatchState>.Failure("tower.occupied", "Une tour existe déjà sur cette cellule.");
        }

        var definition = Config.GetTower(type);
        if (!DefenderGold.CanAfford(definition.Stats.Cost))
        {
            return DomainResult<MatchState>.Failure("tower.gold", "Pas assez d'or.");
        }

        var tower = new Tower(
            Id: NextEntityId,
            Type: type,
            Position: position,
            Stats: definition.Stats,
            Health: definition.Health,
            CooldownTicksRemaining: 0);

        var updated = this with
        {
            DefenderGold = DefenderGold.Subtract(definition.Stats.Cost),
            Simulation = Simulation.WithAddedTower(tower),
            NextEntityId = NextEntityId + 1,
            LastCombatEvents = Array.Empty<CombatEvent>(),
        };

        return DomainResult<MatchState>.Success(updated);
    }

    public DomainResult<MatchState> MoveTower(GridPosition oldPosition, GridPosition newPosition)
    {
        if (Outcome != MatchOutcome.None)
        {
            return DomainResult<MatchState>.Failure("match.finished", "Le match est terminé.");
        }

        if (Phase != MatchPhase.Preparation)
        {
            return DomainResult<MatchState>.Failure("tower.phase", "Les tours ne peuvent être déplacées que pendant la préparation.");
        }

        if (Simulation.Towers.All(t => t.Position != oldPosition))
        {
            return DomainResult<MatchState>.Failure("tower.notfound", "Aucune tour à cet emplacement.");
        }

        if (!Map.IsBuildable(newPosition))
        {
            return DomainResult<MatchState>.Failure("tower.placement", "Impossible de placer une tour sur cette cellule.");
        }

        if (Simulation.Towers.Any(t => t.Position == newPosition))
        {
            return DomainResult<MatchState>.Failure("tower.occupied", "Une tour existe déjà sur cette cellule.");
        }

        var updated = this with
        {
            Simulation = Simulation.WithMovedTower(oldPosition, newPosition),
            LastCombatEvents = Array.Empty<CombatEvent>(),
        };

        return DomainResult<MatchState>.Success(updated);
    }

    public DomainResult<MatchState> SendUnit(UnitType type)
    {
        if (Outcome != MatchOutcome.None)
        {
            return DomainResult<MatchState>.Failure("match.finished", "Le match est terminé.");
        }

        if (Phase != MatchPhase.Wave)
        {
            return DomainResult<MatchState>.Failure("unit.phase", "Les unités ne peuvent être envoyées que pendant une vague.");
        }

        if (WaveSendTicksRemaining <= 0)
        {
            return DomainResult<MatchState>.Failure("unit.window", "La fenêtre d'envoi de la vague est fermée.");
        }

        var definition = Config.GetUnit(type);
        if (!AttackerBudget.CanAfford(definition.Cost))
        {
            return DomainResult<MatchState>.Failure("unit.budget", "Pas assez de budget.");
        }

        var unit = new Unit(
            Id: NextEntityId,
            DistanceAlongPath: 0,
            SpeedPerTick: definition.SpeedPerTick,
            DamageToBase: definition.DamageToBase,
            DamageToTower: definition.DamageToTower,
            AttackRange: definition.AttackRange,
            AttackCooldownTicksBetweenAttacks: definition.AttackCooldownTicksBetweenAttacks,
            AttackCooldownTicksRemaining: 0,
            TargetTowerId: null,
            Health: definition.Health,
            LootGold: definition.LootGold);

        var updated = this with
        {
            AttackerBudget = AttackerBudget.Subtract(definition.Cost),
            Simulation = Simulation.WithAddedUnit(unit),
            NextEntityId = NextEntityId + 1,
            LastCombatEvents = Array.Empty<CombatEvent>(),
        };

        return DomainResult<MatchState>.Success(updated);
    }

    private MatchState TickPreparation()
    {
        var remaining = Math.Max(0, PreparationTicksRemaining - 1);
        var next = this with { PreparationTicksRemaining = remaining, LastCombatEvents = Array.Empty<CombatEvent>() };

        if (remaining > 0)
        {
            return next;
        }

        var start = next.StartWave();
        return start.IsSuccess ? start.Value! : next;
    }

    private MatchState TickWave()
    {
        var sendRemaining = Math.Max(0, WaveSendTicksRemaining - 1);

        var tick = Simulation.AdvanceOneTick(Map);
        var goldEarned = tick.UnitsKilled.Aggregate(Gold.Zero, static (sum, u) => sum.Add(u.LootGold));
        var compensationGold = tick.CombatEvents
            .Where(e => e.Kind == CombatEventKind.UnitHitBase)
            .Aggregate(Gold.Zero, (sum, e) => sum.Add(new Gold(e.Damage.Value * Config.GoldPerBaseDamageTaken)));
        var budgetEarned = new Budget(tick.TowersDestroyed.Count * Config.BudgetBonusPerTowerDestroyed.Value);
        var attackerBudgetAfterTick = AttackerBudget.Add(budgetEarned);

        var stateAfterTick = this with
        {
            WaveSendTicksRemaining = sendRemaining,
            Simulation = tick.State,
            DefenderGold = DefenderGold.Add(goldEarned).Add(compensationGold),
            AttackerBudget = attackerBudgetAfterTick,
            LastCombatEvents = tick.CombatEvents,
        };

        if (tick.State.BaseHealth.Value == 0)
        {
            return stateAfterTick with { Phase = MatchPhase.Finished, Outcome = MatchOutcome.AttackerVictory };
        }

        if (sendRemaining > 0 || tick.State.Units.Count > 0)
        {
            return stateAfterTick;
        }

        if (WaveNumber >= Config.TotalWaves)
        {
            if (CurrentLevel < 3)
            {
                var nextLevel = CurrentLevel + 1;
                var nextMap = DefaultMapFactory.Create(nextLevel);
                var nextState = CreateNew(Config, nextMap) with 
                { 
                    CurrentLevel = nextLevel,
                    DefenderGold = DefenderGold.Add(new Gold(100)) // Bonus de passage de niveau
                };
                return nextState;
            }
            return stateAfterTick with { Phase = MatchPhase.Finished, Outcome = MatchOutcome.DefenderVictory };
        }

        return stateAfterTick with
        {
            Phase = MatchPhase.Preparation,
            PreparationTicksRemaining = Config.PreparationTicks,
            CarryOverBudget = CarryOverBudget.Add(attackerBudgetAfterTick),
            AttackerBudget = Budget.Zero,
        };
    }
}
