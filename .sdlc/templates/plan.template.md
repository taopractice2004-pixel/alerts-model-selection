# Story {{ID}} - {{title}}

<!-- One plan for the whole story lifecycle. Each stage executes ONLY its own sections:
       /implement-story -> "Implementation Plan" (production code only)
       /unit-testing    -> "Validation Plan", "Unit Test Plan", "AC-to-Validation Mapping"
     "Contracts" and "Behavior Rules" are the shared agreement: /implement-story builds to them and
     /unit-testing derives expected results from them.
     Every other section is read-only context. Exact commands live in work.json.
     Developer: review, then set Approval Status to APPROVED (or reply "approved" to /implement-story). -->

## Story Summary
{{1-3 lines: what this story delivers}} | Case: {{SIMPLE | AMBIGUOUS}}

## Acceptance Criteria
- AC1 - {{one testable line}}
- AC2 - {{one testable line}}

## In Scope
- {{item}}

## Out of Scope
- {{item or None}}

## Do Not Modify
- {{paths or components, including `contextPolicy` `doNotModify` paths near this story, or None}}

## Impacted Files

### Files to Modify
<!-- Existing files only. Greenfield with none: "None - greenfield repository". -->
| File | Change | ACs |
|---|---|---|
| {{existing production file path, or None}} | {{what changes}} | {{AC1, AC2}} |

### Files to Create
<!-- Status: TO_CREATE (path fixed by an approved structure or standard) or PROPOSED (suggested). -->
| File | Status | Purpose | ACs |
|---|---|---|---|
| {{production file path, or None}} | {{TO_CREATE / PROPOSED}} | {{purpose}} | {{AC3}} |

## Reuse / Existing Patterns
<!-- Greenfield with no prior implementation: "NOT_ESTABLISHED - no prior implementation exists";
     follow standards-summary.md and this plan instead. -->
| Change | Example file to copy | Reuse instead of creating |
|---|---|---|
| {{e.g. new endpoint}} | {{e.g. src/Api/OrderController.cs}} | {{existing class or helper, or None}} |

## Implementation Plan
<!-- Steps are executed by /implement-story only. Production code only: no build, tests, or
     validation. Contracts and Behavior Rules are also read by /unit-testing as expected results. -->

### Contracts
<!-- Exact shapes the code must have. Only what this story adds or changes; "None" if nothing. -->
- {{endpoint: METHOD /route; request {fields}; responses: 200 {fields}, 400/401/404 {error code or message}}}
- {{method signature: IService.Method(type param) -> ReturnType}}
- {{DTO / model fields added or changed; config keys; events; UI props, if any}}

### Behavior Rules
<!-- Business rules, validation, edge cases, error handling, and logging the code must implement.
     One line each, with the AC it serves. -->
- {{rule or edge case -> expected behavior}} ({{AC2}})
- {{error handling / logging expectation, e.g. use the existing error mapper; never log passwords}}

### Steps
<!-- One line per step: [ACs] file -> class or method: change; follow <example file>.
     Include wiring steps (dependency-injection registration, routes, config keys, feature flags). -->
1. [{{AC1}}] {{path}} -> {{Class.Method}}: {{smallest change}}; follow {{example file}}
2. [{{AC2}}] {{path}} -> {{Class.Method}}: {{smallest change}}
3. [wiring] {{path, e.g. composition root}}: {{registration, route, or config key}}

## Validation Plan
<!-- Executed by /unit-testing only. Exact commands and working directories are in work.json. -->
- Build / type check: {{affected project or module}}
- Lint / static analysis: {{required check, or NOT_CONFIGURED}}
- Regression: {{existing related tests to re-run}}
- Integration / functional: {{existing automated suite to run, manual check needed, or NOT_APPLICABLE}}
- Coverage: {{changed files to measure, or NOT_CONFIGURED}}

## Unit Test Plan
<!-- Executed by /unit-testing only. Actual unit tests only; other validation types are planned in
     the AC-to-Validation Mapping. -->
| Test file | New or existing | Behaviors and edge cases |
|---|---|---|
| {{test file path}} | {{NEW / EXISTING}} | {{behaviors, edge cases, regression scenarios}} |

## AC-to-Validation Mapping
<!-- Executed by /unit-testing only. Validation types:
       UNIT_TEST                - a test from the Unit Test Plan
       UI_COMPONENT_TEST        - a component test, only where the project already has that setup
       BUILD_CHECK              - proven by the build or type check in /unit-testing
       INTEGRATION_FUNCTIONAL   - an existing automated integration suite, or a manual check
     Expected results come from Contracts and Behavior Rules. -->
| AC | Validation type | Validation (test name and file, or check) | Expected result |
|---|---|---|---|
| AC1 | {{UNIT_TEST}} | {{test name in test file}} | {{e.g. 401 with error INVALID_CREDENTIALS}} |

## Dependencies / Risks
- Dependencies: {{internal or external dependency, or None}}
- Risks: {{risk, or None}}
- Stop points: {{changes that need developer confirmation before implementation, e.g. shared files, or None}}

## Open Questions
- Blocking: {{question or None}}
- Non-blocking: {{question or None}}
- Open decisions (greenfield architecture or technical; blocking until decided): {{decision or None}}

## Approval Status
Status: {{DRAFT | APPROVED | WAITING_FOR_HUMAN}}
Approved by: {{developer or PENDING}}
Date: {{date or PENDING}}
