export default class ViewDetailsStatementLocators {

    locators= {
       expandStatement: (siteIDData)=> cy.xpath(`//td[contains(text(),"${siteIDData}")]/ancestor::tr//div[contains(text(),"Approval Team")]/ancestor::tr//div[@class="flex items-center space-x-2"]/div`),
       viewStatementForSiteInvoice1 : (siteID,dateInvoice,invoiceNumber)=> cy.xpath(`(//td[contains(text(),"${siteID}-${dateInvoice}-${invoiceNumber}")]/ancestor::tr//button)[1]`),
       viewStatementForSiteInvoice2 : (siteID,dateInvoice,invoiceNumber)=> cy.xpath(`(//td[contains(text(),"${siteID}-${dateInvoice}-${invoiceNumber}")]/ancestor::tr//button)[1]`),
       mulViewStatementForSiteInvoice2 : (siteID,dateInvoice,invoiceNumber)=> cy.xpath(`(//td[contains(text(),"${siteID}-${dateInvoice}-${invoiceNumber}")]/ancestor::tr//button)[3]`),
       closeStatement :  ()=> cy.xpath(`//button[contains(text(),"X")]`),
       approveStatementButton :  (siteIDData)=> cy.xpath(`(//td[contains(text(),'${siteIDData}')]/ancestor::tr//div[contains(text(),'Approval Team')]/ancestor::tr//button)[2]`),
       yesApproveButton : ()=> cy.xpath('//button[contains(text(),"Yes, Approve")]'),
       cancelApproveButton : ()=> cy.xpath("//button[contains(text(),'Cancel')]"),
       expandStatementWithReadyStatus: (siteIDData)=> cy.xpath(`//td[contains(text(),"${siteIDData}")]/ancestor::tr//div[contains(text(),"Approval Team")]/ancestor::tr//div[@class="flex items-center space-x-2"]/div`),
    


        statementDropdownArrow: ()=>cy.xpath("//*[@class='[&_tr:last-child]:border-0']/tr/td[@class='p-4 align-middle [&:has([role=checkbox])]:pr-0']/div[@class='flex items-center space-x-2']/div")
        
    }
   
}