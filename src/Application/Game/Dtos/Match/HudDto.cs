namespace TowerFluffy.Application.Game.Dtos.Match;

public sealed record HudDto(
    int Tick,
    int Level,
    int BaseHealth,
    int DefenderGold,
    int AttackerBudget,
    int WaveNumber,
    int TotalWaves,
    MatchPhaseDto Phase,
    MatchOutcomeDto Outcome,
    int PreparationTicksRemaining,
    int WaveSendTicksRemaining);
