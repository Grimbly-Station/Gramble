using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Gramble.Utility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Reflection;
using System.Threading.Tasks;
using static Gramble.Modules.ServerStatusModule;

namespace Gramble.Services;

public class InteractionHandlingService
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _handler;
    private readonly IServiceProvider _services;
    private readonly IConfiguration _configuration;

    private RestUserMessage? updatingMessage;

    public InteractionHandlingService(DiscordSocketClient client, InteractionService handler, IServiceProvider services, IConfiguration config)
    {
        _client = client;
        _handler = handler;
        _services = services;
        _configuration = config;
    }

    public async Task InitializeAsync()
    {
        // Process when the client is ready, so we can register our commands.
        _client.Ready += ReadyAsync;
        _handler.Log += LogAsync;

        // Add the public modules that inherit InteractionModuleBase<T> to the InteractionService
        await _handler.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

        // Process the InteractionCreated payloads to execute Interactions commands
        _client.InteractionCreated += HandleInteraction;
        _client.ButtonExecuted += HandleButton;

        // Also process the result of the command execution.
        _handler.InteractionExecuted += HandleInteractionExecute;
    }

    private Task LogAsync(LogMessage log)
    {
        Console.WriteLine(log);
        return Task.CompletedTask;
    }

    private async Task ReadyAsync()
    {
        // Register the commands globally.
        // alternatively you can use _handler.RegisterCommandsGloballyAsync() to register commands to a specific guild.
        await _handler.RegisterCommandsGloballyAsync();
        _ = Task.Run(async () =>
        {
            while (_client.LoginState == LoginState.LoggedIn)
            {
                await RunUpdatingStatus();
                await Task.Delay(30000);
            }
        });
    }

    public async Task RunUpdatingStatus()
    {
        var env = DotEnvUtility.DotEnvDictionary;
        var guildIdString = env["guild_id"];
        var channelIdString = env["channel_id"];

        var guildId = ulong.Parse(guildIdString);
        var channelId = ulong.Parse(channelIdString);

        var channel = _client.GetGuild(guildId).GetTextChannel(channelId);
        var messages = channel.GetMessagesAsync(10).Flatten();

        await foreach (RestUserMessage message in messages)
        {
            await message.DeleteAsync();
        }

        var actions = new ComponentBuilder()
            .WithButton("Stop", "grimbly:status:stop")
            .WithButton("Restart", "grimbly:status:restart")
            .WithButton("Update", "grimbly:status:update")
            .Build();

        var response = await StatusUtility.GetServerStatus();
        var info = JsonConvert.DeserializeObject<ServerInfo>(response);
        var embed = new EmbedBuilder()
            .AddField("Name", info.Name, false)
            .AddField("Players", $"{info.Players}/{info.SoftMaxPlayers}", false)
            .AddField("Round Number", info.RoundId, false)
            .AddField("Panic Bunker", info.PanicBunker ? "Yes" : "No", true)
            .AddField("Baby Jail", info.BabyJail ? "Yes" : "No", true)
            .AddField("Status", GetStatus(info.RunLevel))
            .WithAuthor(new EmbedAuthorBuilder().WithName("Grimbly Station"))
            .WithCurrentTimestamp()
            .WithColor(new Color(255, 85, 0))
            .WithDescription("Status of Grimbly Station")
            .Build();

        if (updatingMessage == null)
        {
            updatingMessage = await channel.SendMessageAsync(
                "Last updated <t:" + DateTimeOffset.UtcNow.ToUnixTimeSeconds() + ":R>",
                embed: embed,
                components: actions
            );
        }
        else
        {
            await updatingMessage.ModifyAsync(x =>
            {
                x.Content = "Last updated <t:" + DateTimeOffset.UtcNow.ToUnixTimeSeconds() + ":R>";
                x.Embed = embed;
            }
            );
        }
    }

    private string GetStatus(int runLevel)
    {
        switch (runLevel)
        {
            case 0:
                return "Pre-game";
            case 1:
                return "In-game";
            case 2:
                return "Post-game";
            default:
                return "No available status";
        }
    }

    private async Task HandleInteraction(SocketInteraction interaction)
    {
        try
        {
            // Create an execution context that matches the generic type parameter of your InteractionModuleBase<T> modules.
            var context = new SocketInteractionContext(_client, interaction);

            // Execute the incoming command.
            var result = await _handler.ExecuteCommandAsync(context, _services);

            // Due to async nature of InteractionFramework, the result here may always be success.
            // That's why we also need to handle the InteractionExecuted event.
            if (!result.IsSuccess)
                switch (result.Error)
                {
                    case InteractionCommandError.UnmetPrecondition:
                        // implement
                        break;
                    default:
                        break;
                }
        }
        catch
        {
            // If Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
            // response, or at least let the user know that something went wrong during the command execution.
            if (interaction.Type is InteractionType.ApplicationCommand)
                await interaction.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
        }
    }
    private async Task HandleButton(SocketMessageComponent component)
    {
        if (!component.Data.CustomId.StartsWith("grimbly:status:")) return;
        var split = component.Data.CustomId.Split(":");

        if (!(split.Length > 2)) return;
        var powerActionString = split[2];
        var powerAction = StatusUtility.TranslateToPowerAction(powerActionString);

        var response = await StatusUtility.HandlePowerRequest(powerAction);
        await component.RespondAsync(response);
    }

    private Task HandleInteractionExecute(ICommandInfo commandInfo, IInteractionContext context, IResult result)
    {
        if (!result.IsSuccess)
            switch (result.Error)
            {
                case InteractionCommandError.UnmetPrecondition:
                    // implement
                    break;
                default:
                    break;
            }

        return Task.CompletedTask;
    }
}