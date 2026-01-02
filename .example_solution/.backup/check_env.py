#!/usr/bin/env python3
"""
check_env.py
- Verifies local environment for the BW auto-doc pipeline.
- Loads environment variables from a .env file (python-dotenv if available; otherwise a lightweight parser).
- Checks: Python version, required Python packages, Graphviz 'dot' on PATH,
  SIR schema exists, output directories are writable, optional OpenAI key.

Usage:
  python ./.roo/tools/bw/check_env.py
  python ./.roo/tools/bw/check_env.py --require-graphs --require-llm --json
  python ./.roo/tools/bw/check_env.py --env-file ./my.env
"""
import argparse
import json
import os
import platform
import shutil
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[3]
DEFAULT_ENV_FILE = REPO_ROOT / ".env"
SCHEMA_PATH = REPO_ROOT / ".roo" / "schemas" / "sir.schema.json"
OUT_DIRS = [
    REPO_ROOT / "out",
    REPO_ROOT / "out" / "sir",
    REPO_ROOT / "out" / "graphs",
    REPO_ROOT / "out" / "docs",
    REPO_ROOT / "out" / "logs",
]

# Import checks must use actual module names:
REQUIRED_PACKAGES = ["lxml", "click", "tqdm", "yaml", "networkx"]
OPTIONAL_PACKAGES = ["jsonschema", "openai"]  # stricter validation, LLM support


def load_env_file(env_file: Path, override: bool = False) -> bool:
    if not env_file or not Path(env_file).exists():
        return False
    try:
        import dotenv  # type: ignore
        dotenv.load_dotenv(dotenv_path=str(env_file), override=override)
        return True
    except Exception:
        pass
    try:
        for raw in Path(env_file).read_text(encoding="utf-8").splitlines():
            line = raw.strip()
            if not line or line.startswith("#"):
                continue
            if line.lower().startswith("export "):
                line = line[7:].strip()
            if "=" not in line:
                continue
            k, v = line.split("=", 1)
            k = k.strip()
            v = v.strip()
            if (v.startswith('"') and v.endswith('"')) or (v.startswith("'") and v.endswith("'")):
                v = v[1:-1]
            if override or k not in os.environ:
                os.environ[k] = v
        return True
    except Exception:
        return False


def check_python_version():
    ok = sys.version_info >= (3, 9)
    return ok, f"Python {sys.version.split()[0]} (need >= 3.9)"


def check_packages(pkgs):
    missing = []
    for p in pkgs:
        try:
            __import__(p)
        except Exception:
            missing.append(p)
    return missing


def check_dot_on_path():
    p = shutil.which("dot")
    return p is not None, p or ""


def ensure_dirs(dirs):
    msgs = []
    ok_all = True
    for d in dirs:
        try:
            d.mkdir(parents=True, exist_ok=True)
            test_file = d / ".write_test"
            test_file.write_text("ok", encoding="utf-8")
            test_file.unlink(missing_ok=True)
            msgs.append((str(d), True, "writable"))
        except Exception as e:
            msgs.append((str(d), False, f"not writable: {e}"))
            ok_all = False
    return ok_all, msgs


def suggest_graphviz_install():
    os_name = platform.system().lower()
    if "windows" in os_name:
        return ("Install Graphviz via installer or a portable ZIP:\n"
                "  1) Download: https://graphviz.org/download/\n"
                "  2) Ensure PATH includes: C:\\Program Files\\Graphviz\\bin (or your portable bin)\n"
                "Verify: open a new terminal, run: dot -V")
    elif "darwin" in os_name or "mac" in os_name:
        return ("Install with Homebrew:\n"
                "  brew install graphviz\n"
                "Verify: dot -V")
    else:
        return ("Install with your package manager (Debian/Ubuntu example):\n"
                "  sudo apt-get update && sudo apt-get install -y graphviz\n"
                "Verify: dot -V")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--require-graphs", dest="require_graphs", action="store_true",
                        help="Fail if Graphviz 'dot' is missing")
    parser.add_argument("--require-llm", dest="require_llm", action="store_true",
                        help="Fail if OPENAI_API_KEY is missing")
    parser.add_argument("--json", action="store_true",
                        help="Emit machine-readable JSON status")
    parser.add_argument("--env-file", type=str, default=str(DEFAULT_ENV_FILE),
                        help="Path to .env file (default: repo-root/.env)")
    args = parser.parse_args()

    env_loaded = load_env_file(Path(args.env_file), override=False)

    status = {"ok": True, "checks": [], "env_file": args.env_file, "env_loaded": env_loaded}

    ok_py, py_msg = check_python_version()
    status["checks"].append({"name": "python_version", "ok": ok_py, "detail": py_msg})
    status["ok"] &= ok_py

    missing_req = check_packages(REQUIRED_PACKAGES)
    status["checks"].append({"name": "python_packages_required", "ok": len(missing_req) == 0,
                             "detail": "missing: " + ", ".join(missing_req) if missing_req else "all present"})
    status["ok"] &= (len(missing_req) == 0)

    missing_opt = check_packages(OPTIONAL_PACKAGES)
    status["checks"].append({"name": "python_packages_optional", "ok": True,
                             "detail": "optional missing: " + ", ".join(missing_opt) if missing_opt else "all optional present"})

    dot_ok, dot_path = check_dot_on_path()
    graph_detail = f"dot found at: {dot_path}" if dot_ok else "dot not found on PATH"
    status["checks"].append({"name": "graphviz_dot", "ok": (dot_ok or not args.require_graphs), "detail": graph_detail})
    if args.require_graphs and not dot_ok:
        status["ok"] = False
        status["graphviz_help"] = suggest_graphviz_install()

    schema_exists = SCHEMA_PATH.exists()
    status["checks"].append({"name": "sir_schema_exists", "ok": schema_exists, "detail": str(SCHEMA_PATH)})
    status["ok"] &= schema_exists

    out_ok, out_msgs = ensure_dirs(OUT_DIRS)
    status["checks"].append({"name": "out_dirs_writable", "ok": out_ok, "detail": out_msgs})
    status["ok"] &= out_ok

    api_key = os.environ.get("OPENAI_API_KEY") or ""
    api_ok = bool(api_key)
    status["checks"].append({"name": "openai_api_key", "ok": (api_ok or not args.require_llm),
                             "detail": "present" if api_ok else "missing"})
    if args.require_llm and not api_ok:
        status["ok"] = False
        status["llm_hint"] = "Add OPENAI_API_KEY to your .env or set it in your shell."

    if args.json:
        print(json.dumps(status, indent=2))
    else:
        print(f"[env] file: {args.env_file} {'LOADED' if env_loaded else 'not found (optional)'}")
        print(f"[python] {py_msg} {'OK' if ok_py else 'FAIL'}")
        print(f"[deps] required: {'OK' if not missing_req else 'MISSING: ' + ', '.join(missing_req)}")
        print(f"[deps] optional: {'OK' if not missing_opt else 'missing: ' + ', '.join(missing_opt)}")
        print(f"[graphviz] {graph_detail} {'OK' if (dot_ok or not args.require_graphs) else 'FAIL'}")
        print(f"[schema] {SCHEMA_PATH} {'OK' if schema_exists else 'MISSING'}")
        for d, ok, msg in out_msgs:
            print(f"[out] {d}: {msg} {'OK' if ok else 'FAIL'}")
        print(f"[llm] OPENAI_API_KEY {'present' if api_ok else 'missing'} "
              f"{'OK' if (api_ok or not args.require_llm) else 'FAIL'}")
        if not dot_ok:
            print("\nTip to install Graphviz:\n" + suggest_graphviz_install())

    sys.exit(0 if status["ok"] else 1)


if __name__ == "__main__":
    main()
