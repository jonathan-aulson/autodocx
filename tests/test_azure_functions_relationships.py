from __future__ import annotations

from pathlib import Path

from autodocx.extractors.azure_functions import AzureFunctionsExtractor
from autodocx.scaffold.signal_scaffold import build_scaffold


def test_azure_functions_extractor_emits_relationships(tmp_path: Path) -> None:
    source = tmp_path / "GetData.cs"
    source.write_text(
        """
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

public static class GetData
{
    [Function("GetData")]
    public static void Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "data/{id}")] HttpRequestData req,
        [QueueOutput("processed-data")] out string queueItem,
        [BlobOutput("files/{id}", Connection = "Storage")] out string blobOutput)
    {
    }
}
""",
        encoding="utf-8",
    )

    extractor = AzureFunctionsExtractor()
    signals = list(extractor.extract(source))
    assert signals, "expected signals from C# HttpTrigger"
    route_signal = next(sig for sig in signals if sig.props.get("method") == "GET")
    relationships = route_signal.props.get("relationships") or []
    target_kinds = {rel["target"]["kind"] for rel in relationships}
    assert "function" in target_kinds, "should capture inbound HTTP trigger"
    assert "queue" in target_kinds, "should capture queue output binding"
    triggers = route_signal.props.get("triggers") or []
    assert triggers and triggers[0].get("path") == "/data/{id}"
    steps = route_signal.props.get("steps") or []
    assert any(step.get("connector", "").startswith("queue") for step in steps)
    service_deps = route_signal.props.get("service_dependencies") or []
    assert "processed-data" in service_deps
    datastores = route_signal.props.get("datasource_tables") or []
    assert any(ds.startswith("files/") for ds in datastores)
    scaffold = build_scaffold(route_signal)
    assert scaffold["io_summary"]["identifiers"] is not None
    assert scaffold["dependencies"]["datastores"]
    assert scaffold["dependencies"]["processes"]
