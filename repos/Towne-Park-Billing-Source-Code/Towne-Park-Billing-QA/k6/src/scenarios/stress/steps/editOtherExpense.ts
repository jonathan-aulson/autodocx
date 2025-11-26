import { HttpClient } from "@/core/http.client";
import { check } from "k6";
import { Counter, Rate, Trend } from "k6/metrics";

const editOtherExpensePatchTime = new Trend(
  "edit_otherexpense_patch_time",
  true
);
const editOtherExpensePatchSuccess = new Rate(
  "edit_otherexpense_patch_success"
);
const editOtherExpensePatchTotal = new Counter("edit_otherexpense_patch_total");
const editOtherExpensePatchErrorCount = new Counter(
  "edit_otherexpense_patch_error_count"
);

export class EditOtherExpense {
  /**
   * Patch Other Expense using a caller-provided payload.
   * `siteId` is optional but will be tagged if provided.
   */
  async patch(client: HttpClient, body: any, siteId?: string): Promise<void> {
    if (!body || typeof body !== "object") {
      throw new Error("EditOtherExpense.patch: invalid or empty body");
    }

    const inferredSiteId =
      siteId ??
      body.siteId ??
      body.site_id ??
      body.siteID ??
      body.site?.id ??
      body.site?.siteId;

    const saveEndpoint = `/api/otherExpense`;
    const jsonBody = JSON.stringify(body);

    editOtherExpensePatchTotal.add(1);
    const start = Date.now();

    const response = await client.patch(saveEndpoint, jsonBody, {
      headers: { "Content-Type": "application/json" },
      tags: {
        operation: "edit_otherexpense_patch_only",
        step: "forecasting_flow",
        ...(inferredSiteId ? { siteId: inferredSiteId } : {}),
      },
    });

    const ok = check(response, {
      "editOtherExpense: patch status is 200/204": (r) =>
        r.status === 200 || r.status === 204,
      "editOtherExpense: patch time < 2s": (r) => r.timings.duration < 2000,
    });

    editOtherExpensePatchTime.add(Date.now() - start, {
      status: String(response.status),
      ...(inferredSiteId ? { siteId: inferredSiteId } : {}),
    });
    editOtherExpensePatchSuccess.add(ok ? 1 : 0);
    if (!ok) editOtherExpensePatchErrorCount.add(1);
  }
}
