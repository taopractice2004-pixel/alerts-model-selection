---
applyTo: "AlertService.API.Tests/**/*.cs,AlertService.Data.SQL.Tests/**/*.cs"
description: "Unit-test rules, including protection against gaming tests. Loaded only when working on test files."
---

# Test Instructions

<!-- /setup-repo-context replaces applyTo with this repository's real test paths. Project facts
     (framework, libraries, location, naming, example test, coverage target) live in section 7 of
     .sdlc/context/project-profile.md; project test rules live in the Testing section of
     .sdlc/context/standards-summary.md. This file holds only the guardrails below. -->

Use the test facts in section 7 of `.sdlc/context/project-profile.md` and the `Testing` section of
`.sdlc/context/standards-summary.md`.

## Writing Tests
- Before writing tests, read one existing nearby test file and copy its structure, naming, fixtures, and mocking style.
- Use the existing test framework and libraries. Do not add new ones without approval.
- Each new test proves a specific acceptance criterion (AC) or behavior. State which one in the AC results table of the `log.md` entry.
- Cover success, failure, edge, and branch paths that matter. Do not write tests only to raise coverage.
- Mock external dependencies, never the class under test.
- Write unit tests, and UI/component tests only where the project already has that setup. Never write integration or end-to-end tests here. Keep mocked tests clearly separate from real integration tests, and never describe a mocked test as proving a real integration works.

## Production Code
Tests never change production code. If a test cannot be written without a production change (for
example a member must become visible to the test project), stop and ask the developer.

## Never Game Tests
Do not, under any circumstances:
- delete, comment out, or skip a failing test
- weaken an assertion or loosen a matcher to make a test pass
- change an expected value to whatever the code currently returns
- change expected behavior in a test unless an approved requirement changed it (record that in `log.md`)
- hardcode results or special-case test inputs in production code
- mock away the behavior the test is supposed to prove

If a correct test fails because the production code is wrong, leave the assertion as it is and
report the failure as a defect. Recommend `/fix-bugs`.
