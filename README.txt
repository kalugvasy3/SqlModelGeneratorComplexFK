SqlModelGenerator (.NET 8 / SQL Server)

Updated documentation: README.md (Russian).

Generates POCO entities, EF Core DbContext, foreign-key navigation properties,
and wrappers for the first stored-procedure result set.
Reads metadata only; does not execute application procedures or modify the database.

Run: dotnet run -c Release
Optional environment variables:
  SQLMODELGENERATOR_CONNECTION_STRING
  SQLMODELGENERATOR_OUTPUT

Choose a fresh output directory when upgrading from the original generator.
Review Report.txt for unsupported relationships/procedures and generated names.
This is an existing-database model, not a complete schema/migration backup.

Local regression checks (no database connection):
  .\Tests\Run-Tests.ps1
