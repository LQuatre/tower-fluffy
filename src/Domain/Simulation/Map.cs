using System;
using System.Collections.Generic;
using System.Linq;

namespace TowerFluffy.Domain.Simulation;

public sealed class Map
{
	private readonly HashSet<GridPosition> _blockedCells;

	public Map(Path path, Grid grid, IEnumerable<GridPosition> blockedCells)
	{
		Path = path ?? throw new ArgumentNullException(nameof(path));
		Grid = grid;
		_blockedCells = new HashSet<GridPosition>(blockedCells ?? throw new ArgumentNullException(nameof(blockedCells)));
	}

	public Path Path { get; }
	public Grid Grid { get; }

	public bool IsWithinBounds(GridPosition position)
	{
		return position.X >= 0
			   && position.X < Grid.Width
			   && position.Y >= 0
			   && position.Y < Grid.Height;
	}

	public bool IsBuildable(GridPosition position)
	{
		return IsWithinBounds(position) && !_blockedCells.Contains(position);
	}

	public IReadOnlyCollection<GridPosition> BlockedCells => _blockedCells.ToArray();

	public WorldPosition GetCellCenter(GridPosition cell)
	{
		if (!IsWithinBounds(cell))
		{
			throw new ArgumentOutOfRangeException(nameof(cell), cell, "Cell is outside map bounds.");
		}

		return new WorldPosition(
			(cell.X * Grid.CellSize) + (Grid.CellSize / 2),
			(cell.Y * Grid.CellSize) + (Grid.CellSize / 2));
	}
}
