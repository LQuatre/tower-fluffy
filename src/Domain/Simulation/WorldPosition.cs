namespace TowerFluffy.Domain.Simulation;

public readonly record struct WorldPosition(int X, int Y);

public static class WorldPositionExtensions
{
	public static int ManhattanDistanceTo(this WorldPosition from, WorldPosition to)
	{
		return Math.Abs(from.X - to.X) + Math.Abs(from.Y - to.Y);
	}
}
