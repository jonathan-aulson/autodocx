# WSL Enablement ToDo

Use GitHub-style checkboxes (`[ ]` vs `[x]`) to track progress. Whenever a task is finished, flip its box and, if helpful, jot a short note or evidence path next to it.

- [x] Document WSL setup guidance (packages, virtualenv activation, repo location) in `README.md` and `developer_onboarding_context.md`.
- [x] Add an automated bootstrap script (`scripts/setup_wsl.sh`) that installs required Debian packages plus optional Azure/Bicep tooling.
- [x] Introduce an `autodocx doctor` subcommand that checks for Graphviz, MkDocs, Azure CLI/Bicep, and `OPENAI_API_KEY`.
- [x] Gate the aggressive `__pycache__` cleanup behind an environment toggle so WSL users on `/mnt/c` can skip slow deletes.
- [x] Emit clear warnings when Bicep compilation or Graphviz diagramming is skipped because the CLIs/binaries are missing.
- [x] Capture a lightweight `analysis/wsl_readiness.md` log describing how to verify the above on future runs.
