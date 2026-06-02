using System;
using System.Collections.Generic;
using System.Linq;
using TowerFluffy.Domain.Shared;
using TowerFluffy.Domain.Combat;

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
    int GoldPerBaseDamageTaken,
    IReadOnlyList<TowerDefinition> Towers,
    IReadOnlyList<UnitDefinition> Units)
{
    public static GameConfig CreateMvpDefaults()
    {
        return new GameConfig(
            TotalWaves: 10,
            PreparationTicks: 60 * 30,
            WaveSendWindowTicks: 60 * 20,
            BaseWaveBudget: new Budget(80),
            WaveBudgetIncrement: new Budget(20),
            BudgetBonusPerTowerDestroyed: new Budget(10),
            StartingGold: new Gold(500),
            StartingBaseHealth: new Health(100),
            GoldPerBaseDamageTaken: 10,
            Towers: new[]
            {
                new TowerDefinition(
                    TowerType.BasicShooter,
                    new TowerStats(
                        Cost: new Gold(50),
                        DamagePerShot: new Damage(5),
                        Range: 250,
                        CooldownTicksBetweenShots: 30),
                    Health: new Health(100)),
                new TowerDefinition(
                    TowerType.Flamethrower,
                    new TowerStats(
                        Cost: new Gold(200),
                        DamagePerShot: new Damage(3),
                        Range: 180,
                        CooldownTicksBetweenShots: 8),
                    Health: new Health(120)),
                new TowerDefinition(
                    TowerType.Sniper,
                    new TowerStats(
                        Cost: new Gold(150),
                        DamagePerShot: new Damage(40),
                        Range: 400,
                        CooldownTicksBetweenShots: 60),
                    Health: new Health(80)),
                new TowerDefinition(
                    TowerType.Cannon,
                    new TowerStats(
                        Cost: new Gold(300),
                        DamagePerShot: new Damage(60),
                        Range: 220,
                        CooldownTicksBetweenShots: 60),
                    Health: new Health(150)),
                new TowerDefinition(
                    TowerType.Laser,
                    new TowerStats(
                        Cost: new Gold(400),
                        DamagePerShot: new Damage(10),
                        Range: 300,
                        CooldownTicksBetweenShots: 10),
                    Health: new Health(150)),
            },
            Units: new[]
            {
                new UnitDefinition(
                    UnitType.Soldat,
                    Cost: new Budget(12),
                    Health: new Health(20),
                    SpeedPerTick: 2,
                    DamageToBase: new Damage(5),
                    DamageToTower: new Damage(2),
                    AttackRange: 150,
                    AttackCooldownTicksBetweenAttacks: 40,
                    LootGold: new Gold(10)),
                new UnitDefinition(
                    UnitType.Brute,
                    Cost: new Budget(60),
                    Health: new Health(80),
                    SpeedPerTick: 1,
                    DamageToBase: new Damage(15),
                    DamageToTower: new Damage(10),
                    AttackRange: 180,
                    AttackCooldownTicksBetweenAttacks: 60,
                    LootGold: new Gold(30)),
                new UnitDefinition(
                    UnitType.Rapide,
                    Cost: new Budget(20),
                    Health: new Health(15),
                    SpeedPerTick: 4,
                    DamageToBase: new Damage(3),
                    DamageToTower: new Damage(1),
                    AttackRange: 100,
                    AttackCooldownTicksBetweenAttacks: 30,
                    LootGold: new Gold(15)),
                new UnitDefinition(
                    UnitType.TireurElite,
                    Cost: new Budget(40),
                    Health: new Health(30),
                    SpeedPerTick: 2,
                    DamageToBase: new Damage(8),
                    DamageToTower: new Damage(8),
                    AttackRange: 250,
                    AttackCooldownTicksBetweenAttacks: 80,
                    LootGold: new Gold(20)),
                new UnitDefinition(
                    UnitType.Tank,
                    Cost: new Budget(100),
                    Health: new Health(300),
                    SpeedPerTick: 1,
                    DamageToBase: new Damage(25),
                    DamageToTower: new Damage(5),
                    AttackRange: 120,
                    AttackCooldownTicksBetweenAttacks: 50,
                    LootGold: new Gold(50)),
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
