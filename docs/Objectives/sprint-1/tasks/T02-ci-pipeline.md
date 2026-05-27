# T02 — CI Pipeline (Continuous Integration)

**Sprint:** 1 | **Phase:** Fase 2 | **Priority:** P0 (gates all merges)

## Context

The **Deployment Pipeline** starts with Continuous Integration: every Pull Request and Commit must trigger automated tests before code can be merged. This is the quality gate that prevents broken code from reaching the **Container Registry** or the Cloud.

## User Story

> As a **developer on the FCG team**,  
> I want tests to run automatically on every PR and commit,  
> so that broken code is caught before it can affect the Release.

## Gherkin Scenarios

```gherkin
Feature: CI Pipeline — Automated Quality Gate

  Background:
    Given the repository is hosted on a version control platform (GitHub/GitLab/Azure DevOps)
    And the CI Pipeline is configured via a pipeline file in the repository

  Scenario: CI runs on Pull Request
    Given a developer opens a Pull Request targeting "main"
    When the Pull Request is created or updated with a new commit
    Then the CI Pipeline triggers automatically
    And all unit tests are executed
    And all integration tests are executed
    And the PR shows a green check when all tests pass

  Scenario: CI blocks merge on test failure
    Given a developer opens a Pull Request with a failing test
    When the CI Pipeline runs
    Then the pipeline status is "failed"
    And the merge button is disabled (branch protection enforced)

  Scenario: CI runs on direct commit to non-main branch
    Given a developer pushes a commit to a feature branch
    When the push event fires
    Then the CI Pipeline triggers
    And test results are reported on the commit

  Scenario: CI produces a test report artifact
    Given the CI Pipeline ran successfully
    When the pipeline completes
    Then a test coverage report is available as a pipeline artifact
    And coverage is above 70%
```

## Acceptance Criteria

- [ ] CI pipeline file exists (`.github/workflows/ci.yml` or equivalent)
- [ ] Triggers: `pull_request` targeting `main`, `push` to any branch
- [ ] **Gate 1:** `npm audit --audit-level=high` — HIGH/CRITICAL blocks merge
- [ ] **Gate 2:** SAST — `npx eslint src/ --plugin security` (or Semgrep) runs and reports
- [ ] **Gate 3:** `npx tsc --noEmit` — TypeScript compiles with zero errors
- [ ] **Gate 4:** Steps include lint → audit → SAST → unit tests → integration tests
- [ ] Pipeline fails fast: any gate failure stops subsequent gates
- [ ] Test results published as pipeline artifact or check annotation
- [ ] Branch protection rule on `main` requires CI to pass before merge
- [ ] Pipeline runs in under 10 minutes

## Best Practices

- Cache dependency installation (`node_modules`, `~/.npm`, `pip cache`) using pipeline cache keys
- Run lint as a separate, parallel job from tests to fail faster
- Use matrix builds if the FCG Platform must support multiple runtimes
- Never store secrets in pipeline YAML — use platform secret store (GitHub Secrets, Azure KV)

## Technical Notes

```yaml
# Pattern: GitHub Actions CI
name: CI
on:
  pull_request:
    branches: [main]
  push:
    branches-ignore: [main]

jobs:
  security-audit:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with: { node-version: '20', cache: 'npm' }
      - run: npm ci
      - name: Dependency CVE scan
        run: npm audit --audit-level=high    # HIGH/CRITICAL blocks merge
      - name: SAST
        run: npx semgrep scan --config=auto src/ --error   # or eslint-plugin-security
      - name: TypeScript compile check
        run: npx tsc --noEmit

  lint:
    runs-on: ubuntu-latest
    needs: security-audit
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with: { node-version: '20', cache: 'npm' }
      - run: npm ci
      - run: npm run lint

  test:
    runs-on: ubuntu-latest
    needs: lint
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with: { node-version: '20', cache: 'npm' }
      - run: npm ci
      - run: npm test -- --coverage
      - uses: actions/upload-artifact@v4
        with:
          name: coverage-report
          path: coverage/
```

## Dependencies

- Repository must have branch protection enabled on `main`

## Definition of Done

- [ ] CI pipeline file committed and running
- [ ] Branch protection rule active
- [ ] Green CI badge visible on README
