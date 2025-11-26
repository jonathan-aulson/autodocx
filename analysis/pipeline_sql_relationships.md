# Azure Pipelines & SQL Relationship Extraction (2025-11-15)

## Azure Pipelines

Command:

```bash
python - <<'PY'
import json, pathlib
target = pathlib.Path('out/sir/azure-pipelines-managed-92c677eb.json')
data = json.loads(target.read_text())
for rel in data["props"]["relationships"]:
    print(rel["operation"]["type"], "->", rel["target"]["kind"], rel["target"]["ref"])
PY
```

Output:

```
deploys_to -> environment STAGE
deploys_to -> environment STAGE
deploys_to -> environment $(PowerPlatformEnvironment)
deploys_to -> environment $(PowerPlatformEnvironment)
publishes -> artifact Solution Files
```

These relationships originate from task inputs (`PowerPlatformSPN`, `PublishBuildArtifacts@1`) and are attached to the pipeline signal so downstream tooling can render deployment sequences and artifact handoffs.

## SQL Foreign Keys

Command:

```bash
python - <<'PY'
import json, pathlib
sir_dir = pathlib.Path('out/sir')
for path in sir_dir.glob('*.json'):
    data = json.loads(path.read_text())
    if data.get("kind") == "db" and data["props"].get("relationships"):
        print(path.name, data["props"]["relationships"])
PY
```

When `CREATE TABLE` statements include `FOREIGN KEY ... REFERENCES ...`, the extractor emits `depends_on` relationships linking child tables to their parents with SQL evidence anchors. (This dataset does not include such references yet, so the loop prints nothing, but the logic is in place and exercised by `tests/test_sql_migrations_relationships.py`.)
