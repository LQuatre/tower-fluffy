using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TowerFluffy.Application.Common.Networking;
using TowerFluffy.Infrastructure.Networking;
using TowerFluffy.UI.Desktop.ViewModels;

namespace TowerFluffy.UI.Desktop.Services;

public sealed class GameNetworkCoordinator : IDisposable
{
    private SignalRGameClient? _networkClient;

    public event Action<bool>? ConnectionStatusChanged;
    public event Action<bool>? ConnectingStatusChanged;
    public event Action<IEnumerable<GameInfoDto>>? GameListReceived;
    public event Action<PlayerRole>? RoleReceived;
    public event Action<bool>? OpponentReadyChanged;
    public event Action<int, long, string?>? GameStarted;
    public event Action<PlayerAction>? PlayerActionReceived;
    public event Action? RoomClosed;
    public event Action<string>? ErrorOccurred;

    public bool IsConnected => _networkClient != null;

    public async Task ConnectAsync(string serverUrl)
    {
        ConnectingStatusChanged?.Invoke(true);
        ErrorOccurred?.Invoke(string.Empty);
        try
        {
            await Task.Delay(500);

            var client = new SignalRGameClient(serverUrl.Trim());
            client.OnPlayerActionReceived += action => PlayerActionReceived?.Invoke(action);
            client.OnGameStarted += (seed, startTime, settingsJson) => GameStarted?.Invoke(seed, startTime, settingsJson);
            client.OnOpponentReady += ready => OpponentReadyChanged?.Invoke(ready);
            client.OnGameListReceived += games => GameListReceived?.Invoke(games);
            client.OnRoleReceived += role => RoleReceived?.Invoke((PlayerRole)role);
            client.OnRoomClosed += () => RoomClosed?.Invoke();

            await client.StartAsync();
            _networkClient = client;
            ConnectionStatusChanged?.Invoke(true);

            await client.GetActiveGames();
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke($"ERREUR RÉSEAU : {ex.Message}");
            throw;
        }
        finally
        {
            ConnectingStatusChanged?.Invoke(false);
        }
    }

    public async Task RefreshGamesAsync()
    {
        if (_networkClient != null)
        {
            await _networkClient.GetActiveGames();
        }
    }

    public async Task JoinGameAsync(string gameId, PlayerRole role)
    {
        if (_networkClient != null)
        {
            await _networkClient.JoinGame(gameId, (int)role);
        }
    }

    public async Task SetReadyAsync(bool ready, string? balancingSettingsJson = null)
    {
        if (_networkClient != null)
        {
            await _networkClient.SetReady(ready, balancingSettingsJson);
        }
    }

    public async Task LeaveGameAsync()
    {
        if (_networkClient != null)
        {
            await _networkClient.LeaveGame();
        }
    }

    public async Task SendActionAsync(
        PlayerActionKind kind,
        int? towerType = null,
        int? x = null,
        int? y = null,
        int? unitType = null,
        int? oldX = null,
        int? oldY = null,
        long totalTicksProcessed = 0)
    {
        if (_networkClient != null)
        {
            var action = new PlayerAction(1, kind, towerType, x, y, unitType, oldX, oldY, totalTicksProcessed);
            await _networkClient.SendPlayerAction(action);
        }
    }

    public void Disconnect()
    {
        if (_networkClient != null)
        {
            var client = _networkClient;
            _networkClient = null;
            Task.Run(async () =>
            {
                try
                {
                    await client.StopAsync();
                }
                catch
                {
                    // On ignore les erreurs lors de l'arrêt
                }
            });
            ConnectionStatusChanged?.Invoke(false);
        }
    }

    public void Dispose()
    {
        Disconnect();
    }
}
