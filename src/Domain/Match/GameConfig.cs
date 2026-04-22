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
            StartingGold: new Gold(200),
            StartingBaseHealth: new Health(100),
            Towers: new[]
            {
                new TowerDefinition(
                    TowerType.BasicShooter,
                    new TowerStats(
                        Cost: new Gold(50),
                        DamagePerShot: new Damage(2),
                        Range: 200,
                        CooldownTicksBetweenShots: 20),
                    Health: new Health(15)),
                new TowerDefinition(
                    TowerType.Flamethrower,
                    new TowerStats(
                        Cost: new Gold(150),
                        DamagePerShot: new Damage(1),
                        Range: 100,
                        CooldownTicksBetweenShots: 1),
                    Health: new Health(30)),
            },
            Units: new[]
            {
                new UnitDefinition(
                    UnitType.Grunt,
                    Cost: new Budget(10),
                    Health: new Health(5),
                    SpeedPerTick: 3,
                    DamageToBase: new Damage(1),
                    DamageToTower: new Damage(2),
                    AttackRange: 100,
                    AttackCooldownTicksBetweenAttacks: 40,
                    LootGold: new Gold(10)),
                new UnitDefinition(
                    UnitType.Brute,
                    Cost: new Budget(40),
                    Health: new Health(30),
                    SpeedPerTick: 1,
                    DamageToBase: new Damage(5),
                    DamageToTower: new Damage(10),
                    AttackRange: 100,
                    AttackCooldownTicksBetweenAttacks: 60,
                    LootGold: new Gold(30)),
            });
    }

    public Budget GetWaveBudget(int waveNumber)
    {
        if (waveNumber < 1 || waveNumber > TotalWaves)
        {
            throw new ArgumentOutOfRangeException(nameof(waveNumber), waveNumber, "Le numéro de vague est hors limites.");
        }

        return BaseWaveBudget.Add(new Budget((waveNumber - 1) * WaveBudgetIncrement.Value));
    }

    public TowerDefinition GetTower(TowerType type)
    {
        var tower = Towers.FirstOrDefault(t => t.Type == type);
        if (EqualityComparer<TowerDefinition>.Default.Equals(tower, default))
        {
            throw new InvalidOperationException($"Type de tour non configuré : {type}");
        }

        return tower;
    }

    public UnitDefinition GetUnit(UnitType type)
    {
        var unit = Units.FirstOrDefault(u => u.Type == type);
        if (EqualityComparer<UnitDefinition>.Default.Equals(unit, default))
        {
            throw new InvalidOperationException($"Type d'unité non configuré : {type}");
        }

        return unit;
    }
}
