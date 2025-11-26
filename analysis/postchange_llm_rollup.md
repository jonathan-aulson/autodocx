# Post-change: LLM Rollup Behaviour (2025-11-13)

- Command:
  ```powershell
  $env:AUTODOCX_DISABLE_LLM='1'
  python -m autodocx_cli scan repos/Towne-Park-Billing-Source-Code/Towne-Park-Billing-PA-Solution --out out --llm-rollup
  Remove-Item Env:AUTODOCX_DISABLE_LLM
  ```
- Output (trimmed):
  ```
   Skipping rollup: AUTODOCX_DISABLE_LLM environment flag detected.
   ...
  Done. Outputs in:
  C:\Users\JonathanAulson\Documents\Projects\auto_doc_engine\out
  ```

The run completed in ~2 seconds with no network calls. The new guard honours either `llm.enabled: false` in configuration or the `AUTODOCX_DISABLE_LLM=1` environment flag, preventing rollup execution when prerequisites are missing.

Unit tests (`tests/test_llm_guards.py`) cover the guard logic and schema validators, and `python -m pytest` now reports 13 passing tests.
