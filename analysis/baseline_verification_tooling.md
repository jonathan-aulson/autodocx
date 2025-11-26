# Baseline: Verification Tooling (2025-11-13)

Current workflow to inspect scan results requires manual Python snippets:

```bash
python -c "import json, pathlib
sir_dir = pathlib.Path('out/sir')
print(len(list(sir_dir.glob('*.json'))))"
```

There is no CLI command that reports component distribution, signal kinds, or connector usage.
This manual process is error-prone and slow, motivating Workstream 6.
