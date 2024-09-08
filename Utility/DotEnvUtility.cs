using dotenv.net;

namespace Gramble.Utility;

public class DotEnvUtility
{
    public static IDictionary<string, string> DotEnvDictionary { get; set; }

    public static void Setup()
    {
        DotEnvDictionary = DotEnv.Read();
    }
}
