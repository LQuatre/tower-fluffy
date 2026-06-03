using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

using SukiUI.Controls;

namespace TowerFluffy.UI.Desktop.Views;

public partial class MainWindow : SukiWindow
{
    private readonly DispatcherTimer _timer;

    public MainWindow()
    {
        InitializeComponent();

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16),
        };
        _timer.Tick += (_, _) => (DataContext as TowerFluffy.UI.Desktop.ViewModels.MainWindowViewModel)?.Tick();
        _timer.Start();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (DataContext is TowerFluffy.UI.Desktop.ViewModels.MainWindowViewModel vm)
        {
            if (vm.CanPlaceTower)
            {
                switch (e.Key)
                {
                    case Key.D1:
                    case Key.NumPad1:
                        vm.CurrentTowerType = TowerFluffy.Domain.Combat.TowerType.BasicShooter;
                        e.Handled = true;
                        break;
                    case Key.D2:
                    case Key.NumPad2:
                        vm.CurrentTowerType = TowerFluffy.Domain.Combat.TowerType.Flamethrower;
                        e.Handled = true;
                        break;
                    case Key.D3:
                    case Key.NumPad3:
                        vm.CurrentTowerType = TowerFluffy.Domain.Combat.TowerType.Sniper;
                        e.Handled = true;
                        break;
                    case Key.D4:
                    case Key.NumPad4:
                        vm.CurrentTowerType = TowerFluffy.Domain.Combat.TowerType.Cannon;
                        e.Handled = true;
                        break;
                    case Key.D5:
                    case Key.NumPad5:
                        vm.CurrentTowerType = TowerFluffy.Domain.Combat.TowerType.Laser;
                        e.Handled = true;
                        break;
                }
            }

            if (e.Key == Key.Q || e.Key == Key.Delete)
            {
                var board = this.FindControl<TowerFluffy.UI.Desktop.Controls.GameBoardControl>("Board");
                if (board != null && board.HoveredCell.HasValue)
                {
                    var cell = board.HoveredCell.Value;
                    if (((System.Windows.Input.ICommand)vm.SellTowerCommand).CanExecute(cell))
                    {
                        ((System.Windows.Input.ICommand)vm.SellTowerCommand).Execute(cell);
                        e.Handled = true;
                    }
                }
            }
        }
    }

    private void TitleBar_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void TitleBarButton_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
    }

    private void Minimize_OnClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void Close_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
