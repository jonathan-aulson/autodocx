from __future__ import annotations
from pathlib import Path
from typing import Iterable, List, Dict, Any
import hashlib
import re
from autodocx.types import Signal

class SQLMigrationsExtractor:
    name = "sql_migrations"
    patterns = ["**/*.sql"]
    CREATE_RE = re.compile(r"(?is)\bcreate\s+table\s+([a-zA-Z0-9_.\[\]\"]+)")
    FOREIGN_DETAIL_RE = re.compile(
        r"(?is)foreign\s+key\s*\((?P<src>[^\)]*)\)\s+references\s+(?P<target>[a-zA-Z0-9_.\[\]\"]+)(?:\s*\((?P<dest>[^\)]*)\))?"
    )

    def detect(self, repo: Path) -> bool:
        return any(repo.glob("**/*.sql"))

    def discover(self, repo: Path) -> Iterable[Path]:
        yield from repo.glob("**/*.sql")

    def extract(self, path: Path) -> Iterable[Signal]:
        signals: List[Signal] = []
        try:
            txt = path.read_text(encoding="utf-8", errors="ignore")
            matches = list(self.CREATE_RE.finditer(txt))
            for idx, match in enumerate(matches):
                table = match.group(1).strip('"[]')
                start = match.start()
                end = matches[idx + 1].start() if idx + 1 < len(matches) else len(txt)
                block = txt[start:end]
                foreign_keys = self._extract_foreign_keys(block, table)
                relationships = self._relationships_from_foreign_keys(foreign_keys, table, path)
                columns = self._parse_columns(block)
                ln = txt[:match.start()].count("\n") + 1
                props: Dict[str, Any] = {"table": table, "file": str(path)}
                if relationships:
                    props["relationships"] = relationships
                if foreign_keys:
                    props["foreign_keys"] = foreign_keys
                if columns:
                    props["columns"] = columns
                    props["inputs_example"] = {"columns": [c["name"] for c in columns[:5]]}
                    props["data_samples"] = [self._mock_data_sample(columns)]
                datastore_refs = {table}
                process_refs = set()
                for fk in foreign_keys:
                    ref_table = (fk.get("references") or {}).get("table")
                    if ref_table:
                        datastore_refs.add(ref_table)
                        process_refs.add(ref_table)
                props["datasource_tables"] = sorted(datastore_refs)
                if process_refs:
                    props["process_calls"] = sorted(process_refs)
                identifier_hints = self._identifier_hints_from_columns(columns)
                if identifier_hints:
                    props["identifier_hints"] = identifier_hints
                signals.append(Signal(kind="db", props=props, evidence=[f"{path}:{ln}-{ln+20}"], subscores={"parsed": 1.0, "schema_evidence": 0.6}))
        except Exception as e:
            signals.append(Signal(kind="doc", props={"name": path.name, "file": str(path), "note": f"SQL parse error: {e}"}, evidence=[f"{path}:1-1"], subscores={"parsed": 0.1}))
        return signals

    def _extract_foreign_keys(self, block: str, table: str) -> List[Dict[str, Any]]:
        fks: List[Dict[str, Any]] = []
        for match in self.FOREIGN_DETAIL_RE.finditer(block):
            target = (match.group("target") or "").strip('[]"')
            src_cols = self._split_fk_columns(match.group("src"))
            dest_cols = self._split_fk_columns(match.group("dest"))
            if not target:
                continue
            fks.append({
                "table": table,
                "columns": src_cols,
                "references": {
                    "table": target,
                    "columns": dest_cols,
                },
            })
        return fks

    def _split_fk_columns(self, raw: Any) -> List[str]:
        if not raw:
            return []
        cols = []
        for col in str(raw).split(","):
            stripped = col.strip().strip('[]"')
            if stripped:
                cols.append(stripped)
        return cols

    def _relationships_from_foreign_keys(self, foreign_keys: List[Dict[str, Any]], table: str, path: Path) -> List[Dict[str, Any]]:
        relationships: List[Dict[str, Any]] = []
        for fk in foreign_keys or []:
            target = (fk.get("references") or {}).get("table") or ""
            if not target:
                continue
            digest = hashlib.sha1(f"{table}-{target}".encode("utf-8")).hexdigest()[:8]
            relationships.append({
                "id": f"{table}_fk_{digest}",
                "source": {"type": "table", "name": table},
                "target": {"kind": "sql_table", "ref": target, "display": target},
                "operation": {"type": "depends_on", "verb": "", "crud": "", "protocol": "sql"},
                "connector": "foreign_key",
                "direction": "outbound",
                "context": {"columns": fk.get("columns"), "references": fk.get("references", {}).get("columns")},
                "roles": ["data.relates"],
                "evidence": [f"{path}:fk"],
                "confidence": 0.8,
            })
        return relationships

    def _parse_columns(self, block: str) -> List[Dict[str, str]]:
        cols = []
        inside = block.split("(", 1)
        if len(inside) < 2:
            return cols
        columns_text = inside[1]
        for line in columns_text.splitlines():
            line = line.strip().rstrip(",")
            if not line or line.upper().startswith(("CONSTRAINT", "PRIMARY", "FOREIGN")):
                continue
            if line.startswith(")") or line.startswith("];"):
                continue
            parts = line.split()
            if not parts:
                continue
            col_name = parts[0].strip('"[]')
            definition = " ".join(parts[1:])
            cols.append({"name": col_name, "definition": definition})
        return cols

    def _identifier_hints_from_columns(self, columns: List[Dict[str, str]]) -> List[str]:
        hints: List[str] = []
        for col in columns or []:
            name = col.get("name")
            if not name:
                continue
            lower = name.lower()
            if lower.endswith(("id", "key", "code", "number")) and name not in hints:
                hints.append(name)
        return hints

    def _mock_data_sample(self, columns: List[Dict[str, str]]) -> Dict[str, Any]:
        sample = {}
        for col in columns[:5]:
            definition = (col.get("definition") or "").strip()
            split = definition.split()
            type_hint = split[0].lower() if split else "value"
            sample[col["name"]] = f"<{type_hint}>"
        return {"example_row": sample}
