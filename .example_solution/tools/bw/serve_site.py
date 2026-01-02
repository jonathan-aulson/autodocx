#!/usr/bin/env python3
# tools/serve_site.py
# Bootstrap MkDocs (if needed) and run a local dev server.

import subprocess, sys
from pathlib import Path

HERE = Path(__file__).resolve()
REPO_ROOT = HERE.parents[1]
BOOT = REPO_ROOT / "tools" / "mkdocs_bootstrap.py"
MKDOCS = "mkdocs"

def main():
    # always (re)bootstrap to heal after out/ wipes
    subprocess.run([sys.executable, str(BOOT), "--force-home"], check=False)

    # serve with auto-reload
    # (MkDocs’ dev server auto-reloads on changes. :contentReference[oaicite:2]{index=2})
    subprocess.run([MKDOCS, "serve", "-f", str(REPO_ROOT / "mkdocs.yml")], check=False)

if __name__ == "__main__":
    sys.exit(main())
