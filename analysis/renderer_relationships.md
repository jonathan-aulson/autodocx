# Renderer Relationship Output (2025-11-15)

Command executed to demonstrate the relationship-aware renderer sections:

```bash
python scripts/render_relationship_demo.py
```

This script calls `render_business_component_page` with a synthetic workflow that includes one HTTP call and one Dataverse write. The generated Markdown lives at `analysis/demo_docs/components/DemoGroup/DemoWorkflow.md` and now contains the new sections:

```
## 🧩 Relationship Highlights
- External HTTP/API calls: 1
- Data touchpoints (SQL/Dataverse/SharePoint): 1
- Sample flows:
  - CallAPI calls https://example.com/api [http]
  - UpdateDataverse writes accounts [dataverse]

## 📊 Dependency Matrix
| Target Kind | Operation | Count |
|-------------|-----------|-------|
| dataverse | writes | 1 |
| http | calls | 1 |
```

These blocks confirm the renderer surfaces both narrative and quantitative relationship context once the mapper provides `relationships` and `relationship_matrix` data.

## Production Sample (2025-11-15)

After rerunning the Towne Park scan, the MkDocs component page for `workflow_BellServiceFeeChildFlow-42A88C06-CE84-EF11-AC20-0022480A57AC.json` now includes:

```
## Relationship Highlights
- Data touchpoints: 1
- Sample flows:
  - Initialize_BellServiceFeeAmount_Variable calls initializevariable [initializevariable]
  - Condition_-_are_parameters_empty_or_invoice_not_selected calls if [if]
  - Compose_Empty_Line_Items calls compose [compose]

## Dependency Matrix
| Target Kind | Operation | Count |
|-------------|-----------|-------|
| action | calls | 9 |
| compose | calls | 3 |
| if | calls | 3 |
| initializevariable | calls | 2 |
| parsejson | calls | 1 |
| response | calls | 1 |
| setvariable | calls | 1 |
| sql | reads | 1 |
```

Path: `out/docs/components/Towne-Park-Billing-PA-Solution/workflow_BellServiceFeeChildFlow-42A88C06-CE84-EF11-AC20-0022480A57AC.json.md`
