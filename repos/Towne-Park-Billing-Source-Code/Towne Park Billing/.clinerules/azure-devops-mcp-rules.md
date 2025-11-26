# Cline Rule: Azure DevOps HTML to Plain Text Conversion

## Rule Description
When using the MCP Azure DevOps server, always convert HTML content to plain text format for better readability and processing.

## Instructions for the Model

### HTML Conversion Requirements
- **Convert all HTML to plain text** when working with Azure DevOps content
- Strip HTML tags while preserving the essential content structure
- Maintain readability by converting HTML formatting to appropriate plain text equivalents

### Specific Conversion Guidelines

#### Text Formatting
- `<strong>` or `<b>` → **bold text** (using markdown bold)
- `<em>` or `<i>` → *italic text* (using markdown italic)
- `<code>` → `inline code` (using markdown backticks)
- `<pre>` → code blocks (using markdown code fences)

#### Lists and Structure
- `<ul>` and `<li>` → Convert to markdown bullet lists
- `<ol>` and `<li>` → Convert to markdown numbered lists
- `<h1>` through `<h6>` → Convert to markdown headers
- `<p>` → Separate paragraphs with line breaks

#### Links and References
- `<a href="url">text</a>` → Convert to markdown links: `[text](url)`
- Preserve URL references for work items, pull requests, and other Azure DevOps resources

#### Tables
- `<table>`, `<tr>`, `<td>`, `<th>` → Convert to markdown table format
- Maintain column alignment where possible

### Azure DevOps Specific Content
When processing Azure DevOps items (work items, pull requests, comments, etc.):
- Convert HTML descriptions and comments to clean plain text
- Preserve formatting that aids in understanding (bullets, numbering, emphasis)
- Maintain links to related work items, commits, and other Azure DevOps resources
- Convert HTML entities (e.g., `&amp;`, `&lt;`, `&gt;`) to their plain text equivalents

### Implementation Notes
- Use appropriate HTML parsing libraries or regex patterns for conversion
- Test conversion with common Azure DevOps HTML patterns
- Ensure converted text remains readable and maintains logical structure
- Handle malformed or nested HTML gracefully

### Example Usage
```
Before: <p>This is a <strong>critical bug</strong> that affects <em>all users</em>. See <a href="#123">Work Item 123</a> for details.</p>

After: This is a **critical bug** that affects *all users*. See [Work Item 123](#123) for details.
```

## Rule Application
This rule applies to all interactions with the MCP Azure DevOps server, including:
- Work item descriptions and comments
- Pull request descriptions and comments
- Build and release notes
- Wiki content
- Any other HTML content retrieved from Azure DevOps

---
*Rule Version: 1.0*
*Created for: Cline MCP Azure DevOps Integration*