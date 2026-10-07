---
applyTo: "**/*.cs,**/*.csproj"
description: "Compact AI-enforceable backend .NET rules. Full source: standards/backend-dotnet-standards.md."
---

# Backend .NET Rules (compact)

Full source of truth: `standards/backend-dotnet-standards.md`. Read that file only when an exact
rule or detail not listed here is required. Review limits and quality checks are in
`code-review.instructions.md`.

- Use PascalCase for namespaces, classes, interfaces, methods, properties, and public members;
  camelCase for locals and parameters; prefix interfaces with `I`; verb-based method names.
- One primary class per file; one namespace per file; braces on new lines and always present.
- Prefer C# aliases (`int`, `string`) over CTS type names; avoid embedded assignments and
  complex ternaries.
- Keep classes cohesive; prefer generics and interfaces over concrete implementations.
- Keep database/persistence and external calls out of UI code; route through dedicated layers.
- Favor interfaces, abstractions, and SOLID-aligned design.
