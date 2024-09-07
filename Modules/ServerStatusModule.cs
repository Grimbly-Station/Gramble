using Discord;
using Discord.Interactions;
using Gramble.Enums;
using Newtonsoft.Json;

namespace Gramble.Modules;

public class ServerStatusModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("status", "Get a server's status.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task Status()
    {
        var response = await GetServerStatus();
        
        if (response is string)
        {
            await RespondAsync((string) response);
        } else if (response is Embed)
        {
            await RespondAsync(embed: (Embed) response);
        }
    }

    private async Task<object> GetServerStatus()
    {
        HttpClient client = new HttpClient();
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://grimbly.net:1212/status");
        HttpResponseMessage response = await client.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            var jsonString = await response.Content.ReadAsStringAsync();
            var info = JsonConvert.DeserializeObject<ServerInfo>(jsonString);
            var embed = new EmbedBuilder()
                .AddField("Name", info.Name, false)
                .AddField("Players", $"{info.Players}/{info.Soft_Max_Players}", false)
                .AddField("Round Number", info.Round_Id, false)
                .AddField("Panic Bunker", info.PanicBunker ? "Yes" : "No", true)
                .AddField("Baby Jail", info.BabyJail ? "Yes" : "No", true)
                .AddField("Status", GetStatus(info.RunLevel))
                .WithAuthor(new EmbedAuthorBuilder().WithName("Grimbly Station"))
                .WithCurrentTimestamp()
                .WithColor(new Color(255, 85, 0))
                .WithDescription("Status of Grimbly Station")
                .Build();

            return embed;
        }
        else
        {
            return "Our server is currently offline!";
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

    public struct ServerInfo
    {
        public string Name { get; set; }
        public int Players { get; set; }
        public List<string> Tags { get; set; }
        public string? Map { get; set; }
        public int Round_Id { get; set; }
        public int Soft_Max_Players { get; set; }
        public bool PanicBunker { get; set; }
        public bool BabyJail { get; set; }
        public int RunLevel { get; set; }
        public string Preset { get; set; }
    }
}