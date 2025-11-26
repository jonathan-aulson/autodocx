
import { Given, Then, When } from "@badeball/cypress-cucumber-preprocessor";
import CustomerMainPageCommon from '../../Common/customerMainPageCommon'
import StatementMainPageCommon from '../../Common/statementMainPageCommon'
import FixedFeePageObjects from '../../Pages/Customer/ContractDetail/FixedFeePageObject'
import GeneralInfoPageObject from '../../Pages/Customer/GeneralInfoPageObejct'
import ContractDetailsLocators from '../../Locator/Customers/ContractDetailsLocators'
import "cypress-xpath"

const conDetail = new ContractDetailsLocators()
const commonCustomer = new CustomerMainPageCommon()
const commonStatement = new StatementMainPageCommon()
const feeFixed = new FixedFeePageObjects()
const genInfo =  new GeneralInfoPageObject()
//const FixedFeedata=require('../../../../fixtures/FixedFee/FixedFee.json').FixedFee;
const FixedFeedata=require('../../../fixtures/FixedFee/FixedFee.json').FixedFee;


Given('Log into application', () => {

    
    
    cy.visit(Cypress.env('baseUrl'));
    commonCustomer.ClickOnSignInWithMicrosoftButton()
    cy.loginWithSSO()

})

When('Go to the view detail for a site',()=>{

    commonCustomer.getSearchBar()
    cy.wait(1000)
    conDetail.locators.viewDetailforSite().click()
    cy.wait(1000)

})

Then('Enter the General Info and save it',()=>{

    genInfo.clickOnGeneralInfoTab()
    genInfo.clickOnEditButton()
    genInfo.enterVendorId('202020')
    genInfo.enterSiteName('Auto Test Test')
    genInfo.clickOnSaveButton()
})

Then('Go to Contract details',()=>{

    feeFixed.clickOnContractDetailTab()
    cy.wait(1000)
    feeFixed.clickOnEditButton()
    feeFixed.expandFixedFee()
    // feeFixed.enableFixedFee()
})

Then('Enter the details for Fixed Fee and save it',()=>{

    feeFixed.enterFixedFee()
    feeFixed.clickOnSaveButton({force:true})
    cy.wait(2000)
    feeFixed.clickOnEditButton()
    feeFixed.clickOnSaveButton({force:true})
})

Then('Genrate a Statement for the site',()=>{

    commonCustomer.getMainCustomerButton()
    commonCustomer.getSearchBar()
    conDetail.locators.generateStatementforSite('0500').click()
    cy.wait(1000)
    conDetail.locators.adminProcessforStatementButton().click()

})

Then('Verify that the generated statement includes all General Info and reflects Fixed fee',()=>{


    commonStatement.getMainStatementsButton()
    cy.wait(2000)
    // commonStatement.getSearchBar()
    // cy.wait(1000)
    feeFixed.waitForStatement(5)
    feeFixed.viewStamentforsite()

})

When('I configure side id for fixed fee',()=>{
   feeFixed.insertAllDetailsForFixedFee()
})

Then("I generate Statement for the site",()=>{
    feeFixed.iGenerateStatementOfTheSiteId(FixedFeedata.site_id)
})

Then("I generate Statement for amount fixed fee site",()=>
{
    feeFixed.iGenerateStatementOfTheSiteId(FixedFeedata.amount_site_id)
})

Then("I verify fixed fee details from the invoice",()=>{
    commonStatement.getMainStatementsButton()
    cy.wait(2000)
    feeFixed.verifyFixedFeeDetailsFromInvoice(FixedFeedata.site_id)
    
})

Then("I verify fixed fee entries of amount from the invoice",()=>
{
    commonStatement.getMainStatementsButton()
    cy.wait(2000)
    feeFixed.verifyFixedFeeAmountFromInvoice(FixedFeedata.amount_site_id)
})

When("I negivate to contract details page",()=>{
    feeFixed.iNevigateToContractDetailsPage()
})

When("I negivate to contract details page of random option",()=>
{
    feeFixed.iNevigateToContractDetailsPageForRandomOption()
})

Then("I verify toggle button is getting on and off successfully",()=>{
    feeFixed.verifyToggleButtonIsGettingOnAndOffSuccessfully()
})

When("I click on contract details page edit buttton",()=>{
    feeFixed.iClickOnContractDetailsPageEditButton()
})

Then("I verify add button gets enable and disable when toggle button gets on and off",()=>
{
    feeFixed.verifyAddButtonGetsEnableadDisableWhenToggleButtonGetsOnAndOff()
})


Then("I select random option for fixed fee to generate the statement and verify",()=>
{
    feeFixed.selectRandomOptionForFixedFeeAndGenerateStatementAndVerify(FixedFeedata.amount_site_id)
})

When("I insert amounts for fixed fee options",()=>
{
    feeFixed.insertAmountsForFixedFeeOptions()
    feeFixed.iGenerateStatementOfTheSiteId(FixedFeedata.amount_site_id)
})