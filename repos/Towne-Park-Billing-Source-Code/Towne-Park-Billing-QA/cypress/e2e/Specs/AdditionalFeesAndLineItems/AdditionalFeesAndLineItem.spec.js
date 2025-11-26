import { Given, Then, When } from "@badeball/cypress-cucumber-preprocessor";
import CustomerMainPageCommon from '../../Common/customerMainPageCommon'
import AdditionalFeesAndLineItemPageObject from '../../Pages/Customer/ContractDetail/AdditionalFeesAndLineItemPageObject'
import "cypress-xpath"


const commonCustomer = new CustomerMainPageCommon()
const AdditionalFeesAndLineItem = new AdditionalFeesAndLineItemPageObject()

Given('Log into application', () => 
{
    

    cy.visit(Cypress.env('baseUrl'));
    commonCustomer.ClickOnSignInWithMicrosoftButton()
    cy.loginWithSSO()

})


When('I configure side id for additional fees and line items',()=>
{
    AdditionalFeesAndLineItem.configureSiteIdForAdditionalFeesAndLineItemsForMidMonthBilling()
})

When('I generate statement for additional fees or line items',()=>
{
    AdditionalFeesAndLineItem.generateStatementForAdditionalFeesOrLineItems()
})

Then('I verify additional fees or line items details from invoice',()=>
{
    AdditionalFeesAndLineItem.verifyAdditionalFeesOrLineItemsDetailsFromInvoice()
})

When('I nevigate to contract detials page',()=>
{
    AdditionalFeesAndLineItem.nevigateToContractDetialsPage()
})

Then('I verify all toggle button is working as expected',()=>
{
    AdditionalFeesAndLineItem.verifyToggleButtonWorking()
})