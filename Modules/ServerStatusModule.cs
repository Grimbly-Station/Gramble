using Discord;
using Discord.Interactions;
using Gramble.Enums;
using Gramble.Utility;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace Gramble.Modules;

public class ServerStatusModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("status", "Get a server's status.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task Status()
    {
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

        await RespondAsync(embed: embed);
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
        public int RoundId { get; set; }
        public int SoftMaxPlayers { get; set; }
        public bool PanicBunker { get; set; }
        public bool BabyJail { get; set; }
        public int RunLevel { get; set; }
        public string Preset { get; set; }
    }
}