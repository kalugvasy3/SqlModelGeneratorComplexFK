namespace SqlModelGenerator.Generators;

public sealed record CodeGeneratorOptions(
    string RootNamespace,
    string OutputRoot,
    string DbContextName);
