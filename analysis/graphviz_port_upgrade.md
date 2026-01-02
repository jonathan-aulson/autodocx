# Graphviz Port Rendering Upgrade Plan

Our objective is to eliminate Graphviz “unrecognized port” warnings by designing a deliberate port schema for workflow diagrams, enriching the exported metadata so ports are meaningful, and updating the renderer to honor that metadata. This plan tracks the work end-to-end with checkboxes so we can record progress as each deliverable lands.

## Phase 1 – Schema & Metadata Readiness

- [x] **Inventory control constructs & port needs.** Document which workflow nodes benefit from explicit ports (e.g., Logic Apps `scope`, `switch`, `condition`, Power Automate `Apply to each`, custom scopes) and what metadata we already capture (`steps[*].control_type`, `control_edges[*].branch`, `run_after`, `relationships`). Map the gaps (per-branch labeling, directionality) so we know what to extract. _Result:_ Existing signals already expose `control_edges.parent/branch/children` plus `steps[*].branch` hints, giving us everything needed for branch-level ports without extractor changes.
- [x] **Define the port schema & data contract.** Specify the new JSON fields for exported graphs: each node may carry `ports: [{"name": "branch_success", "label": "Success", "direction": "out"}]`, edges may reference `source_port` / `target_port`, and we’ll reserve default ports (`in_main`, `out_default`) for nodes without special handling. _Result:_ Schema appended below (“Port Metadata Contract”) so downstream tooling knows how to read/write these fields.
- [x] **Metadata enrichment rules.** Design how we’ll populate `ports` purely from the existing signal metadata, and call out any extractor gaps that require follow-up (e.g., if a connector doesn’t emit `control_type`, we fall back to a generic port). _Result:_ Ports derive from three sources: (1) implicit defaults for every node (`in_main`, `out_default`), (2) named branch ports for each `control_edges[*]` entry using a slug of `branch`, and (3) synthetic ports for “external” relationship nodes (`in_external` / `out_external`). If an extractor can’t emit `control_edges`, we track that component in the “Follow-ups” table once Phase 4 runs.

## Phase 2 – Export Pipeline Enhancements

- [x] **Normalize node identifiers.** Update `autodocx/visuals/flow_export.py` so `_node_id` emits colon-free IDs (e.g., `step__apply_to_each`) and attaches the new `ports` collection per node. _Delivered via `_node_id` + `_default_ports` helpers (see commit in `flow_export.py`)._
- [x] **Annotate edges with port usage.** While exporting control edges and `run_after` links, set `source_port` / `target_port` according to the schema (branch edges map to their branch-named port; sequential edges hit `out_default → in_main`). _Branch/sequence/external edges now include the metadata._
- [x] **Unit tests for metadata.** Add focused tests that build a synthetic workflow signal and assert the exported JSON includes the expected ports + edge references. _`tests/test_flow_export_ports.py` covers the new JSON shape._

## Phase 3 – Renderer & Workflow Diagram Upgrades

- [x] **Port-aware node rendering.** Teach `autodocx/visuals/flow_renderer.py` to render HTML/record-style nodes when `ports` are present, declaring each `PORT="..."` in the label so Graphviz recognizes them. _Implemented via `_build_node_attrs` and `_build_port_table` helpers._
- [x] **Edge emission with ports.** When `source_port` / `target_port` exist, emit `dot.edge(f"{src}:{port}", ...)`; default back to simple node IDs when ports are absent so legacy diagrams still work. _`_node_ref_with_port` now wires edges appropriately._
- [x] **Visual polish & regression tests.** Capture baseline DOT/SVG snippets (or run lightweight rendering tests) to ensure the ported nodes appear correctly and warnings disappear during `autodocx scan`. _`tests/test_flow_renderer_ports.py` asserts the DOT source contains the declared ports; full pipeline run pending in Phase 4 verification._

## Phase 4 – Documentation & Follow-up

- [x] **Update developer docs.** Amend `analysis/business_doc_upgrade.md` (or a new snippet) describing how to add new control constructs to the port schema, so future extractors know what metadata to provide. _Documented the contract + extractor guidance in `developer_onboarding_context.md` §8._
- [x] **Verify in practice.** Run the pipeline on Towne Park + BW samples, confirm no Graphviz port warnings remain, and note any connectors still lacking metadata so we can backfill extractor support. _`autodocx scan repos/bw-samples-master --out out/bw --debug` and `autodocx scan repos/Towne-Park-Billing-Source-Code --out out/towne --debug` completed without Graphviz warnings; control-node audit found no missing ports._

---

### Port Metadata Contract (reference for Phases 2 & 3)

```json5
{
  "nodes": [
    {
      "id": "step__scope-try",
      "name": "Scope: Try",
      "kind": "control",
      "ports": [
        {"name": "in_main", "label": "", "direction": "in"},
        {"name": "branch_success", "label": "Success", "direction": "out"},
        {"name": "branch_failure", "label": "Failure", "direction": "out"}
      ]
    }
  ],
  "edges": [
    {
      "source": "step__scope-try",
      "target": "step__respond",
      "kind": "branch",
      "source_port": "branch_success",
      "target_port": "in_main"
    }
  ]
}
```

Naming rules:

- Port names are lowercase slugs prefixed with their semantic bucket (`branch_`, `loop_`, `default_`).
- Every node implicitly exposes `in_main`/`out_default`; we only emit them when a node participates in a ported edge to keep JSON lean.
- Branch metadata comes from `control_edges[*].branch`; loop metadata comes from `steps[*].control_type == "foreach"` plus `steps[*].branch` for “after each” connectors.
