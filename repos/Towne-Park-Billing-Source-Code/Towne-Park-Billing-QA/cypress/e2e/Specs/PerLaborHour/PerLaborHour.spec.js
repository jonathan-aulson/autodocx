import { Given, Then, When } from "@badeball/cypress-cucumber-preprocessor";
import CustomerMainPageCommon from '../../Common/customerMainPageCommon';
import PerLaborHourPageObject from '../../Pages/Customer/ContractDetail/PerLaborHourPageObjects';
import "cypress-xpath"


const commonCustomer = new CustomerMainPageCommon()
const perLaborHour = new PerLaborHourPageObject()
const PerLaborHourData=require('../../../fixtures/PerLaborHour/PerLaborHour.json').PerLaborHour;

Given('Log into application', () => 
{
    

    cy.visit(Cypress.env('baseUrl'));
    commonCustomer.ClickOnSignInWithMicrosoftButton()
    cy.loginWithSSO()

})

When('I configure per labor hour for the site',() =>
{
    perLaborHour.configurePerLaborHourForTheSite()
})

When('I generate statement for per labor hour',()=>
{
    perLaborHour.generateStatementForLaborPerHour()
})

Then('I should get entries in the invoice',()=>
{
    perLaborHour.verifyPerLaborEntriesFromInvoice(PerLaborHourData.site_id)
})

When('I nevigate to contract details',()=>
{
    perLaborHour.nevigateToContractDetailsPage()
})

Then('I verify correct string present for per labor hour',()=>
{
    perLaborHour.verifyCorrectStringPresentForPerLaborHour()
})

Then('I verify add button enable when per labor hour toggle button gets on',()=>
{
    perLaborHour.verifyAddButtonEnableWhenPerLaborHourToggleButtonGetsOn()
})