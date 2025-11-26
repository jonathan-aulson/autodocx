# Baseline: Graph Health (2025-11-13)

- Command:
  ```bash
  python -c "import json, pathlib, networkx as nx
  graph = json.loads(pathlib.Path('out/graph.json').read_text())
  G = nx.DiGraph()
  for n in graph.get('nodes', []):
      G.add_node(n['id'])
  for e in graph.get('edges', []):
      G.add_edge(e['source'], e['target'])
  print('Weakly connected components:', nx.number_weakly_connected_components(G))
  print('Total nodes:', G.number_of_nodes(), 'edges:', G.number_of_edges())"
  ```
- Output:
  ```
  Weakly connected components: 33
  Total nodes: 33 edges: 0
  ```

- Command:
  ```bash
  python -c "import json, pathlib
  sir_dir = pathlib.Path('out/sir')
  total = 0
  infinite = 0
  for path in sir_dir.glob('*.json'):
      total += 1
      data = json.loads(path.read_text())
      dist = (data.get('graph_features') or {}).get('nearest_marker_distance')
      if dist in (None, float('inf')) or str(dist).lower() == 'inf':
          infinite += 1
  print('SIRs with infinite/none nearest distance:', infinite, 'of', total)"
  ```
- Output:
  ```
  SIRs with infinite/none nearest distance: 30 of 33
  ```

Conclusion: the transient graph lacks edges and distance features are unusable, matching the Workstream 2 hypothesis.
