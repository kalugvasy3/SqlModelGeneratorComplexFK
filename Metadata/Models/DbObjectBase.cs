namespace SqlModelGenerator.Metadata.Models;

public abstract class DbObjectBase
{
    public string SetName { get; set; } = string.Empty;
    public int ObjectId { get; set; }
    public string Schema { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public List<DbColumnInfo> Columns { get; } = new();
}
