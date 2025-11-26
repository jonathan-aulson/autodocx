import { Document, Image, Page, StyleSheet, Text, View } from '@react-pdf/renderer';
import React from 'react';
import { format } from "date-fns"

const styles = StyleSheet.create({
  page: {
    padding: 20,
    fontSize: 10,
    backgroundColor: '#ffffff',
    fontFamily: 'Helvetica',
  },
  headerSection: {
    borderBottomWidth: 1,
    marginBottom: 15,
    border: '1px solid #ccc',
    borderRadius: 5,
  },
  headerSubSection: {
    display: 'flex',
    paddingLeft: 15,
    paddingRight: 15,
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
  },
  paddingTop: {
     paddingTop: 15,
  },
  paddingBottom: {
    paddingBottom: 15, 
  },
  topRightSection: {
    alignItems: 'flex-start',
    paddingLeft: '15%',
  },
  addressText: {
    marginTop: -12,
    paddingRight: 15,
  },
  addressPart: {
    marginLeft: 6
  },
  semiBoldText: {
    fontFamily: 'Helvetica-Bold',
    fontWeight: 'semibold',
  },
  textSmall: {
    fontsize: 14,
    lineheight: 20,
  },
  width45: {
    width: '50%',
  },
  marginTop: {
    marginTop: 2,
  },
  billToMarginTop: {
    marginTop: -19,
  },
  logo: {
    paddingLeft: 5,
    maxWidth: 196,
  },
  uppLogo: {
    height: 96, 
    marginTop: -10,
  },
  column: {
    flex: 1,
    flexShrink: 1,
  },
  invoiceText: {
    fontSize: 10,
    fontFamily: 'Helvetica-Bold',
    marginBottom: 5,
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
  },
  invoiceTextDescription: {
    fontSize: 10,
    fontFamily: 'Helvetica-Bold',
    color: '#64738b',
    marginBottom: 5,
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
  },
  invoiceDetailSection: {
    marginTop: 8,
    marginBottom: 15,
    border: '1px solid #ccc',
    borderRadius: 5,
    padding: 10,
  },
  summarySection: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    flexDirection: 'row',
    marginBottom: 8,
  },
  paymentSection: {
    paddingLeft: 10,
    paddingTop: 5,
  },
  summaryItem: {
    display: 'flex',
    paddingTop: 10,
    paddingBottom: 0,
    paddingLeft: 10,
    paddingRight: 10,
    border: '1px solid #ccc',
    borderRadius: 5,
    marginRight: 5,
    gap: 5,
    flexGrow: 1,
    flexShrink: 1,
    flexBasis: 0,
    minHeight: 80,
  },
  invoiceItemLabel: {
    color: '#64738b',
  },
  summaryItemLabel: {
    minHeight: 25,
  },
  invoiceItemText: {
    fontFamily: 'Helvetica',
    fontWeight: 400,
  },
  boldText: {
    fontFamily: 'Helvetica-Bold',
    fontWeight: 'bolder',
  },
  largeBoldText: {
    fontSize: 16,
  },
  invoiceDetailsHeader: {
    fontSize: 16,
    fontFamily: 'Helvetica-Bold',
    marginBottom: 5,
  },
  inviceTitle: {
    display: 'flex',
    flexDirection: 'row',
    alignItems: 'top',
    gap: 5,
  },
  table: {
    width: '100%',
  },
  tableRow: {
    flexDirection: 'row',
  },
  tableRowCol: {
    flexDirection: 'row',
    borderBottomWidth: 0.5,
    borderBottomColor: '#ccc',
  },
  tableColHeader: {
    padding: 5,
    
  },
  tableCol: {
    padding: 2,
    paddingTop: 14,
    paddingBottom: 10,
  },
  titleCol: {
    flex: 2.5,
  },
  descriptionCol: {
    flex: 2.5,
  },
  amountCol: {
    flex: 1,
    textAlign: 'right',
  },
  totalSectionWrapper: {
    marginTop: 10,
    borderRadius: 5,
    padding: 10,
  },
  totalSection: {
    display: 'flex',
    flexDirection: 'row',
    justifyContent: 'space-between',
  },
  totalLabel: {
    marginRight: 5,
    fontFamily: 'Helvetica-Bold',
  },
  footer: {
    display: 'flex',
    justifyContent: 'space-between',
    border: '1px solid #ccc',
    borderRadius: 5,
    padding: 15,
  },
  paymentDetails: {
    flexDirection: 'row',
    justifyContent: 'space-between',
  },
  inquiryText: {
    textAlign: 'justify',
    marginBottom: 15,
  },
  posection: {
    padding: 10,
  },
  servicePeriodContainer: {
    alignItems: "center",
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 3,
  },
  label: {
    width: 40, // fixed width to align the text consistently
  },
  date: {
    fontWeight: 'bold',
  }
});

const InvoiceComponent = ({ invoiceData }) => {
  const totalAmount = invoiceData.totalAmount;

  const generalConfig = Object.fromEntries(invoiceData.generalConfig.map(item => [item.key, item.value]));

  return (
   <Document>
      {invoiceData.invoices.map((invoice, index) => (
        <Page size="A4" style={styles.page} key={index} wrap>
        {/* Header Section */}
          <View>
            <View style={styles.headerSection}>
              <View style={[styles.headerSubSection, styles.paddingTop]}>
                <View style={[styles.width45]}></View>
                <View style={[styles.topRightSection, styles.width45]}>
                  <Image 
                    src={invoiceData.customerSiteData.glString.startsWith("22") ? "./public/upp-logo.png" : "./public/tp-logo.png"} 
                    style={invoiceData.customerSiteData.glString.startsWith("22") ? [styles.logo, styles.uppLogo] : styles.logo} 
                  />
                </View>
              </View>

              <View style={[styles.headerSubSection, styles.paddingBottom]}>
        {/* Invoice Recipient Section */}           
             <View style={[styles.width45, styles.billToMarginTop]}>
                  <Text style={[styles.boldText, styles.textSmall]}>Bill to:</Text>
                  <Text style={[styles.textSmall]}>{invoice.siteName || invoiceData.customerSiteData.siteName}</Text>
                  <Text style={[styles.textSmall]}>{invoiceData.customerSiteData.invoiceRecipient}</Text>
                  <Text style={[styles.textSmall]}>
                    {invoiceData.customerSiteData.address.substring(0, invoiceData.customerSiteData.address.indexOf(',') + 1)}
                    {'\n'}
                    {invoiceData.customerSiteData.address.substring(invoiceData.customerSiteData.address.indexOf(',') + 1).trim()}
                  </Text>
                </View>
    {/* Company Contact Info with Logo */}
                <View style={[styles.topRightSection, styles.width45, styles.marginTop]}>
                  <Text style={[styles.textSmall, styles.addressPart]}>
                    {generalConfig.TowneParksAddress.substring(0, generalConfig.TowneParksAddress.indexOf("Suite 300") + "Suite 300".length)}
                    {'\n'}
                    {generalConfig.TowneParksAddress.substring(generalConfig.TowneParksAddress.indexOf("Suite 300") + "Suite 300".length).trim()}
                  </Text>
                  <Text style={[styles.textSmall, styles.addressPart]}>{generalConfig.TowneParksEmail}</Text>
                </View>
              </View>
            </View>
                   {/* Invoice Summary Section */}

            <View style={styles.summarySection}>
              <View style={styles.summaryItem}>
                <Text style={[styles.invoiceItemLabel, styles.summaryItemLabel, styles.textSmall]}>Invoice Number</Text>
                <Text style={[styles.invoiceItemText, styles.textSmall]}>{invoice.invoiceNumber}</Text>
              </View>
              {invoice.invoiceDate && invoice.invoiceDate.trim() !== "" && (
                <View style={styles.summaryItem}>
                  <Text style={[styles.invoiceItemLabel, styles.summaryItemLabel, styles.textSmall]}>Invoice Date</Text>
                  <Text style={[styles.invoiceItemText, styles.textSmall]}>{format(new Date(invoice.invoiceDate), 'yyyy-MM-dd')}</Text>
                </View>
              )}
              <View style={styles.summaryItem}>
                <Text style={[styles.invoiceItemLabel, styles.summaryItemLabel, styles.textSmall]}>Service Period</Text>
                <View style={styles.row}>
                  <Text style={styles.label}>From</Text>
                  <Text style={styles.date}>{invoiceData.servicePeriodStart}</Text>
                </View>
                <View style={styles.row}>
                  <Text style={styles.label}>to</Text>
                  <Text style={styles.date}>{invoiceData.servicePeriodEnd}</Text>
                </View>
              </View>
              <View style={[styles.summaryItem]}>
                <Text style={[styles.invoiceItemLabel, styles.summaryItemLabel, styles.textSmall]}>Payment Terms</Text>
                <Text style={[styles.invoiceItemText, styles.textSmall]}>{invoice.paymentTerms}</Text>
              </View>
              <View style={[styles.summaryItem, {marginRight: 0}]}>
                <Text style={[styles.invoiceItemLabel, styles.summaryItemLabel, styles.textSmall]}>Amount Due</Text>
                <Text style={[styles.invoiceItemText, styles.textSmall]}>{new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(invoice.amount)}</Text>
              </View>
            </View>
            {invoiceData.purchaseOrder && (
            <View style={[styles.posection]}>
              <Text style={[styles.invoiceItemText, styles.textSmall]}>
                <Text style={[styles.invoiceItemLabel, styles.summaryItemLabel, styles.textSmall]}>Purchase Order Number:</Text> {invoiceData.purchaseOrder}
              </Text>
            </View>
          )}
          </View>
   {/* Invoice Details Section */}
          <View wrap>
            <View style={styles.invoiceDetailSection}>
              <Text style={styles.invoiceDetailsHeader}>Invoice Details</Text>
                <View style={styles.inviceTitle}>
              <View style={styles.column}><Text style={styles.invoiceText}>{invoice.title}</Text></View>
              <View style={styles.column}><Text style={styles.invoiceTextDescription}>{invoice.description}</Text></View>
            </View>
              <View style={styles.table}>
                <View style={[styles.tableRow]}>
                  <Text style={[styles.tableColHeader, styles.titleCol, styles.invoiceItemLabel, styles.textSmall]}>Title</Text>
                  <Text style={[styles.tableColHeader, styles.descriptionCol, styles.invoiceItemLabel, styles.textSmall]}>Description</Text>
                  <Text style={[styles.tableColHeader, styles.amountCol, styles.invoiceItemLabel, styles.textSmall]}>Amount</Text>
                </View>
                
                {invoice.lineItems.map((item, index) => (
                  <View 
                 wrap={false}
                    style={[styles.tableRowCol, index === invoice.lineItems.length - 1 && { borderBottomWidth: 0 }]} 
                    key={index}
                  >
                    <Text style={[styles.tableCol, styles.titleCol, styles.invoiceItemText, styles.textSmall]}>{item.title}</Text>
                    <Text style={[styles.tableCol, styles.descriptionCol, styles.invoiceItemText, styles.textSmall]}>{item.description}</Text>
                    <Text style={[styles.tableCol, styles.amountCol, styles.invoiceItemText, styles.textSmall]}>
                      {new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(item.amount)}
    </Text>
                  </View>
                ))}
              </View>
            </View>
          </View>


           {/* Total Amount Due Section (without borders) */}
          <View>
            <View style={styles.totalSectionWrapper}>
              <View style={styles.totalSection}>
                <Text style={[styles.invoiceItemLabel, styles.textSmall, { marginLeft: -10 }]}>Total Amount Due</Text>
                <Text style={[styles.boldText, styles.largeBoldText]}>
                  {new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(invoice.amount)}
                </Text>
              </View>
            </View>
      {/* Payment Information Section */}
            <View style={styles.footer}>
              <Text style={[styles.semiBoldText, styles.inquiryText]}>
                For information regarding your account, please contact Towne Park Accounting at{"\n"}{generalConfig.TowneParksEmail}
              </Text>
              <View style={styles.paymentDetails}>
                <View>
                  <Text style={styles.boldText}>Check Payments:</Text>
                  <Text style={[styles.invoiceItemText, styles.textSmall]}>
                    Please make checks payable to {invoiceData.customerSiteData.glString.startsWith("22") && generalConfig.UPPGlobalLegalName ? generalConfig.UPPGlobalLegalName : generalConfig.TowneParksLegalName}
                  </Text>
                  <Text style={[styles.invoiceItemText, styles.textSmall]}>Remit payment to:</Text>
                  <View>
                    <Text style={[styles.invoiceItemText, styles.textSmall]}>
                      {invoiceData.customerSiteData.glString.startsWith("22") ? 
                      `${generalConfig.TowneParksAddress.substring(0, generalConfig.TowneParksAddress.indexOf("Suite 300") + "Suite 300".length)}\n${generalConfig.TowneParksAddress.substring(generalConfig.TowneParksAddress.indexOf("Suite 300") + "Suite 300".length).trim()}`
                      : `P.O. Box ${generalConfig.TowneParksPOBox.substring(0, generalConfig.TowneParksPOBox.indexOf(',') + 1)} ${generalConfig.TowneParksPOBox.substring(generalConfig.TowneParksPOBox.indexOf(',') + 1).trim()}`}
                    </Text>
                  </View>
                </View>
                <View>
                  <Text style={styles.boldText}>ACH Payments:</Text>
                  <Text style={[styles.invoiceItemText, styles.textSmall]}>Account number: {generalConfig.TowneParksAccountNumber}</Text>
                  <Text style={[styles.invoiceItemText, styles.textSmall]}>ABA: {generalConfig.TowneParksABA}</Text>
                    {/* <Text style={[styles.invoiceItemText, styles.textSmall]}>E-mail remittance to:</Text> */}
                  <Text style={[styles.invoiceItemText, styles.textSmall]}>{generalConfig.TowneParksEmail}</Text>
                </View>
              </View>
            </View>
          </View>
        </Page>
      ))}
    </Document>
  );
};

export default InvoiceComponent;