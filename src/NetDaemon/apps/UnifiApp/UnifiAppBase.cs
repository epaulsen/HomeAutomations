using HomeAutomations.Extensions;
using HomeAutomations.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace HomeAutomations.apps.UnifiApp;

public class UnifiAppBase
{
    protected UnifiYamlConfig? _config = null;
    protected UnifiYamlConfig? LoadConfig()
    {
        var file = Path.Combine(ConfigFolder.Path, "unifi.yaml");
        if (!File.Exists(file))
        {
            return null;
        }

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();
        return deserializer.Deserialize<UnifiYamlConfig>(File.ReadAllText(file));
    }
}