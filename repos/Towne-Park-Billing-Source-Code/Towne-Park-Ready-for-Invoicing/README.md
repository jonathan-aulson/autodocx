# Towne Park - Ready for Invoicing Infrastructure

This repository manages the infrastructure deployment for the **Towne Park Ready for Invoicing** solution using Bicep templates and shell scripts for various environments (DEV, UAT, and PROD).

---

## 📁 Project Structure

TOWNE-PARK-READY-FOR-INVOICING
├── deployment/
│ ├── dev-deploy-infrastructure.sh # Deployment script for DEV environment
│ ├── uat-deploy-infrastructure.sh # Deployment script for UAT environment
│ └── prod-deploy-infrastructure.sh # Deployment script for PROD environment
│
├── infrastructure/
│ ├── main.bicep # Main Bicep template for infrastructure
│ └── params/
│ ├── dev.bicepparam # Parameter file for DEV
│ ├── uat.bicepparam # Parameter file for UAT
│ └── prod.bicepparam # Parameter file for PROD
│
└── README.md # Project documentation

---

---

## 🚀 Deployment Steps

Each environment has its own deployment script. Before executing, ensure you're logged in to Azure CLI with the correct subscription.

### ✅ To deploy to DEV:
Run:
- `sh deployment/dev-deploy-infrastructure.sh`

### ✅ To deploy to UAT:
Run:
- `sh deployment/uat-deploy-infrastructure.sh`

### ✅ To deploy to PROD:
Run:
- `sh deployment/prod-deploy-infrastructure.sh`

---

## After Deployment Instructions

Once the deployment completed successfully, Below Azure Resources will be created in portal.azure.com in respective resource group
1. commondataservice-1
2. excelonlinebusiness
3. office365
4. sharepointonline
5. sql-1
6. lapp-ready-for-invoicing-dev-eastus2-01

Then User need to edit and authenticate below connections with **Flow@townepark.com**
1. commondataservice-1
2. excelonlinebusiness
3. office365
4. sharepointonline

Then User need to open the Logic App **lapp-ready-for-invoicing-dev-eastus2-01** in desginer mode.
- Edit any SQL stage
- Create new connection and add the RSS SQL User connection details
- Update all the other SQL stage with the newly created sql connection.
- Save the Logic App.