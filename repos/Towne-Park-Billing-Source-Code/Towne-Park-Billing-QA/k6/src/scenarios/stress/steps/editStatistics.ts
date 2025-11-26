import { HttpClient } from "@/core/http.client";
import { check } from "k6";
import { Counter, Rate, Trend } from "k6/metrics";

const editStatisticsPatchTime = new Trend("edit_statistics_patch_time", true);
const editStatisticsPatchSuccess = new Rate("edit_statistics_patch_success");
const editStatisticsPatchTotal = new Counter("edit_statistics_patch_total");
const editStatisticsPatchErrorCount = new Counter(
  "edit_statistics_patch_error_count"
);

export class EditStatistics {
  /**
   * Patch statistics using a caller-provided payload.
   * Pass `siteId` if you want guaranteed tagging (in case body doesn't include it).
   */
  async patch(client: HttpClient, body: any, siteId?: string): Promise<void> {
    if (!body || typeof body !== "object") {
      throw new Error("EditStatistics.patch: invalid or empty body");
    }

    const inferredSiteId =
      siteId ??
      body.siteId ??
      body.site_id ??
      body.siteID ??
      body.site?.id ??
      body.site?.siteId;

    const saveEndpoint = `/api/siteStatistics`;

    // If your API needs siteId in the body, uncomment the merge:
    // const jsonBody = JSON.stringify({ siteId: inferredSiteId, ...body });
    const jsonBody = JSON.stringify(body);

    editStatisticsPatchTotal.add(1);
    const start = Date.now();

    const response = await client.patch(saveEndpoint, jsonBody, {
      headers: { "Content-Type": "application/json" },
      tags: {
        operation: "edit_statistics_patch_only",
        step: "forecasting_flow",
        ...(inferredSiteId ? { siteId: inferredSiteId } : {}),
      },
    });

    const ok = check(response, {
      "editStatistics: status 200/204": (r) =>
        r.status === 200 || r.status === 204,
      "editStatistics: < 2s": (r) => r.timings.duration < 2000,
    });

    editStatisticsPatchTime.add(Date.now() - start, {
      status: String(response.status),
      ...(inferredSiteId ? { siteId: inferredSiteId } : {}),
    });
    editStatisticsPatchSuccess.add(ok ? 1 : 0);
    if (!ok) editStatisticsPatchErrorCount.add(1);
  }
}
