using System;
using System.Collections.Generic;
using System.Linq;
using TowerFluffy.Domain.Simulation;

namespace TowerFluffy.Domain.Match;

public sealed record GameConfig(
    int TotalWaves,
    int PreparationTicks,
    int WaveSendWindowTicks,
    Budget BaseWaveBudget,
    Budget WaveBudgetIncrement,
    Budget BudgetBonusPerTowerDestroyed,
    Gold StartingGold,
    Health StartingBaseHealth,
    IReadOnlyList<TowerDefinition> Towers,
    IReadOnlyList<UnitDefinition> Units)
{
    public static GameConfig CreateMvpDefaults()
    {
        return new GameConfig(
            TotalWaves: 10,
            PreparationTicks: 60 * 30,
            WaveSendWindowTicks: 60 * 20,
            BaseWaveBudget: new Budget(100),
            WaveBudgetIncrement: new Budget(20),
            BudgetBonusPerTowerDestroyed: new Budget(10),
            StartingGold: new Gold(100),
            StartingBaseHealth: new Health(100),
            Towers: new[]
            {
                new TowerDefinition(
                    TowerType.BasicShooter,
                    new TowerStats(
                        Cost: new Gold(50),
                        DamagePerShot: new Damage(1),
                        Range: 160,
                        CooldownTicksBetweenShots: 15),
                    Health: new Health(10)),
            },
            Units: new[]
            {
                new UnitDefinition(
                    UnitType.Grunt,
                    Cost: new Budget(10),
                    Health: new Health(3),
                    SpeedPerTick: 4,
                    DamageToBase: new Damage(1),
                    DamageToTower: new Damage(1),
                    AttackRange: 80,
                    AttackCooldownTicksBetweenAttacks: 30,
                    LootGold: new Gold(5)),
            });
    }

    public Budget GetWaveBudget(int waveNumber)
    {
        if (waveNumber < 1 || waveNumber > TotalWaves)
        {
            throw new ArgumentOutOfRangeException(nameof(waveNumber), waveNumber, "Wave number is out of range.");
        }

        return BaseWaveBudget.Add(new Budget((waveNumber - 1) * WaveBudgetIncrement.Value));
    }

    public TowerDefinition GetTower(TowerType type)
    {
        var tower = Towers.FirstOrDefault(t => t.Type == type);
        if (EqualityComparer<TowerDefinition>.Default.Equals(tower, default))
        {
            throw new InvalidOperationException($"Tower type not configured: {type}");
        }

        return tower;
    }

    public UnitDefinition GetUnit(UnitType type)
    {
        var unit = Units.FirstOrDefault(u => u.Type == type);
        if (EqualityComparer<UnitDefinition>.Default.Equals(unit, default))
        {
            throw new InvalidOperationException($"Unit type not configured: {type}");
        }

        return unit;
    }
}
