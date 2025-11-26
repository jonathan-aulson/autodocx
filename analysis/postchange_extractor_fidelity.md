# Post-change: Extractor Fidelity (2025-11-13)

After updating `LogicAppsWDLExtractor`, the same scan now yields workflow SIRs with populated connectors.

Excerpt from `out/sir/CapacityAlertFlow-CC0D1317-DC4C-F011-877A-002248029144.json.json`:

```json
{
  "kind": "workflow",
  "props": {
    "triggers": [
      {"name": "Recurrence", "type": "Recurrence", "schedule": {"frequency": "Day", "interval": 1}}
    ],
    "steps": [
      {"name": "Get_the_tenant_capacity_details_for_the_tenant", "connector": "shared_powerplatformadminv2"},
      {"name": "Filter_array_Tenant_Capacity_Type", "connector": "Query"},
      {"name": "SendMail", "connector": "shared_bs-5fsendmailbyserviceprincipal-5f793423c41454f92f"}
    ]
  }
}
```

Renderer output reflects the richer data; connectors now include the backing API names instead of remaining blank.
