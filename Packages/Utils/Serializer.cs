using Newtonsoft.Json;

namespace Packages.Utils;

public static class Serializer
{
    public static string Serialize<T>(T input) where T : class
    {
        return JsonConvert.SerializeObject(input, Formatting.Indented);
    }

    public static T Deserialize<T>(string input) where T : class
    {
        return JsonConvert.DeserializeObject<T>(input);
    }
}