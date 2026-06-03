using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using TowerFluffy.Application.Common.Networking;
using TowerFluffy.Domain.Engine;
using TowerFluffy.Domain.Combat;

namespace TowerFluffy.Infrastructure.Networking;

public class SignalRGameClient : IGameHub
{
    private readonly HubConnection _connection;

    public SignalRGameClient(string serverUrl)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(serverUrl)
            .AddMessagePackProtocol(options =>
            {
                options.SerializerOptions = MessagePackSerializerOptions.Standard
                    .WithResolver(CompositeResolver.Create(
                        StandardResolver.Instance,
                        ContractlessStandardResolver.Instance
                    ));
            })
            .WithAutomaticReconnect()
            .Build();

        // Register handlers
        _connection.On<GameState>("ReceiveGameState", (state) => OnGameStateReceived?.Invoke(state));
        _connection.On<CombatEvent>("ReceiveCombatEvent", (e) => OnCombatEventReceived?.Invoke(e));
        _connection.On<string, string>("ReceiveChat", (sender, message) => OnChatReceived?.Invoke(sender, message));
        _connection.On<PlayerAction>("ReceivePlayerAction", (action) => OnPlayerActionReceived?.Invoke(action));
        _connection.On<int, long, string?>("ReceiveGameStarted", (seed, startTime, settingsJson) => OnGameStarted?.Invoke(seed, startTime, settingsJson));
        _connection.On<bool>("ReceiveOpponentReady", (isReady) => OnOpponentReady?.Invoke(isReady));
        _connection.On("ReceiveRoomClosed", () => OnRoomClosed?.Invoke());
        _connection.On<int>("ReceiveRole", (role) => OnRoleReceived?.Invoke(role));
        _connection.On<List<GameInfoDto>>("ReceiveGameList", (games) => OnGameListReceived?.Invoke(games));
    }

    public event Action<GameState>? OnGameStateReceived;
    public event Action<CombatEvent>? OnCombatEventReceived;
    public event Action<string, string>? OnChatReceived;
    public event Action<PlayerAction>? OnPlayerActionReceived;
    public event Action<int, long, string?>? OnGameStarted;
    public event Action<bool>? OnOpponentReady;
    public event Action? OnRoomClosed;
    public event Action<int>? OnRoleReceived;
    public event Action<List<GameInfoDto>>? OnGameListReceived;

    public async Task StartAsync()
    {
        await _connection.StartAsync();
    }

    public async Task StopAsync()
    {
        await _connection.StopAsync();
    }

    public async Task JoinGame(string gameId, int requestedRole)
    {
        await _connection.InvokeAsync("JoinGame", gameId, requestedRole);
    }

    public async Task LeaveGame()
    {
        await _connection.InvokeAsync("LeaveGame");
    }

    public async Task GetActiveGames()
    {
        await _connection.InvokeAsync(nameof(GetActiveGames));
    }

    public async Task SetReady(bool isReady, string? balancingSettingsJson)
    {
        await _connection.InvokeAsync(nameof(SetReady), isReady, balancingSettingsJson);
    }

    public async Task SendPlayerAction(PlayerAction action)
    {
        await _connection.InvokeAsync(nameof(SendPlayerAction), action);
    }

    public async Task SendChat(string message)
    {
        await _connection.InvokeAsync(nameof(SendChat), message);
    }
}
