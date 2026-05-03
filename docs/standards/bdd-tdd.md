# BDD And TDD Standards

## BDD

User-visible behavior starts with a scenario. Scenarios may live in product docs,
test names, Gherkin files, or test fixtures depending on the implementation
slice.

Good scenarios use domain language:

```gherkin
Scenario: Ingest starts when a manifest appears
  Given an ingest package directory exists
  And the package contains manifest.json
  When the watcher scans the ingest mount
  Then a package workflow is started
  And the package files are scanned for classification
```

## TDD

Production behavior changes require a failing test first.

Required cycle:

1. RED: write a focused failing test.
2. Verify RED: run it and confirm the expected failure.
3. GREEN: write the smallest implementation.
4. Verify GREEN: run the test and confirm it passes.
5. REFACTOR: clean up while tests remain green.

Agents must report RED/GREEN evidence when implementing features or bug fixes.

## Exceptions

TDD exceptions require explicit user approval or a documented reason in the task
result. Acceptable exceptions include documentation-only changes, generated
files, and tooling scripts where command-level validation is the primary proof.

## Completion Rule

A feature, behavior change, or bug fix is not complete until:

- acceptance behavior is described
- tests or validation prove the behavior
- impacted docs are updated
- validation evidence is reported
