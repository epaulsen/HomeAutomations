using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace HomeAutomations.Apps.CostSensor;

/// <summary>
/// Type converter for deserializing CronSchedule enum from YAML
/// </summary>
public class CronScheduleTypeConverter : IYamlTypeConverter
{
    public bool Accepts(Type type)
    {
        return type == typeof(CronSchedule);
    }

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        var scalar = parser.Consume<Scalar>();
        var value = scalar.Value;

        if (string.IsNullOrEmpty(value) || value.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            return CronSchedule.None;
        }

        return value.ToLowerInvariant() switch
        {
            "daily" => CronSchedule.Daily,
            "monthly" => CronSchedule.Monthly,
            "yearly" => CronSchedule.Yearly,
            _ => CronSchedule.None
        };
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        var cronSchedule = (CronSchedule)(value ?? CronSchedule.None);
        var yamlValue = cronSchedule switch
        {
            CronSchedule.Daily => "daily",
            CronSchedule.Monthly => "monthly",
            CronSchedule.Yearly => "yearly",
            _ => "null"
        };

        emitter.Emit(new Scalar(yamlValue));
    }
}
