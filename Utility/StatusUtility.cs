using Gramble.Enums;
using System.Text.RegularExpressions;

namespace Gramble.Utility;

public class StatusUtility
{
    public static async Task<string> GetServerStatus()
    {
        HttpClient client = new HttpClient();
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://grimbly.net:1212/status");
        HttpResponseMessage response = await client.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            var jsonString = await response.Content.ReadAsStringAsync();
            var regex = new Regex("_\\.");
            jsonString = regex.Replace(jsonString, s => s.ToString().ToUpper())
                .Replace("_", "");



            return jsonString;
        }
        else
        {
            return "Our server is currently offline!";
        }
    }

    public static async Task<string> HandlePowerRequest(PowerAction powerAction)
    {
        string powerActionString = StringifyPowerAction(powerAction);
        DotEnvUtility.DotEnvDictionary.TryGetValue("apiToken", out string? apiToken);
        DotEnvUtility.DotEnvDictionary.TryGetValue("host", out string? host);

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

    public static string StringifyPowerAction(PowerAction powerAction)
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

    public static PowerAction TranslateToPowerAction(string powerActionString)
    {
        switch (powerActionString)
        {
            case "stop":
                return PowerAction.Stop;
            case "restart":
                return PowerAction.Restart;
            case "update":
                return PowerAction.Update;
            default:
                return PowerAction.Restart;
        }
    }
}
