
import { Given, Then, When } from "@badeball/cypress-cucumber-preprocessor";
import CustomerMainPageCommon from '../../Common/customerMainPageCommon'
import StatementMainPageCommon from '../../Common/statementMainPageCommon'
import MultipleInvoicesPageObjects from '../../Pages/Customer/ContractDetail/MultipleInvoicesPageObjects'
import GeneralInfoPageObject from '../../Pages/Customer/GeneralInfoPageObejct'
import ContractDetailsLocators from '../../Locator/Customers/ContractDetailsLocators'

const commonCustomer = new CustomerMainPageCommon();
const commonStatement = new StatementMainPageCommon();
const mulInvoice = new MultipleInvoicesPageObjects();
const genInfo =  new GeneralInfoPageObject();
const conDetail = new ContractDetailsLocators();
const multipleInvoiceData = require('../../../fixtures/MultipleInvoices/MultipleInvoices.json').MultipleInvoices;

const siteIDdata = multipleInvoiceData.SiteId[0]


Given('Log into application', () => {

    

    cy.visit(Cypress.env('baseUrl'));
    commonCustomer.ClickOnSignInWithMicrosoftButton()
    cy.loginWithSSO()

})

When('Go to the view detail for a site',()=>{

    mulInvoice.getSearchBar()
    cy.wait(1000)
    mulInvoice.viewSiteDetails()
    cy.wait(1000)
})


Then('Go to Contract details',()=>{
    cy.reload()
    cy.wait(2000)
    mulInvoice.clickOnContractDetailTab()
    cy.wait(1000)
    mulInvoice.clickOnEditButton()
    cy.wait(2000)
    mulInvoice.expandMultipleInvoices()
    cy.wait(1000)
    mulInvoice.enableMultipleInvoice()
})

Then('Enter the details for Multipe invoice',()=>{
    mulInvoice.enterInvoiceInfo()
    cy.wait(1000)
})

Then('Enter the details for Fixed fee and select the invoice and save it',()=>{
    mulInvoice.expandFixedFee()
    cy.wait(2000)
    mulInvoice.enableFixedFee()
    mulInvoice.checkAndDeleteAllFixedFeeServices()
    mulInvoice.selectAndInsertFixedFeeOptionAndAmount()
    mulInvoice.enterFixedFee()
    cy.scrollTo('top', { duration: 1000 })
    cy.wait(5000)
    mulInvoice.clickOnSaveButton()
    cy.wait(1000)
    cy.contains('Contract updated successfully!').should('exist')
    cy.wait(1000)

})

Then('Genrate a Statement for the site',()=>{

    commonCustomer.getMainCustomerButton()
    cy.wait(2000)
    mulInvoice.getSearchBar()
    cy.wait(1000)
    mulInvoice.generateStatement()
    cy.wait(1000)

})

Then('Verify that the generated statement includes all General Info and reflects Multiple Invoices',()=>{

    commonStatement.getMainStatementsButton()
    cy.wait(1000)
    mulInvoice.waitForStatement(15)
    mulInvoice.expandStamentForSite()
    cy.wait(2000)
    mulInvoice.viewStamentforsite1("01")
    mulInvoice.verifyFirstInvoice()
    mulInvoice.verifyFirstInvoiceAmount()
    mulInvoice.closeInvocieStatement()
    cy.wait(2000)
    mulInvoice.viewStamentforsite2("02")
    mulInvoice.verifySecondInvoice()
    mulInvoice.verifySecondInvoiceAmount()
    mulInvoice.closeInvocieStatement()
    cy.wait(1000)
    mulInvoice.clickOnApproveStatementButton()
    mulInvoice.clickOnYesApproveButton()
    cy.wait(1000)
    cy.contains('The statement has been marked as Ready to Send.').should('exist')
    mulInvoice.clearContactDetail()

})


Then('Add new Row for Multiple Invoice and verfiy it is showing in dropdown',()=>{
    cy.wait(1000)
    mulInvoice.clickOnAddInvoice()
    cy.wait(1000)
    mulInvoice.expandFixedFee()
    cy.wait(2000)
    mulInvoice.enableFixedFee()
    mulInvoice.selectAndInsertFixedFeeOptionAndAmount()
    mulInvoice.enterFixedFee()
    cy.wait(1000)
    mulInvoice.verifyAfterNewInvoiceIsAdded()
})

Then('Disabled the Invoice and Enabled the Invoice',()=>{

    cy.wait(1000)
    mulInvoice.disableMultipleInvoice()
    cy.wait(1000)
    mulInvoice.enableMultipleInvoice()
})

Then('After enabling the Invoice option, verify that the default options is displayed correctly',()=>{
    mulInvoice.verifyAfterReEnablingInvoice()
    cy.wait(1000)
    mulInvoice.clickOnCancelButton()
})