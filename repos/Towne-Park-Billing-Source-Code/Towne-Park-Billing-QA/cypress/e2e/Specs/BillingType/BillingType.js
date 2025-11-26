
import { Given, Then, When } from "@badeball/cypress-cucumber-preprocessor";
import CustomerMainPageCommon from '../../Common/customerMainPageCommon'
import StatementMainPageCommon from '../../Common/statementMainPageCommon'
import BillingTypePageObjects from '../../Pages/Customer/ContractDetail/BillingTypePageObjects'
import GeneralInfoPageObject from '../../Pages/Customer/GeneralInfoPageObejct'
import ContractDetailsLocators from '../../Locator/Customers/ContractDetailsLocators'

const commonCustomer = new CustomerMainPageCommon();
const commonStatement = new StatementMainPageCommon();
const billType = new BillingTypePageObjects();
// const genInfo =  new GeneralInfoPageObject();
// const conDetail = new ContractDetailsLocators();
// const billingTypeData = require('../../../fixtures/BillingType/BillingType.json').BillingType;

// const siteIDdata = multipleInvoiceData.SiteId[0]


Given('Log into application', () => {

  
    cy.visit(Cypress.env('baseUrl'));
    commonCustomer.ClickOnSignInWithMicrosoftButton()
    cy.loginWithSSO()

})

When('Go to the view detail for a site',()=>{

    billType.getSearchBar()
    cy.wait(1000)
    billType.viewSiteDetails()
    cy.wait(1000)
})

When('Go to the view detail for a site that is Advanced Billing type',()=>{

    billType.getSearchBarAdv()
    cy.wait(1000)
    billType.viewSiteDetailsAdv()
    cy.wait(1000)
})



Then('Go to Contract details',()=>{
    billType.clickOnContractDetailTab()
    cy.wait(1000)
    billType.clickOnEditButton()
    
})

Then('Select the Billing type as  Appears',()=>{
    billType.selectArrearsOptionFromDropdown()
    cy.wait(1000)
})

Then('Select the Billing type as Advanced',()=>{
    billType.selectAdvanceOptionFromDropdown()
    cy.wait(1000)
})

Then('Enter the details for Fixed Fee',()=>{
    billType.expandFixedFee()
    cy.wait(2000)
    billType.enableFixedFee()
    billType.checkAndDeleteAllFixedFeeServices()
    billType.selectAndInsertFixedFeeOptionAndAmount()
    billType.enterFixedFee()
    cy.scrollTo('top', { duration: 1000 })
    cy.wait(5000)
    billType.clickOnSaveButton()
    cy.wait(1000)
    cy.contains('Contract updated successfully!').should('exist')
    cy.wait(1000)

})

Then('Genrate a Statement for the site',()=>{
    commonCustomer.getMainCustomerButton()
    cy.wait(2000)
    billType.getSearchBar()
    cy.wait(1000)
    billType.generateStatement()
    cy.wait(1000)

})

Then('Genrate a Statement for the site that is Advanced Billing type',()=>{
    commonCustomer.getMainCustomerButton()
    cy.wait(2000)
    billType.getSearchBarAdv()
    cy.wait(1000)
    billType.generateStatementAdv()
    cy.wait(1000)

})

Then('Verify that the generated statement for pervious month and Fixed Fee amount',()=>{

    commonStatement.getMainStatementsButton()
    cy.wait(1000)
    billType.waitForStatementArrears(15)
    cy.wait(1000)
    billType.verifyArrearsMonth()
    billType.expandStamentForSite()
    billType.verifyStatement()
    billType.closeInvocieStatement()
    cy.wait(1000)
    billType.clickOnApproveStatementButton()
    billType.clickOnYesApproveButton()
    cy.wait(1000)
    cy.contains('The statement has been marked as Ready to Send.').should('exist')

})

Then('Verify that the generated statement for next month and Fixed Fee amount',()=>{

    commonStatement.getMainStatementsButton()
    cy.wait(5000)
    cy.reload()
    cy.wait(15000)
    cy.reload()
    cy.wait(15000)
    billType.waitForStatementAdvanced(15)
    cy.wait(1000)
    billType.verifyAdvancedMonth()
    billType.expandStamentForSiteWithReadyStatus()
    billType.verifyStatementAdv()
    billType.closeInvocieStatement()
    cy.wait(1000)
    billType.clearContactDetailAdv()

})

