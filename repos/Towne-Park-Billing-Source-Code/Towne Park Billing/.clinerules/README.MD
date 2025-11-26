# Cline and Clinerules

## Introduction
Cline is a tool designed to streamline development processes by providing guidelines and rules for coding standards. This document outlines how to use Cline, contribute to project rules, and leverage best practices for high-quality code output.


## Usage
- install cline extension for vscode.
- You can either use your own api key or use models through github pro license.
- https://github.com/dorukyy/cline-tips 

## Project Rules and Guidelines (`.clinerules/`)

### Purpose of `.clinerules/`
The `.clinerules/` directory, located at the project root, is a dedicated space for storing project-specific coding standards, architectural guidelines, naming conventions, and best practices for both frontend (React with TypeScript) and backend (.NET Azure Functions) development.

These rules are referenced by the Cline tool and serve as a guide for developers to ensure that all code contributions meet the team's established standards. Maintaining clear, version-controlled rules within this directory offers several benefits:
- **Accelerated Onboarding:** New team members can quickly understand project conventions and best practices.
- **Improved Consistency:** Ensures a uniform coding style and architectural approach across the codebase.
- **Reduced Code Review Friction:** Clear guidelines minimize subjective debates during code reviews.
- **Enhanced Maintainability:** Consistent code is easier to understand, debug, and extend.

### How to Add or Update Rules

**Where to Add Rules:**
All project-specific rules reside within the `.clinerules/` directory.

**Process for Adding/Updating Rules:**
1.  **Create or Modify a Markdown File:**
    *   To add a new set of guidelines (e.g., for a new technology or a specific architectural pattern), create a new Markdown file (e.g., `api-security-guidelines.md`, `frontend-performance-patterns.md`).
    *   To update existing rules, modify the relevant Markdown file (e.g., `coding-guidelines.md`, `project-summary.md`).
2.  **Structure and Content:**
    *   Use clear, descriptive headings (e.g., `## Naming Conventions`, `### Component Structure`).
    *   Employ bullet points, numbered lists, and code blocks for readability and clarity.
    *   Provide concrete code examples to illustrate correct and incorrect practices.
    *   Explain the rationale behind each rule or guideline to foster understanding.
3.  **Versioning and Review:**
    *   Commit changes to rule files as part of your regular development workflow.
    *   If a rule is deprecated or replaced, clearly mark it as such and explain the reasoning.
    *   Discuss significant changes to rules with the team to ensure consensus.

**Why Add Rules?**
Documenting and enforcing rules within `.clinerules/` is crucial because:
- They serve as the single source of truth for coding style, architectural decisions, and development processes.
- They empower developers to write consistent, high-quality code.
- They facilitate smoother collaboration and knowledge sharing within the team.

### Examples of Rules
The existing `coding-guidelines.md` file provides a comprehensive example of how rules should be structured. Key areas covered include:
- **General Standards:** Naming conventions, documentation, modularity (DRY).
- **Project Structure:** Organization of frontend and backend code.
- **Frontend (React with TypeScript):** Component structure, state management, testing (Jest & React Testing Library), DTOs, and naming patterns for UI elements (`data-qa-id`).
- **Backend (.NET):** Layered architecture, dependency injection, data access (Entity Framework), testing (xUnit/NUnit & Moq), VOs, and Dataverse integration.
- **Unit Testing Guidelines:** TDD principles, CI/CD integration, code coverage targets.
- **Glossary of Terms:** Definitions of key business and technical terms.

When adding new rules, aim for a similar level of detail and clarity.

## Features and Best Practices for Improving Code Output

Beyond the formal rules in `.clinerules/`, this project emphasizes several features and best practices to enhance code quality, maintainability, and developer productivity. New developers should familiarize themselves with these concepts, detailed further in `coding-guidelines.md` and `project-summary.md`.

### 1. Data Transfer Objects (DTOs) and Value Objects (VOs)
-   **Frontend (DTOs):** Use DTOs to define the shape of data being passed between components and services. This ensures clear data contracts and type safety.
-   **Backend (VOs & DTOs):**
    *   Employ VOs to encapsulate domain logic and ensure immutability for core business concepts.
    *   Use DTOs for data exchange between layers (e.g., API controllers and services) and for external communication.

### 2. Mapping
-   Implement mapping functions (e.g., using Mapperly for .NET) to convert between DTOs, VOs, and domain models. This centralizes transformation logic and keeps entities clean.

### 3. Comprehensive Testing
-   **Unit Tests:** Write unit tests for all new features and components.
    *   **Frontend:** Use Jest and React Testing Library.
    *   **Backend:** Use xUnit or NUnit, with mocking frameworks like Moq.
-   **Test-Driven Development (TDD):** Adopt TDD principles where practical.
-   **Code Coverage:** Aim for at least 80% code coverage across both frontend and backend.
-   **CI/CD Integration:** Ensure all tests are integrated into CI/CD pipelines to catch regressions early.

### 4. Consistent Naming Conventions
-   Adhere strictly to the naming conventions outlined in `coding-guidelines.md` for:
    *   Files, classes, functions, and variables.
    *   Interactive UI elements (`data-qa-id` pattern: `[component-type]-[iteration/index (optional)]-[specific-action/purpose]`). This is crucial for end-to-end testing and analytics.

### 5. Modular Component and File Organization
-   **Frontend:** Organize React components by feature, with each feature in its own directory. Use functional components and hooks.
-   **Backend:** Structure .NET projects into logical layers (Controllers, Services, Repositories, Models). Utilize Dependency Injection.

### 6. Clear Documentation
-   Maintain clear and concise documentation for all components, services, and complex logic. This includes inline comments, READMEs for modules, and comprehensive commit messages.

### 7. Leveraging the Glossary
-   Refer to the **Glossary of Terms** in `coding-guidelines.md` to ensure consistent use of domain-specific language across the codebase and documentation.

### Why These Practices Matter
Adhering to these rules and best practices is not merely about following procedures; it's about building a robust, maintainable, and scalable application. They contribute to:
-   **Higher Code Quality:** Fewer bugs and more reliable features.
-   **Easier Onboarding:** New team members can become productive more quickly.
-   **Improved Collaboration:** A shared understanding of standards reduces friction.
-   **Long-Term Maintainability:** The codebase remains understandable and adaptable over time.

## Contributing
Contributions are welcome! Please ensure that any contributions adhere to the guidelines outlined in the `.clinerules/` directory and follow the best practices described above.
