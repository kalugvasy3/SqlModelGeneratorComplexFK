using System.Text;
using SqlModelGenerator.Metadata.Models;

namespace SqlModelGenerator.Generators;

public sealed class ReportGenerator
{
    private readonly CodeGeneratorOptions _options;
    public ReportGenerator(CodeGeneratorOptions options) => _options = options;
    public void Generate(DatabaseModel model)
    {
        var sb = new StringBuilder();
        sb.AppendLine("SQL Model Generator report");
        sb.AppendLine($"Tables: {model.Tables.Count}; views: {model.Views.Count}; procedures: {model.Procedures.Count}");
        sb.AppendLine($"Foreign keys: {model.ForeignKeys.Count}; generated: {model.ForeignKeys.Count(k => k.SkipReason == null)}");
        sb.AppendLine("This is a model of an existing database, not a complete schema/migration backup.");
        sb.AppendLine("Only the first procedure result set is described. Generated list methods buffer that result in memory.");
        sb.AppendLine("Procedure DTOs: omitted INPUT requests SQL DEFAULT; explicit null sends NULL. INPUT/OUTPUT must be explicitly initialized (null allowed).");
        sb.AppendLine("SQL Server validates defaults/required inputs; sys.parameters does not reliably expose T-SQL defaults. RETURN status codes are not captured.");
        sb.AppendLine();
        sb.AppendLine(_options.ShouldBuildTypeScript
            ? $"TypeScript: enabled; folder: {_options.TypeScriptOutputFolder}"
            : "TypeScript: disabled (requires True and an output folder).");
        if (_options.ShouldBuildTypeScript)
        {
            sb.AppendLine("TypeScript properties follow ASP.NET Core camelCase JSON naming. Dates/TimeSpan/GUID/binary values are strings; SQL NULL maps to null.");
            sb.AppendLine("TypeScript numbers follow default JSON serialization: bigint/decimal/money can lose precision in JavaScript. Use string-valued API DTOs for exact values.");
            sb.AppendLine("Navigation properties are optional; generated classes do not hydrate JSON or load related data.");
        }
        sb.AppendLine();
        sb.AppendLine("Names:");
        foreach (var obj in model.Tables.Cast<DbObjectBase>().Concat(model.Views))
            sb.AppendLine($"- [{obj.Schema}].[{obj.Name}] => {obj.ClassName}");
        sb.AppendLine();
        sb.AppendLine("Foreign keys:");
        foreach (var fk in model.ForeignKeys)
        {
            sb.AppendLine($"- {fk.Name}: [{fk.DependentTable.Schema}].[{fk.DependentTable.Name}] -> [{fk.PrincipalTable.Schema}].[{fk.PrincipalTable.Name}]");
            sb.AppendLine("  " + string.Join(", ", fk.Columns.Select(c => c.DependentColumn + " -> " + c.PrincipalColumn)));
            sb.AppendLine(fk.SkipReason != null ? "  SKIPPED: " + fk.SkipReason
                : $"  {(fk.IsUnique ? "one-to-one" : "many-to-one")}; {(fk.IsRequired ? "required" : "optional")}; DELETE {fk.DeleteAction}; UPDATE {fk.UpdateAction}; navigation {fk.DependentNavigation} / {fk.PrincipalNavigation}");
        }
        sb.AppendLine();
        sb.AppendLine("Procedures:");
        foreach (var proc in model.Procedures)
        {
            sb.AppendLine($"- [{proc.Schema}].[{proc.Name}] => {proc.MethodName}Async; parameters={proc.Parameters.Count}; first-result-columns={proc.ResultColumns.Count}" +
                (proc.GenerationError == null ? "" : "; NOT EXECUTABLE: " + proc.GenerationError));
            sb.AppendLine(proc.ParametersComplete
                ? $"  Parameters: {proc.ParametersClassName}; OUTPUT: {(proc.Parameters.Any(p => p.IsOutput) ? proc.OutputClassName : "none")}."
                : "  Parameter/OUTPUT DTOs skipped: incomplete parameter metadata.");
        }
        sb.AppendLine();
        sb.AppendLine("Warnings:");
        foreach (var warning in model.Warnings) sb.AppendLine("- " + warning);
        if (model.Warnings.Count == 0) sb.AppendLine("None.");
        File.WriteAllText(Path.Combine(_options.OutputRoot, "Report.txt"), sb.ToString());
    }
}
