# Post-change Component Grouping (2025-11-13)

- Command:
  ```bash
  python -c "import json, pathlib, collections
  sir_dir = pathlib.Path('out/sir')
  counts = collections.Counter()
  for path in sir_dir.glob('*.json'):
      data = json.loads(path.read_text())
      comp = data.get('component_or_service')
      counts[comp]+=1
  print('Component counts:', counts)
  missing = counts.get(None, 0) + counts.get('', 0)
  print('SIRs without component:', missing, 'of', sum(counts.values()))"
  ```

- Output:
  ```
  Component counts: Counter({'BillingSystem': 29, 'BillingSystemMonitoring': 2, 'cline_rules': 1, 'BillingSystemEnterpriseDataRetrieval': 1})
  SIRs without component: 0 of 33
  ```

Conclusion: Component identifiers are now populated, producing four distinct groups.
