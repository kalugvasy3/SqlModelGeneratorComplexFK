SqlModelGenerator (.NET 8 / SQL Server)

Full documentation: README.md (English first, Russian translation below).

Generates POCO entities, EF Core DbContext, foreign-key navigation properties,
parameter/OUTPUT DTOs and wrappers for the first stored-procedure result set.
Optionally generates TypeScript classes for Angular/frontend JSON contracts.
Reads metadata only; does not execute application procedures or modify the database.

Procedure parameter objects preserve omitted inputs vs explicit NULL; see README.md.
Existing scalar method signatures remain available.

Run: dotnet run -c Release
Optional environment variables:
  SQLMODELGENERATOR_CONNECTION_STRING
  SQLMODELGENERATOR_OUTPUT
  SQLMODELGENERATOR_BUILD_TYPESCRIPT_CLASSES (True/False; default False)
  SQLMODELGENERATOR_TYPESCRIPT_OUTPUT (separate output folder)

TypeScript is generated only when the flag is True and its folder is nonempty.
The same two defaults can be edited near the top of Program.cs.

Choose a fresh output directory when upgrading from the original generator.
Review Report.txt for unsupported relationships/procedures and generated names.
This is an existing-database model, not a complete schema/migration backup.

Local regression checks (no database connection; Node.js required for TS tests):
  npm --prefix .\Tests\TypeScriptSmoke install
  .\Tests\Run-Tests.ps1
