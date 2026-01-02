from __future__ import annotations
from pathlib import Path
from typing import Iterable, List, Dict, Any
import json, subprocess, yaml, shutil, tempfile, os
from autodocx.types import Signal

try:
    from rich import print as rprint
except Exception:  # pragma: no cover - fallback when rich unavailable
    def rprint(msg):
        print(msg)

class BicepExtractor:
    name = "bicep"
    patterns = ["**/*.bicep"]
    _warnings_emitted: set[str] = set()

    def detect(self, repo: Path) -> bool:
        return any(repo.glob("**/*.bicep"))

    def discover(self, repo: Path) -> Iterable[Path]:
        yield from repo.glob("**/*.bicep")

    def _warn_once(self, key: str, message: str) -> None:
        if key in self._warnings_emitted:
            return
        self._warnings_emitted.add(key)
        try:
            rprint(f"[yellow]{message}[/yellow]")
        except Exception:
            print(message)

    def _build_to_arm(self, path: Path) -> Dict[str, Any] | None:
        # Try az bicep; fallback to bicep CLI; else None
        cmds = [
            ["az", "bicep", "build", "--file", str(path)],
            ["bicep", "build", str(path)]
        ]
        available_cmds = [cmd for cmd in cmds if shutil.which(cmd[0])]
        if not available_cmds:
            self._warn_once(
                "bicep_cli_missing",
                "Bicep extractor skipped compilation because neither 'az bicep' nor 'bicep' was found on PATH. "
                "Run ./scripts/setup_wsl.sh or install the CLI manually to enable richer infra signals."
            )
            return None
        for cmd in available_cmds:
            tmp_path = None
            try:
                with tempfile.NamedTemporaryFile("w+", delete=False, suffix=".json") as tmp:
                    tmp_path = tmp.name
                cmd_with_out = cmd + ["--outfile", tmp_path]
                proc = subprocess.run(cmd_with_out, check=True, capture_output=True, text=True)
                if proc.stderr.strip():
                    self._warn_once(f"bicep_warning_{path}", proc.stderr.strip())
                text = Path(tmp_path).read_text(encoding="utf-8")
                doc = json.loads(text)
                return doc
            except subprocess.CalledProcessError as exc:
                message = (exc.stderr or exc.stdout or "").strip()
                if message:
                    self._warn_once(f"bicep_cmd_error_{path}", message)
                continue
            except json.JSONDecodeError as exc:
                self._warn_once(
                    f"bicep_json_error_{path}",
                    f"Bicep extractor produced invalid JSON for {path.name}: {exc}",
                )
                continue
            finally:
                if tmp_path:
                    try:
                        os.unlink(tmp_path)
                    except OSError:
                        pass
        self._warn_once(
            "bicep_compile_failed",
            f"Bicep extractor could not compile {path.name}; falling back to doc-only signal."
        )
        return None

    def extract(self, path: Path) -> Iterable[Signal]:
        signals: List[Signal] = []
        try:
            arm = self._build_to_arm(path)
            if not isinstance(arm, dict):
                # Best-effort mark as infra doc
                signals.append(Signal(kind="doc", props={"name": path.name, "file": str(path), "note": "Bicep compile failed or CLI not available"}, evidence=[f"{path}:1-1"], subscores={"parsed": 0.2}))
                return signals

            # Generic ARM resource extraction
            resources = arm.get("resources") or []
            for res in resources:
                rtype = res.get("type")
                name = res.get("name")
                signals.append(Signal(kind="infra", props={"resource_type": rtype, "name": name, "file": str(path)}, evidence=[f"{path}:resources:{rtype}.{name}"], subscores={"parsed": 1.0}))

                # Logic Apps in ARM → parse workflows' definition (same as LogicApps)
                if rtype == "Microsoft.Logic/workflows":
                    definition = (res.get("properties") or {}).get("definition") or {}
                    if isinstance(definition, dict) and isinstance(definition.get("triggers"), dict) and isinstance(definition.get("actions"), dict):
                        # Basic parse (summary only)
                        triggers = [{"name": n, "type": (b or {}).get("type")} for n,b in (definition.get("triggers") or {}).items()]
                        steps = []
                        for an, node in (definition.get("actions") or {}).items():
                            atype = (node or {}).get("type")
                            inputs = (node or {}).get("inputs") or {}
                            conn = (((inputs.get("host") or {}).get("connection") or {}).get("name") or "").strip()
                            method = (inputs.get("method") or "")
                            uri = inputs.get("uri") or inputs.get("path")
                            steps.append({"name": an, "type": atype, "connector": conn, "method": method, "url_or_path": uri})
                        content_version = (res.get("properties") or {}).get("definition", {}).get("contentVersion") or ""
                        signals.append(Signal(
                            kind="workflow",
                            props={"name": name, "file": str(path), "engine": "logicapps", "wf_kind": "logicapps_consumption", "version": content_version, "triggers": triggers, "steps": steps, "calls_flows": []},
                            evidence=[f"{path}:resources:Microsoft.Logic/workflows:{name}"],
                            subscores={"parsed": 1.0, "schema_evidence": 0.4}
                        ))
        except Exception as e:
            signals.append(Signal(kind="doc", props={"name": path.name, "file": str(path), "note": f"Bicep parse error: {e}"}, evidence=[f"{path}:1-1"], subscores={"parsed": 0.1}))
        return signals
