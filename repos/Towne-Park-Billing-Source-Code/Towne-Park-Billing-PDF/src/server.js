require("@babel/register")({
  presets: ["@babel/preset-env", "@babel/preset-react"],
});

const fastify = require("fastify")({ logger: true });

const ReactPDF = require("@react-pdf/renderer");

const React = require("react");

const InvoiceComponent = require("./InvoiceComponent").default;

const ApiKeyValidator = require("./middleware/apiKeyValidator");

const apiKeyValidator = new ApiKeyValidator();



// const sampleInvoiceDataREAL =
// {
//   id: "da3eb53d-8e96-ef11-8a6a-0022480a57ac",
//   createdMonth: "2024-10",
//   servicePeriodStart: "2024-09-01",
//   servicePeriodEnd: "2024-09-30",
//   totalAmount: 7070.0000000000,
//   status: "Ready To Send",
//   amNotes: null,
//   forecastData: "{\"forecastedRevenue\":48111.1,\"postedRevenue\":48111.1,\"invoicedRevenue\":37703.525,\"totalActualRevenue\":85814.625,\"forecastDeviationPercentage\":78.36762202485498,\"forecastDeviationAmount\":37703.525,\"forecastLastUpdated\":\"2024-10-28 18:47\"}",
//   invoices: [
//     {
//       amount: 4040.0000000000,
//       invoiceDate: "9/30/2024 12:00:00 AM",
//       invoiceNumber: "1078-202409-01",
//       paymentTerms: "Due by 1st Day of the Month",
//       title: "September Invoice Title",
//       description: "Description for the September Invoice",
//       lineItems: [
//         {
//           title: "Screener lorem ipsum dolor sit amet, consectetur adipiscing elit. Donec in elit ut mauris facilisis semper dolor sit amet, consectetur adipiscing elit. Donec in elit ut mauris facilisis semper",
//           description: "This is a short description",
//           code: "4720",
//           amount: 1010.0
//         },
//         {
//           title: "Balet",
//           description: "This is a short description, but longer than the sample description",
//           code: "4720",
//           amount: 1010.0
//         },
//         {
//           title: "Screener",
//           description: "This is a another short description",
//           code: "4720",
//           amount: 1010.0
//         },
//         {
//           title: "Screener",
//           description: "This is a long description, lorem ipsum dolor sit amet, consectetur adipiscing elit. Donec in elit ut mauris facilisis semper dolor sit amet, consectetur adipiscing elit. Donec in elit ut mauris facilisis semper.",
//           code: "4720",
//           amount: 1010.0
//         },
//         {
//           title: "Screener",
//           description: "This is a long description, lorem ipsum dolor sit amet, consectetur adipiscing elit. Donec in elit ut mauris facilisis semper dolor sit amet, consectetur adipiscing elit. Donec in elit ut mauris facilisis semper.",
//           code: "4720",
//           amount: 1010.0
//         },
//         {
//           title: "Screener",
//           description: "This is a long description, lorem ipsum dolor sit amet, consectetur adipiscing elit. Donec in elit ut mauris facilisis semper dolor sit amet, consectetur adipiscing elit. Donec in elit ut mauris facilisis semper.",
//           code: "4720",
//           amount: 1010.0
//         },
//         {
//           title: "Screener",
//           description: "This is a long description, lorem ipsum dolor sit amet, consectetur adipiscing elit. Donec in elit ut mauris facilisis semper dolor sit amet, consectetur adipiscing elit. Donec in elit ut mauris facilisis semper.",
//           code: "4720",
//           amount: 1010.0
//         },
//         {
//           title: "Screener",
//           description: "This is a long description, lorem ipsum dolor sit amet, consectetur adipiscing elit. Donec in elit ut mauris facilisis semper dolor sit amet, consectetur adipiscing elit. Donec in elit ut mauris facilisis semper.",
//           code: "4720",
//           amount: 1010.0
//         },
//         {
//           title: "Screener",
//           description: "This is a long description, lorem ipsum dolor sit amet, consectetur adipiscing elit. Donec in elit ut mauris facilisis semper dolor sit amet, consectetur adipiscing elit. Donec in elit ut mauris facilisis semper.",
//           code: "4720",
//           amount: 1010.0
//         },
//         {
//           title: "Screener",
//           description: "This is a long description, lorem ipsum dolor sit amet, consectetur adipiscing elit. Donec in elit ut mauris facilisis semper dolor sit amet, consectetur adipiscing elit. Donec in elit ut mauris facilisis semper.",
//           code: "4720",
//           amount: 1010.0
//         },
//         {
//           title: "Screener",
//           description: "This is a long description, lorem ipsum dolor sit amet, consectetur adipiscing elit. Donec in elit ut mauris facilisis semper dolor sit amet, consectetur adipiscing elit. Donec in elit ut mauris facilisis semper.",
//           code: "4720",
//           amount: 1010.0
//         },
//       ]
//     },
//     {
//       amount: 1010.0000000000,
//       invoiceDate: "9/30/2024 12:00:00 AM",
//       invoiceNumber: "1078-202409-02",
//       paymentTerms: "Due by 1st Day of the Month",
//       title: "November Invoice",
//       description: "Description for the November Invoice Title",
//       lineItems: [
//         {
//           title: "Screener",
//           description: "This is a sample description, lorem ipsum dolor sit amet, consectetur adipiscing elit. Donec in elit ut mauris facilisis semper.",
//           code: "4720",
//           amount: 1010.0
//         }
//       ]
//     },
//     {
//       amount: 1010.0000000000,
//       invoiceDate: "9/30/2024 12:00:00 AM",
//       invoiceNumber: "1078-202409-03",
//       paymentTerms: "Due by 1st Day of the Month",
//       title: "Invoice Title",
//       description: "Description for the Invoice",
//       lineItems: [
//         {
//           title: "Screener",
//           description: "This is a sample description",
//           code: "4720",
//           amount: 1010.0
//         }
//       ]
//     }
//   ],
//   customerSiteData: {
//     customerSiteId: "7a295e39-3f92-ef11-8a6a-0022480a57ac",
//     address: "6000 West Osceola Parkway Kissimmee, FL 34746",
//     siteName: "Gaylord Palms Bell",
//     accountManager: "sergio Hernandez",
//     siteNumber: "1078",
//     invoiceRecipient: "ATTN: Accounts Payable",
//     billingContactEmail: "Yesenia.Salas@gaylordhotels.com; Joelle.Christophe@Gaylordhotels.com",
//     accountManagerId: "1129709",
//     startDate: "11/1/2014 12:00:00 AM",
//     closeDate: "1/1/0001 12:00:00 AM",
//     district: "D - Orlando",
//     glString: "02-03-1006-"
//   },
//   generalConfig: [
//     {
//       key: "TowneParksAddress",
//       value: "450 Plymouth Road, Suite 300 Plymouth Meeting, PA 19462"
//     },
//     {
//       key: "TowneParksLegalName",
//       value: "Towne Park, LLC"
//     },
//     {
//       key: "TowneParksPOBox",
//       value: "79349,Baltimore, MD 21279-0349"
//     },
//     {
//       key: "TowneParksPhone",
//       value: "800-291-6111"
//     },
//     {
//       key: "TowneParksAccountNumber",
//       value: "85190688"
//     },
//     {
//       key: "TowneParksABA",
//       value: "021-052-053"
//     },
//     {
//       key: "TowneParksEmail",
//       value: "accountsreceivable@townepark.com"
//     }
//   ]
// };

fastify.post("/generate-invoice", {
  preHandler: apiKeyValidator.createMiddleware()
}, async (request, reply) => {

  try {
    const invoiceData = request.body;

    // Generate PDF stream with react-pdf
    reply.type("application/pdf");

    const pdfStream = await ReactPDF.renderToStream(
      <InvoiceComponent invoiceData={invoiceData} />
    );

    return reply.send(pdfStream);
  } catch (error) {
    fastify.log.error("Error generating invoice:", error);

    return reply
      .status(500)
      .send("An error occurred while generating the invoice");
  }
});

// Run the server! Docs -> https://fastify.dev/docs/latest/
fastify.listen({ port: 3000, host: "0.0.0.0" }, (err) => {
  if (err) {
    fastify.log.error(err);

    process.exit(1);
  }
});
