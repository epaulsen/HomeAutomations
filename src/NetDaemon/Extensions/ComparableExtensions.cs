using HomeAutomations.Models;

namespace HomeAutomations.Extensions;

public static class ComparableExtensions
{
    public static bool IsIdenticalTo(this List<ClientDevice> list, List<ClientDevice> other)
    {
        if (list.Count != other.Count) return false;

        var set1 = new HashSet<ClientDevice>(list);
        var set2 = new HashSet<ClientDevice>(other);

        return set1.SetEquals(set2);
    }
}