# GitHub Synchronization and Branching Strategy

## Overview

This document outlines the GitHub repository structure, synchronization challenges, and the proposed branching framework to support the safe modernization of the Perspect Medical Billing (PMB) system while production updates are still occurring.

---

## Repositories in Use

| Repository         | Purpose                                 | Owner       |
|--------------------|-----------------------------------------|-------------|
| `pmb-dotnet8`       | Development repo for modernization      | Colin       |
| `pmb-prod`          | Production repo to mirror legacy VPS    | Ben         |

---

## The Synchronization Challenge

During modernization the system in `pmb-dotnet8`, Ben continues to apply live updates directly in production. This introduces risks:

- **Code Drift**: Ben’s production changes might not be reflected in Colin’s dev repo.
- **Merge Conflicts**: Future merges between production and dev branches may become difficult if histories diverge.
- **Deployment Gaps**: Without a structured flow, there's risk of losing important bug fixes or introducing regressions in modernization.

---

## Sync Solution and Best Practices

1. **Add production repo as a remote**:
   ```bash
   git remote add prod https://github.com/perspectmc/pmb-prod.git
   ```

2. **Fetch updates regularly from prod**:
   ```bash
   git fetch prod
   git diff main..prod/main
   ```

3. **Only merge when ready**, and with caution:
   ```bash
   git checkout main
   git merge prod/main --allow-unrelated-histories
   ```

### Conflict Mitigation:
- Use `git diff` and `git diff --stat` before merging.
- Automate pre-merge checks using a shell or PowerShell script.
- Resolve conflicts manually in VS Code if necessary.

### Modernization Impact:
After merging `prod/main` into your `main`, you'll often need to:
```bash
git checkout dev
git merge main
```
This keeps your modernized `dev` branch up to date with the latest bug fixes or live edits.

---

## Branching Framework

| Branch               | Purpose                                               |
|----------------------|-------------------------------------------------------|
| `main`               | Mirrors production (`prod/main`). No dev allowed.     |
| `dev`                | Integration branch for all modernization work.        |
| `feature/...`        | Isolated new features (e.g. Excel import).            |
| `cleanup/...`        | Code cleanup, naming fixes, structure refactors.      |
| `migration/net8`     | All work related to upgrading to .NET 8.              |
| `hotfix/...`         | Emergency fixes; rarely used.                         |
| `legacy-snapshot`    | Tag representing Colin’s original code upload.        |

---

## Dev-to-Prod Promotion Strategy

1. All work merges into `dev`.
2. Once stable, `dev` is pushed to a new production repo or VPS.
3. This new environment replaces the legacy VPS, allowing for parallel running or clean cutover.

---

## Additional Recommendations

- **Ben must upload full source code** (not publish output) to `pmb-prod`.
- **Conflicts will occur** — establish a regular merge inspection routine.
- **Tag milestones** like `legacy-snapshot`, `v1.0-dev-complete`, etc.
- **Document testing and deployment** in future `aidocs/DevOps/` folder.

---

## Action Items

- [ ] Confirm Ben’s full source code upload.
- [ ] Tag `legacy-snapshot` in `pmb-dotnet8`.
- [ ] Create automation for safe merge inspection.
- [ ] Formalize test plan for the `dev` branch.
