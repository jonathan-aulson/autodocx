# Baseline: LLM Rollup Behaviour (2025-11-13)

- Command:
  ```bash
  python -m autodocx_cli scan repos/Towne-Park-Billing-Source-Code/Towne-Park-Billing-PA-Solution --out out --llm-rollup
  ```
- Outcome: command ran for ~9 minutes and timed out. Logs (from `openai._base_client` debug output) show the pipeline attempted to call the OpenAI Responses API despite no API key being configured. The request stalled, no Markdown was produced, and the CLI run terminated on timeout.

Conclusion: rollup lacks guards for missing API credentials and couples network calls directly inside the monolithic `rollup.py`, confirming Workstream 4 hypotheses.
