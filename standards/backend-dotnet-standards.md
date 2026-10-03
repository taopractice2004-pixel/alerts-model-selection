# Backend .NET Standards

## Naming Conventions

- Use PascalCase for namespaces, classes, interfaces, methods, properties, enums, and public members.
- Use camelCase for local variables and parameters.
- Prefix interfaces with `I`.
- Use meaningful verb-based names for methods.
- Avoid Hungarian notation and avoid misleading abbreviations.

## File And Layout Rules

- Keep one namespace per file and one primary class per file.
- Place braces on new lines and always include braces for conditionals and loops.
- Keep indentation consistent with the team editor settings.
- Prefer `//` or XML documentation comments over block comments.
- Group members in a predictable order such as fields, properties, constructors, handlers, private methods, and public methods.

## Language Usage

- Prefer C# aliases like `int` and `string` over CTS type names.
- Do not use exceptions for normal control flow.
- Re-throw with `throw;`, not `throw ex;`.
- Avoid embedded assignments and overly complex ternary expressions.
- Prefer generics and interfaces over concrete implementations where possible.
- Avoid methods with too many parameters; introduce a request object when needed.

## Good Practices

- Keep methods short and single-purpose.
- Keep classes cohesive; split overly large files when responsibilities grow.
- Avoid hardcoded strings, numbers, file paths, and configuration values.
- Return empty collections instead of `null`.
- Prefer `Any()` over count checks when testing whether a collection has items.
- Use `StringBuilder` for repeated string manipulation.
- Catch specific exceptions and dispose resources deterministically.

## Architecture Guidance

- Use layered or multi-tier separation between UI, business, and data access concerns.
- Keep database access out of UI code.
- Route persistence and external system calls through dedicated layers.
- Favor interfaces, abstractions, and SOLID-aligned design.
- Centralize reusable logic in utilities or shared base abstractions only when it improves cohesion.