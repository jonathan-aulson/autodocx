# Relationships Baseline (2025-11-14)

- Command:
  ```bash
  python -c "import json, pathlib
  sir_dir=pathlib.Path('out/sir')
  info=[]
  for path in sorted(sir_dir.glob('*.json')):
      data=json.loads(path.read_text())
      if data.get('kind')=='workflow':
          props=data.get('props') or {}
          info.append({
              'sir': path.name,
              'has_relations': 'relationships' in props,
              'step_count': len(props.get('steps') or []),
              'connector_samples': sorted({(step.get('connector') or '').lower()
                                           for step in (props.get('steps') or [])
                                           if step.get('connector')})[:3]
          })
      if len(info) >= 3:
          break
  print(info)"
  ```
- Output:
  ```
  [{'sir': 'BellServiceFeeChildFlow-42A88C06-CE84-EF11-AC20-0022480A57AC.json.json', 'has_relations': False, 'step_count': 12, 'connector_samples': ['compose', 'if', 'initializevariable']},
   {'sir': 'BillableAccountsChildFlow20241108-0E80E27B-0B9E-EF11-8A6A-0022480A57AC.json.json', 'has_relations': False, 'step_count': 14, 'connector_samples': ['compose', 'if', 'initializevariable']},
   {'sir': 'CapacityAlertFlow-CC0D1317-DC4C-F011-877A-002248029144.json.json', 'has_relations': False, 'step_count': 8, 'connector_samples': ['compose', 'foreach', 'initializevariable']}]
  ```

**Conclusion:** Even though workflows list multiple steps and connectors, no `relationships` metadata exists. The LLM currently receives only a flat step list, confirming the hypothesis that richer relationships must be extracted upstream.
