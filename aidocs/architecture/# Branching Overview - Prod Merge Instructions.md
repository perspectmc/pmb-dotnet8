> 📘 For strategic overview and remote architecture context, see: `# GitHub Synchronization and Branching S.md`

# Branching Overview

## Branches

- `main`: Active development branch. Includes AI documentation, scripts, upgrade planning, and modernization work.
- `prod/main`: This is the version of the code that reflects what’s currently running in production. It’s considered the official reference for what our clients are actually using. Note: If files or folders appear to be missing, they were likely removed intentionally by Ben and may be re-uploaded later. Only trust `prod/main` as complete when confirmed.
- `dev`: General development branch used for testing features before merging into `main`.
- `feature/excel-import`: Dedicated branch for developing the Excel-to-claims automation feature.
- `cleanup/code-modernization`: Branch focused on code cleanup and applying modernization best practices.
- `cleanup/deprecated-removal`: Used for identifying and safely removing obsolete files.
- `migration/net8`: Branch for tracking the .NET 8 upgrade and related changes.

## Merge Policy

- Weekly merges **from `prod/main` → `main`** to track changes in production.
- Use `--no-commit` to stage changes for review.
- AI folders (`aidocs/`, `infra/`, etc.) are excluded from overwrite using `git checkout --`.

## Diff Check

To compare the branches before merging:
```bash
git fetch prod
git diff --name-status main..prod/main | grep '^M\|^A\|^D'
```

## To sync:

```bash
# Step 1 – Fetch the latest changes from production
git checkout main
git fetch prod

# Step 2 – View only the changed files (not the entire list)
git diff --name-status main..prod/main | grep '^[MAD]'

# Step 3 – Create or reset the sync branch
git branch -D sync/prod-to-main  
git checkout -b sync/prod-to-main
```

# Step 4 – Stash any uncommitted local changes to avoid merge conflicts
git stash push -m "Temp stash before prod merge"

# Step 5 – Start the merge process (stages changes but doesn’t commit yet)
git merge --no-commit --allow-unrelated-histories prod/main

# Step 5.1 – If a merge conflict occurs with .gitignore, resolve by taking Ben’s version
git checkout prod/main -- .gitignore
git add .gitignore

# Step 6 – Restore development-only folders (e.g., AI docs). Only run for folders that exist in Git.
git checkout main -- aidocs
# If infra is tracked by Git, uncomment the next line:
# git checkout main -- infra

# Step 7 – Finalize the merge
git commit -m "Merge prod/main into sync/prod-to-main (AI docs and infra preserved)"

# Step 8 – Push the merged branch
git push origin sync/prod-to-main

# Step 9 – Optionally re-apply your stashed changes
git stash pop

# Step 10 – Complete the merge via GitHub pull request
Because the `main` branch is protected, direct pushes or merges into `main` from the terminal are blocked to prevent accidental or unauthorized changes.

To finalize the sync:
1. Visit the GitHub URL provided after pushing (or go to https://github.com/perspectmc/pmb-dotnet8/pulls).
2. Open a pull request from `sync/prod-to-main` into `main`.
3. Review the changes if needed, then click **Merge Pull Request**.

### PR Template (Use for Weekly prod/main → main merge)

**Purpose:**  
Sync latest production changes into main branch while preserving AI documentation and development tooling.

**Preserved Folders:**  
- `aidocs/`
- `infra/` (if applicable)

**Merge Method:**  
Manual merge via sync/prod-to-main using `--no-commit`, `.gitignore` conflict resolved, and AI folders restored.

**Notes:**  
No business logic modified. This is part of the regular weekly production mirror sync.