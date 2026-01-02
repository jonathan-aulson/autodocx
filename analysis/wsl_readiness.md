# WSL Readiness Log

This log captures the steps and evidence needed to keep AutodocX healthy inside WSL Ubuntu.

## 2025-12-08 Baseline

- Added `scripts/setup_wsl.sh` to install Python 3.10 dev headers, Graphviz, fonts, MkDocs, Azure CLI, and the Bicep CLI. Run `./scripts/setup_wsl.sh --help` for optional flags.
- Updated `README.md` and `developer_onboarding_context.md` with explicit WSL guidance (keep repo under `/home/<user>`, activate venv via `source .venv/bin/activate`, set `AUTODOCX_SKIP_PYCLEAN=1` if the repo ever lives on `/mnt/c`).
- Introduced `autodocx doctor` which checks Graphviz, MkDocs, Azure CLI/Bicep, and `OPENAI_API_KEY` before scans. Capture its output in this log before major runs:

  ```bash
  autodocx doctor | tee analysis/wsl_readiness_doctor.log
  ```

- Bicep extraction and Graphviz rendering now emit explicit warnings when required binaries are missing so gaps are obvious in CLI output.
- MkDocs renderer short-circuits with a yellow warning if the `mkdocs` CLI is absent, preventing silent failures.

Record future verification runs (doctor output, sample scan commands, etc.) below with timestamps to keep a traceable history.
