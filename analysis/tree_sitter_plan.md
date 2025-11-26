## Tree-Sitter Adoption Plan (Workstream 7)

### Goals
- Normalize multi-language AST parsing so autodocx can emit first-class `code_entity`, `ui_component`, and `integration` signals.
- Feed parsed entities into Option1 artifacts and LLM prompts to improve business-friendly narratives (process flows, capabilities, ownership).

### Initial Language Coverage
| Language / Framework | File Patterns | Entity Types | Rationale |
| --- | --- | --- | --- |
| Python (`tree_sitter_python`) | `**/*.py` | `function`, `class` | API endpoints, orchestration scripts, docstrings. |
| C# (`tree_sitter_c_sharp`) | `**/*.cs` | `class`, `interface`, `method` | Azure Functions, SDK integrations, domain services. |
| JavaScript/TypeScript (`tree_sitter_javascript`, `tree_sitter_typescript`, `tree_sitter_tsx`) | `**/*.js`, `**/*.ts`, `**/*.tsx`, `**/*.jsx` | `function`, `class`, `method`, React components | UI layers, client SDK usage, integration logic. |
| Future (planned) | `**/*.go`, `**/*.java`, `**/*.rb` | `function`, `method`, `struct` | Targeted per-repo needs. |

### Architecture
1. **Helper Module (`autodocx/tree_sitter_support.py`)**
   - Cached language handles, parser creation, and availability checks.
   - `tree_sitter_available()` toggles extractors/tests when deps are missing.
2. **Extractor (`autodocx/extractors/tree_sitter_code.py`)**
   - Discovers code files, parses AST, emits `code_entity` signals (name, type, docstring, evidence lines).
   - Docstring heuristics:
     - Python: first string literal in block.
     - JS/TS/TSX: leading `/** ... */` or `//` comment above the declaration.
     - C#: leading `///` XML comment.
3. **Mapper Integration**
   - Option1 artifacts store `code_entity` payloads (`entity_type`, `docstring`, span) and tag capabilities (`exposes code component`).
4. **LLM Context & Prompts**
   - `sanitize_artifacts` forwards `code_entities`, `ui_components`, and `integrations` so prompts can highlight them.
   - Prompt templates instruct the model to call out key code modules/UI entry points/integrations when summarizing components.

### Deliverables / Tracking
- [x] Dependencies + helper module.
- [x] Initial extractor + entry point + tests.
- [x] Expand docstring coverage (TSX/JSX, C# method summaries) via `_leading_comment_text` heuristics.
- [x] Document how tree-sitter signals feed downstream mapping/LLM prompts (this file + Option1/LLM references).
- [ ] Add tree-sitter-powered UI component detection (ties into Workstream 8).

### Downstream Usage
1. **Option1 Mapper** captures `code_entities` (name, entity_type, docstring, span) so downstream artifacts include “Key Code Modules” and cross-link UI/integration hints.
2. **LLM Context**: `context_builder.sanitize_artifacts` forwards trimmed `code_entities` arrays; prompt templates explicitly instruct the model to cite them inside relationships_summary / dependency_matrix narratives.
3. **Renderer**: Component pages read the same arrays and generate the “Key Code Modules” section without waiting for LLM rollups, keeping business docs grounded even when LLM is disabled.
4. **Future**: AST data will bootstrap glossary/business-entity inference, change-impact analysis, and role detection for governance reviews.
