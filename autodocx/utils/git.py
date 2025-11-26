from __future__ import annotations
from pathlib import Path
def is_git_repo(root: Path) -> bool:
    return (root / ".git").exists()
