# Effort Dial

## Default Mode
`low`

## Modes
- `low`: cache first, deterministic pre-pass first, skip `requirement-analysis.md` unless blocked
- `standard`: cache first, compact artifacts, create `requirement-analysis.md` only for real ambiguity
- `deep`: broader reasoning for risky or complex stories

## Selection Rule
Use `low` by default. Escalate only when the story is ambiguous, risky, or repeatedly fails
focused validation.
