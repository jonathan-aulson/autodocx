/// <reference types="cypress"/>

export default class CustomersLoctors {

    locator = {
         
        viewDetailUsingSiteID: (siteId)=> cy.xpath(`//div[contains(text(),"${siteId}")]/ancestor::tr//button[@class="inline-flex items-center justify-center whitespace-nowrap rounded-md text-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:pointer-events-none disabled:opacity-50 hover:bg-accent hover:text-accent-foreground h-8 w-8 p-0"][2]`),
        siteidsearchbox:()=>cy.xpath("//*[@placeholder='Search...']"),
        viewdetailbtn:()=>cy.xpath("(//*[@class='flex space-x-4']/button[@data-state='closed'])[2]"),
        generateStatementButton:()=>cy.xpath("(//*[@class='border-b transition-colors hover:bg-muted/50 data-[state=selected]:bg-muted']/td/div[@class='flex space-x-4']/button)[1]"),
        adminProceedButtonFromModel:()=>cy.xpath("//*[text()='Admin Proceed']"),
        requestAcceptedPopup: () => cy.xpath("//div[@class='grid gap-1']/div[text()='Request accepted']").should("be.visible"),
        successfullPopup: () => cy.xpath("//div[@class='grid gap-1']/div[text()='Statement generation was successfully requested by Admin.']"),
        statementXpath:(site_id)=>cy.xpath(`//td[contains(text(),'${site_id}')]/ancestor::tr//div[contains(text(),'Approval Team')]`),
        statementsButtonFromTopBar:()=>cy.xpath("//*[@class='space-x-6 hidden md:block']//*[text()='Statements']"),
        viewStatementButton:()=>cy.xpath("//*[@class='[&_tr:last-child]:border-0']/tr[@class='data-[state=selected]:bg-muted border-b transition-colors hover:bg-muted/50']//*[text()='View Invoice']/ancestor::button"),
        statementDropDown:()=>cy.xpath("//*[@class='[&_tr:last-child]:border-0']/tr/td[@class='p-4 align-middle [&:has([role=checkbox])]:pr-0']/div[@class='flex items-center space-x-2']/div"),
        invoiceTotalAmountValue:(total)=>cy.xpath(`//*[@class='fixed inset-0 bg-black bg-opacity-70 flex justify-center items-center z-50']/div[@class='bg-white dark:bg-gray-900 max-h-full overflow-auto p-6 rounded-lg']//*[@class='rounded-lg border bg-card text-card-foreground shadow-sm max-w-full']//*[text()="${total}"]`)
        

    }
    
}