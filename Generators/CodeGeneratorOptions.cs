namespace SqlModelGenerator.Generators;

public sealed record CodeGeneratorOptions(
    string RootNamespace,
    string OutputRoot,
    string DbContextName,
    bool BuildTypeScriptClasses = false,
    string? TypeScriptOutputFolder = null)
{
    // Both settings are opt-in. A missing/blank folder never falls back to a C# output directory.
    public bool ShouldBuildTypeScript => BuildTypeScriptClasses && !string.IsNullOrWhiteSpace(TypeScriptOutputFolder);
}
