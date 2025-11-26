/// <reference types="cypress"/>

import ContractDetailsLocators from '../../../Locator/Customers/ContractDetailsLocators'
import CustomersLoctors from '../../../Locator/Customers/CustomersLocators'
import ViewDetailsStatementLocators from '../../../Locator/Statements/ViewDetailsStatementLocators'
import StatementMainPageCommon from '../../../Common/statementMainPageCommon'

const conD = new ContractDetailsLocators()
const custLoc=new CustomersLoctors()
const commonStatement = new StatementMainPageCommon()
const stateMultiple = new ViewDetailsStatementLocators()
const additionalFeeLineItemData=require('../../../../fixtures/AdditionalFeesAndLineItem/AdditionalFeesAndLineItem.json').AdditionalFeeLineItem;


export default class FixedFeePageObjects {

searchSiteId()
{
    custLoc.locator.siteidsearchbox().click().type(additionalFeeLineItemData.site_id);
    cy.wait(2000);
}

clickOnViewDetailButton()
{
  custLoc.locator.viewdetailbtn().should("be.visible").click()
  cy.wait(1000);
}

clickOnContractDetailButton()
{
  conD.locators.contractdetailsButton().should("be.visible").click()
  cy.wait(1000);
}

clickEditButton()
{
  conD.locators.editButton().should("be.visible").click()
}

insertDeviationPercentageAndAmount()
{
  conD.locators.deviationpercentage().should("be.visible").click().clear().type(additionalFeeLineItemData.deviation_percentage)
  conD.locators.deviationamount().should("be.visible").click().clear().type(additionalFeeLineItemData.deviation_amount)
}

clickAdditionalFeesOrLineItemsToExpand()
{
  conD.additionalFeesAndLineItem.additionalFeesAndLineItemExpandButton().should("be.visible").click()
}

toggleOnMidMonthAdvanceButton()
{
    conD.additionalFeesAndLineItem.midMonthAdvanceToggelButton()
      .then($button => 
      {
          const status=$button.attr("data-state");
          if (status==="unchecked") 
          {
              cy.log('Toggle button is OFF, turning it ON');
              cy.wrap($button).click();
          } 
          else 
          {
              cy.log('Toggle button is already ON');
          }
      });
}

insertAdvancementAmount()
{
    conD.additionalFeesAndLineItem.advancementAmountTextBox().click().clear().type(additionalFeeLineItemData.advancement_amount)
}

selectLessMidMonthBillingLineItemTitle()
{
    conD.additionalFeesAndLineItem.linItemTitleButton().click()
    conD.additionalFeesAndLineItem.lessMidMonthBillingOption(additionalFeeLineItemData.lineItemTitleOption[0]).last().click()
}

toggleOnTowneParkDepositedRevenue()
{
    conD.additionalFeesAndLineItem.towneParkDepositedRevenueToggelButton()
      .then($button => 
      {
          const status=$button.attr("data-state");
          if (status==="unchecked") 
          {
              cy.log('Toggle button is OFF, turning it ON');
              cy.wrap($button).click();
          } 
          else 
          {
              cy.log('Toggle button is already ON');
          }
      });
}

toggleOnTowneParkResponsibleForParkingTax()
{
    
    conD.additionalFeesAndLineItem.towneParkResponsibleForParkingTaxToggelButton()
      .then($button => 
      {
          const status=$button.attr("data-state");
          if (status==="unchecked") 
          {
              cy.log('Toggle button is OFF, turning it ON');
              cy.wrap($button).click();
          } 
          else 
          {
              cy.log('Toggle button is already ON');
          }
      });
}

toggleOnBellServiceFee()
{
    conD.additionalFeesAndLineItem.bellServiceFeeToggelButton()
      .then($button => 
      {
          const status=$button.attr("data-state");
          if (status==="unchecked") 
          {
              cy.log('Toggle button is OFF, turning it ON');
              cy.wrap($button).click();
          } 
          else 
          {
              cy.log('Toggle button is already ON');
          }
      });
}

clickOnSaveButton()
{
  conD.locators.saveButton().should("be.visible").click()
  cy.wait(2000)
}

waitForSeconds(sec)
{
    cy.wait(sec)
}

clickCustomersButton()
{
  conD.locators.customersbuttonfromtopbar().should("be.visible").click()
  cy.wait(2000)
}

verifySuccessMsgPopup()
{
  conD.locators.successmsgpopup().should("be.visible")
}

verifySuccessFullySavedPopup()
{
  conD.locators.successfulltsavedpopup().should("be.visible")
}

configureSiteIdForAdditionalFeesAndLineItemsForMidMonthBilling()
{
    this.searchSiteId()
    this.clickOnViewDetailButton()
    this.clickOnContractDetailButton()
    this.clickEditButton()
    this.insertDeviationPercentageAndAmount()
    this.clickAdditionalFeesOrLineItemsToExpand()
    this.waitForSeconds(100)
    this.toggleOnMidMonthAdvanceButton()
    this.insertAdvancementAmount()
    this.selectLessMidMonthBillingLineItemTitle()
    this.toggleOnTowneParkDepositedRevenue()
    this.waitForSeconds(1000)
    this.toggleOnTowneParkResponsibleForParkingTax()
    this.toggleOnBellServiceFee()
    this.clickOnSaveButton()
    this.clickEditButton()
    this.clickOnSaveButton()
    this.waitForSeconds(2000)
    this.verifySuccessMsgPopup()
    this.verifySuccessFullySavedPopup()
    this.clickCustomersButton()
    this.waitForSeconds(2000)
}

clickGenerateStatementButton()
{
  custLoc.locator.generateStatementButton().should("be.visible").click()
}

clickAdminProceedButtonModalComponent()
{
  cy.wait(2000)
  custLoc.locator.adminProceedButtonFromModel().click()
  cy.wait(5000)
}

verifyRequestAcceptedPopupDisplayed()
{
  custLoc.locator.requestAcceptedPopup().should("be.visible")
}

verifySuccessfullPopupDisplayed()
{
  custLoc.locator.successfullPopup().should("be.visible")
}

generateStatementForAdditionalFeesOrLineItems()
{
    this.searchSiteId()
    this.clickGenerateStatementButton()
    this.clickAdminProceedButtonModalComponent()
    this.verifyRequestAcceptedPopupDisplayed()
    this.verifySuccessfullPopupDisplayed()
}

clickOnStatementDropdown()
{
    custLoc.locator.statementDropDown().last().click()
}

clickViewStatementButton()
{
    custLoc.locator.viewStatementButton().click()
}

clickStatementFromTopBar()
  {
    custLoc.locator.statementsButtonFromTopBar().click()
  }

verifyDetailsOfInvoiceForAdditionalFeesAndLineItem()
{
    const actualAmount=additionalFeeLineItemData.midMonthAdvance
    const formattedAmount = new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: 'USD',
      }).format(-actualAmount);
    conD.additionalFeesAndLineItem.invoiceDetailsForMidMonthAdvance(formattedAmount).should("be.visible")
}

waitForStatement(maxRetries)
{
    let retryCount = 0;
    // const statementXPath =xpath
    const statementXPath = `//td[contains(text(),'${additionalFeeLineItemData.site_id}')]/ancestor::tr//div[contains(text(),'Approval Team')]`;

    function checkStatement() {
        cy.wait(1000); // Brief wait for UI updates
        commonStatement.getSearchBar(additionalFeeLineItemData.site_id); // Perform search

        // Manually evaluate XPath without failing Cypress command
        cy.document().then((doc) => {
            cy.wait(5000)
            const result = doc.evaluate(
                statementXPath,
                doc,
                null,
                XPathResult.FIRST_ORDERED_NODE_TYPE,
                null
            );
            const element = result.singleNodeValue;

            if (element) {
                cy.log('✅ Statement found! Proceeding...');
            } else if (retryCount < maxRetries) {
                retryCount++;
                cy.log(`🔄 Reloading... Attempt ${retryCount}/${maxRetries}`);
                conD.locators.customersbuttonfromtopbar().click()
                conD.locators.statementsbuttonfromtopbar().click()
                cy.reload();
                cy.wait(20000).then(checkStatement); // Wait after reload and retry
            } else {
                cy.log('❌ Max retries reached. Statement not found.');
                throw new Error('Statement not found after maximum retries'); // Optionally fail the test
            }
        });
    }

    cy.wrap(null).then(checkStatement); // Start the retry loop
}

toggleOffMidMonthAdvanceButton()
{
    conD.additionalFeesAndLineItem.midMonthAdvanceToggelButton()
      .then($button => 
      {
          const status=$button.attr("data-state");
          if (status==="checked") 
          {
              cy.log('Toggle button is ON, turning it OFF');
              cy.wrap($button).click();
          } 
          else 
          {
              cy.log('Toggle button is already OFF');
          }
      });
}

toggleOffTowneParkResponsibleForParkingTax()
{
    
    conD.additionalFeesAndLineItem.towneParkResponsibleForParkingTaxToggelButton()
      .then($button => 
      {
          const status=$button.attr("data-state");
          if (status==="checked") 
          {
              cy.log('Toggle button is ON, turning it OFF');
              cy.wrap($button).click();
          } 
          else 
          {
              cy.log('Toggle button is already OFF');
          }
      });
}

toggleOffBellServiceFee()
{
    conD.additionalFeesAndLineItem.bellServiceFeeToggelButton()
      .then($button => 
      {
          const status=$button.attr("data-state");
          if (status==="checked") 
          {
              cy.log('Toggle button is ON, turning it OFF');
              cy.wrap($button).click();
          } 
          else 
          {
              cy.log('Toggle button is already OFF');
          }
      });
}

clickOnApproveStatementButton() {
    stateMultiple.locators.approveStatementButton(additionalFeeLineItemData.site_id).click()
}
  
clickOnYesApproveButton() {
    stateMultiple.locators.yesApproveButton().click()
}

toggleOffTowneParkDepositedRevenue()
{
    conD.additionalFeesAndLineItem.towneParkDepositedRevenueToggelButton()
      .then($button => 
      {
          const status=$button.attr("data-state");
          if (status==="checked") 
          {
              cy.log('Toggle button is ON, turning it OFF');
              cy.wrap($button).click();
          } 
          else 
          {
              cy.log('Toggle button is already OFF');
          }
      });
}

verifyAdditionalFeesOrLineItemsDetailsFromInvoice()
{
    this.waitForSeconds(2000)
    cy.reload()
    this.waitForSeconds(5000)
    cy.reload()
    this.waitForSeconds(3000)
    this.clickStatementFromTopBar()
    this.waitForStatement(15)
    this.clickOnStatementDropdown()
    this.clickViewStatementButton()
    this.verifyDetailsOfInvoiceForAdditionalFeesAndLineItem()
    conD.locators.invoiceCloseButton().click()
    cy.wait(1000)
    this.clickOnApproveStatementButton()
    this.clickOnYesApproveButton()
    cy.wait(1000)
    this.clickCustomersButton()
    this.searchSiteId()
    this.clickOnViewDetailButton()
    this.clickOnContractDetailButton()
    this.clickEditButton()
    this.clickAdditionalFeesOrLineItemsToExpand()
    this.waitForSeconds(100)
    this.toggleOffMidMonthAdvanceButton()
    this.waitForSeconds(1000)
    this.toggleOffTowneParkResponsibleForParkingTax()
    this.toggleOffTowneParkDepositedRevenue()
    this.toggleOffBellServiceFee()
    this.clickOnSaveButton()
    this.clickEditButton()
    this.clickOnSaveButton()
    this.waitForSeconds(2000)
    this.verifySuccessMsgPopup()
    this.verifySuccessFullySavedPopup()
    this.waitForSeconds(2000)
}

nevigateToContractDetialsPage()
{
    this.searchSiteId()
    this.clickOnViewDetailButton()
    this.clickOnContractDetailButton()
    this.clickEditButton()
}

midMonthAdvanceToggleButtonWorking()
{
    conD.additionalFeesAndLineItem.midMonthAdvanceToggelButton().click()
    conD.additionalFeesAndLineItem.midMonthAdvanceToggelButton()
      .then($button => 
      {
          const status=$button.attr("data-state");
          if (status==="checked") 
          {
              conD.additionalFeesAndLineItem.advancementAmountTextBox().should("exist")
              conD.additionalFeesAndLineItem.linItemTitleButton().should("exist")
          } 
          else 
          {
            conD.additionalFeesAndLineItem.advancementAmountTextBox().should("not.exist")
            conD.additionalFeesAndLineItem.linItemTitleButton().should("not.exist")
          }
      });
    
      conD.additionalFeesAndLineItem.midMonthAdvanceToggelButton().click()
      conD.additionalFeesAndLineItem.midMonthAdvanceToggelButton()
        .then($button => 
        {
            const status=$button.attr("data-state");
            if (status==="unchecked") 
            {
                conD.additionalFeesAndLineItem.advancementAmountTextBox().should("not.exist")
            } 
            else 
            {
              conD.additionalFeesAndLineItem.advancementAmountTextBox().should("exist")
            }
        });
        conD.additionalFeesAndLineItem.midMonthAdvanceToggelButton().click()
}

townePaekDepositedRevenueToggleButtonWorking()
{
    conD.additionalFeesAndLineItem.towneParkDepositedRevenueToggelButton().click()
    conD.additionalFeesAndLineItem.towneParkDepositedRevenueToggelButton()
      .then($button => 
      {
          const status=$button.attr("data-state");
          if (status==="checked") 
          {
              conD.additionalFeesAndLineItem.towneParkDepositedRevenueToggelOnButton().should("be.visible")
          }
      });
    
      conD.additionalFeesAndLineItem.towneParkDepositedRevenueToggelButton().click()
      conD.additionalFeesAndLineItem.towneParkDepositedRevenueToggelButton()
        .then($button => 
        {
            const status=$button.attr("data-state");
            if (status==="unchecked") 
            {
                conD.additionalFeesAndLineItem.towneParkDepositedRevenueToggelOffButton().should("be.visible")
            }
        });
}

towneParkResponsibleForParkingTaxToggleButtonWorking()
{
    conD.additionalFeesAndLineItem.towneParkResponsibleForParkingTaxToggelButton().click()
    conD.additionalFeesAndLineItem.towneParkResponsibleForParkingTaxToggelButton()
      .then($button => 
      {
          const status=$button.attr("data-state");
          if (status==="checked") 
          {
              conD.additionalFeesAndLineItem.towneParkResponsibleForParkingTaxOnToggelButton().should("be.visible")
          }
      });
    
      conD.additionalFeesAndLineItem.towneParkResponsibleForParkingTaxToggelButton().click()
      conD.additionalFeesAndLineItem.towneParkResponsibleForParkingTaxToggelButton()
        .then($button => 
        {
            const status=$button.attr("data-state");
            if (status==="unchecked") 
            {
                conD.additionalFeesAndLineItem.towneParkResponsibleForParkingTaxOffToggelButton().should("be.visible")
            }
        });
}

bellServiceFeeToggleButtonWorking()
{
    conD.additionalFeesAndLineItem.bellServiceFeeToggelButton().click()
    conD.additionalFeesAndLineItem.bellServiceFeeToggelButton()
      .then($button => 
      {
          const status=$button.attr("data-state");
          if (status==="checked") 
          {
              conD.additionalFeesAndLineItem.bellServiceFeeToggelOnButton().should("be.visible")
          }
      });
    
      conD.additionalFeesAndLineItem.bellServiceFeeToggelButton().click()
      conD.additionalFeesAndLineItem.bellServiceFeeToggelButton()
        .then($button => 
        {
            const status=$button.attr("data-state");
            if (status==="unchecked") 
            {
                conD.additionalFeesAndLineItem.bellServiceFeeToggelOffButton().should("be.visible")
            }
        });
}

verifyToggleButtonWorking()
{
    this.clickAdditionalFeesOrLineItemsToExpand()
    this.waitForSeconds(100)
    this.toggleOnMidMonthAdvanceButton()
    this.midMonthAdvanceToggleButtonWorking()
    this.townePaekDepositedRevenueToggleButtonWorking()
    this.towneParkResponsibleForParkingTaxToggleButtonWorking()
    this.bellServiceFeeToggleButtonWorking()

}

}

