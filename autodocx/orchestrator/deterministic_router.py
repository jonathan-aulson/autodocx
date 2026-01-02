from __future__ import annotations

from pathlib import Path
from typing import Dict, Iterable, List, Sequence, Set, Tuple


def plan_extractions(
    roots: Iterable[Path],
    extractors: Sequence[object],
    packaging_lookup: Dict[Path, Dict[str, str]] | None = None,
) -> Tuple[Dict[Path, List[str]], List[str]]:
    """
    Deterministically assign files to extractors by running each extractor's discover()
    against every scan root. Returns (assignments, errors) where assignments maps absolute
    file paths to the list of extractor names that should process them.
    """
    assignments: Dict[Path, List[str]] = {}
    errors: List[str] = []
    resolved_roots = [Path(root).resolve() for root in roots]

    for extractor in extractors:
        name = getattr(extractor, "name", extractor.__class__.__name__)
        seen: Set[Path] = set()
        for root in resolved_roots:
            try:
                discovered = extractor.discover(root)
            except Exception as exc:  # pragma: no cover - defensive guard
                errors.append(f"{name}@{root}: {exc}")
                continue

            for candidate in discovered or []:
                cand_path = Path(candidate)
                if not cand_path.is_absolute():
                    cand_path = (root / cand_path).resolve()
                else:
                    cand_path = cand_path.resolve()
                if cand_path in seen:
                    continue
                if not cand_path.is_file():
                    continue
                seen.add(cand_path)
                assignments.setdefault(cand_path, []).append(name)

    # Optionally sort assignments to prioritize module-aware extraction ordering
    if packaging_lookup:
        assignments = dict(
            sorted(
                assignments.items(),
                key=lambda kv: (
                    packaging_lookup.get(kv[0], {}).get("module") or "",
                    str(kv[0]),
                ),
            )
        )

    return assignments, errors
