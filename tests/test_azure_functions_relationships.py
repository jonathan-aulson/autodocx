from __future__ import annotations

from pathlib import Path

from autodocx.extractors.azure_functions import AzureFunctionsExtractor


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
        [QueueOutput("processed-data")] out string queueItem)
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
