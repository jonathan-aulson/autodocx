import { HttpClient } from "@/core/http.client";
import { check } from "k6";
import { Counter, Rate, Trend } from "k6/metrics";

const editOtherRevenuePatchTime = new Trend(
  "edit_otherrevenue_patch_time",
  true
);
const editOtherRevenuePatchSuccess = new Rate(
  "edit_otherrevenue_patch_success"
);
const editOtherRevenuePatchTotal = new Counter("edit_otherrevenue_patch_total");
const editOtherRevenuePatchErrorCount = new Counter(
  "edit_otherrevenue_patch_error_count"
);

export class EditOtherRevenue {
  /**
   * Patch Other Revenue using a caller-provided payload.
   * `siteId` optional; used only for tagging if provided/inferrable.
   */
  async patch(client: HttpClient, body: any, siteId?: string): Promise<void> {
    if (!body || typeof body !== "object") {
      throw new Error("EditOtherRevenue.patch: invalid or empty body");
    }

    const inferredSiteId =
      siteId ??
      body.siteId ??
      body.site_id ??
      body.siteID ??
      body.site?.id ??
      body.site?.siteId;

    const saveEndpoint = `/api/otherRevenue`; // match your backend route
    const jsonBody = JSON.stringify(body);

    editOtherRevenuePatchTotal.add(1);
    const start = Date.now();

    const response = await client.patch(saveEndpoint, jsonBody, {
      headers: { "Content-Type": "application/json" },
      tags: {
        operation: "edit_otherrevenue_patch_only",
        step: "forecasting_flow",
        ...(inferredSiteId ? { siteId: inferredSiteId } : {}),
      },
    });

    const ok = check(response, {
      "editOtherRevenue: status 200/204": (r) =>
        r.status === 200 || r.status === 204,
      "editOtherRevenue: < 2s": (r) => r.timings.duration < 2000,
    });

    editOtherRevenuePatchTime.add(Date.now() - start, {
      status: String(response.status),
      ...(inferredSiteId ? { siteId: inferredSiteId } : {}),
    });
    editOtherRevenuePatchSuccess.add(ok ? 1 : 0);
    if (!ok) editOtherRevenuePatchErrorCount.add(1);
  }
}
