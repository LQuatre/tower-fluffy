using MessagePack;
using MessagePack.Resolvers;
using TowerFluffy.Server.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR()
    .AddMessagePackProtocol(options =>
    {
        options.SerializerOptions = MessagePackSerializerOptions.Standard
            .WithResolver(CompositeResolver.Create(
                StandardResolver.Instance,
                ContractlessStandardResolver.Instance
            ));
    });

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(origin => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors();

app.MapHub<GameHub>("/gameHub");

app.MapGet("/", () => "TowerFluffy Game Server is running.");

app.Run();
