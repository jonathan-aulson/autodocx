from __future__ import annotations

import json
from pathlib import Path
from typing import Iterable

from jsonschema import Draft7Validator


SCHEMA_PATH = Path(__file__).resolve().parent / "schema" / "narrative_option1.json"


def validate_artifacts_file(path: Path) -> None:
    data = json.loads(Path(path).read_text(encoding="utf-8"))
    schema = json.loads(SCHEMA_PATH.read_text(encoding="utf-8"))
    validator = Draft7Validator(schema)
    errors = _format_errors(validator.iter_errors(data))
    if errors:
        raise ValueError("Artifact schema validation failed:\n" + "\n".join(errors))


def _format_errors(errors: Iterable) -> list[str]:
    formatted: list[str] = []
    for err in errors:
        location = "/".join(str(p) for p in err.path)
        formatted.append(f"- {location or 'artifact'}: {err.message}")
    return formatted
