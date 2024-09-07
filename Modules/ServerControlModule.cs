using Discord;
using Discord.Interactions;
using dotenv.net;
using Gramble.Configuration;
using Gramble.Enums;
using Microsoft.Extensions.Configuration;

namespace Gramble.Modules;
public class ServerControlModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IDictionary<string, string> env = DotEnv.Read();

    [SlashCommand("poweraction", "Send a power/update action to a Grimbly server.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task PowerAction(PowerAction powerAction)
    {
        
        var response = await HandleRequest(powerAction);
        await ReplyAsync(response);
    }

    private async Task<string> HandleRequest(PowerAction powerAction)
    {
        string powerActionString = StringifyPowerAction(powerAction);
        env.TryGetValue("apiToken", out string? apiToken);
        env.TryGetValue("host", out string? host);

        if (apiToken == null)
        {
            return "No API token!?";
        }

        HttpClient client = new HttpClient();
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, host + "/instances/grimble/" + powerActionString);
        request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes("grimble" + ":" + apiToken)));

        HttpResponseMessage response = await client.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            return "Successfully sent power action ``" + powerActionString.ToUpper() + "`` to instance ``GRIMBLE``";
        }
        else
        {
            return "Failed with status code " + response.StatusCode;
        }
    }

    private string StringifyPowerAction(PowerAction powerAction)
    {
        switch (powerAction)
        {
            case Enums.PowerAction.Stop:
                return "stop";
            case Enums.PowerAction.Restart:
                return "restart";
            case Enums.PowerAction.Update:
                return "update";
            default:
                return "";
        }
    }
}