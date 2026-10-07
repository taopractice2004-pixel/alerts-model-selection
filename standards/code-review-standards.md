# Code Review Standards

## Purpose

The pre-PR review checklist enforced by `/code-review`, so that recurring L1/L2 review comments
are caught and fixed before a pull request is raised. The limits below are team-owned defaults:
adjust the numbers here, never inside the skill. Every rule has an id that findings reference.

This file is the single home of the code-quality rules that reviewers check (size, complexity,
parameters, duplication, null handling, smells, error handling, resources, security, test
quality). The other standards keep only rules specific to their area (naming and layout,
architecture, API contracts, database, UI) and do not repeat these.

## Severity

- **BLOCKER** — must be fixed before PR: security risk, swallowed error, data-loss risk, or a
  measurable limit above its hard threshold.
- **MAJOR** — should be fixed before PR: a measurable limit above its target, or a smell that
  reviewers routinely reject.
- **MINOR** — note for the developer: style or readability suggestion, or a pre-existing issue the
  change did not make worse. Never blocks; it is fixed only when the developer approves it
  (`/fix-bugs <WORK-ID> approve <ids>`).

## Review Scope

- Review only code the work changed: the files in `work.json` → `exact_source_files` and
  `exact_test_files`, narrowed to the changed lines when a diff is available.
- Method-level limits (CR-SIZE, CR-CPLX, CR-PARAM) apply to every method the change touched. If the
  method already broke the limit before the change and the change did not make it worse, report
  MINOR (`pre-existing`).
- Findings in test files are always MINOR: they are reported for the developer and never block;
  an approved one is fixed by the next `/unit-testing`.
- Do not report untouched code, formatting a formatter fixes automatically, business correctness
  (verified by `/unit-testing`), or architecture redesign (left to human L2 judgement).

## Measurable Limits

| Rule id | Measure | Target (above → MAJOR) | Hard limit (above → BLOCKER) |
|---|---|---|---|
| CR-SIZE-01 | Lines per method/function body (excluding blank and comment lines); for a React component, applies to each hook callback and handler inside it, not to the component as a whole | 30 | 50 |
| CR-SIZE-02 | Lines per class/module file | 300 | 500 |
| CR-SIZE-03 | Lines per React component, including JSX (excluding blank and comment lines) | 150 | 250 |
| CR-CPLX-01 | Cyclomatic complexity per method | 10 | 15 |
| CR-CPLX-03 | Nesting depth of control flow | 3 | 4 |
| CR-PARAM-01 | Parameters per method (constructors with dependency injection excluded) | 5 | 7 |
| CR-DUP-01 | Duplicated block (same logic repeated) | 5 lines | 10 lines |
| CR-COV-01 | Coverage of new/changed code, from the `/unit-testing` result | 90% | 80% |

When a repository-configured static analyzer reports a measure, its measured value is
authoritative, but the severity always comes from this table, even if the analyzer's own
threshold is set differently. When the analyzer reports only "limit exceeded" without the value,
or no analyzer is configured, measure manually and note `measured manually` on the finding.

## Analyzer Mapping

How warnings from commonly used analyzers map to rule ids. Only analyzers configured in the
repository are used (see `/code-review` → Analyzers); this table does not require any of them.

| Rule id | .NET (Roslyn / SonarAnalyzer.CSharp) | ESLint (core and plugins) |
|---|---|---|
| CR-SIZE-01 / CR-SIZE-03 | S138 | `max-lines-per-function` |
| CR-SIZE-02 | — | `max-lines` |
| CR-CPLX-01 | CA1502, S1541 | `complexity` |
| CR-CPLX-03 | S134 | `max-depth` |
| CR-PARAM-01 | S107 | `max-params` |
| CR-REACT-01 | — | `react-hooks/exhaustive-deps` |
| CR-REACT-02 | — | `react-hooks/rules-of-hooks` |
| CR-REACT-03 | — | `react/jsx-key`, `react/no-array-index-key` |
| CR-REACT-07 | — | `@typescript-eslint/no-explicit-any` |
| CR-SEC-04 | — | `react/no-danger` |

## Rules

### Complexity and size (CR-CPLX, CR-SIZE)
- Prefer guard clauses / early returns over nested `if` blocks.
- Replace long `if`/`else if` or `switch` chains on a type or code with a lookup table or
  polymorphism when the chain exceeds the complexity target.
- Split long methods by responsibility into well-named private methods.

### Parameters (CR-PARAM)
- Above the limit, group related parameters into a request/options object.
- Boolean flag parameters that switch behavior (`DoWork(true)`) → MAJOR; prefer two intention-named
  methods or an enum.

### Redundant and duplicated code (CR-DUP)
- "Minimal code" means no unnecessary code, not the fewest characters. Readability wins over
  cleverness.
- No copy-pasted logic: reuse an existing helper/utility in the repository before writing a new one.
- No redundant code: unnecessary `else` after `return`, conditions that are always true/false,
  re-checking a value already validated, unused variables, parameters, or private members.

### Null and empty handling (CR-NULL)
- Check nulls and bounds (indexes, ranges, sizes) at trust boundaries (public methods, external
  input); do not repeat checks on values that cannot be null.
- Return empty collections, never `null`.
- .NET:
  - CR-NULL-01: use `is null` / `is not null` consistently instead of `== null` / `!= null`.
  - CR-NULL-02: use `string.IsNullOrWhiteSpace` for user/external input; `string.IsNullOrEmpty`
    only where whitespace is a valid value; never `str == ""` or `str.Length == 0` without a null
    check.
  - CR-NULL-03: guard public method arguments with `ArgumentNullException.ThrowIfNull(arg)` (or the
    project's existing guard helper).
  - CR-NULL-04: prefer `?.` and `??` / `??=` over nested null-check `if` blocks.
  - CR-NULL-05: respect nullable reference type annotations when the project enables them; no `!`
    (null-forgiving) without a reason.
- TypeScript / JavaScript: use optional chaining `?.` and nullish coalescing `??` (not `||` when
  `0`, `false`, or `""` are valid values); strict equality `===` / `!==` only.

### Code smells (CR-SMELL)
- CR-SMELL-01: dead code, commented-out code, unused imports/usings, suppressed warnings without a
  documented reason.
- CR-SMELL-02: magic numbers and hardcoded strings, paths, URLs, or config values.
- CR-SMELL-03: debug leftovers (`Console.WriteLine`, `console.log`, `Debug.Print`, temporary flags).
- CR-SMELL-04: `TODO` / `FIXME` without a work item reference.
- CR-SMELL-05: class with more than one clear responsibility (god class) introduced or grown by the
  change.
- CR-SMELL-06: misleading or unclear names; abbreviations; methods not named with a verb.

### Error handling (CR-ERR)
- CR-ERR-01 (BLOCKER): swallowed exception (empty `catch`, or catch that only logs and continues
  where failure must propagate).
- CR-ERR-02: catching the base exception type where a specific type is expected.
- CR-ERR-03 (.NET): `throw ex;` instead of `throw;`.
- CR-ERR-04: exceptions used for normal control flow.
- CR-ERR-05: error messages or logs that leak internals or sensitive data.
- CR-ERR-06: failure paths (invalid input, missing data, failed external calls) not handled
  explicitly.

### Resources, async, performance (CR-PERF)
- CR-PERF-01: disposable resources not released (`using` / `try-finally` / framework equivalent).
- CR-PERF-02 (.NET): `async void` outside event handlers; blocking on async code (`.Result`,
  `.Wait()`, `.GetAwaiter().GetResult()`).
- CR-PERF-03: database or remote calls inside loops (N+1); loading full data sets to filter in
  memory.
- CR-PERF-04: string concatenation in loops (`StringBuilder` / join); `Count() > 0` instead of
  `Any()`.

### Security (CR-SEC) — always BLOCKER
- CR-SEC-01: SQL or command built by string concatenation with input.
- CR-SEC-02: secrets, tokens, or credentials in code, config committed to source, logs, URLs, or
  client-side storage.
- CR-SEC-03: missing input validation at a trust boundary introduced by the change.
- CR-SEC-04 (React): `dangerouslySetInnerHTML`, or building HTML or URLs (`href`, `src`) from user
  or API data, without sanitizing it.
- CR-SEC-05: cookies carrying authentication or session data without `Secure`, `HttpOnly`, and
  `SameSite`.

### React (CR-REACT)
Applies to `.tsx` / `.jsx` files and to `.ts` / `.js` files that define components or hooks.
Use together with the `frontend-react` standard, which covers component structure,
data layer, styling, and localization.
- CR-REACT-01 (MAJOR): incomplete dependency array in `useEffect` / `useMemo` / `useCallback`, or
  the exhaustive-deps lint rule disabled without a comment explaining why.
- CR-REACT-02 (BLOCKER): hook called conditionally, inside a loop, or outside a component or
  custom hook (breaks the rules of hooks).
- CR-REACT-03 (MAJOR): list items rendered without a stable, unique `key`; array index used as
  the key for a list that can reorder, insert, or delete.
- CR-REACT-04 (MAJOR): effect that starts a subscription, timer, event listener, or request
  without cleanup (unsubscribe, clear, remove, or abort) in the returned function.
- CR-REACT-05 (MAJOR): state mutated directly (`state.items.push(...)`); state update that
  depends on the previous value without the functional form (`setCount(c => c + 1)`).
- CR-REACT-06 (MINOR): state that copies or derives from props or other state and could be
  computed during render instead.
- CR-REACT-07 (MAJOR, TypeScript): `any` in props, state, hook return types, or API response
  types without a comment explaining why.
- CR-REACT-08 (MAJOR): a prop passed through two or more components that do not use it (prop
  drilling); use composition, context, or the project's state layer, following existing patterns.
- CR-REACT-09 (MINOR): new inline object, array, or function props passed to a memoized child
  (`React.memo`, `useMemo` / `useCallback` consumers), defeating the memoization.
- CR-REACT-10 (MAJOR): data fetching or API calls inside a presentational component instead of
  the approved data layer (per `frontend-react`).

### Scope and architecture (CR-SCOPE)
- CR-SCOPE-01: changes in files outside `work.json` → `exact_source_files` / `exact_test_files`
  without a recorded reason.
- CR-SCOPE-02: unrelated refactoring, renames, or formatting churn in touched files.
- CR-SCOPE-03: layering violation (data access from UI/controller, business logic in the delivery
  layer) per the architecture standards that apply to the file (`backend-dotnet`,
  `service-architecture`, `frontend-react`).

### Test quality (CR-TEST) — always MINOR
- CR-TEST-01: test without a meaningful assertion.
- CR-TEST-02: test not following Arrange-Act-Assert or testing several behaviors at once.
- CR-TEST-03: unclear test names (name should state scenario and expected result).
- CR-TEST-04: non-deterministic test (time, randomness, ordering, real network/file system).
- CR-TEST-05 (React): component test that checks internals (state, private functions, CSS class
  names) or relies only on snapshots, instead of user-visible behavior (query by role, label, or
  text).

### Readiness for PR (CR-PR)
- CR-PR-01: generated files or build output in the change.
- CR-PR-02: public API added or changed without the documentation comments the project uses.
