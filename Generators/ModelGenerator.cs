using SqlModelGenerator.Metadata;
using SqlModelGenerator.Metadata.Models;

namespace SqlModelGenerator.Generators;

public static class ModelGenerator
{
    public static void ValidateOptions(CodeGeneratorOptions options)
    {
        if (options.ShouldBuildTypeScript && Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.OutputRoot))
            .Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.TypeScriptOutputFolder!)), StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("TypeScriptOutputFolder must be different from the C# output folder.");
    }

    // Generate and validate both sets before publishing either; filesystem copy failures can still interrupt publication.
    public static void Generate(DatabaseModel model, CodeGeneratorOptions options)
    {
        ValidateOptions(options);
        ModelPreparation.Prepare(model, options.DbContextName);
        using var csharp = new GeneratedOutput(options.OutputRoot, ".sqlmodel-files.json", ".cs");
        using var typescript = options.ShouldBuildTypeScript
            ? new GeneratedOutput(options.TypeScriptOutputFolder!, ".sqlmodel-typescript-files.json", ".ts") : null;
        var staged = options with { OutputRoot = csharp.Staging };
        var poco = new PocoGenerator(staged);
        poco.GenerateTables(model.Tables);
        poco.GenerateViews(model.Views);
        new ProcedureGenerator(staged).Generate(model.Procedures);
        new DbContextGenerator(staged).Generate(model);
        new ReportGenerator(staged).Generate(model);
        if (typescript != null) new TypeScriptGenerator().Generate(model, typescript.Staging);
        csharp.ValidatePublication();
        typescript?.ValidatePublication();
        csharp.Publish();
        typescript?.Publish();
    }
}
