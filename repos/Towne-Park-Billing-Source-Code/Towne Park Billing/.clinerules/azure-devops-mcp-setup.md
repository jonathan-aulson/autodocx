# Azure DevOps MCP Setup

## Overview

The **official Microsoft Azure DevOps MCP** (Model Context Protocol) server has been configured for this project to provide seamless integration with Azure DevOps operations.


### MCP Settings
- Server name: `azureDevOps`
- Organization: `towne-park`
- Authentication: Azure CLI (no PAT tokens needed)
- Input: Prompts for Azure DevOps organization name (default: `towne-park`)

### Available Tools

#### Core Operations
- `core_list_projects`  
  *Auto-approve*  
  Retrieve a list of projects in your Azure DevOps organization.  
  **Parameters:**  
  - `stateFilter`: Filter projects by their state. Defaults to 'wellFormed'.  
  - `top`: The maximum number of projects to return. Defaults to 100.  
  - `skip`: The number of projects to skip for pagination. Defaults to 0.  
  - `continuationToken`: Continuation token for pagination.  

- `core_list_project_teams`  
  *Auto-approve*  
  Retrieve a list of teams for the specified Azure DevOps project.  
  **Parameters:**  
  - `project*`: The name or ID of the Azure DevOps project.  
  - `mine`: If true, only return teams that the authenticated user is a member of.  
  - `top`: The maximum number of teams to return. Defaults to 100.  
  - `skip`: The number of teams to skip for pagination. Defaults to 0.  

#### Work Item Management
- `wit_my_work_items`  
  *Auto-approve*  
  Retrieve a list of work items relevant to the authenticated user.  
  **Parameters:**  
  - `project*`: The name or ID of the Azure DevOps project.  
  - `type`: The type of work items to retrieve. Defaults to 'assignedtome'.  
  - `top`: The maximum number of work items to return. Defaults to 50.  
  - `includeCompleted`: Whether to include completed work items. Defaults to false.  

- `wit_get_work_item`  
  *Auto-approve*  
  Get a single work item by ID.  
  **Parameters:**  
  - `id*`: The ID of the work item to retrieve.  
  - `project*`: The name or ID of the Azure DevOps project.  
  - `fields`: Optional list of fields to include in the response.  
  - `asOf`: Optional date to retrieve the work item as of a specific time.  
  - `expand`: Expand options include 'all', 'fields', 'links', 'none', and 'relations'. Defaults to 'none'.  

- `wit_create_work_item`  
  *Auto-approve*  
  Create a new work item in a specified project and work item type.  
  **Parameters:**  
  - `project*`: The name or ID of the Azure DevOps project.  
  - `workItemType*`: The type of work item to create, e.g., 'Task', 'Bug', etc.  
  - `fields*`: A record of field names and values to set on the new work item.  

- `wit_update_work_item`  
  *Auto-approve*  
  Update a work item by ID with specified fields.  
  **Parameters:**  
  - `id*`: The ID of the work item to update.  
  - `updates*`: An array of field updates to apply to the work item.  

- `wit_link_work_item_to_pull_request`  
  *Auto-approve*  
  Link a single work item to an existing pull request.  
  **Parameters:**  
  - `repositoryId*`: The ID of the repository containing the pull request.  
  - `pullRequestId*`: The ID of the pull request to link to.  
  - `workItemId*`: The ID of the work item to link to the pull request.  

#### Repository Operations
- `repo_list_repos_by_project`  
  *Auto-approve*  
  Retrieve a list of repositories for a given project.  
  **Parameters:**  
  - `project*`: The name or ID of the Azure DevOps project.  

- `repo_list_pull_requests_by_repo`  
  *Auto-approve*  
  Retrieve a list of pull requests for a given repository.  
  **Parameters:**  
  - `repositoryId*`: The ID of the repository where the pull requests are located.  
  - `created_by_me`: Filter pull requests created by the current user.  
  - `i_am_reviewer`: Filter pull requests where the current user is a reviewer.  

- `repo_create_pull_request`  
  *Auto-approve*  
  Create a new pull request.  
  **Parameters:**  
  - `repositoryId*`: The ID of the repository where the pull request will be created.  
  - `sourceRefName*`: The source branch name for the pull request.  
  - `targetRefName*`: The target branch name for the pull request.  
  - `title*`: The title of the pull request.  
  - `description`: The description of the pull request.  
  - `isDraft`: Indicates whether the pull request is a draft. Defaults to false.  

- `repo_get_pull_request_by_id`  
  *Auto-approve*  
  Get a pull request by its ID.  
  **Parameters:**  
  - `repositoryId*`: The ID of the repository where the pull request is located.  
  - `pullRequestId*`: The ID of the pull request to retrieve.  

#### Search Capabilities
- `search_code`  
  *Auto-approve*  
  Get the code search results for a given search text.  
  **Parameters:**  
  - `searchRequest*`: Search request object.  

- `search_workitem`  
  *Auto-approve*  
  Get work item search results for a given search text.  
  **Parameters:**  
  - `searchRequest*`: Search request object.  

- `search_wiki`  
  *Auto-approve*  
  Get wiki search results for a given search text.  
  **Parameters:**  
  - `searchRequest*`: Search request object.  

#### Build & Release
- `build_get_builds`  
  *Auto-approve*  
  Retrieves a list of builds for a given project.  
  **Parameters:**  
  - `project*`: Project ID or name to get builds for  
  - ... (see full parameter list in documentation)  

- `build_run_build`  
  *Auto-approve*  
  Triggers a new build for a specified definition.  
  **Parameters:**  
  - `project*`: Project ID or name to run the build in  
  - `definitionId*`: ID of the build definition to run  
  - `sourceBranch`: Source branch to run the build from  

- `release_get_releases`  
  *Auto-approve*  
  Retrieves a list of releases for a given project.  
  **Parameters:**  
  - `project`: Project ID or name to get releases for  
  - ... (see full parameter list in documentation)  

## Integration 

### Automated Work Item Management
```
"Link this code change to work item #2136"
"Create a task for implementing the new PayrollForecast component"
"Update work item status after completing the feature"
```

### Pull Request Automation
```
"Create a PR for the payroll forecasting feature"
"Link work item #2136 to the current branch changes"
"Review open PRs for the forecasting project"
```

### Code Discovery
```
"Search for existing payroll-related code in the repository"
"Find similar forecasting implementations"
"Show me code that handles job title calculations"
```

### Project Context
```
"List current sprint work items"
"Show builds for the Towne Park Billing project"
"Get test results for the latest build"
```

## Example Prompts

### Development Workflow
- "Show me work items assigned to me for the current sprint"
- "Create a pull request for task #2136 with payroll forecasting changes"
- "Link the current branch to work item #2136"
- "Search for existing code that handles job group calculations"

### Code Review
- "List open pull requests that need review"
- "Get details for pull request #[ID]"
- "Add a comment to PR #[ID] about the TypeScript improvements"

### Project Management
- "Show builds for the Towne Park Billing project"
- "Create a bug work item for the forecasting calculation issue"
- "Update work item #2136 status to 'In Progress'"
