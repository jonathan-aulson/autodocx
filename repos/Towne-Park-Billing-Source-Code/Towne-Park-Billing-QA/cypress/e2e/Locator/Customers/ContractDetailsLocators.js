/// <reference types="cypress"/>

export default class ContractDetailsLocators {


  locators = {

    viewDetailforSite: (siteID) => cy.xpath(`(//div[contains(text(),'${siteID}')]/ancestor::tr//button)[3]`),
    generateStatementforSite: (genStatement) => cy.xpath(`(//div[contains(text(),'${genStatement}')]/ancestor::tr//button)[2]`),
    adminProcessforStatementButton: () => cy.xpath('//button[contains(text(),"Admin Proceed")]'),
    contractDetailsTab: () => cy.get('button[data-testid="contract-details-tab1"]'),
    searchBar: () => cy.get('input[placeholder="Search..."]'),
    editButton: () => cy.xpath("//button[text()='Edit']"),
    saveButton: () => cy.xpath('//button[@type="submit"]'),
    cancelButton: () => cy.get('button[data-testid="cancel-button"]'),
    cancelYesButton: ()=> cy.get('button[data-qa-id="button-cancelDialogYes"]'),
    contractdetailsButton: () => cy.xpath("//button[text()='Contract Details']"),
    deviationpercentage: () => cy.xpath("//*[text()='Deviation Percentage']/parent::div/input[@type='text']"),
    deviationamount: () => cy.xpath("//*[text()='Deviation Amount']/parent::div/input[@type='text']"),
    successmsgpopup: () => cy.xpath("//*[@class='grid gap-1']/div[text()='Success!']"),
    successfulltsavedpopup: () => cy.xpath("//*[@class='grid gap-1']/div[text()='Contract updated successfully!']"),
    customersbuttonfromtopbar: () => cy.xpath("//*[@class='space-x-6 hidden md:block']/a[text()='Customers']"),
        statementsbuttonfromtopbar:()=>cy.xpath("//*[@class='space-x-6 hidden md:block']/a[text()='Statements']"),
    invoiceCloseButton: () => cy.xpath("//*[@class='fixed inset-0 bg-black bg-opacity-70 flex justify-center items-center z-50']/div[@class='bg-white dark:bg-gray-900 max-h-full overflow-auto p-6 rounded-lg']/div/button[text()='X']")
  }

  generalSetupLocator = {

    generalSetupExpandButton: () => cy.xpath("//button[contains(text(),'Multiple Invoices')]"),
    contractType: () => cy.get('input[name="contractType"]'),
    depositsDropdown: () => cy.get('button[class="flex h-10 w-full items-center justify-between rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 [&>span]:line-clamp-1"]'),
    depositsYesOption: () => cy.xpath('//label[contains(text(),"Deposits")]/parent::div[@class="space-y-2"]/select[@aria-hidden="true"]').invoke('val', 'value="true"').trigger('change'),
    depositsNoOption: () => cy.xpath('//label[contains(text(),"Deposits")]/parent::div[@class="space-y-2"]/select[@aria-hidden="true"]').invoke('val', 'value="false"').trigger('change'),

    billingTypeDropdownButton: () => cy.xpath('//label[contains(text(),"Billing Type")]/following-sibling::button'),
    advancedOptionValueToBillingTypeDropdown: () => cy.xpath('//label[contains(text(),"Billing Type")]/following-sibling::select').select('Advanced',{force:true}),
    arrearsOptionValueToBillingTypeDropdown: () => cy.xpath('//label[contains(text(),"Billing Type")]/following-sibling::select').select('Arrears',{force:true})


  }

  mulitipleInvoicesLocator = {

    viewDetailforSite: (siteID) => cy.xpath(`(//div[contains(text(),'${siteID}')]/ancestor::tr//button)[3]`),
    multipleInvioveExpandButton: () => cy.xpath("//button[contains(text(),'Multiple Invoices')]"),
    enableMulipleInvoice: () => cy.xpath("(//button[contains(text(),'Multiple Invoices')]/ancestor::h3/following-sibling::div//button)[1]"),
    invoiceTitle1TextBox: () => cy.get('input[data-testid="invoice-group-title-0"]'),
    invoiceDescription1TextBox: () => cy.get('input[placeholder="Description for Invoice 1"]'),

    invoiceTitle2TextBox: () => cy.get('input[data-testid="invoice-group-title-1"]'),
    invoiceDescription2TextBox: () => cy.get('input[placeholder="Description for Invoice 2"]'),

    addInvoiceButton: () => cy.xpath('//button[contains(text(),"Add Invoice")]'),
    disableConfirmButton: () => cy.xpath('//button[contains(text(),"Yes, Proceed")]'),
    disableCancelButton: () => cy.xpath('//div[@class="flex flex-col-reverse sm:flex-row sm:justify-end sm:space-x-2 mt-4"]/button[contains(text(),"Cancel")]'),




  }

  fixedFeeLocator = {

    fixedFeeExpandButton: () => cy.xpath('//button[contains(text(),"Fixed Fee")]'),
    enableFixedFeeButton: () => cy.xpath("(//button[contains(text(),'Fixed Fee')]/ancestor::h3/following-sibling::div//button)[1]"),
    displayNameTextBox: () => cy.xpath('//label[contains(text(),"Display Name")]//following-sibling::input'),
    feeTextBox: () => cy.xpath('//label[contains(text(),"Fee")]//following-sibling::input'),
    invoiceCancelButton: () => cy.xpath('//button[contains(text(),"X")]'),
    approvalTeamButton: () => cy.xpath("(//tr[td[contains(text(),'0500')] and td/div[contains(text(),'Approval Team')]]//button)[2]"),
    yesApproveButton: () => cy.xpath("//button[contains(text(),'Yes, Approve')] "),
    cancelApproveButton: () => cy.xpath("//button[contains(text(),'Cancel')]"),
    expandforInvoiceButton: () => cy.xpath("(//tr[td[contains(text(),'0500')] and td/div[contains(text(),'Approval Team')]]//td//div)[6]"),
    feeFixedInput: (services) => cy.xpath(`//p[contains(text(),'${services}')]/ancestor::div/following-sibling::div/following-sibling::div/input`),
    selectInvoiceOption2: (services, optionValue) => {
      const baseXPath = `//p[contains(text(),'${services}')]/ancestor::div/following-sibling::div`;
      cy.xpath(`${baseXPath}//button/following-sibling::select/option[@value=${optionValue}]`)
        .invoke('val')
        .then((value) => {
          cy.xpath(`${baseXPath}//button/following-sibling::select`).select(value, { force: true });
        });
    },
    selectInvoiceOption1: (services, optionValue) => {
      const baseXPath = `//p[contains(text(),'${services}')]/ancestor::div/following-sibling::div`;

      cy.xpath(`${baseXPath}//button/following-sibling::select/option[@value=${optionValue}]`)
        .invoke('val')
        .then((value) => {
          cy.xpath(`${baseXPath}//button/following-sibling::select`).first().select(value, { force: true });
        });
    },
    invoiceDropdownButton: (dropDownButton) => cy.xpath(`(//p[contains(text(),'${dropDownButton}')]/ancestor::div/following-sibling::div//button)[1]`),

    togglebutton: () => cy.xpath("//*[text()='Fixed Fee']/parent::h3/parent::div//button[@role='switch']"),
    fixedfeeaddbutton: () => cy.xpath("//*[text()='Fixed Fee']/parent::h3/parent::div//button[text()='Add']"),
    fixedfeemultipleoption: (option) => cy.xpath(`//*[text()='Fixed Fee']/parent::h3/parent::div[@class='border-b']//*[text()="${option}"]/ancestor::div[@class='flex items-start justify-between space-x-2']/div[3]/input[@placeholder='Fee']`),
    fixedFeeDeleteButton: () => cy.xpath("//div[@class='flex items-start justify-between space-x-2']/div[@class='space-y-2 flex flex-col items-center w-auto flex-shrink-0']/button"),
    fixedFeeToggleOnButton: () => cy.xpath("//*[text()='Fixed Fee']/ancestor::div[@data-state='open']//button[@value='on']"),
    fixedFeeToggleButton: () => cy.xpath("//*[text()='Fixed Fee']/ancestor::div[@data-state='open']//button[@role='switch']"),
    fixedFeeToggleOffButton: () => cy.xpath("//*[text()='Fixed Fee']/parent::h3/parent::div//button[@data-state='unchecked']")


  }

    additionalFeesAndLineItem = {

        additionalFeesAndLineItemExpandButton: () => cy.xpath('//button[contains(text(),"Additional Fees or Line Items")]'),
        midMonthAdvanceToggelButton: () => cy.xpath('//button[contains(text(),"Additional Fees or Line Items")]/parent::h3/parent::div/div[@data-state="open"]//*[text()="Mid-Month Advance"]/parent::div[@class="space-y-0.5"]/following-sibling::button'),
        advancementAmountTextBox: () => cy.xpath('//*[@class="space-y-4 pl-6 gap-4 p-4 border rounded-lg"]//div[@class="space-y-2"]/input[@placeholder="Amount"]'),
        towneParkDepositedRevenueToggelButton: () => cy.xpath('//*[@class="space-y-4"]//*[text()="Towne Park Deposited Revenue"]/ancestor::div[@class="flex items-center justify-between"]/button'),
        towneParkDepositedRevenueToggelOnButton: () => cy.xpath('//*[@class="space-y-4"]//*[text()="Towne Park Deposited Revenue"]/ancestor::div[@class="flex items-center justify-between"]/button[@aria-checked="true"]'),
        towneParkDepositedRevenueToggelOffButton: () => cy.xpath('//*[@class="space-y-4"]//*[text()="Towne Park Deposited Revenue"]/ancestor::div[@class="flex items-center justify-between"]/button[@aria-checked="false"]'),
        towneParkResponsibleForParkingTaxToggelButton: () => cy.xpath('//*[@class="space-y-4"]//*[text()="Towne Park Deposited Revenue"]/ancestor::div[@class="border-b"]//*[text()="Towne Park Responsible for Parking Tax"]/ancestor::div[@class="flex items-center justify-between"]/button'),
        towneParkResponsibleForParkingTaxOffToggelButton: () => cy.xpath('//*[@class="space-y-4"]//*[text()="Towne Park Deposited Revenue"]/ancestor::div[@class="border-b"]//*[text()="Towne Park Responsible for Parking Tax"]/ancestor::div[@class="flex items-center justify-between"]/button[@aria-checked="false"]'),
        towneParkResponsibleForParkingTaxOnToggelButton: () => cy.xpath('//*[@class="space-y-4"]//*[text()="Towne Park Deposited Revenue"]/ancestor::div[@class="border-b"]//*[text()="Towne Park Responsible for Parking Tax"]/ancestor::div[@class="flex items-center justify-between"]/button[@aria-checked="true"]'),
        bellServiceFeeToggelButton: () => cy.xpath('//*[@class="space-y-4"]//*[text()="Bell Service Fee"]/ancestor::div[@class="flex items-center justify-between"]/button'),
        bellServiceFeeToggelOnButton: () => cy.xpath('//*[@class="space-y-4"]//*[text()="Bell Service Fee"]/ancestor::div[@class="flex items-center justify-between"]/button[@aria-checked="true"]'),
        bellServiceFeeToggelOffButton: () => cy.xpath('//*[@class="space-y-4"]//*[text()="Bell Service Fee"]/ancestor::div[@class="flex items-center justify-between"]/button[@aria-checked="false"]'),
        toggelButtonForOptions: (option) => cy.xpath(`//*[@class="space-y-4"]//*[text()="Towne Park Deposited Revenue"]/ancestor::div[@class="border-b"]//*[text()="${option}"]/ancestor::div[@class="flex items-center justify-between"]/button`),
        invoiceDetailsForMidMonthAdvance: (amount) => cy.xpath(`//*[@class="bg-white dark:bg-gray-900 max-h-full overflow-auto p-6 rounded-lg"]//*[text()="Mid-Month Advance"]/following-sibling::td//*[text()="Mid Month Billing"]/parent::div/parent::td/following-sibling::td[text()="${amount}"]`),
        linItemTitleButton: ()=> cy.xpath("//*[@class='space-y-4 pl-6 gap-4 p-4 border rounded-lg']//label[text()='Line-Item Title']/following-sibling::button"),
        lessMidMonthBillingOption: (option)=> cy.xpath(`//*[text()="${option}"]`)

    }

    perLaborHour = {
        perLaborHourExpandButton: () => cy.xpath('//button[contains(text(),"Per Labor Hour")]'),
        perLaborHourToggleOnButton: () => cy.xpath('//button[@data-qa-id="switch-component-perLaborHourEnabled"]'),
        addButton: () => cy.xpath('//button[@data-qa-id="button-addPerLaborHourJobDropdown"]'),
        multipleOption:(option)=> cy.xpath(`//*[@class='h-full w-full rounded-[inherit]']/div/div[text()="${option}"]`),
        standardRateBox: (option)=> cy.xpath(`//*[text()="${option}"]/ancestor::div[@class='flex items-start justify-between space-x-2']//div/input[@placeholder='Standard Rate']`),
        overtimeRateBox: (option)=> cy.xpath(`//*[text()="${option}"]/ancestor::div[@class='flex items-start justify-between space-x-2']//div/input[@placeholder='Overtime Rate']`),
        jobCodeBox:(option)=> cy.xpath(`//*[text()="${option}"]/ancestor::div[@class='flex items-start justify-between space-x-2']//div/input[@placeholder='Enter Job Code']`),
        detailsFromInvoice: (option)=> cy.xpath(`//*[@class='flex flex-col gap-6 mx-auto max-w-10xl p-6 md:p-8 lg:p-10 relative']/div[@class='rounded-lg border bg-card text-card-foreground shadow-sm max-w-full']/div[@class='p-6 pt-0']//table//*[text()="${option}"]/following-sibling::td/div`),
        totalHoursWorked: (option)=> cy.xpath(`//*[@class='flex flex-col gap-6 mx-auto max-w-10xl p-6 md:p-8 lg:p-10 relative']/div[@class='rounded-lg border bg-card text-card-foreground shadow-sm max-w-full']/div[@class='p-6 pt-0']//table//*[text()="${option}"]/following-sibling::td/div/p[contains(text(), 'Total Hours Worked')]`),
        towneParkServiceFee: (option)=> cy.xpath(`//*[@class='flex flex-col gap-6 mx-auto max-w-10xl p-6 md:p-8 lg:p-10 relative']/div[@class='rounded-lg border bg-card text-card-foreground shadow-sm max-w-full']/div[@class='p-6 pt-0']//table//*[text()="${option}"]/following-sibling::td/div/p[contains(text(), 'Towne Park Service Fee')]`),
        totalOvertimeHoursWorked: (option)=> cy.xpath(`//*[@class='flex flex-col gap-6 mx-auto max-w-10xl p-6 md:p-8 lg:p-10 relative']/div[@class='rounded-lg border bg-card text-card-foreground shadow-sm max-w-full']/div[@class='p-6 pt-0']//table//*[text()="${option}"]/following-sibling::td/div/p[contains(text(), 'Total Overtime / Holiday Hours Worked')]`),
        towneParkServiceOvertimeFee: (option)=> cy.xpath(`//*[@class='flex flex-col gap-6 mx-auto max-w-10xl p-6 md:p-8 lg:p-10 relative']/div[@class='rounded-lg border bg-card text-card-foreground shadow-sm max-w-full']/div[@class='p-6 pt-0']//table//*[text()="${option}"]/following-sibling::td/div/p[contains(text(), 'Towne Park Service Overtime Fee at')]`),
        perLaborHourDeleteButton: ()=> cy.xpath("//*[text()='Per Labor Hour']/parent::h3/parent::div//*[text()='Action']/following-sibling::button[@data-qa-id='button-removePerLaborHourJob-0']"),
        stringOfPerLaborHour: (option)=>cy.xpath(`//*[text()="${option}"]`)
    }

    perOccupiedRooms = {
        perOccupiedRoomExpandButton: () => cy.xpath('//button[contains(text(),"Per Occupied Room")]'),
        perOccupiedRoomToggleButton: () => cy.xpath('//*[@data-qa-id="switch-component-perOccupiedRoomEnabled"]'),
        PerOccupiedRoomRateField: () => cy.xpath('//*[text()="Rate"]/following-sibling::input[@placeholder="Room Rate"]')
    }



}