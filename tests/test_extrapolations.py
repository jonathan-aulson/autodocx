from autodocx.narratives.extrapolations import extrapolate_context


def test_extrapolate_context_generates_hypotheses() -> None:
    sir = {
        "name": "movies.search.FetchResults",
        "business_scaffold": {
            "dependencies": {
                "processes": ["movies.search.LookupCatalog", "movies.search.SortResults"],
                "datastores": ["MoviesDB"],
                "services": ["queue/results"],
            },
            "interfaces": [{"kind": "REST", "method": "GET", "endpoint": "/movies"}],
        },
    }
    interdeps_slice = {
        "family_peers": ["movies.search.SortResults"],
        "shared_datastores_with": ["movies.search.SortResults"],
        "shared_identifiers_with": ["movies.search.LookupCatalog"],
    }
    hypotheses = extrapolate_context(sir, interdeps_slice)
    texts = " ".join(h["hypothesis"] for h in hypotheses)
    assert "Search" in texts or "search" in texts.lower()
    assert any("datastore" in h["rationale"].lower() for h in hypotheses)
