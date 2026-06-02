using TowerFluffy.Domain.Shared;

namespace TowerFluffy.Domain.Environment;

public readonly record struct Grid(int Width, int Height, int CellSize);
