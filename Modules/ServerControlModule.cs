using Discord;
using Discord.Interactions;
using dotenv.net;
using Gramble.Configuration;
using Gramble.Enums;
using Gramble.Utility;
using Microsoft.Extensions.Configuration;

namespace Gramble.Modules;
public class ServerControlModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("poweraction", "Send a power/update action to a Grimbly server.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task PowerAction(PowerAction powerAction)
    {
        var response = await StatusUtility.HandlePowerRequest(powerAction);
        await RespondAsync(response);
    }
}