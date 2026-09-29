namespace SqlModelGenerator.Metadata.Models;

public sealed class DbParameterInfo
{
    public string PropertyName { get; set; } = string.Empty;
    public int Ordinal { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ParameterName => Name.StartsWith("@") ? Name : "@" + Name;
    public string VariableName { get; set; } = string.Empty;
    public string SqlTypeName { get; set; } = string.Empty;
    public string ClrTypeName { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsOutput { get; set; }
    public bool HasDefaultValue { get; set; }
    public int? MaxLength { get; set; }
    public byte? Precision { get; set; }
    public byte? Scale { get; set; }
}
