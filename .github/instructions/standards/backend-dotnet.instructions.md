---
applyTo: "**/*.cs,**/*.csproj"
description: "Compact AI-enforceable backend .NET rules. Full source: standards/backend-dotnet-standards.md."
---

# Backend .NET Rules (compact)

Full source of truth: `standards/backend-dotnet-standards.md`. Read that file only when an exact
rule or detail not listed here is required.

- Use PascalCase for namespaces, classes, interfaces, methods, properties, and public members;
  camelCase for locals and parameters; prefix interfaces with `I`.
- One primary class per file; one namespace per file; braces on new lines and always present.
- Prefer C# aliases (`int`, `string`) over CTS type names.
- Do not use exceptions for normal control flow; re-throw with `throw;`, never `throw ex;`.
- Catch specific exceptions; dispose resources deterministically (`using`/`IDisposable`).
- Keep methods short and single-purpose; avoid long parameter lists (use a request object).
- No hardcoded strings, numbers, paths, or config values.
- Return empty collections instead of `null`; prefer `Any()` over count checks.
- Use `StringBuilder` for repeated string concatenation.
- Keep database/persistence and external calls out of UI code; route through dedicated layers.
- Favor interfaces, abstractions, and SOLID-aligned design.
