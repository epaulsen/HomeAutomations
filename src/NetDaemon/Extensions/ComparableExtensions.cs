using HomeAutomations.Models;

namespace HomeAutomations.Extensions;

public static class ComparableExtensions
{
    public static bool IsIdenticalTo(this List<ClientDevice> list, List<ClientDevice> other)
    {
        try
        {
            if (list.Count != other.Count) return false;

            var set1 = new HashSet<ClientDevice>(list.OrderBy(d => d.Id));
            var set2 = new HashSet<ClientDevice>(other.OrderBy(d => d.Id));

            return set1.SetEquals(set2);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }


    }
}