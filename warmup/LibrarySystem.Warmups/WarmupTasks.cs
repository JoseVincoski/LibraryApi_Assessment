using System.Text;

namespace LibrarySystem.Warmups;

public static class WarmupTasks
{
    public static bool IsPowerOfTwo(int n)
    {
        return n > 0 && (n & (n - 1)) == 0;
    }

    public static string? ReverseTitle(string? title)
    {
        if (string.IsNullOrEmpty(title)) return title;

        char[] chars = title.ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }

    public static string GenerateReplicas(string? title, int times)
    {
        if (string.IsNullOrEmpty(title) || times <= 0) return string.Empty;

        var sb = new StringBuilder(title.Length * times);
        for (int i = 0; i < times; i++)
        {
            sb.Append(title);
        }
        return sb.ToString();
    }

    public static IEnumerable<int> GetOddBookIds(int maxLimit)
    {
        for (int i = 1; i < maxLimit; i += 2)
        {
            yield return i;
        }
    }
}