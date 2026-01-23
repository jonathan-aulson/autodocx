# Business Doc Upgrade – Verification Workflow

This workflow documents how to validate documentation/output quality against golden fixtures so regressions are caught early.

## Prereqs
- `.venv` with project deps installed (`pip install -e .` plus extras you need for tests).
- Graphviz installed for deterministic diagrams.
- `out/fixtures/bw-golden/` populated (generated via `autodocx_cli` scan on `repos/bw-samples-master`).

## Generate/refresh goldens
```bash
python -m autodocx_cli scan ./repos/bw-samples-master/bw-samples-master \
  --out out \
  --mkdocs-build \
  --llm-rollup
# layout helper mirrors outputs into out/fixtures/bw-golden (signals/docs/diagrams/manifests)
```

## Quick health checks
```bash
# Ensure golden fixtures are present
python - <<'PY'
from pathlib import Path
base = Path("out/fixtures/bw-golden")
assert base.exists(), "Missing bw-golden fixtures"
assert (base / "signals" / "sir_v2").exists(), "Missing sir_v2 in fixtures"
assert (base / "docs" / "curated").exists(), "Missing curated docs in fixtures"
print("Golden fixtures located:", base)
PY

# Run regression tests (includes golden fixture presence checks)
python -m pytest tests/test_golden_bw_fixtures.py -q
```

## Visual sanity
- Open `out/diagrams/deterministic_svg` and `out/diagrams/llm_svg` (or fixture equivalents) and spot-check a couple SVGs.
- Browse `out/docs/curated/` (or fixture copy) to confirm diagrams/links resolve.

## What to do on failure
- If fixtures are missing: regenerate goldens (command above) and commit updated fixture snapshots.
- If tests fail on content: diff the corresponding fixture files under `out/fixtures/bw-golden` vs. current `out/` outputs to identify regressions; update code or refresh goldens intentionally with a clear note in the commit.
