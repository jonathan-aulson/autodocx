from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path
from typing import List, Dict

from autodocx.utils.environment import load_project_dotenv

load_project_dotenv()

FIXTURE_REPOS: List[Dict[str, str]] = [
    {"name": "bw-samples-master", "path": "repos/bw-samples-master/bw-samples-master"},
    {"name": "powerbuilder-rest-client", "path": "repos/PowerBuilder-RestClient-Example-master/PowerBuilder-RestClient-Example-master"},
    {"name": "powerbuilder-ribbon", "path": "repos/PowerBuilder-RibbonBar-Example-main/PowerBuilder-RibbonBar-Example-main"},
    {"name": "powerbuilder-powerserver-console", "path": "repos/PowerServer-Console-PB-Example-main/PowerServer-Console-PB-Example-main"},
    {"name": "powerbuilder-2017", "path": "repos/powerbuilder-2017-master/2017-master"},
]


def run_scan(repo_path: Path, out_dir: Path, debug: bool) -> None:
    cmd = [sys.executable, "-m", "autodocx_cli", "scan", str(repo_path), "--out", str(out_dir)]
    if debug:
        cmd.append("--debug")
    print(f"\n[fixture] Running {' '.join(cmd)}")
    subprocess.run(cmd, check=True)


def main() -> None:
    parser = argparse.ArgumentParser(description="Run fixture scans for BW/PowerBuilder sample repos")
    parser.add_argument("--debug", action="store_true", help="Pass --debug to autodocx scans")
    parser.add_argument("--skip-existing", action="store_true", help="Skip scans whose output folder already exists")
    args = parser.parse_args()

    out_root = Path("out/fixtures").resolve()
    out_root.mkdir(parents=True, exist_ok=True)

    for repo in FIXTURE_REPOS:
        repo_path = Path(repo["path"]).resolve()
        if not repo_path.exists():
            print(f"[fixture] Skipping {repo['name']} (missing path {repo_path})")
            continue
        target = out_root / repo["name"]
        if args.skip_existing and target.exists():
            print(f"[fixture] Skipping {repo['name']} (output exists at {target})")
            continue
        target.mkdir(parents=True, exist_ok=True)
        run_scan(repo_path, target, args.debug)


if __name__ == "__main__":
    main()
