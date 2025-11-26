# Towne Park Billing PDF Generator

A tool for generating and processing billing PDFs for Towne Park services.

## Overview

This application handles the generation and processing of billing PDF documents for Towne Park services, automating the billing workflow and ensuring consistent document formatting.

## Quick Start

1. Install dependencies:
npm install

2. Run the build command; which is defined in the package.json as “babel src --out-dir dist”

npm run build

3. For development purposes, run the following

npx babel-node src/server.js

4. Hit endpoint
http://localhost:3000/generate-invoice

### Document Component (@react-pdf/renderer)

The `Document` component is the root container for PDF generation using @react-pdf/renderer. It provides the following capabilities:

- Multiple page support
- Custom page sizes
- Document-level styling
- Custom metadata
- Font registration
- File attachment support

Example usage:
```jsx
<Document>
  <Page>
    {/* Content */}
  </Page>
</Document>
```

#### Key Features

- **Document Properties**: Set PDF metadata like author, title, keywords
- **Multi-page Support**: Generate documents with multiple pages
- **Font Management**: Register and use custom fonts
- **File Attachments**: Attach files to PDF documents
- **Language Support**: Unicode text support for multiple languages

For more details, refer to:
- [@react-pdf/renderer Documentation](https://react-pdf.org/)

##### Testing

You can test the PDF generation endpoint using Postman or the following curl command:

```powershell
curl -X POST http://localhost:3000/generate-invoice `
-H "Content-Type: application/json" `
-d '{
  "id": "da3eb53d-8e96-ef11-8a6a-0022480a57ac",
  "createdMonth": "2024-10",
  "servicePeriodStart": "2024-09-01",
  "servicePeriodEnd": "2024-09-30",
  "totalAmount": 7070.0000000000,
  "status": "Ready To Send",
  "amNotes": null,
  "purchaseOrder": "PO 12345",
  "forecastData": "{\"forecastedRevenue\":48111.1,\"postedRevenue\":48111.1,\"invoicedRevenue\":37703.525,\"totalActualRevenue\":85814.625,\"forecastDeviationPercentage\":78.36762202485498,\"forecastDeviationAmount\":37703.525,\"forecastLastUpdated\":\"2024-10-28 18:47\"}",
  "invoices": [
    {
      "amount": 4040.0000000000,
      "invoiceDate": "9/30/2024 12:00:00 AM",
      "invoiceNumber": "1078-202409-01",
      "paymentTerms": "Due by 1st Day of the Month",
      "title": "September Invoice Title",
      "description": "Description for the September Invoice",
      "lineItems": [
        {
          "title": "Screener lorem ipsum dolor sit amet",
          "description": "This is a short description",
          "code": "4720",
          "amount": 1010.0
        },
        {
          "title": "Balet",
          "description": "This is a short description, but longer than the sample description",
          "code": "4720",
          "amount": 1010.0
        },
        {
          "title": "Screener",
          "description": "This is a another short description",
          "code": "4720",
          "amount": 1010.0
        },
        {
          "title": "Screener",
          "description": "This is a long description, lorem ipsum dolor sit amet, consectetur adipiscing elit. Donec in elit ut mauris facilisis semper dolor sit amet, consectetur adipiscing elit. Donec in elit ut mauris facilisis semper.",
          "code": "4720",
          "amount": 1010.0
        }
      ]
    }
  ],
  "customerSiteData": {
    "customerSiteId": "7a295e39-3f92-ef11-8a6a-0022480a57ac",
    "address": "6000 West Osceola Parkway Kissimmee, FL 34746",
    "siteName": "Gaylord Palms Bell",
    "accountManager": "sergio Hernandez",
    "siteNumber": "1078",
    "invoiceRecipient": "ATTN: Accounts Payable",
    "billingContactEmail": "Yesenia.Salas@gaylordhotels.com; Joelle.Christophe@Gaylordhotels.com",
    "accountManagerId": "1129709",
    "startDate": "11/1/2014 12:00:00 AM",
    "closeDate": "1/1/0001 12:00:00 AM",
    "district": "D - Orlando",
    "glString": "02-03-1006-"
  },
  "generalConfig": [
    {
      "key": "TowneParksAddress",
      "value": "450 Plymouth Road, Suite 300 Plymouth Meeting, PA 19462"
    },
    {
      "key": "TowneParksLegalName",
      "value": "Towne Park, LLC"
    },
    {
      "key": "TowneParksPOBox",
      "value": "79349, Baltimore, MD 21279-0349"
    },
    {
      "key": "TowneParksPhone",
      "value": "800-291-6111"
    },
    {
      "key": "TowneParksAccountNumber",
      "value": "85190688"
    },
    {
      "key": "TowneParksABA",
      "value": "021-052-053"
    },
    {
      "key": "TowneParksEmail",
      "value": "accountsreceivable@townepark.com"
    },
    {
        "key": "UPPGlobalLegalName",
        "value": "UPP Global, LLC"
    }
  ]
}' --output invoice.pdf
```

The command will generate a PDF file named `invoice.pdf` in your current directory. 

### Sample Request Body Structure

The request body should include:
- Basic invoice information (id, dates, amounts)
- Array of invoices with line items
- Customer site data
- General configuration settings

##### Deployment

To deploy the container app to the appropriate Azure resource, follow these steps:

1. Ensure you have the Azure CLI installed and are signed in.
2. Sign in to the Towne Park VPN.
3. Open PowerShell in the root folder of the project (e.g., `C:\users\...\Towne-Park-Billing-PDF`).
4. Get the registry password from Johnn Hesseltine or Chris Thompson.
5. Run the following command to deploy to the Azure resource:

    ```bash
    az containerapp up -n billing-pdf-creation-dev-eastus2 --environment env-billing-dev-eastus2 -g stapp-billing-dev-e2-01 --registry-server acrbillingdeveastus2.azurecr.io --registry-username admin --registry-password {registry password} -i towne-park/billing-pdf-creation:v1.0.0 --subscription 72bb1233-bb68-442b-85f3-ef6bf21a6216 -l eastus2 --source .
    ```

---

##### Logs

To view the container app logs, follow these steps:

1. Ensure you have the Azure CLI installed and are signed in.
2. Sign in to the Towne Park VPN.
3. Open PowerShell in the root folder of the project (e.g., `C:\users\...\Towne-Park-Billing-PDF`).
4. Run the following command to view the logs:

    ```bash
    az containerapp logs show -n billing-pdf-creation-dev-eastus2 -g stapp-billing-dev-e2-01
    ```
