# UI/Integration Workstream Validation (2025-11-15)

Command executed (after adding UI/integration extractors) to validate against the Towne Park repo:

```bash
python -m autodocx_cli scan repos/Towne-Park-Billing-Source-Code --out out --debug --llm-rollup
```

Summary of new signal volumes (`out/sir/*.json`):

| Signal Type | Count | Sample |
|-------------|-------|--------|
| `ui_component` | 65 | React components such as `AdminPanel`, `AnimatedLoadingText`, `App` under `Towne Park Billing/src/...` |
| `integration` | 8 | C# imports: `Azure.Storage.Blobs`, `System.Net.Http.Headers`, etc. |
| `process_diagram` | _none found_ (no BPMN/draw.io files in repo) |
| `business_entity` | _none found_ (no swimlanes available) |

Example UI component (React):
```
{'framework': 'react', 'name': 'AdminPanel',
 'file': 'Towne Park Billing/src/components/AdminPanel/AdminPanel.tsx',
 'routes': [], 'component_or_service': 'Towne_Park_Billing'}
```

Example integration hit:
```
{'library': 'Azure.Storage.Blobs', 'integration_kind': 'azure_storage',
 'file': 'Towne Park Billing/api/src/Functions/Reports.cs', 'language': 'cs'}
```

These signals now appear in Option1 artifacts (`code_entities`, `ui_components`, `integrations` arrays) and flow into business pages + LLM prompts.

## Business Entity & Glossary Heuristics (2025-11-15)

- Added `[Authorize(Roles="...")]` parsing plus UI component-name analysis so `BillingAdminPage` -> **Billing Administrator** and `[Authorize(Roles="Finance,OpsManager")]` -> **Finance**, **OpsManager**.
- Regression tests live in `tests/test_business_entities_extractor.py` and cover BPMN, authorization attributes, and TSX component samples.
- Group/component business docs now surface these entities inside the 📚 Glossary block (with the source, e.g., `component_name` vs `authorize_attribute`) so reviewers can see which user personas map to each section.
- The renderer also consumes the new `process_flows` + `integration_summary` extras to render Mermaid diagrams on MkDocs system pages, giving stakeholders a visual of how those UI/integration signals roll up.
