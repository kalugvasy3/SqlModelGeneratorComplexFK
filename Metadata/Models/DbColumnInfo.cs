namespace SqlModelGenerator.Metadata.Models;

public sealed class DbColumnInfo
{
    public string? DefaultSql { get; set; }
    public string? ComputedSql { get; set; }
    public bool IsStored { get; set; }
    public int Ordinal { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string SqlTypeName { get; set; } = string.Empty;
    public string ClrTypeName { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsIdentity { get; set; }
    public bool IsComputed { get; set; }
    public int? MaxLength { get; set; }
    public byte? Precision { get; set; }
    public byte? Scale { get; set; }
}
