# Hierarchy Normalization Plan

Goal: Align pipeline outputs to the target hierarchy (repo → constellation → family/module → component → process → interface → resource → artifact) and generate docs accordingly.

## Plan
- [x] Define taxonomy and ownership rules (component detection, blacklist/whitelist, artifact → owner mapping).
- [x] Update extractors to emit ownership metadata (component/family/constellation) for processes, interfaces, resources, and artifacts.
- [x] Enrich interdependencies and doc_context with hierarchy blocks (repo, constellations, families, components, processes, interfaces, resources, artifacts).
- [x] Update renderer/prompt to render hierarchical sections and embed processes/interfaces under components; add packaging/artifacts sections.
- [x] Adjust doc planner to generate repo/constellation/family/component/process/interface pages; skip component pages for artifact-only items.
- [x] Add regression tests/linters for hierarchy (no artifact-only components, required sections present, ownership links present, interdep edges populated).
- [ ] Migrate existing outputs to the new hierarchy (move artifact pages into owners’ packaging/artifacts sections; regen docs/nav).
- [ ] Run full scan on BW sample repo and verify gates; raise coverage thresholds if healthy.
