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
            sb.AppendLine($"- [{proc.Schema}].[{proc.Name}] => {proc.MethodName}Async; parameters={proc.Parameters.Count}; first-result-columns={proc.ResultColumns.Count}" +
                (proc.GenerationError == null ? "" : "; NOT EXECUTABLE: " + proc.GenerationError));
        sb.AppendLine();
        sb.AppendLine("Warnings:");
        foreach (var warning in model.Warnings) sb.AppendLine("- " + warning);
        if (model.Warnings.Count == 0) sb.AppendLine("None.");
        File.WriteAllText(Path.Combine(_options.OutputRoot, "Report.txt"), sb.ToString());
    }
}
