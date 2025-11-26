import { HttpClient } from "@/core/http.client";
import { check } from "k6";
import { Counter, Rate, Trend } from "k6/metrics";

const editParkingRatesPatchTime = new Trend(
  "edit_parkingrates_patch_time",
  true
);
const editParkingRatesPatchSuccess = new Rate(
  "edit_parkingrates_patch_success"
);
const editParkingRatesPatchTotal = new Counter("edit_parkingrates_patch_total");
const editParkingRatesPatchErrorCount = new Counter(
  "edit_parkingrates_patch_error_count"
);

export class EditParkingRates {
  /**
   * Patch parking rates with a caller-provided payload.
   * `siteId` is optional; if present we tag metrics with it.
   */
  async patch(client: HttpClient, body: any, siteId?: string): Promise<void> {
    if (!body || typeof body !== "object") {
      throw new Error("EditParkingRates.patch: invalid or empty body");
    }

    const inferredSiteId =
      siteId ??
      body.siteId ??
      body.site_id ??
      body.siteID ??
      body.site?.id ??
      body.site?.siteId;

    // Adjust endpoint casing if your API differs
    const saveEndpoint = `/api/parkingRates`;
    const jsonBody = JSON.stringify(body);

    editParkingRatesPatchTotal.add(1);
    const start = Date.now();

    const response = await client.patch(saveEndpoint, jsonBody, {
      headers: { "Content-Type": "application/json" },
      tags: {
        operation: "edit_parkingrates_patch_only",
        step: "forecasting_flow",
        ...(inferredSiteId ? { siteId: inferredSiteId } : {}),
      },
    });

    const ok = check(response, {
      "editParkingRates: status 200/204": (r) =>
        r.status === 200 || r.status === 204,
      "editParkingRates: < 2s": (r) => r.timings.duration < 2000,
    });

    editParkingRatesPatchTime.add(Date.now() - start, {
      status: String(response.status),
      ...(inferredSiteId ? { siteId: inferredSiteId } : {}),
    });
    editParkingRatesPatchSuccess.add(ok ? 1 : 0);
    if (!ok) editParkingRatesPatchErrorCount.add(1);
  }
}
