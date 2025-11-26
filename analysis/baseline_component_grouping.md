# Baseline: Component Grouping (2025-11-13)

- Command:
  ```bash
  python -c "import json, pathlib, collections
  sir_dir = pathlib.Path('out/sir')
  counts = collections.Counter()
  for path in sir_dir.glob('*.json'):
      data = json.loads(path.read_text())
      comp = data.get('component_or_service') or data.get('props',{}).get('component_or_service') or ''
      counts[comp]+=1
  print('Component counts:', counts)
  missing = counts.get('', 0)
  print('SIRs without component:', missing, 'of', sum(counts.values()))"
  ```

- Output:
  ```
  Component counts: Counter({'': 70})
  SIRs without component: 70 of 70
  ```

Conclusion: All SIRs lack a `component_or_service`, confirming the hypothesis for Workstream 1.
