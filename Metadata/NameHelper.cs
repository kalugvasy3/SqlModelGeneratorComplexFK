using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SqlModelGenerator.Metadata;

public static class NameHelper
{
    private static readonly Regex Separators = new(@"[^\p{L}\p{Nd}]+", RegexOptions.Compiled);
    private static readonly HashSet<string> Keywords = new((
        "abstract as base bool break byte case catch char checked class const continue decimal default delegate " +
        "do double else enum event explicit extern false finally fixed float for foreach goto if implicit in int " +
        "interface internal is lock long namespace new null object operator out override params private protected " +
        "public readonly ref return sbyte sealed short sizeof stackalloc static string struct switch this throw " +
        "true try typeof uint ulong unchecked unsafe ushort using virtual void volatile while record required " +
        "file scoped init value var dynamic async await yield partial global").Split(' '), StringComparer.Ordinal);

    public static string ToPascalCase(string value)
    {
        var result = new StringBuilder();
        foreach (var token in Separators.Split(value).Where(x => x.Length > 0))
        {
            if (result.Length == 0 && char.IsDigit(token[0])) result.Append('_');
            result.Append(char.ToUpperInvariant(token[0])).Append(token.AsSpan(1));
        }
        return result.Length == 0 ? "Unnamed" : result.ToString();
    }

    public static string ToCamelCase(string value)
    {
        var name = ToPascalCase(value);
        return char.ToLowerInvariant(name[0]) + name[1..];
    }

    public static string EscapeIdentifier(string value) => Keywords.Contains(value) ? "@" + value : value;
    public static string Literal(string value) => JsonSerializer.Serialize(value);
    public static string SqlIdentifier(string value) => "[" + value.Replace("]", "]]") + "]";
    public static string Unique(string desired, HashSet<string> used)
    {
        var name = desired.TrimStart('@');
        var candidate = name;
        for (var suffix = 2; !used.Add(candidate); suffix++) candidate = name + suffix;
        return EscapeIdentifier(candidate);
    }
}
