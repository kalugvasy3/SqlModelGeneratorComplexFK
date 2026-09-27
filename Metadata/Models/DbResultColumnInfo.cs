namespace SqlModelGenerator.Metadata.Models;

public sealed class DbResultColumnInfo
{
    public int Ordinal { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string SqlTypeName { get; set; } = string.Empty;
    public string ClrTypeName { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
}
