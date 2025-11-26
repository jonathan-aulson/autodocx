from __future__ import annotations

from pathlib import Path

from autodocx.extractors.sql_migrations import SQLMigrationsExtractor


SQL_TEXT = """
CREATE TABLE dbo.Parent (
    Id INT PRIMARY KEY
);

CREATE TABLE dbo.Child (
    Id INT PRIMARY KEY,
    ParentId INT,
    CONSTRAINT FK_Child_Parent FOREIGN KEY (ParentId) REFERENCES dbo.Parent(Id)
);
"""


def test_sql_migrations_extractor_relationships(tmp_path: Path) -> None:
    sql_path = tmp_path / "schema.sql"
    sql_path.write_text(SQL_TEXT, encoding="utf-8")

    extractor = SQLMigrationsExtractor()
    signals = list(extractor.extract(sql_path))
    child_signal = next(sig for sig in signals if sig.props.get("table") == "dbo.Child")
    relationships = child_signal.props.get("relationships") or []
    assert relationships, "Expected foreign key relationship"
    assert relationships[0]["target"]["ref"] == "dbo.Parent"
