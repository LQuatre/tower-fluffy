using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using TowerFluffy.Application.Game.Dtos.Combat;
using TowerFluffy.Application.Game.Dtos.Environment;
using TowerFluffy.Application.Game.Dtos.Match;

namespace TowerFluffy.UI.Desktop.Controls;

public sealed class GameBoardControl : Control
{
    private const int LineTtlTicks = 3;
    private const int PulseTtlTicks = 6;

    public static readonly StyledProperty<GameSnapshotDto?> SnapshotProperty =
        AvaloniaProperty.Register<GameBoardControl, GameSnapshotDto?>(nameof(Snapshot));

    public static readonly StyledProperty<ICommand?> PlaceTowerCommandProperty =
        AvaloniaProperty.Register<GameBoardControl, ICommand?>(nameof(PlaceTowerCommand));

    public static readonly StyledProperty<ICommand?> SellTowerCommandProperty =
        AvaloniaProperty.Register<GameBoardControl, ICommand?>(nameof(SellTowerCommand));

    public GameSnapshotDto? Snapshot
    {
        get => GetValue(SnapshotProperty);
        set => SetValue(SnapshotProperty, value);
    }

    public ICommand? PlaceTowerCommand
    {
        get => GetValue(PlaceTowerCommandProperty);
        set => SetValue(PlaceTowerCommandProperty, value);
    }

    public ICommand? SellTowerCommand
    {
        get => GetValue(SellTowerCommandProperty);
        set => SetValue(SellTowerCommandProperty, value);
    }

    private readonly List<LineEffect> _lineEffects = new();
    private readonly List<PulseEffect> _pulseEffects = new();

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SnapshotProperty)
        {
            if (change.NewValue is GameSnapshotDto snapshot)
            {
                UpdateCombatEffects(snapshot);
            }

            InvalidateVisual();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var snapshot = Snapshot;
        if (snapshot is null)
        {
            return new Size(640, 400);
        }

        return new Size(snapshot.Map.Width * snapshot.Map.CellSize, snapshot.Map.Height * snapshot.Map.CellSize);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var snapshot = Snapshot;
        if (snapshot is null)
        {
            return;
        }

        var cellSize = snapshot.Map.CellSize;
        var boardWidth = snapshot.Map.Width * cellSize;
        var boardHeight = snapshot.Map.Height * cellSize;

        var boardRect = new Rect(0, 0, boardWidth, boardHeight);

        // Futuristic Colors
        var defenderColor = Color.Parse("#00F2FF");
        var attackerColor = Color.Parse("#FF00E5");
        var gridColor = Color.Parse("#48FFFFFF");
        var pathColor = Color.Parse("#15FFFFFF");

        var defenderBrush = new SolidColorBrush(defenderColor);
        var attackerBrush = new SolidColorBrush(attackerColor);
        var gridPen = new Pen(new SolidColorBrush(gridColor), thickness: 0.8);

        RenderBoard(context, boardRect, Brushes.Transparent);
        RenderGrid(context, snapshot, gridPen);
        RenderPath(context, snapshot, pathColor, cellSize);
        RenderTowers(context, snapshot, defenderBrush, cellSize);
        RenderUnits(context, snapshot, attackerBrush, cellSize);
        RenderCombatEffects(context, snapshot, defenderBrush, attackerBrush, cellSize);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var snapshot = Snapshot;
        if (snapshot is null) return;

        var point = e.GetPosition(this);
        var cellSize = snapshot.Map.CellSize;
        if (cellSize <= 0) return;

        var cellX = (int)(point.X / cellSize);
        var cellY = (int)(point.Y / cellSize);
        var cell = new GridPositionDto(cellX, cellY);

        // On ne déclenche le "Pressed" que s'il y a une tour à ramasser
        var hasTower = snapshot.Towers.Any(t => t.Cell.X == cellX && t.Cell.Y == cellY);

        if (e.ClickCount == 2)
        {
            var sellCommand = SellTowerCommand;
            if (hasTower && sellCommand != null && sellCommand.CanExecute(cell))
            {
                sellCommand.Execute(cell);
                return;
            }
        }

        var command = PlaceTowerCommand;
        if (hasTower && command != null && command.CanExecute(cell))
        {
            command.Execute(cell);
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        var snapshot = Snapshot;
        if (snapshot is null) return;

        var point = e.GetPosition(this);
        var cellSize = snapshot.Map.CellSize;
        if (cellSize <= 0) return;

        var cellX = (int)(point.X / cellSize);
        var cellY = (int)(point.Y / cellSize);
        var cell = new GridPositionDto(cellX, cellY);

        var command = PlaceTowerCommand;
        if (command != null && command.CanExecute(cell))
        {
            // On envoie le signal de relâchement (pour poser la tour)
            command.Execute(cell);
            InvalidateVisual();
        }
    }

    private IBrush? TryGetThemeBrush(string key)
    {
        var current = Avalonia.Application.Current;
        if (current?.TryFindResource(key, current.ActualThemeVariant, out var value) == true)
        {
            return value as IBrush;
        }

        return null;
    }

    private static void RenderBoard(DrawingContext context, Rect boardRect, IBrush background)
    {
        context.DrawRectangle(background, pen: null, boardRect);
    }

    private static void RenderGrid(DrawingContext context, GameSnapshotDto snapshot, Pen gridPen)
    {
        var cellSize = snapshot.Map.CellSize;

        var boardWidth = snapshot.Map.Width * cellSize;
        var boardHeight = snapshot.Map.Height * cellSize;

        for (var x = 0; x <= snapshot.Map.Width; x++)
        {
            var xPixel = x * cellSize;
            context.DrawLine(gridPen, new Point(xPixel, 0), new Point(xPixel, boardHeight));
        }

        for (var y = 0; y <= snapshot.Map.Height; y++)
        {
            var yPixel = y * cellSize;
            context.DrawLine(gridPen, new Point(0, yPixel), new Point(boardWidth, yPixel));
        }
    }

    private static void RenderPath(DrawingContext context, GameSnapshotDto snapshot, Color pathColor, int cellSize)
    {
        if (snapshot.Map.Waypoints.Count < 2)
        {
            return;
        }

        var pathBrush = new SolidColorBrush(pathColor);
        var pathPen = new Pen(pathBrush, thickness: cellSize * 0.8)
        {
            LineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round,
        };

        var centerPen = new Pen(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), thickness: 2)
        {
            DashStyle = new DashStyle(new[] { 4.0, 6.0 }, offset: 0),
        };

        for (var i = 0; i < snapshot.Map.Waypoints.Count - 1; i++)
        {
            var p1 = new Point(snapshot.Map.Waypoints[i].X, snapshot.Map.Waypoints[i].Y);
            var p2 = new Point(snapshot.Map.Waypoints[i + 1].X, snapshot.Map.Waypoints[i + 1].Y);

            context.DrawLine(pathPen, p1, p2);
            context.DrawLine(centerPen, p1, p2);
        }

        var start = new Point(snapshot.Map.Waypoints[0].X, snapshot.Map.Waypoints[0].Y);
        var end = new Point(snapshot.Map.Waypoints[^1].X, snapshot.Map.Waypoints[^1].Y);

        DrawEndpointMarker(context, start, new SolidColorBrush(Color.Parse("#FF2E2E")), size: cellSize * 0.2);
        DrawEndpointMarker(context, end, new SolidColorBrush(Color.Parse("#00FF94")), size: cellSize * 0.3);
    }

    private static void DrawHatch(DrawingContext context, Rect rect, Pen pen)
    {
        var inset = Math.Max(2, rect.Width * 0.12);
        var inner = rect.Deflate(inset);

        context.DrawLine(pen, new Point(inner.Left, inner.Top), new Point(inner.Right, inner.Bottom));
        context.DrawLine(pen, new Point(inner.Right, inner.Top), new Point(inner.Left, inner.Bottom));
    }

    private static void DrawEndpointMarker(DrawingContext context, Point center, IBrush stroke, double size)
    {
        var pen = new Pen(CreateTintBrush(stroke, alpha: 160), thickness: 2);

        context.DrawEllipse(Brushes.Transparent, pen, center, size, size);
        context.DrawLine(pen, new Point(center.X - size, center.Y), new Point(center.X + size, center.Y));
        context.DrawLine(pen, new Point(center.X, center.Y - size), new Point(center.X, center.Y + size));
    }

    private static void RenderTowers(
        DrawingContext context,
        GameSnapshotDto snapshot,
        ISolidColorBrush brush,
        int cellSize)
    {
        var glowBrush = new SolidColorBrush(brush.Color, opacity: 0.2);
        var strongPen = new Pen(brush, thickness: 2);

        foreach (var tower in snapshot.Towers)
        {
            var towerBrush = tower.Type switch
            {
                TowerTypeDto.BasicShooter => brush, // Default defender color
                TowerTypeDto.Flamethrower => new SolidColorBrush(Color.Parse("#FF4500")), // OrangeRed
                TowerTypeDto.Sniper => new SolidColorBrush(Color.Parse("#FFD700")), // Gold
                TowerTypeDto.Cannon => new SolidColorBrush(Color.Parse("#8A2BE2")), // BlueViolet
                TowerTypeDto.Laser => new SolidColorBrush(Color.Parse("#00FF7F")), // SpringGreen
                _ => brush
            };
            var towerGlowBrush = new SolidColorBrush(towerBrush.Color, opacity: 0.2);
            var towerStrongPen = new Pen(towerBrush, thickness: 2);

            var cellRect = new Rect(tower.Cell.X * cellSize, tower.Cell.Y * cellSize, cellSize, cellSize);
            var inset = cellSize * 0.2;
            var outer = cellRect.Deflate(inset);

            // Glow
            context.DrawEllipse(towerGlowBrush, null, outer.Center, outer.Width * 0.8, outer.Height * 0.8);

            var geometry = CreateCutCornerRectGeometry(outer, cut: cellSize * 0.15);
            context.DrawGeometry(new SolidColorBrush(towerBrush.Color, 0.1), towerStrongPen, geometry);

            // Core
            var coreRadius = cellSize * 0.1;
            context.DrawEllipse(towerBrush, null, outer.Center, coreRadius, coreRadius);

            DrawHealthBar(context, tower.Health, GetMaxHealth(tower.Type), new Point(outer.Left, outer.Top - 8), towerBrush, outer.Width);
        }
    }

    private static void RenderUnits(
        DrawingContext context,
        GameSnapshotDto snapshot,
        ISolidColorBrush brush,
        int cellSize)
    {
        foreach (var unit in snapshot.Units)
        {
            var unitBrush = unit.Type switch
            {
                UnitTypeDto.Soldat => brush, // Default attacker color
                UnitTypeDto.Brute => new SolidColorBrush(Color.Parse("#FF8C00")), // DarkOrange
                UnitTypeDto.Rapide => new SolidColorBrush(Color.Parse("#FF1493")), // DeepPink
                UnitTypeDto.TireurElite => new SolidColorBrush(Color.Parse("#4169E1")), // RoyalBlue
                UnitTypeDto.Tank => new SolidColorBrush(Color.Parse("#8B0000")), // DarkRed
                _ => brush
            };
            
            var strongPen = new Pen(unitBrush, thickness: 2);
            var trailBrush = new SolidColorBrush(unitBrush.Color, opacity: 0.3);
            var trailPen = new Pen(trailBrush, thickness: 2) { LineCap = PenLineCap.Round };

            var center = new Point(unit.Position.X, unit.Position.Y);
            var direction = new Vector(unit.Direction.X, unit.Direction.Y);
            var radius = cellSize * 0.15;

            DrawTrail(context, center, direction, trailPen, length: radius * 2.5);

            var unitGeometry = CreateArrowGeometry(center, radius, direction);
            context.DrawGeometry(new SolidColorBrush(unitBrush.Color, 0.2), strongPen, unitGeometry);

            DrawHealthBar(context, unit.Health, GetMaxHealth(unit.Type), new Point(center.X - radius, center.Y - radius - 8), unitBrush, radius * 2);
        }
    }

    private static void DrawTrail(DrawingContext context, Point center, Vector direction, Pen pen, double length)
    {
        var lengthSquared = (direction.X * direction.X) + (direction.Y * direction.Y);
        if (lengthSquared < 0.0001)
        {
            return;
        }

        var back = new Point(center.X - (direction.X * length), center.Y - (direction.Y * length));
        context.DrawLine(pen, back, center);
    }

    private static Vector GetDominantPathDirection(GameSnapshotDto snapshot)
    {
        if (snapshot.Map.Waypoints.Count < 2)
        {
            return new Vector(1, 0);
        }

        var start = snapshot.Map.Waypoints[0];
        var end = snapshot.Map.Waypoints[^1];
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;

        if (Math.Abs(dx) >= Math.Abs(dy))
        {
            return new Vector(Math.Sign(dx), 0);
        }

        return new Vector(0, Math.Sign(dy));
    }

    private static Geometry CreateCutCornerRectGeometry(Rect rect, double cut)
    {
        var geometry = new StreamGeometry();
        using (var geometryContext = geometry.Open())
        {
            geometryContext.BeginFigure(new Point(rect.Left + cut, rect.Top), isFilled: true);
            geometryContext.LineTo(new Point(rect.Right - cut, rect.Top));
            geometryContext.LineTo(new Point(rect.Right, rect.Top + cut));
            geometryContext.LineTo(new Point(rect.Right, rect.Bottom - cut));
            geometryContext.LineTo(new Point(rect.Right - cut, rect.Bottom));
            geometryContext.LineTo(new Point(rect.Left + cut, rect.Bottom));
            geometryContext.LineTo(new Point(rect.Left, rect.Bottom - cut));
            geometryContext.LineTo(new Point(rect.Left, rect.Top + cut));
            geometryContext.EndFigure(isClosed: true);
        }

        return geometry;
    }

    private static Geometry CreateArrowGeometry(Point center, double radius, Vector direction)
    {
        var lengthSquared = (direction.X * direction.X) + (direction.Y * direction.Y);
        if (lengthSquared < 0.0001)
        {
            direction = new Vector(1, 0);
        }

        var perp = new Vector(-direction.Y, direction.X);
        var tip = center + (direction * radius);
        var baseCenter = center - (direction * (radius * 0.6));
        var left = baseCenter + (perp * (radius * 0.65));
        var right = baseCenter - (perp * (radius * 0.65));

        var geometry = new StreamGeometry();
        using (var geometryContext = geometry.Open())
        {
            geometryContext.BeginFigure(tip, isFilled: true);
            geometryContext.LineTo(left);
            geometryContext.LineTo(right);
            geometryContext.EndFigure(isClosed: true);
        }

        return geometry;
    }

    private static int GetMaxHealth(TowerTypeDto type) => type switch
    {
        TowerTypeDto.BasicShooter => 100,
        TowerTypeDto.Flamethrower => 120,
        TowerTypeDto.Sniper => 80,
        TowerTypeDto.Cannon => 150,
        TowerTypeDto.Laser => 150,
        _ => 100
    };

    private static int GetMaxHealth(UnitTypeDto type) => type switch
    {
        UnitTypeDto.Soldat => 20,
        UnitTypeDto.Brute => 80,
        UnitTypeDto.Rapide => 15,
        UnitTypeDto.TireurElite => 30,
        UnitTypeDto.Tank => 300,
        _ => 20
    };

    private static void DrawHealthBar(DrawingContext context, int health, int maxHealth, Point anchor, IBrush stroke, double width)
    {
        if (health <= 0) return;

        double fraction = Math.Clamp((double)health / maxHealth, 0.0, 1.0);
        var height = 4.0;
        
        var bgRect = new Rect(anchor.X, anchor.Y, width, height);
        context.DrawRectangle(new SolidColorBrush(Color.Parse("#80000000")), null, bgRect);
        
        var fgRect = new Rect(anchor.X, anchor.Y, width * fraction, height);
        var color = fraction > 0.5 ? Color.Parse("#32CD32") : (fraction > 0.2 ? Color.Parse("#FFA500") : Color.Parse("#FF0000"));
        context.DrawRectangle(new SolidColorBrush(color), null, fgRect);

        context.DrawRectangle(null, new Pen(stroke, 0.5), bgRect);
    }

    private static IBrush CreateTintBrush(IBrush source, byte alpha)
    {
        if (source is ISolidColorBrush solid)
        {
            var color = solid.Color;
            return new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
        }

        return Brushes.Transparent;
    }

    private void UpdateCombatEffects(GameSnapshotDto snapshot)
    {
        var currentTick = snapshot.Hud.Tick;
        if (currentTick < 0)
        {
            _lineEffects.Clear();
            _pulseEffects.Clear();
            return;
        }

        _lineEffects.RemoveAll(e => (currentTick - e.SpawnTick) >= e.Ttl);
        _pulseEffects.RemoveAll(e => (currentTick - e.SpawnTick) >= PulseTtlTicks);

        foreach (var e in snapshot.CombatEvents)
        {
            var from = new Point(e.From.X, e.From.Y);
            var to = new Point(e.To.X, e.To.Y);
            var isKill = e.TargetDestroyed;
            var sourceType = e.SourceTowerType;
            var ttl = sourceType == TowerTypeDto.Flamethrower ? 8 : LineTtlTicks;
            var isAttacker = e.Kind == CombatEventKindDto.UnitAttackTower || e.Kind == CombatEventKindDto.UnitHitBase;
            _lineEffects.Add(new LineEffect(e.Tick, from, to, isKill, sourceType, ttl, isAttacker));
            _pulseEffects.Add(new PulseEffect(e.Tick, to, isKill));
        }
    }

    private void RenderCombatEffects(
        DrawingContext context,
        GameSnapshotDto snapshot,
        IBrush defenderBrush,
        IBrush attackerBrush,
        int cellSize)
    {
        var currentTick = snapshot.Hud.Tick;
        if (currentTick < 0)
        {
            return;
        }

        var baseLineThickness = Math.Max(2.0, cellSize * 0.08);
        var basePulseRadius = Math.Max(6.0, cellSize * 0.15);

        foreach (var effect in _lineEffects)
        {
            var age = currentTick - effect.SpawnTick;
            if (age < 0) continue;

            var progress = GetProgress(age, effect.Ttl);
            var alpha = GetFadedAlpha(maxAlpha: effect.IsKill ? 255 : 180, progress);
            var thickness = effect.IsKill ? baseLineThickness * 1.5 : baseLineThickness;

            var brush = (effect.IsKill || effect.IsAttacker) ? attackerBrush : defenderBrush;

            if (effect.SourceType == TowerTypeDto.Flamethrower)
            {
                // Rendu FLAMMES en CÔNE + PARTICULES
                var fireColors = new[] { "#FFFF00", "#FFD700", "#FF8C00", "#FF4500" }; // Du centre vers l'extérieur
                var random = new Random(effect.SpawnTick + (int)effect.From.X + (int)effect.From.Y);
                
                var dir = new Vector(effect.To.X - effect.From.X, effect.To.Y - effect.From.Y);
                var length = dir.Length;
                if (length > 0.01)
                {
                    dir /= length;
                    var perp = new Vector(-dir.Y, dir.X);
                    
                    // 1. Dessiner le cône principal (plus opaque)
                    var coneWidth = length * 0.4; // Plus large
                    var p1 = effect.From;
                    var p2 = effect.To + perp * coneWidth;
                    var p3 = effect.To - perp * coneWidth;
                    
                    var coneGeometry = new StreamGeometry();
                    using (var geoCtx = coneGeometry.Open())
                    {
                        geoCtx.BeginFigure(p1, isFilled: true);
                        geoCtx.LineTo(p2);
                        geoCtx.LineTo(p3);
                        geoCtx.EndFigure(isClosed: true);
                    }
                    
                    // Dégradé pour le cône
                    var coneBrush = new RadialGradientBrush
                    {
                        Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                        GradientStops =
                        {
                            new GradientStop(Color.Parse("#FFFFFFFF"), 0.0),
                            new GradientStop(Color.Parse("#FFFF4500"), 0.4),
                            new GradientStop(Color.Parse("#00FF4500"), 1.0)
                        }
                    };
                    context.DrawGeometry(new SolidColorBrush(Color.Parse("#FF4500"), (byte)(alpha * 0.4)), null, coneGeometry);
                    
                    // 2. Dessiner des "Boules de feu" (plus gros, moins pixels)
                    for (int i = 0; i < 12; i++)
                    {
                        var dist = random.NextDouble() * length;
                        var spread = (dist / length) * coneWidth;
                        var offset = perp * (random.NextDouble() * spread * 2 - spread);
                        var pos = effect.From + dir * dist + offset;
                        
                        var pSize = (random.NextDouble() * 6 + 3.0) * (1.0 - progress);
                        var pColor = Color.Parse(fireColors[random.Next(fireColors.Length)]);
                        var pBrush = new SolidColorBrush(pColor, (byte)(alpha * 0.8));
                        
                        context.DrawEllipse(pBrush, null, pos, pSize, pSize);
                    }
                }
            }
            else
            {
                var pen = new Pen(CreateTintBrush(brush, alpha), thickness) { LineCap = PenLineCap.Round };
                context.DrawLine(pen, effect.From, effect.To);
            }
        }

        foreach (var effect in _pulseEffects)
        {
            var age = currentTick - effect.SpawnTick;
            if (age < 0) continue;

            var progress = GetProgress(age, PulseTtlTicks);
            var alpha = GetFadedAlpha(maxAlpha: 180, progress);

            var radius = basePulseRadius + (progress * basePulseRadius * 1.5);
            var pen = new Pen(CreateTintBrush(attackerBrush, alpha), 2);
            var fill = CreateTintBrush(attackerBrush, (byte)(alpha / 3));

            context.DrawEllipse(fill, pen, effect.Center, radius, radius);
        }
    }

    private static double GetProgress(int age, int ttl)
    {
        if (ttl <= 1)
        {
            return 1.0;
        }

        var clamped = Math.Clamp(age, 0, ttl - 1);
        return clamped / (double)(ttl - 1);
    }

    private static byte GetFadedAlpha(int maxAlpha, double progress)
    {
        var a = (int)Math.Round(maxAlpha * (1.0 - progress));
        return (byte)Math.Clamp(a, 0, 255);
    }

    private readonly record struct LineEffect(int SpawnTick, Point From, Point To, bool IsKill, TowerTypeDto? SourceType, int Ttl, bool IsAttacker);

    private readonly record struct PulseEffect(int SpawnTick, Point Center, bool IsKill);
}
