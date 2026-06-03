using TowerFluffy.Application.Game;
using TowerFluffy.Domain.Combat;
using TowerFluffy.Domain.Shared;
using TowerFluffy.Domain.Match;
using TowerFluffy.Domain.Engine;
using TowerFluffy.Domain.Environment;
using TowerFluffy.Application.Common.Networking;
using TowerFluffy.UI.Desktop.Services;
using ReactiveUI;
using System;
using System.Globalization;
using System.Reactive;
using System.Threading.Tasks;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

using RxUnit = System.Reactive.Unit;

namespace TowerFluffy.UI.Desktop.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private GameSession _session;
    private MatchState _snapshot;
    private string? _lastError;
    private readonly GameNetworkCoordinator _networkCoordinator;
    private DateTime? _gameStartTime;
    private long _totalTicksProcessed;
    private bool _isConnected;
    private bool _isConnecting;
    private bool _isReady;
    private bool _isGameStarted;
    private bool _isOpponentReady;
    private bool _isInGameRoom;
    private string _serverUrl = $"http://{GetLocalIPAddress()}:5128/gameHub";
    private System.Collections.ObjectModel.ObservableCollection<GameInfoDto> _availableGames = new();
    private PlayerRole _selectedRole = PlayerRole.Both;
    private GridPosition? _movingTowerFrom;
    private TowerType _currentTowerType = TowerType.BasicShooter;

    public MainWindowViewModel()
        : this(GameSession.CreateMvp())
    {
    }

    public MainWindowViewModel(GameSession session)
    {
        _session = session;
        _snapshot = _session.State;
        _networkCoordinator = new GameNetworkCoordinator();

        // Abonnements aux événements réseau
        _networkCoordinator.ConnectionStatusChanged += isConnected => IsConnected = isConnected;
        _networkCoordinator.ConnectingStatusChanged += isConnecting => IsConnecting = isConnecting;
        _networkCoordinator.GameListReceived += games => Avalonia.Threading.Dispatcher.UIThread.Post(() => {
            _availableGames.Clear();
            foreach (var g in games) _availableGames.Add(g);
        });
        _networkCoordinator.RoleReceived += role => Avalonia.Threading.Dispatcher.UIThread.Post(() => SelectedRole = role);
        _networkCoordinator.OpponentReadyChanged += ready => Avalonia.Threading.Dispatcher.UIThread.Post(() => IsOpponentReady = ready);
        _networkCoordinator.GameStarted += (seed, startTime) => Avalonia.Threading.Dispatcher.UIThread.Post(() => {
            _session = CreateSession(seed);
            _gameStartTime = new DateTime(startTime, DateTimeKind.Utc);
            _totalTicksProcessed = 0;
            Snapshot = _session.State;
            IsGameStarted = true;
        });
        _networkCoordinator.PlayerActionReceived += HandleNetworkAction;
        _networkCoordinator.ErrorOccurred += err => Avalonia.Threading.Dispatcher.UIThread.Post(() => LastError = string.IsNullOrEmpty(err) ? null : err);
        _networkCoordinator.RoomClosed += () => {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                if (IsInGameRoom)
                {
                    ExecuteReplay();
                    LastError = "Le salon a été fermé.";
                }
            });
        };

        SkipPreparationCommand = ReactiveCommand.Create(ExecuteSkipPreparation);
        SendSoldatCommand = ReactiveCommand.Create(ExecuteSendSoldat);
        SendBruteCommand = ReactiveCommand.Create(ExecuteSendBrute);
        SendRapideCommand = ReactiveCommand.Create(ExecuteSendRapide);
        SendTireurEliteCommand = ReactiveCommand.Create(ExecuteSendTireurElite);
        SendTankCommand = ReactiveCommand.Create(ExecuteSendTank);
        PlaceTowerCommand = ReactiveCommand.Create<GridPosition>(ExecutePlaceTower);
        SellTowerCommand = ReactiveCommand.Create<GridPosition>(ExecuteSellTower);
        SetBasicTowerCommand = ReactiveCommand.Create(() => { CurrentTowerType = TowerType.BasicShooter; });
        SetFlamethrowerCommand = ReactiveCommand.Create(() => { CurrentTowerType = TowerType.Flamethrower; });
        SetSniperTowerCommand = ReactiveCommand.Create(() => { CurrentTowerType = TowerType.Sniper; });
        SetCannonTowerCommand = ReactiveCommand.Create(() => { CurrentTowerType = TowerType.Cannon; });
        SetLaserTowerCommand = ReactiveCommand.Create(() => { CurrentTowerType = TowerType.Laser; });
        ConnectCommand = ReactiveCommand.CreateFromTask(ExecuteConnect);
        RefreshGamesCommand = ReactiveCommand.CreateFromTask(ExecuteRefreshGames);
        JoinSpecificGameCommand = ReactiveCommand.CreateFromTask<string>(ExecuteJoinSpecificGame);
        StartSoloCommand = ReactiveCommand.Create(ExecuteStartSolo);
        ReplayCommand = ReactiveCommand.Create(ExecuteReplay);
        QuitCommand = ReactiveCommand.Create(ExecuteQuit);

        // Connexion automatique au démarrage
        Task.Run(async () => await ExecuteConnect());

        // Démarrer la musique en boucle
        SoundEffects.PlayTheme();

        InitializeBalancing();
    }

    public string ServerUrl
    {
        get => _serverUrl;
        set => this.RaiseAndSetIfChanged(ref _serverUrl, value);
    }

    // --- BALANCING OPTIONS ---
    private int _selectedBalancingTowerIndex = 0;
    public int SelectedBalancingTowerIndex
    {
        get => _selectedBalancingTowerIndex;
        set {
            this.RaiseAndSetIfChanged(ref _selectedBalancingTowerIndex, value);
            _isUpdatingFields = true;
            UpdateTowerBalancingFields();
            _isUpdatingFields = false;
        }
    }

    private int _selectedBalancingUnitIndex = 0;
    public int SelectedBalancingUnitIndex
    {
        get => _selectedBalancingUnitIndex;
        set {
            this.RaiseAndSetIfChanged(ref _selectedBalancingUnitIndex, value);
            _isUpdatingFields = true;
            UpdateUnitBalancingFields();
            _isUpdatingFields = false;
        }
    }

    private bool _isUpdatingFields = false;

    private int _balancingTowerCost;
    public int BalancingTowerCost
    {
        get => _balancingTowerCost;
        set {
            this.RaiseAndSetIfChanged(ref _balancingTowerCost, value);
            if (!_isUpdatingFields) SaveTowerBalancingChanges();
        }
    }

    private int _balancingTowerDamage;
    public int BalancingTowerDamage
    {
        get => _balancingTowerDamage;
        set {
            this.RaiseAndSetIfChanged(ref _balancingTowerDamage, value);
            if (!_isUpdatingFields) SaveTowerBalancingChanges();
        }
    }

    private int _balancingTowerRange;
    public int BalancingTowerRange
    {
        get => _balancingTowerRange;
        set {
            this.RaiseAndSetIfChanged(ref _balancingTowerRange, value);
            if (!_isUpdatingFields) SaveTowerBalancingChanges();
        }
    }

    private int _balancingTowerCooldown;
    public int BalancingTowerCooldown
    {
        get => _balancingTowerCooldown;
        set {
            this.RaiseAndSetIfChanged(ref _balancingTowerCooldown, value);
            if (!_isUpdatingFields) SaveTowerBalancingChanges();
        }
    }

    private int _balancingUnitCost;
    public int BalancingUnitCost
    {
        get => _balancingUnitCost;
        set {
            this.RaiseAndSetIfChanged(ref _balancingUnitCost, value);
            if (!_isUpdatingFields) SaveUnitBalancingChanges();
        }
    }

    private int _balancingUnitHealth;
    public int BalancingUnitHealth
    {
        get => _balancingUnitHealth;
        set {
            this.RaiseAndSetIfChanged(ref _balancingUnitHealth, value);
            if (!_isUpdatingFields) SaveUnitBalancingChanges();
        }
    }

    private int _balancingUnitSpeed;
    public int BalancingUnitSpeed
    {
        get => _balancingUnitSpeed;
        set {
            this.RaiseAndSetIfChanged(ref _balancingUnitSpeed, value);
            if (!_isUpdatingFields) SaveUnitBalancingChanges();
        }
    }

    private int _balancingUnitBounty;
    public int BalancingUnitBounty
    {
        get => _balancingUnitBounty;
        set {
            this.RaiseAndSetIfChanged(ref _balancingUnitBounty, value);
            if (!_isUpdatingFields) SaveUnitBalancingChanges();
        }
    }

    private bool _isBalancingMenuVisible = false;
    public bool IsBalancingMenuVisible
    {
        get => _isBalancingMenuVisible;
        set => this.RaiseAndSetIfChanged(ref _isBalancingMenuVisible, value);
    }

    public System.Collections.ObjectModel.ObservableCollection<GameInfoDto> AvailableGames => _availableGames;

    public bool IsConnected
    {
        get => _isConnected;
        private set 
        {
            this.RaiseAndSetIfChanged(ref _isConnected, value);
            this.RaisePropertyChanged(nameof(IsLobbyVisible));
            this.RaisePropertyChanged(nameof(IsWaitingRoomVisible));
            this.RaisePropertyChanged(nameof(IsConnectionVisible));
        }
    }

    public bool IsInGameRoom
    {
        get => _isInGameRoom;
        private set 
        {
            this.RaiseAndSetIfChanged(ref _isInGameRoom, value);
            this.RaisePropertyChanged(nameof(IsLobbyVisible));
            this.RaisePropertyChanged(nameof(IsWaitingRoomVisible));
            this.RaisePropertyChanged(nameof(IsConnectionVisible));
        }
    }

    public bool IsConnecting
    {
        get => _isConnecting;
        private set => this.RaiseAndSetIfChanged(ref _isConnecting, value);
    }

    public PlayerRole SelectedRole
    {
        get => _selectedRole;
        set 
        {
            this.RaiseAndSetIfChanged(ref _selectedRole, value);
            this.RaisePropertyChanged(nameof(CanPlaceTower));
            this.RaisePropertyChanged(nameof(CanSendUnits));
            this.RaisePropertyChanged(nameof(CanSkipPreparation));
            this.RaisePropertyChanged(nameof(IsSoloMode));
            this.RaisePropertyChanged(nameof(IsDefenderShopVisible));
            this.RaisePropertyChanged(nameof(IsAttackerShopVisible));
            if (value != PlayerRole.Both)
            {
                IsBalancingMenuVisible = false;
            }
        }
    }

    public bool CanPlaceTower => SelectedRole is PlayerRole.Both or PlayerRole.Defender;
    public bool CanSendUnits => SelectedRole is PlayerRole.Both or PlayerRole.Attacker;
    public bool CanSkipPreparation => SelectedRole is PlayerRole.Both or PlayerRole.Defender;
    public bool IsDefenderShopVisible => SelectedRole == PlayerRole.Defender || (SelectedRole == PlayerRole.Both && IsDefenderPhase);
    public bool IsAttackerShopVisible => SelectedRole == PlayerRole.Attacker || (SelectedRole == PlayerRole.Both && !IsDefenderPhase);

    public bool IsReady
    {
        get => _isReady;
        set 
        {
            this.RaiseAndSetIfChanged(ref _isReady, value);
            _ = _networkCoordinator.SetReadyAsync(value);
        }
    }

    public bool IsGameStarted
    {
        get => _isGameStarted;
        private set 
        {
            this.RaiseAndSetIfChanged(ref _isGameStarted, value);
            this.RaisePropertyChanged(nameof(IsLobbyVisible));
            this.RaisePropertyChanged(nameof(IsWaitingRoomVisible));
            this.RaisePropertyChanged(nameof(IsConnectionVisible));
        }
    }

    public bool IsOpponentReady
    {
        get => _isOpponentReady;
        private set => this.RaiseAndSetIfChanged(ref _isOpponentReady, value);
    }

    public bool IsLobbyVisible => false; // Désormais fusionné dans IsConnectionVisible
    public bool IsWaitingRoomVisible => IsConnected && IsInGameRoom && !IsGameStarted;
    public bool IsConnectionVisible => !IsInGameRoom && !IsGameStarted;
    public bool IsSoloMode => SelectedRole == PlayerRole.Both;

    public MatchState Snapshot
    {
        get => _snapshot;
        private set
        {
            this.RaiseAndSetIfChanged(ref _snapshot, value);
            this.RaisePropertyChanged(nameof(PreparationTimeFormatted));
            this.RaisePropertyChanged(nameof(WaveSendTimeFormatted));
            this.RaisePropertyChanged(nameof(IsPreparationTimerVisible));
            this.RaisePropertyChanged(nameof(PhaseFormatted));
            this.RaisePropertyChanged(nameof(ActivePhaseLabel));
            this.RaisePropertyChanged(nameof(ActivePhaseTime));
            this.RaisePropertyChanged(nameof(IsDefenderPhase));
            this.RaisePropertyChanged(nameof(IsGameFinished));
            this.RaisePropertyChanged(nameof(GameResultMessage));
            this.RaisePropertyChanged(nameof(GameResultColor));
            // Notifications pour les liaisons directes simples
            this.RaisePropertyChanged(nameof(BaseHealth));
            this.RaisePropertyChanged(nameof(DefenderGold));
            this.RaisePropertyChanged(nameof(AttackerBudget));
            this.RaisePropertyChanged(nameof(WaveNumber));
            this.RaisePropertyChanged(nameof(Level));
            this.RaisePropertyChanged(nameof(IsDefenderShopVisible));
            this.RaisePropertyChanged(nameof(IsAttackerShopVisible));
        }
    }

    // Propriétés plates pour liaisons simples dans MainWindow.axaml
    public int BaseHealth => Snapshot.Simulation.BaseHealth.Value;
    public int DefenderGold => Snapshot.DefenderGold.Value;
    public int AttackerBudget => Snapshot.AttackerBudget.Value;
    public int WaveNumber => Snapshot.WaveNumber;
    public int Level => Snapshot.CurrentLevel;

    public bool IsGameFinished => Snapshot.Phase == MatchPhase.Finished;

    public string GameResultMessage => Snapshot.Outcome switch
    {
        MatchOutcome.DefenderVictory => SelectedRole switch
        {
            PlayerRole.Defender => "VICTOIRE ! LA BASE EST SAUVE.",
            PlayerRole.Attacker => "DÉFAITE... L'ASSAUT A ÉCHOUÉ.",
            _ => "FIN DE MISSION : DÉFENSE VICTORIEUSE"
        },
        MatchOutcome.AttackerVictory => SelectedRole switch
        {
            PlayerRole.Attacker => "VICTOIRE ! LE NOYAU EST DÉTRUIT.",
            PlayerRole.Defender => "DÉFAITE... LA BASE A SUCCOMBÉ.",
            _ => "FIN DE MISSION : ATTAQUE VICTORIEUSE"
        },
        _ => "MATCH NUL"
    };

    public string GameResultColor => Snapshot.Outcome switch
    {
        MatchOutcome.DefenderVictory => SelectedRole == PlayerRole.Defender ? "#00F2FF" : "#FF2E2E",
        MatchOutcome.AttackerVictory => SelectedRole == PlayerRole.Attacker ? "#FF00E5" : "#FF2E2E",
        _ => "#FFFFFF"
    };

    public string PreparationTimeFormatted => $"{Snapshot.PreparationTicksRemaining / 60.0:F1} sec";
    public string WaveSendTimeFormatted => $"{Snapshot.WaveSendTicksRemaining / 60.0:F1} sec";
    public bool IsPreparationTimerVisible => Snapshot.PreparationTicksRemaining > 0;
    public bool IsSkipButtonVisible => IsPreparationTimerVisible && CanSkipPreparation;

    // COÛTS TACTIQUES (Synchronisés avec GameConfig)
    private int GetTowerCost(TowerType type)
    {
        var idx = _customTowers.FindIndex(t => t.Type == type);
        return idx != -1 ? _customTowers[idx].Stats.Cost.Value : 0;
    }

    private int GetUnitCost(UnitType type)
    {
        var idx = _customUnits.FindIndex(u => u.Type == type);
        return idx != -1 ? _customUnits[idx].Cost.Value : 0;
    }

    private string GetTowerStatsString(TowerType type)
    {
        var idx = _customTowers.FindIndex(t => t.Type == type);
        if (idx == -1) return "";
        var t = _customTowers[idx];
        return $"Dégâts: {t.Stats.DamagePerShot.Value} | Portée: {t.Stats.Range} | Cadence: {(t.Stats.CooldownTicksBetweenShots / 60.0):F1}s | PV: {t.Health.Value}";
    }

    private string GetUnitStatsString(UnitType type)
    {
        var idx = _customUnits.FindIndex(u => u.Type == type);
        if (idx == -1) return "";
        var u = _customUnits[idx];
        return $"PV: {u.Health.Value} | Vitesse: {u.SpeedPerTick} | Portée: {u.AttackRange} | Butin: {u.LootGold.Value} CR";
    }

    public int BasicTowerCost => GetTowerCost(TowerType.BasicShooter);
    public int FlamethrowerCost => GetTowerCost(TowerType.Flamethrower);
    public int SniperTowerCost => GetTowerCost(TowerType.Sniper);
    public int CannonTowerCost => GetTowerCost(TowerType.Cannon);
    public int LaserTowerCost => GetTowerCost(TowerType.Laser);
    public int SoldatCost => GetUnitCost(UnitType.Soldat);
    public int BruteCost => GetUnitCost(UnitType.Brute);
    public int RapideCost => GetUnitCost(UnitType.Rapide);
    public int TireurEliteCost => GetUnitCost(UnitType.TireurElite);
    public int TankCost => GetUnitCost(UnitType.Tank);

    public string PhaseFormatted => Snapshot.Phase switch
    {
        MatchPhase.Preparation => "Préparation",
        MatchPhase.Wave => "Vague",
        MatchPhase.Finished => "Terminé",
        _ => Snapshot.Phase.ToString()
    };

    public string ActivePhaseLabel => Snapshot.Phase == MatchPhase.Preparation ? "PHASE DE DÉFENSE" : "PHASE D'ATTAQUE";
    public string ActivePhaseTime => Snapshot.Phase == MatchPhase.Preparation ? PreparationTimeFormatted : WaveSendTimeFormatted;
    public bool IsDefenderPhase => Snapshot.Phase == MatchPhase.Preparation;

    public string? LastError
    {
        get => _lastError;
        private set => this.RaiseAndSetIfChanged(ref _lastError, value);
    }

    public TowerType CurrentTowerType
    {
        get => _currentTowerType;
        set {
            this.RaiseAndSetIfChanged(ref _currentTowerType, value);
            this.RaisePropertyChanged(nameof(CurrentTowerStats));
        }
    }

    public string CurrentTowerStats => GetTowerStatsString(CurrentTowerType);

    public string SoldatStats => GetUnitStatsString(UnitType.Soldat);
    public string BruteStats => GetUnitStatsString(UnitType.Brute);
    public string RapideStats => GetUnitStatsString(UnitType.Rapide);
    public string TireurEliteStats => GetUnitStatsString(UnitType.TireurElite);
    public string TankStats => GetUnitStatsString(UnitType.Tank);

    public ReactiveCommand<RxUnit, RxUnit> SkipPreparationCommand { get; }
    public ReactiveCommand<RxUnit, RxUnit> SendSoldatCommand { get; }
    public ReactiveCommand<RxUnit, RxUnit> SendBruteCommand { get; }
    public ReactiveCommand<RxUnit, RxUnit> SendRapideCommand { get; }
    public ReactiveCommand<RxUnit, RxUnit> SendTireurEliteCommand { get; }
    public ReactiveCommand<RxUnit, RxUnit> SendTankCommand { get; }
    public ReactiveCommand<GridPosition, RxUnit> PlaceTowerCommand { get; }
    public ReactiveCommand<GridPosition, RxUnit> SellTowerCommand { get; }
    public ReactiveCommand<RxUnit, RxUnit> SetBasicTowerCommand { get; }
    public ReactiveCommand<RxUnit, RxUnit> SetFlamethrowerCommand { get; }
    public ReactiveCommand<RxUnit, RxUnit> SetSniperTowerCommand { get; }
    public ReactiveCommand<RxUnit, RxUnit> SetCannonTowerCommand { get; }
    public ReactiveCommand<RxUnit, RxUnit> SetLaserTowerCommand { get; }
    public ReactiveCommand<RxUnit, RxUnit> ConnectCommand { get; }
    public ReactiveCommand<RxUnit, RxUnit> RefreshGamesCommand { get; }
    public ReactiveCommand<string, RxUnit> JoinSpecificGameCommand { get; }
    public ReactiveCommand<RxUnit, RxUnit> StartSoloCommand { get; }
    public ReactiveCommand<RxUnit, RxUnit> ReplayCommand { get; }
    public ReactiveCommand<RxUnit, RxUnit> QuitCommand { get; }

    public void Tick()
    {
        if (!IsGameStarted || _gameStartTime == null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var elapsed = now - _gameStartTime.Value;
        
        long targetTicks = (long)(elapsed.TotalSeconds * 60);
        int ticksToProcess = (int)(targetTicks - _totalTicksProcessed);

        if (ticksToProcess <= 0) return;

        ticksToProcess = Math.Min(ticksToProcess, 10);
        
        _session.Tick(ticksToProcess);
        _totalTicksProcessed += ticksToProcess;
        
        Snapshot = _session.State;

        SoundEffects.PlayEvents(Snapshot.LastCombatEvents, Snapshot.Simulation.Units);
    }

    private void ExecuteSkipPreparation()
    {
        Apply(_session.SkipPreparation());
        BroadcastAction(PlayerActionKind.SkipPreparation);
    }

    private void ExecuteSendSoldat()
    {
        if (!CanSendUnits) return;
        Apply(_session.SendUnit(UnitType.Soldat));
        BroadcastAction(PlayerActionKind.SendWave, unitType: (int)UnitType.Soldat);
    }

    private void ExecuteSendBrute()
    {
        if (!CanSendUnits) return;
        Apply(_session.SendUnit(UnitType.Brute));
        BroadcastAction(PlayerActionKind.SendWave, unitType: (int)UnitType.Brute);
    }

    private void ExecuteSendRapide()
    {
        if (!CanSendUnits) return;
        Apply(_session.SendUnit(UnitType.Rapide));
        BroadcastAction(PlayerActionKind.SendWave, unitType: (int)UnitType.Rapide);
    }

    private void ExecuteSendTireurElite()
    {
        if (!CanSendUnits) return;
        Apply(_session.SendUnit(UnitType.TireurElite));
        BroadcastAction(PlayerActionKind.SendWave, unitType: (int)UnitType.TireurElite);
    }

    private void ExecuteSendTank()
    {
        if (!CanSendUnits) return;
        Apply(_session.SendUnit(UnitType.Tank));
        BroadcastAction(PlayerActionKind.SendWave, unitType: (int)UnitType.Tank);
    }

    private void ExecutePlaceTower(GridPosition position)
    {
        if (!CanPlaceTower) return;

        if (Snapshot.Phase != MatchPhase.Preparation)
        {
            LastError = "ACTION IMPOSSIBLE : Attendez la phase de préparation.";
            return;
        }

        var existingTower = Snapshot.Simulation.Towers.FirstOrDefault(t => t.Position.X == position.X && t.Position.Y == position.Y);

        if (_movingTowerFrom != null && existingTower == null)
        {
            var oldPos = _movingTowerFrom.Value;
            var result = _session.MoveTower(oldPos, position);
            
            if (result.IsSuccess)
            {
                BroadcastAction(PlayerActionKind.MoveTower, x: position.X, y: position.Y, oldX: oldPos.X, oldY: oldPos.Y);
                _movingTowerFrom = null;
                Apply(result);
                LastError = null;
                return;
            }
        }

        if (existingTower != null)
        {
            _movingTowerFrom = position;
            LastError = "DÉPLACEMENT : Maintenez et glissez vers une case vide.";
            return;
        }

        if (_movingTowerFrom == null)
        {
            Apply(_session.PlaceTower(CurrentTowerType, position));
            BroadcastAction(PlayerActionKind.PlaceTower, towerType: (int)CurrentTowerType, x: position.X, y: position.Y);
        }
        
        _movingTowerFrom = null;
    }

    private void ExecuteSellTower(GridPosition position)
    {
        if (!CanPlaceTower) return;

        if (Snapshot.Phase != MatchPhase.Preparation)
        {
            LastError = "ACTION IMPOSSIBLE : Attendez la phase de préparation.";
            return;
        }

        var result = _session.SellTower(position);
        if (result.IsSuccess)
        {
            BroadcastAction(PlayerActionKind.SellTower, x: position.X, y: position.Y);
            Apply(result);
        }
        else
        {
            LastError = result.ErrorMessage;
        }
    }

    private async Task ExecuteConnect()
    {
        IsConnecting = true;
        LastError = null;
        try
        {
            await _networkCoordinator.ConnectAsync(ServerUrl);
        }
        catch (Exception)
        {
            // L'erreur réseau est interceptée par le coordinateur
        }
        finally
        {
            IsConnecting = false;
        }
    }

    private async Task ExecuteRefreshGames()
    {
        await _networkCoordinator.RefreshGamesAsync();
    }

    private async Task ExecuteJoinSpecificGame(string gameId)
    {
        await _networkCoordinator.JoinGameAsync(gameId, SelectedRole);
        IsInGameRoom = true;
    }

    private void ExecuteStartSolo()
    {
        _session = CreateSession();
        _gameStartTime = DateTime.UtcNow;
        _totalTicksProcessed = 0;
        Snapshot = _session.State;
        IsGameStarted = true;
    }

    private void ExecuteReplay()
    {
        if (IsConnected)
        {
            Task.Run(async () => await _networkCoordinator.LeaveGameAsync());
        }

        IsGameStarted = false;
        IsInGameRoom = false;
        IsReady = false;
        IsOpponentReady = false;
        _gameStartTime = null;
        _totalTicksProcessed = 0;

        _session = CreateSession();
        Snapshot = _session.State;
    }

    private void ExecuteQuit()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    private void HandleNetworkAction(PlayerAction action)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() => {
            switch (action.Kind)
            {
                case PlayerActionKind.PlaceTower:
                    if (action.TowerType.HasValue && action.X.HasValue && action.Y.HasValue)
                    {
                        Apply(_session.PlaceTower((TowerType)action.TowerType.Value, new GridPosition(action.X.Value, action.Y.Value)));
                    }
                    break;
                case PlayerActionKind.SendWave:
                    if (action.UnitType.HasValue)
                    {
                        Apply(_session.SendUnit((UnitType)action.UnitType.Value));
                    }
                    break;
                case PlayerActionKind.MoveTower:
                    if (action.X.HasValue && action.Y.HasValue && action.OldX.HasValue && action.OldY.HasValue)
                    {
                        Apply(_session.MoveTower(
                            new GridPosition(action.OldX.Value, action.OldY.Value), 
                            new GridPosition(action.X.Value, action.Y.Value)));
                    }
                    break;
                case PlayerActionKind.SkipPreparation:
                    Apply(_session.SkipPreparation());
                    break;
                case PlayerActionKind.SellTower:
                    if (action.X.HasValue && action.Y.HasValue)
                    {
                        Apply(_session.SellTower(new GridPosition(action.X.Value, action.Y.Value)));
                    }
                    break;
            }
        });
    }

    private void BroadcastAction(PlayerActionKind kind, int? towerType = null, int? x = null, int? y = null, int? unitType = null, int? oldX = null, int? oldY = null)
    {
        if (IsConnected)
        {
            _ = _networkCoordinator.SendActionAsync(kind, towerType, x, y, unitType, oldX, oldY, _totalTicksProcessed);
        }
    }

    private void Apply(CommandResult result)
    {
        if (!result.IsSuccess)
        {
            LastError = result.ErrorMessage;
            return;
        }

        LastError = null;
        Snapshot = _session.State;
    }

    private static string GetLocalIPAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    var ipStr = ip.ToString();
                    if (!ipStr.StartsWith("127."))
                    {
                        return ipStr;
                    }
                }
            }
        }
        catch
        {
            // Fallback
        }
        return "localhost";
    }

    // --- BALANCING IMPLEMENTATION ---
    private static readonly string SettingsFilePath = "balancing_settings.json";

    private List<TowerDefinition> _customTowers = new();
    private List<UnitDefinition> _customUnits = new();
    private GameConfig _customConfig = null!;

    public GameConfig ActiveConfig => _customConfig;

    private void InitializeBalancing()
    {
        var defaults = GameConfig.CreateMvpDefaults();
        
        if (System.IO.File.Exists(SettingsFilePath))
        {
            try
            {
                var json = System.IO.File.ReadAllText(SettingsFilePath);
                var data = System.Text.Json.JsonSerializer.Deserialize<BalancingData>(json);
                if (data != null)
                {
                    _customTowers = data.Towers.Select(t => {
                        var def = defaults.Towers.FirstOrDefault(x => x.Type == t.Type);
                        return new TowerDefinition(t.Type, new TowerStats(new Gold(t.Cost), new Damage(t.Damage), t.Range, t.Cooldown), def.Health);
                    }).ToList();

                    _customUnits = data.Units.Select(u => {
                        var def = defaults.Units.FirstOrDefault(x => x.Type == u.Type);
                        return new UnitDefinition(
                            u.Type,
                            new Budget(u.Cost),
                            new Health(u.Health),
                            u.Speed,
                            def.DamageToBase,
                            def.DamageToTower,
                            def.AttackRange,
                            def.AttackCooldownTicksBetweenAttacks,
                            new Gold(u.Bounty)
                        );
                    }).ToList();

                    RecreateConfig();
                    
                    _isUpdatingFields = true;
                    UpdateTowerBalancingFields();
                    UpdateUnitBalancingFields();
                    _isUpdatingFields = false;
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load balancing settings: {ex.Message}");
            }
        }

        // Recommended balanced defaults:
        _customTowers = defaults.Towers.Select(t => {
            if (t.Type == TowerType.Sniper)
            {
                // Sniper balance: make it 200 gold and slightly slower (cooldown 75 ticks instead of 60)
                return new TowerDefinition(t.Type, new TowerStats(new Gold(200), t.Stats.DamagePerShot, t.Stats.Range, 75), t.Health);
            }
            return t;
        }).ToList();

        _customUnits = defaults.Units.Select(u => {
            // Adjust unit bounties to avoid feeding the defender's economy:
            int newBounty = u.Type switch
            {
                UnitType.Soldat => 5,
                UnitType.Brute => 20,
                UnitType.Rapide => 8,
                UnitType.TireurElite => 15,
                UnitType.Tank => 40,
                _ => u.LootGold.Value
            };
            return new UnitDefinition(
                u.Type,
                u.Cost,
                u.Health,
                u.SpeedPerTick,
                u.DamageToBase,
                u.DamageToTower,
                u.AttackRange,
                u.AttackCooldownTicksBetweenAttacks,
                new Gold(newBounty)
            );
        }).ToList();

        RecreateConfig();
        
        _isUpdatingFields = true;
        UpdateTowerBalancingFields();
        UpdateUnitBalancingFields();
        _isUpdatingFields = false;
    }

    private void RecreateConfig()
    {
        var defaults = GameConfig.CreateMvpDefaults();
        _customConfig = new GameConfig(
            defaults.TotalWaves,
            defaults.PreparationTicks,
            defaults.WaveSendWindowTicks,
            defaults.BaseWaveBudget,
            defaults.WaveBudgetIncrement,
            defaults.BudgetBonusPerTowerDestroyed,
            defaults.StartingGold,
            defaults.StartingBaseHealth,
            defaults.GoldPerBaseDamageTaken,
            _customTowers,
            _customUnits
        );
    }

    private void UpdateTowerBalancingFields()
    {
        var type = (TowerType)SelectedBalancingTowerIndex;
        var idx = _customTowers.FindIndex(x => x.Type == type);
        if (idx != -1)
        {
            var t = _customTowers[idx];
            BalancingTowerCost = t.Stats.Cost.Value;
            BalancingTowerDamage = t.Stats.DamagePerShot.Value;
            BalancingTowerRange = t.Stats.Range;
            BalancingTowerCooldown = t.Stats.CooldownTicksBetweenShots;
        }
    }

    private void UpdateUnitBalancingFields()
    {
        var type = (UnitType)SelectedBalancingUnitIndex;
        var idx = _customUnits.FindIndex(x => x.Type == type);
        if (idx != -1)
        {
            var u = _customUnits[idx];
            BalancingUnitCost = u.Cost.Value;
            BalancingUnitHealth = u.Health.Value;
            BalancingUnitSpeed = u.SpeedPerTick;
            BalancingUnitBounty = u.LootGold.Value;
        }
    }

    private void SaveTowerBalancingChanges()
    {
        var type = (TowerType)SelectedBalancingTowerIndex;
        var idx = _customTowers.FindIndex(x => x.Type == type);
        if (idx != -1)
        {
            var existing = _customTowers[idx];
            _customTowers[idx] = new TowerDefinition(
                type,
                new TowerStats(new Gold(BalancingTowerCost), new Damage(BalancingTowerDamage), BalancingTowerRange, BalancingTowerCooldown),
                existing.Health
            );
            RecreateConfig();

            this.RaisePropertyChanged(nameof(BasicTowerCost));
            this.RaisePropertyChanged(nameof(FlamethrowerCost));
            this.RaisePropertyChanged(nameof(SniperTowerCost));
            this.RaisePropertyChanged(nameof(CannonTowerCost));
            this.RaisePropertyChanged(nameof(LaserTowerCost));
            this.RaisePropertyChanged(nameof(CurrentTowerStats));

            SaveSettingsToJson();
        }
    }

    private void SaveUnitBalancingChanges()
    {
        var type = (UnitType)SelectedBalancingUnitIndex;
        var idx = _customUnits.FindIndex(x => x.Type == type);
        if (idx != -1)
        {
            var existing = _customUnits[idx];
            _customUnits[idx] = new UnitDefinition(
                type,
                new Budget(BalancingUnitCost),
                new Health(BalancingUnitHealth),
                BalancingUnitSpeed,
                existing.DamageToBase,
                existing.DamageToTower,
                existing.AttackRange,
                existing.AttackCooldownTicksBetweenAttacks,
                new Gold(BalancingUnitBounty)
            );
            RecreateConfig();

            this.RaisePropertyChanged(nameof(SoldatCost));
            this.RaisePropertyChanged(nameof(BruteCost));
            this.RaisePropertyChanged(nameof(RapideCost));
            this.RaisePropertyChanged(nameof(TireurEliteCost));
            this.RaisePropertyChanged(nameof(TankCost));
            this.RaisePropertyChanged(nameof(SoldatStats));
            this.RaisePropertyChanged(nameof(BruteStats));
            this.RaisePropertyChanged(nameof(RapideStats));
            this.RaisePropertyChanged(nameof(TireurEliteStats));
            this.RaisePropertyChanged(nameof(TankStats));

            SaveSettingsToJson();
        }
    }

    private void SaveSettingsToJson()
    {
        try
        {
            var data = new BalancingData
            {
                Towers = _customTowers.Select(t => new TowerBalancingData
                {
                    Type = t.Type,
                    Cost = t.Stats.Cost.Value,
                    Damage = t.Stats.DamagePerShot.Value,
                    Range = t.Stats.Range,
                    Cooldown = t.Stats.CooldownTicksBetweenShots
                }).ToList(),
                Units = _customUnits.Select(u => new UnitBalancingData
                {
                    Type = u.Type,
                    Cost = u.Cost.Value,
                    Health = u.Health.Value,
                    Speed = u.SpeedPerTick,
                    Bounty = u.LootGold.Value
                }).ToList()
            };

            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            var json = System.Text.Json.JsonSerializer.Serialize(data, options);
            System.IO.File.WriteAllText(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save balancing settings: {ex.Message}");
        }
    }

    private GameSession CreateSession(int? seed = null)
    {
        var finalSeed = seed ?? new Random().Next();
        var map = DefaultMapFactory.Create(1, finalSeed);
        return new GameSession(ActiveConfig, map, finalSeed);
    }
}

public enum PlayerRole
{
    Both,
    Attacker,
    Defender
}

public sealed class BalancingData
{
    public List<TowerBalancingData> Towers { get; set; } = new();
    public List<UnitBalancingData> Units { get; set; } = new();
}

public sealed class TowerBalancingData
{
    public TowerType Type { get; set; }
    public int Cost { get; set; }
    public int Damage { get; set; }
    public int Range { get; set; }
    public int Cooldown { get; set; }
}

public sealed class UnitBalancingData
{
    public UnitType Type { get; set; }
    public int Cost { get; set; }
    public int Health { get; set; }
    public int Speed { get; set; }
    public int Bounty { get; set; }
}
