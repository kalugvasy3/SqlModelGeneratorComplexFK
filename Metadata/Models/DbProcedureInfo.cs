namespace SqlModelGenerator.Metadata.Models;

public sealed class DbProcedureInfo
{
    public string SetName { get; set; } = string.Empty;
    public int ObjectId { get; set; }
    public string? GenerationError { get; set; }
    public string Schema { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public List<DbParameterInfo> Parameters { get; } = new();
    public List<DbResultColumnInfo> ResultColumns { get; } = new();
    public bool HasResultSet => ResultColumns.Count > 0;
    public string ResultClassName => MethodName + "Result";
}
