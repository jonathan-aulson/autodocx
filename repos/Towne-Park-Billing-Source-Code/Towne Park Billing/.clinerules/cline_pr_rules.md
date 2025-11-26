# Cline PR Creation Rules - Towne Park Billing Repository

## Repository Scope
**This ruleset applies ONLY to the "Towne Park Billing" repository** in the towne-park Azure DevOps organization.

🔵 **CRITICAL: This is an Azure DevOps Repository** 
- **Platform**: Azure DevOps (NOT GitHub)
- **CLI Tool**: Use Azure CLI (`az`) commands
- **Organization**: towne-park
- **Project**: Towne Park Billing
- **Repository**: Towne Park Billing (ID: b4b93ec6-2f56-4306-94ee-a17b83b1e616)

## Git Flow Structure - Developer Workflow
Our developer git flow: `develop` ← `feature/` ← `task/`

🚫 **CRITICAL**: Never create PRs targeting `master` branch  
✅ **Maximum target**: `develop` branch is the highest level for developer PRs

✅ **Confirmed**: The `develop` branch exists and is actively used in this repository.

## Branch-Based PR Rules

### Task Branch PRs (`task/` → `feature/`)
**When source branch starts with `task/`:**

- **Target Branch**: Always target the parent feature branch (never `master`)
- **Reviewers**: 
  
- **Work Items**: Link the task story as related work item
- **Process**:
  1. Search Azure DevOps for the task branch
  2. Find the parent User Story
  3. Identify the associated feature branch
  4. Set feature branch as target

### Feature Branch PRs (`feature/` → `develop`)
**When source branch starts with `feature/`:**

- **Target Branch**: Always target `develop` branch (never `master`)
- **Reviewers**:
  
- **Work Items**: Link the User Story as related work item

### 🚫 Forbidden Operations
- **Never create PRs targeting `master` branch**
- **Never create PRs from `develop` to `master`** (this is handled by senior developers/release managers)

## Commit Message Standards
Follow [Conventional Commits 1.0.0](https://www.conventionalcommits.org/) specification:

### Format
```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

### Required Types
- `feat`: New feature (correlates with MINOR in SemVer)
- `fix`: Bug fix (correlates with PATCH in SemVer)
- `BREAKING CHANGE`: Breaking API change (correlates with MAJOR in SemVer)

### Additional Allowed Types
- `build`: Changes to build system or dependencies
- `chore`: Maintenance tasks
- `ci`: CI/CD changes
- `docs`: Documentation updates
- `style`: Code style changes (formatting, missing semicolons, etc.)
- `refactor`: Code changes that neither fix bugs nor add features
- `perf`: Performance improvements
- `test`: Adding or modifying tests

### Examples
```bash
# Feature commit
feat(api): add user authentication endpoint

# Bug fix
fix(parser): resolve array parsing issue with multiple spaces

# Breaking change
feat!: migrate to new authentication system

BREAKING CHANGE: old auth tokens are no longer supported

# With scope and work item reference
feat(billing): implement recurring payment processing

Refs: #12345
```

## PR Title Standards
- Use conventional commit format for PR titles
- Include work item reference when available
- Examples:
  - `feat(api): add user authentication - Task #12345`
  - `fix(billing): resolve payment processing error - Story #12340`

## PR Description Template
```markdown
## Summary
Brief description of changes made.

## Type of Change
- [ ] Bug fix (non-breaking change which fixes an issue)
- [ ] New feature (non-breaking change which adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] Documentation update
- [ ] Code refactoring
- [ ] Performance improvement
- [ ] Test addition/modification

## Related Work Items
- Task/Story: #[WorkItemId]
- Related: #[AdditionalWorkItemId] (if applicable)

## Testing
- [ ] Unit tests added/updated
- [ ] Integration tests added/updated
- [ ] Manual testing completed
- [ ] All tests passing

## Checklist
- [ ] Code follows project coding standards
- [ ] Self-review completed
- [ ] Documentation updated (if applicable)
- [ ] No new warnings introduced
- [ ] Related work items updated
```

## Automation Rules for Cline

### Repository Validation
```javascript
// Ensure this ruleset only applies to the Towne Park Billing repository
const VALID_REPOSITORY_ID = "b4b93ec6-2f56-4306-94ee-a17b83b1e616";
const VALID_REPOSITORY_NAME = "Towne Park Billing";
const FORBIDDEN_TARGET = "master";

if (currentRepository.id !== VALID_REPOSITORY_ID) {
  throw new Error("This ruleset only applies to the Towne Park Billing repository");
}

if (targetBranch === FORBIDDEN_TARGET) {
  throw new Error("🚫 FORBIDDEN: Never create PRs targeting master branch. Use develop as maximum target.");
}
```

### Branch Detection Logic
```javascript
// Pseudo-code for branch detection in Towne Park Billing repo
if (sourceBranch.startsWith('task/')) {
  // Task branch logic
  targetBranch = findParentFeatureBranch(sourceBranch);
  if (!targetBranch || targetBranch === 'master') {
    throw new Error("Invalid target: Task branches must target feature branches, never master");
  }
  
  workItem = findTaskWorkItem(sourceBranch);
} else if (sourceBranch.startsWith('feature/')) {
  // Feature branch logic
  targetBranch = 'develop'; // Fixed target, never master
  
  workItem = findUserStoryWorkItem(sourceBranch);
} else {
  throw new Error("Only task/ and feature/ branches are supported for PRs");
}
```

### Work Item Hierarchy Validation
Ensure the following Azure DevOps work item hierarchy is respected:
- **Epic** → **Feature** → **User Story** → **Task**
- Task branches should link to Task work items
- Feature branches should link to User Story work items
- Verify parent-child relationships exist in Azure DevOps before creating PRs

### Validation Rules
1. **Branch Naming**: Ensure branches follow `task/` or `feature/` conventions
2. **Commit Messages**: Validate against Conventional Commits specification
3. **Work Item Association**: Ensure proper work item linking
4. **Reviewer Assignment**: Verify correct reviewer assignment based on branch type
5. **Target Branch**: Confirm correct target branch selection

## Error Handling
- If parent feature branch cannot be found for task branch, prompt for manual selection
- If work items cannot be automatically detected, prompt for manual input
- Provide clear error messages with resolution steps

## Azure DevOps PR Creation Commands

### Prerequisites
```bash
# Verify Azure CLI is installed
az --version

# Verify Azure DevOps extension
az extension list | grep devops

# If not installed, add the extension
az extension add --name azure-devops
```

### PR Creation Process

#### Method 1: Simple Description (Recommended)
```bash
# Create PR with basic description
az repos pr create \
  --source-branch <branch-name> \
  --target-branch develop \
  --title "<conventional-commit-title>" \
  --description "Brief description of changes"

# Then update with full description if needed
az repos pr update --id <returned-pr-id> --description "Full description here"
```

#### Method 2: File-Based Description (For Complex Descriptions)
```bash
# Create description file first
cat > pr_description.md << 'EOF'
## Summary
Brief description of changes made.

## Type of Change
- [x] Bug fix (non-breaking change which fixes an issue)
- [ ] New feature (non-breaking change which adds functionality)

## Related Work Items
- Task/Story: #[WorkItemId]

## Testing
- [x] Manual testing completed
- [x] All tests passing
EOF

# Create PR with file content
az repos pr create \
  --source-branch <branch-name> \
  --target-branch develop \
  --title "<title>" \
  --description "$(cat pr_description.md)"
```

### ❌ Commands to AVOID

```bash
# NEVER use GitHub CLI
gh pr create --title "..." --body "..."

# AVOID complex heredoc in single command (causes empty descriptions)
az repos pr create --description "$(cat <<'EOF'
Multi-line content here...
EOF
)"
```

### Validation Checklist
Before creating PRs, verify:
- [ ] Using `az` command (not `gh`)
- [ ] Target branch is `develop` (never `master`)
- [ ] Description is properly formatted
- [ ] Work items are linked correctly

## Important Setup Notes

### Repository Configuration
- **Organization**: towne-park
- **Project**: Towne Park Billing  
- **Repository**: Towne Park Billing (ID: b4b93ec6-2f56-4306-94ee-a17b83b1e616)
- **Default Branch**: master
- **Active Development Branch**: develop
- **Work Item Types Available**: Epic, Feature, User Story, Task, Bug

### Branch Validation
✅ **Current Active Branches Include**:
- `develop` - Primary development branch
- `feature/` branches - For new features
- `task/` branches - For individual tasks
- `bugfix/` branches - For bug fixes


**Note**: These are the exact Azure DevOps user identifiers found in the system.