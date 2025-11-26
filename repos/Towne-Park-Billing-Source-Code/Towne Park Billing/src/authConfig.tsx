//TODO add role logic



// export const roles = {
//   appUsers: 'app-users'
// }

export const routes = {
  customersList: "/billing/customers",
  customersDetails: "/billing/customers/:customerSiteId/detail",
  customersDetailsWithId: (customerSiteId: string) => `/billing/customers/${customerSiteId}/detail`,
  forecasts: "/forecasts",
  statements: "/billing/statements",
  adminPanel: "/billing/admin-panel",
  pnlView: "/pnl-view",
  pnlViewWithId: "/pnl-view/:customerId",
  pnlViewWithIdFunction: (customerNumber: string) => `/pnl-view/${customerNumber}`,
}