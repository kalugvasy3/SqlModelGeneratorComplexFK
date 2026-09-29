# Validation: 2026-09-29 — procedure parameter and OUTPUT DTOs

- Release generator build: 0 errors, 0 warnings.
- Generated C# compiles; all previous EF mappings and legacy procedure calls pass (19 synthetic tables, 7 generated foreign keys).
- New DTO overloads passed typed OUTPUT, DBNull, input/output initial values, result materialization and cancellation checks.
- Legacy null/default calls and implicit numeric conversions compile without ambiguous overloads.
- Missing input, explicit null, explicit zero and an omitted middle argument generate distinct expected SQL/parameters.
- Missing OUTPUT initialization and null DTOs are rejected before command execution.
- System.Text.Json web defaults preserve flat camelCase JSON, presence, null and zero through round trips, including global ignore-null settings.
- ASP.NET Core MVC object validation accepts the DTOs without evaluating a throwing wrapper getter.
- Incomplete parameter metadata produces no partial DTO; unknown result metadata still permits known parameter contracts while execution remains blocked.
- TypeScript 5.9.3 strictly checked 38 files, including input/output types, optional input fields and required INPUT/OUTPUT fields. Runtime JSON tests passed.
- SQL execution was intercepted locally. No SQL Server database was contacted; actual default evaluation by a live server remains untested.

Command: `Tests/Run-Tests.ps1 -NoRestore -TypeScriptCompiler <existing TypeScript 5.9.3 lib/typescript.js>`.

---

# Validation: 2026-09-29 — optional TypeScript generation

- Release build: 0 errors, 0 warnings.
- Existing C# generation and EF model/procedure checks passed: 18 synthetic tables and 7 generated foreign keys.
- Optional flag/folder combinations passed; disabling TypeScript preserves existing output.
- TypeScript 5.9.3 checked 27 source files with strict nullability, exact optional properties, isolated modules and verbatim module syntax.
- JSON contracts checked for camelCase/acronyms, nullable columns, dates, binary values, numeric types, Unicode names, a property named constructor, procedure results, and cyclic navigation types.
- Runtime checks confirmed that classes do not create fake default values or recursively construct relationships.
- Publication checks covered regeneration, stale-file cleanup, custom-file preservation, and preventing C# publication on a TypeScript ownership conflict.
- No SQL Server connection was opened; SQL commands in the existing C# checks remain intercepted locally.

Command: `Tests/Run-Tests.ps1 -NoRestore -TypeScriptCompiler <existing TypeScript 5.9.3 lib/typescript.js>`.
The standalone test compiler can instead be installed with `npm --prefix Tests/TypeScriptSmoke install`.

---

# Проверка 2026-09-26

Команда: `Tests/Run-Tests.ps1 -NoRestore` после восстановления NuGet-зависимостей.

Среда: Windows, .NET SDK 10.0.401; целевая платформа net8.0.
Сгенерированный код: EF Core SqlServer 8.0.27, Microsoft.Data.SqlClient 5.2.2.

- Release-сборка генератора: **0 ошибок, 0 предупреждений**.
- Синтетическая схема: 17 таблиц, 9 FK; 7 связей сгенерированы и проверены EF, 2 ожидаемо пропущены (disabled и keyless).
- Скомпилированы варианты: основная схема, пустая схема, только представления, только процедуры, повторная генерация после переименования.
- Проверены составной PK/FK и порядок колонок, альтернативный ключ, one-to-one, self-reference, optional/required и действия DELETE.
- Проверены точность decimal, Unicode-длина, identity, неidentity PK, default/computed SQL и rowversion.
- Проверены чтение столбцов DMV по именам, конфликтующие/Unicode-имена, экранирование SQL-идентификаторов и строк C#.
- Вызовы обёрток процедур проверены через EF-interceptors: сформированный SQL, SqlParameter.Size, OUTPUT/DBNull, CancellationToken и материализация результата.
- Проверены диагностические заглушки процедур с неизвестной/неоднозначной схемой.
- Проверена повторная генерация: устаревший файл удаляется по манифесту, посторонний файл сохраняется.

**Подключений к SQL Server не было.** Открытие соединений и исполнение команд в тестах подавляются interceptors. Системные SQL-запросы чтения каталога пока не проверены на живом SQL Server; права и особенности рабочей схемы требуют отдельного запуска генератора пользователем.

Исходники до изменений сохранены в резервном ZIP в рабочем каталоге Codex.
