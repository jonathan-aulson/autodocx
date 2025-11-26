# Enhanced Pull Request Review Guidelines

## Overview
This document provides comprehensive guidelines for conducting thorough, consistent, and efficient pull request reviews using Azure DevOps MCP tools and manual processes. It covers both backend (.NET Core APIs) and frontend (React + TypeScript) architecture.

## Available Azure DevOps MCP Tools

### Pull Request Management
- `list_pull_requests` - List pull requests in a repository
- `get_pull_request_comments` - Get comments from a specific pull request
- `add_pull_request_comment` - Add a comment to a pull request
- `update_pull_request` - Update an existing pull request with new properties, link work items, and manage reviewers

### Repository & Code Analysis
- `get_file_content` - Get content of a file or directory from a repository
- `search_code` - Search for code across repositories in a project
- `list_repositories` - List repositories in a project
- `get_repository_details` - Get detailed information about a repository

### Work Item Integration
- `get_work_item` - Get details of a specific work item
- `search_work_items` - Search for work items across projects

### Project Context
- `get_project_details` - Get comprehensive details of a project
- `list_projects` - List all projects in an organization

**Default Configuration**: Most commands use defaults for Towne Park setup:
- Default Organization: `towne-park`
- Default Project: `Towne Park Billing`

## Architecture Overview

### Backend Stack (.NET Core)
- **API Layer**: `/api/src/Functions/` - Azure Functions with HTTP triggers
- **Models**: `/api/src/Models/Dto/` - Data Transfer Objects
- **Services**: `/api/src/Services/` - Business logic layer
- **Adapters**: `/api/src/Adapters/` - External service integration
- **Tests**: `/api/tests/` - Unit and integration tests

### Frontend Stack (React + TypeScript)
- **Framework**: React 18 + TypeScript + Vite
- **Styling**: TailwindCSS + Radix UI components
- **State Management**: React hooks + Context API
- **Forms**: React Hook Form + Zod validation
- **Routing**: React Router DOM
- **Testing**: Jest + React Testing Library
- **Charts**: Nivo charts library
- **Tables**: TanStack React Table

### Frontend Architecture Structure
```
/src/
├── components/           # Reusable UI components
│   ├── ui/              # Base UI components (Radix UI)
│   ├── [FeatureName]/   # Feature-specific components
│   └── [SharedComponent].tsx
├── pages/               # Route components
├── hooks/               # Custom React hooks
├── contexts/            # React Context providers
├── lib/                 # Utility functions and configurations
├── assets/              # Static assets
└── __test__/            # Frontend tests
```

## PR Analysis Workflow

### 1. Initial Information Gathering
```
# Get PR details
Use: list_pull_requests
Parameters: repositoryId, status="active"

# Get specific PR information
Use: get_pull_request_comments
Parameters: repositoryId, pullRequestId

# Check related work items
Use: get_work_item
Parameters: id=[work_item_id_from_PR]
```

### 2. Code Analysis Priority Order

#### Backend Changes (40% of review time)
1. **API Endpoints**: `/api/src/Functions/[Feature].cs`
2. **DTOs & Models**: `/api/src/Models/Dto/[Feature]Dto.cs`
3. **Services & Adapters**: `/api/src/Services/`, `/api/src/Adapters/`
4. **Unit Tests**: `/api/tests/Functions/[Feature]Tests.cs`

#### Frontend Changes (40% of review time)
1. **Components**: `/src/components/[Feature]/`
2. **Pages**: `/src/pages/[Feature]/`
3. **Hooks**: `/src/hooks/[Feature]Hook.ts`
4. **Context**: `/src/contexts/[Feature]Context.tsx`
5. **Tests**: `/src/__test__/`

#### Configuration & Infrastructure (20% of review time)
1. **Package.json** - Dependencies and scripts
2. **Configuration files** - Vite, TypeScript, Tailwind
3. **Static web app config**

### 3. Review Process Steps with MCP Commands
```
# Examine changed files (backend)
Use: get_file_content
Parameters: repositoryId, path="/api/src/Functions/[Feature].cs", version=[branch_name], versionType="branch"

# Examine changed files (frontend)
Use: get_file_content
Parameters: repositoryId, path="/src/components/[Feature]/[Component].tsx", version=[branch_name], versionType="branch"

# Search for related patterns
Use: search_code
Parameters: searchText="[feature_keyword]", filters={Branch: [branch_name]}
```

## Review Decision Framework

| Criteria | Approve | Approve with Suggestions | Changes Requested |
|----------|---------|-------------------------|-------------------|
| **Functionality** | ✅ Works correctly | ✅ Works, minor improvements | ❌ Bugs/issues exist |
| **Security** | ✅ No vulnerabilities | ⚠️ Minor security considerations | ❌ Security issues present |
| **Code Quality** | ✅ Clean, maintainable | ⚠️ Some improvements needed | ❌ Poor quality/structure |
| **Testing** | ✅ Comprehensive tests | ⚠️ Adequate coverage | ❌ Missing/insufficient tests |
| **Performance** | ✅ Optimized | ⚠️ Minor performance issues | ❌ Significant performance problems |
| **Accessibility** | ✅ WCAG compliant | ⚠️ Minor a11y issues | ❌ Major accessibility problems |
| **Type Safety** | ✅ Fully typed | ⚠️ Some any types | ❌ Poor TypeScript usage |

## Code Quality Checklist

### Backend Requirements (.NET Core)
- [ ] Proper async/await usage
- [ ] DTO validation attributes present
- [ ] Appropriate HTTP status codes
- [ ] Comprehensive error handling
- [ ] Input validation and sanitization
- [ ] Unit tests with good coverage
- [ ] Dependency injection patterns
- [ ] Logging implementation

### Frontend Requirements (React + TypeScript)
- [ ] TypeScript types properly defined
- [ ] React hooks used correctly (dependencies, cleanup)
- [ ] Proper component composition and reusability
- [ ] Form validation with Zod schemas
- [ ] Accessibility attributes (ARIA, semantic HTML)
- [ ] Responsive design implementation
- [ ] Error boundary handling
- [ ] Loading and error states
- [ ] Proper event handling and cleanup
- [ ] Performance optimization (useMemo, useCallback where needed)

### Shared Requirements
- [ ] No code duplication
- [ ] Security considerations addressed
- [ ] Performance implications considered
- [ ] Integration with existing architecture
- [ ] Documentation/comments where needed

## Technology-Specific Review Guidelines

### React Component Review
```typescript
// Check for proper component structure
interface ComponentProps {
  // Props should be properly typed
}

const Component: React.FC<ComponentProps> = ({ prop1, prop2 }) => {
  // Check for proper hook usage
  const [state, setState] = useState<Type>(initialValue);
  
  // Check for proper effect cleanup
  useEffect(() => {
    // Effect logic
    return () => {
      // Cleanup
    };
  }, [dependencies]); // Verify dependencies
  
  // Check for proper error handling
  // Check for accessibility
  // Check for performance optimizations
};
```

### Form Validation Review
```typescript
// Zod schema validation
const schema = z.object({
  field: z.string().min(1, "Required field"),
  // Check for comprehensive validation
});

// React Hook Form integration
const { register, handleSubmit, formState: { errors } } = useForm({
  resolver: zodResolver(schema)
});
```

### API Integration Review
```typescript
// Check for proper error handling
const fetchData = async () => {
  try {
    const response = await fetch('/api/endpoint');
    if (!response.ok) throw new Error('API Error');
    return await response.json();
  } catch (error) {
    // Proper error handling
  }
};
```

### Styling & UI Review
- [ ] TailwindCSS classes used appropriately
- [ ] Radix UI components implemented correctly
- [ ] Responsive design patterns
- [ ] Consistent spacing and typography
- [ ] Dark/light theme support
- [ ] Component variants properly implemented

## MCP-Based Review Process

### Step 1: Identify and Get PR
```
# Find the specific PR
Use: list_pull_requests
Example: 
{
  "repositoryId": "Towne Park Billing",
  "status": "active",
  "top": 50
}

# Get PR comments and context
Use: get_pull_request_comments
Example:
{
  "repositoryId": "Towne Park Billing", 
  "pullRequestId": 637
}
```

### Step 2: Analyze Backend Changes
```
# Get API function content
Use: get_file_content
Example:
{
  "repositoryId": "Towne Park Billing",
  "path": "/api/src/Functions/JobCodes.cs",
  "version": "task/2206-edit-job-title",
  "versionType": "branch"
}

# Get DTO definitions
Use: get_file_content
Example:
{
  "repositoryId": "Towne Park Billing",
  "path": "/api/src/Models/Dto/JobCodeDto.cs",
  "version": "task/2206-edit-job-title",
  "versionType": "branch"
}

# Get unit tests
Use: get_file_content
Example:
{
  "repositoryId": "Towne Park Billing",
  "path": "/api/tests/Functions/JobCodesTests.cs",
  "version": "task/2206-edit-job-title",
  "versionType": "branch"
}
```

### Step 3: Analyze Frontend Changes
```
# Get React components
Use: get_file_content
Example:
{
  "repositoryId": "Towne Park Billing",
  "path": "/src/components/AdminPanel/JobCodeManagement.tsx",
  "version": "task/2206-edit-job-title",
  "versionType": "branch"
}

# Get page components
Use: get_file_content
Example:
{
  "repositoryId": "Towne Park Billing",
  "path": "/src/pages/JobCodes.tsx",
  "version": "task/2206-edit-job-title",
  "versionType": "branch"
}

# Get custom hooks
Use: get_file_content
Example:
{
  "repositoryId": "Towne Park Billing",
  "path": "/src/hooks/useJobCodes.ts",
  "versionType": "branch"
}
```

### Step 4: Search for Integration Points
```
# Search for API usage patterns
Use: search_code
Example:
{
  "searchText": "jobcodes/title",
  "filters": {
    "Branch": ["task/2206-edit-job-title"]
  }
}

# Search for component usage
Use: search_code
Example:
{
  "searchText": "JobCodeManagement",
  "filters": {
    "Branch": ["task/2206-edit-job-title"]
  }
}
```

## Common Frontend Issues to Watch For

### React-Specific Issues
```
# Search for common React anti-patterns
{
  "searchText": "useEffect",
  "filters": {"Branch": ["target-branch"]}
}
```
- Missing dependency arrays in useEffect
- Infinite re-render loops
- Memory leaks from uncleared timeouts/subscriptions
- Direct state mutations
- Improper key props in lists
- Missing error boundaries

### TypeScript Issues
```
# Search for TypeScript concerns
{
  "searchText": "any",
  "filters": {"Branch": ["target-branch"]}
}
```
- Usage of `any` types
- Missing interface definitions
- Incorrect type assertions
- Missing generic constraints
- Non-null assertions without safety

### Performance Issues
```
# Search for performance patterns
{
  "searchText": "useMemo|useCallback",
  "filters": {"Branch": ["target-branch"]}
}
```
- Missing memoization for expensive calculations
- Unnecessary re-renders
- Large bundle sizes
- Unoptimized images
- Blocking operations in render

### Accessibility Issues
- Missing ARIA attributes
- Poor keyboard navigation
- Insufficient color contrast
- Missing alternative text
- Improper semantic HTML
- Focus management issues

## Frontend Path Patterns to Examine

### Component Structure
```
/src/components/
├── ui/                  # Base components (Button, Input, etc.)
├── [FeatureName]/       # Feature-specific components
│   ├── [Feature]List.tsx
│   ├── [Feature]Form.tsx
│   ├── [Feature]Modal.tsx
│   └── index.ts         # Barrel exports
└── [SharedComponent].tsx
```

### Common File Extensions and Purposes
- `.tsx` - React components with JSX
- `.ts` - TypeScript utilities, hooks, types
- `.css` - Component-specific styles
- `.test.tsx` - Component tests
- `.stories.tsx` - Storybook stories (if applicable)

## Standardized Review Comment Template

```
# Pull Request Review: [PR Title]

## Summary
[Brief overview of changes covering both backend and frontend modifications]

## Backend Analysis
### 🟢 API Implementation Strengths
- [List positive aspects of API changes]

### 🟡 Backend Areas for Improvement
#### 1. [Issue Category - e.g., Input Validation]
**File: `/api/src/Functions/[File].cs` - Lines [X-Y]**
```csharp
// Current code example
[problematic code snippet]
```
**Suggestion**: [Specific improvement recommendation]

## Frontend Analysis
### 🟢 UI/UX Implementation Strengths
- [List positive aspects of frontend changes]

### 🟡 Frontend Areas for Improvement
#### 1. [Issue Category - e.g., TypeScript Usage]
**File: `/src/components/[Feature]/[Component].tsx` - Lines [X-Y]**
```typescript
// Current code example
[problematic code snippet]
```
**Suggestion**: [Specific improvement recommendation]

### 🟢 Testing Assessment
**Backend Tests:**
- [Evaluation of API tests]

**Frontend Tests:**
- [Evaluation of component tests]

## Cross-cutting Concerns
- [ ] API-Frontend integration properly implemented
- [ ] Error handling consistent between layers
- [ ] Loading states properly managed
- [ ] Data flow and state management appropriate

## Recommendation
**[Decision]** - [Brief justification covering both backend and frontend]

## Related Work Items
- [Verification of linked tasks/stories]
```

## Quick Reference MCP Commands

### Full-Stack Review Commands
```bash
# Backend analysis
get_file_content(repositoryId="Towne Park Billing", path="/api/src/Functions/[Feature].cs", version="branch-name", versionType="branch")
get_file_content(repositoryId="Towne Park Billing", path="/api/tests/Functions/[Feature]Tests.cs", version="branch-name", versionType="branch")

# Frontend analysis
get_file_content(repositoryId="Towne Park Billing", path="/src/components/[Feature]/[Component].tsx", version="branch-name", versionType="branch")
get_file_content(repositoryId="Towne Park Billing", path="/src/pages/[Feature].tsx", version="branch-name", versionType="branch")

# Configuration analysis
get_file_content(repositoryId="Towne Park Billing", path="/package.json", version="branch-name", versionType="branch")

# Integration analysis
search_code(searchText="[api_endpoint_path]", filters={"Branch": ["branch-name"]})
search_code(searchText="[component_name]", filters={"Branch": ["branch-name"]})
```

## Best Practices for Full-Stack Review

### Efficiency Guidelines
1. **Start with the feature overview** - Understand the complete user story
2. **Review backend API first** - Ensure data contracts are solid
3. **Then review frontend implementation** - Check API integration
4. **Verify end-to-end functionality** - Ensure the complete flow works
5. **Check error handling across layers** - Consistent error experience

### Common Integration Issues
- API response shape doesn't match frontend expectations
- Missing error handling in API calls
- Inconsistent loading states
- Type mismatches between backend DTOs and frontend interfaces

---

*This enhanced guide ensures comprehensive, consistent, and efficient full-stack pull request reviews covering both backend .NET Core APIs and React TypeScript frontend with clear MCP tool usage and fallback procedures.*