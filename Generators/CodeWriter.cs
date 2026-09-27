using System.Text;

namespace SqlModelGenerator.Generators;

public sealed class CodeWriter
{
    private readonly StringBuilder _sb = new();
    private int _indent;

    public void WriteLine(string text = "")
    {
        if (text.Length == 0)
        {
            _sb.AppendLine();
            return;
        }

        _sb.Append(new string(' ', _indent * 4));
        _sb.AppendLine(text);
    }

    public void OpenBlock()
    {
        WriteLine("{");
        _indent++;
    }

    public void CloseBlock(string suffix = "")
    {
        _indent--;
        WriteLine("}" + suffix);
    }

    public override string ToString() => _sb.ToString();
}
