# Post-change: Verification Tooling (2025-11-13)

- Command:
  ```bash
  python -m autodocx_cli stats --out out
  ```
- Output (abridged):
  ```
  Output directory:
  C:\Users\JonathanAulson\Documents\Projects\auto_doc_engine\out
  SIR count: 33 | Artifacts: 33
  Top components:
    - BillingSystem: 29
    - BillingSystemMonitoring: 2
    - Towne-Park-Billing-PA-Solution: 1
    - BillingSystemEnterpriseDataRetrieval: 1
  Signal kinds:
    - workflow: 32
    - doc: 1
  Top connectors (workflow steps):
    - initializevariable: 119
    - compose: 54
    - if: 51
    - parsejson: 28
    - shared_commondataserviceforapps: 27
    ...
  ```

The new `autodocx stats` subcommand surfaces component distribution, signal kinds, and connector frequency, eliminating the manual Python snippets previously required.
