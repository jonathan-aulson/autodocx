import { HttpClient } from "@/core/http.client";
import { check } from "k6";
import { Counter, Rate, Trend } from "k6/metrics";

const editPayrollPatchTime = new Trend("edit_payroll_patch_time", true);
const editPayrollPatchSuccess = new Rate("edit_payroll_patch_success");
const editPayrollPatchTotal = new Counter("edit_payroll_patch_total");
const editPayrollPatchErrorCount = new Counter(
  "edit_payroll_patch_error_count"
);

export class EditPayroll {
  /**
   * Patch payroll using a per-site payload passed by the caller.
   * `siteId` is optional; if it's embedded inside the payload we’ll infer it for tagging.
   */
  async patch(client: HttpClient, body: any, siteId?: string): Promise<void> {
    if (!body || typeof body !== "object") {
      throw new Error("EditPayroll.patch: invalid or empty body");
    }

    const inferredSiteId =
      siteId ??
      body.siteId ??
      body.site_id ??
      body.siteID ??
      body.site?.id ??
      body.site?.siteId;

    const saveEndpoint = `/api/payroll`;
    const jsonBody = JSON.stringify(body);

    editPayrollPatchTotal.add(1);
    const start = Date.now();

    const response = await client.patch(saveEndpoint, jsonBody, {
      headers: { "Content-Type": "application/json" }, // keep if your HttpClient doesn't default this
      tags: {
        operation: "edit_payroll_patch_only",
        step: "forecasting_flow",
        ...(inferredSiteId ? { siteId: inferredSiteId } : {}),
      },
    });

    const ok = check(response, {
      "editPayroll: patch status is 200/204": (r) =>
        r.status === 200 || r.status === 204,
      "editPayroll: patch time < 2s": (r) => r.timings.duration < 2000,
    });

    editPayrollPatchTime.add(Date.now() - start, {
      status: String(response.status),
      ...(inferredSiteId ? { siteId: inferredSiteId } : {}),
    });
    editPayrollPatchSuccess.add(ok ? 1 : 0);
    if (!ok) editPayrollPatchErrorCount.add(1);
  }
}
