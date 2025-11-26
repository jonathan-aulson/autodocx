# Azure DevOps PR Creation Workflow

## Workflow: /create-pr.md

This workflow automates the creation of Pull Requests in Azure DevOps for the Towne Park Billing repository following the established git flow and PR rules.

## Prerequisites

1. Azure DevOps MCP server configured and connected
2. Current working directory is the Towne Park Billing repository
3. You are on a `task/` or `feature/` branch
4. Changes are committed and pushed to the remote repository

## Workflow Steps

### Step 1: Repository Validation
```bash
# Validate we're in the correct repository
echo "🔍 Validating repository..."
```

**Validation Checks:**
- Confirm repository ID: `b4b93ec6-2f56-4306-94ee-a17b83b1e616`
- Confirm repository name: "Towne Park Billing"
- Verify we're in towne-park organization

**Action:** Use `azureDevOps:repo_get_repo_by_name_or_id` to validate current repository

### Step 2: Branch Analysis
```bash
# Get current branch information
git branch --show-current
```

**Branch Detection Logic:**
- If branch starts with `task/` → Task branch workflow
- If branch starts with `feature/` → Feature branch workflow
- If branch starts with `bugfix/` → Bug fix workflow
- Otherwise → Error and exit

**Action:** Parse branch name and determine workflow type

### Step 3: Work Item Discovery

#### For Task Branches (`task/`)
**Objective:** Find parent feature branch and associated task work item

**Actions:**
1. Use `azureDevOps:search_workitem` to find task work item by branch name
2. Extract task ID from search results
3. Use `azureDevOps:wit_get_work_item` to get full task details
4. Find parent User Story from task relationships
5. Identify associated feature branch from User Story

**Validation:**
- Ensure task work item exists
- Verify parent User Story relationship
- Confirm feature branch exists in repository

#### For Feature Branches (`feature/`)
**Objective:** Find associated User Story work item

**Actions:**
1. Use `azureDevOps:search_workitem` to find User Story by branch name
2. Extract User Story ID from search results
3. Use `azureDevOps:wit_get_work_item` to get full User Story details
4. Set target branch to `develop`

**Validation:**
- Ensure User Story work item exists
- Confirm `develop` branch exists

### Step 4: Target Branch Determination

**Branch Type Rules:**
- `task/` branches → Target parent feature branch
- `feature/` branches → Target `develop` branch
- `bugfix/` branches → Target `develop` branch

**🚫 CRITICAL VALIDATION:**
- **NEVER** target `master` branch
- **ALWAYS** validate target branch exists
- **ALWAYS** confirm target branch is not `master`

### Step 5: Reviewer Assignment

**Reviewer Logic:**
- Task branches → Assign feature branch owner + code reviewers
- Feature branches → Assign senior developers + architects
- Bug fix branches → Assign QA team + original feature developers

**Default Reviewers:**
- Technical leads for the affected area
- QA representatives for testing validation
- Architecture team for significant changes

### Step 6: Commit Message Validation

**Validation Rules:**
1. Follow Conventional Commits 1.0.0 specification
2. Ensure proper type prefix (`feat:`, `fix:`, `chore:`, etc.)
3. Include scope when applicable
4. Validate description format

**Example Valid Formats:**
```
feat(api): add user authentication endpoint
fix(billing): resolve payment processing error
chore(deps): update npm dependencies
```

### Step 7: PR Title Generation

**Title Format:**
```
<type>[optional scope]: <description> - <WorkItemType> #<WorkItemId>
```

**Examples:**
- `feat(api): add user authentication - Task #12345`
- `fix(billing): resolve payment processing error - Story #12340`
- `chore(deps): update dependencies - Task #12346`

### Step 8: PR Description Generation

**Template Structure:**
```markdown
## Summary
[Auto-generated from commit messages and work item description]

## Type of Change
[Auto-detected from conventional commit type]

## Related Work Items
- [WorkItemType]: #[WorkItemId] - [WorkItemTitle]
- Related: [Additional work items if found]

## Testing
[Auto-populated checklist based on change type]

## Technical Notes
[Extracted from commit messages and code analysis]

## Checklist
[Standard checklist items]
```

### Step 9: PR Creation

**Actions:**
1. Use `azureDevOps:repo_create_pull_request` with:
   - `repositoryId`: "b4b93ec6-2f56-4306-94ee-a17b83b1e616"
   - `sourceRefName`: Current branch (with refs/heads/ prefix)
   - `targetRefName`: Determined target branch (with refs/heads/ prefix)
   - `title`: Generated PR title
   - `description`: Generated PR description
   - `isDraft`: false (unless specified)

### Step 10: Work Item Linking

**Actions:**
1. Get created PR ID from response
2. Use `azureDevOps:wit_link_work_item_to_pull_request` to link:
   - Primary work item (Task or User Story)
   - Related work items (if any)

### Step 11: Success Confirmation

**Final Actions:**
1. Display PR URL and ID
2. Show linked work items
3. List assigned reviewers
4. Provide next steps guidance

## Error Handling

### Common Error Scenarios:
1. **Invalid Repository:** Exit with clear message about ruleset scope
2. **Master Branch Target:** Block creation with forbidden operation error
3. **Missing Work Item:** Prompt for manual work item selection
4. **Invalid Branch Name:** Provide branch naming guidance
5. **Missing Target Branch:** List available branches for manual selection

### Error Messages:
```
🚫 FORBIDDEN: Never create PRs targeting master branch. Use develop as maximum target.
❌ ERROR: This ruleset only applies to the Towne Park Billing repository
⚠️  WARNING: Cannot find associated work item. Please specify manually.
🔍 INFO: Available feature branches for task PR targeting...
```

## Usage Examples

### Creating PR from Task Branch:
```
/create-pr.md
```

### Creating PR from Feature Branch:
```
/create-pr.md
```

### Creating Draft PR:
```
/create-pr.md --draft
```

### Manual Work Item Override:
```
/create-pr.md --work-item 12345
```

## Integration Points

### Azure DevOps MCP Functions Used:
- `azureDevOps:repo_get_repo_by_name_or_id`
- `azureDevOps:repo_list_branches_by_repo`
- `azureDevOps:search_workitem`
- `azureDevOps:wit_get_work_item`
- `azureDevOps:repo_create_pull_request`
- `azureDevOps:wit_link_work_item_to_pull_request`

### Git Commands Used:
- `git branch --show-current`
- `git log --oneline -10`
- `git status --porcelain`

## Workflow Configuration

### Environment Variables:
- `AZDO_ORG`: "towne-park"
- `AZDO_PROJECT`: "Towne Park Billing"
- `AZDO_REPO_ID`: "b4b93ec6-2f56-4306-94ee-a17b83b1e616"

### Constants:
- `FORBIDDEN_TARGET`: "master"
- `DEFAULT_TARGET`: "develop"
- `VALID_BRANCH_PREFIXES`: ["task/", "feature/", "bugfix/"]

## Security Notes

- All Azure DevOps operations require proper authentication
- Repository access is validated before any operations
- Work item linking respects Azure DevOps permissions
- PR creation follows organization security policies

---

**Note:** This workflow enforces the strict branching and PR rules for the Towne Park Billing repository. It prevents any accidental PRs to the master branch and ensures proper work item linking and conventional commit compliance.