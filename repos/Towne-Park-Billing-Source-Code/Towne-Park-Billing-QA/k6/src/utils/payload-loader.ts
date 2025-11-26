declare function open(path: string): string;

type Site = { siteName: string; siteId: string; siteNo: string };
type SitesJson = { siteData: Site[] };
type Kind =
  | "statistics"
  | "payroll"
  | "parkingRates"
  | "otherRevenue"
  | "otherExpense";

type Payloads = {
  [siteId: string]: Partial<Record<Kind, any>>;
};

// ENV chooses which folder (uat/dev/qa...), DATA_ROOT points to where those folders live.
// Example run:
// k6 run -e ENV=uat -e DATA_ROOT=. dist/tests/statisticsEditSaveStress.js
const ENV = __ENV.ENV || "uat";
const DATA_ROOT = __ENV.DATA_ROOT || "../src/data"; // <- set this so paths don’t depend on utils/ location
console.log(`Using ENV=${ENV}, DATA_ROOT=${DATA_ROOT}`);
const BASE = `${DATA_ROOT}/${ENV}`;
console.log(`Using BASE=${BASE}`);
const sites: SitesJson = JSON.parse(open(`${BASE}/sites.json`));

const payloads: Payloads = {};
const siteIdToNo: Record<string, string> = {};

for (const s of sites.siteData) {
  siteIdToNo[s.siteId] = s.siteNo;
  const dir = `${BASE}/${s.siteNo}`;

  const tryOpen = (file: string) => {
    try {
      return JSON.parse(open(`${dir}/${file}`));
    } catch {
      return undefined;
    }
  };

  payloads[s.siteId] = {
    statistics: tryOpen("site-statistics-data.json"),
    payroll: tryOpen("payroll-data.json"),
    parkingRates: tryOpen("parkingrates-data.json"),
    otherRevenue: tryOpen("otherrevenue-data.json"),
    otherExpense: tryOpen("otherexpense-data.json"),
  };
}

export const allSiteIds = sites.siteData.map((s) => s.siteId);

export function getSitePayload(siteId: string, kind: Kind) {
  const p = payloads[siteId];
  return p ? p[kind] : undefined;
}

export function getSiteNo(siteId: string) {
  return siteIdToNo[siteId];
}
