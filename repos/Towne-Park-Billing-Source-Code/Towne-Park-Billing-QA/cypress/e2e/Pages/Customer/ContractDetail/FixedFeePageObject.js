/// <reference types="cypress"/>

import ContractDetailsLocators from '../../../Locator/Customers/ContractDetailsLocators'
import StatementMainPageCommon from '../../../Common/statementMainPageCommon'
import ViewDetailsStatementLocators from '../../../Locator/Statements/ViewDetailsStatementLocators'
import CustomersLoctors from '../../../Locator/Customers/CustomersLocators'

const conD = new ContractDetailsLocators()
const commonStatement = new StatementMainPageCommon()
const stateMultiple = new ViewDetailsStatementLocators()
const custLoc = new CustomersLoctors()
const FixedFeedata = require('../../../../fixtures/FixedFee/FixedFee.json').FixedFee;

export default class FixedFeePageObjects {

  insertSiteId(site_id) {
    custLoc.locator.siteidsearchbox().click().clear().type(site_id);
    cy.wait(3000);
  }

  selectRandomOptionForFixedFeeAndGenerateStatementAndVerify(siteId) {
    this.insertDeviationPercentageAndAmount()
    this.expandFixedFee()
    this.clickOnToggleOffButtonAndVerify()
    this.selectRandomFixedFeeOptionAndInsertAmountAndVerifyInvoice(siteId)

  }

  selectRandomFixedFeeOptionAndInsertAmountAndVerifyInvoice(siteId) {
    conD.fixedFeeLocator.fixedfeeaddbutton().click();

    const options = FixedFeedata.options;

    if (options && options.length > 0) {
      const randomIndex = Math.floor(Math.random() * options.length);
      const randomOption = options[randomIndex];
      cy.log('Randomly selected option: ' + randomOption);
      cy.contains(randomOption).click();
      cy.wait(1000)
      //conD.fixedFeeLocator.fixedfeemultipleoption(randomOption).click().clear().clear().type(FixedFeedata[randomOption]);
      conD.fixedFeeLocator.fixedfeemultipleoption(randomOption).click().clear().type(FixedFeedata[randomOption] / 10);

      cy.wait(2000);
      this.clickOnSaveButton();
      cy.wait(2000);
      this.verifySuccessMsgPopup();
      this.verifySuccessFullySavedPopup();
      this.clickCustomersButton();
      this.iGenerateStatementOfTheSiteId(siteId);
      this.clickStatementFromTopBar();
      cy.wait(30000);
      cy.reload();
      cy.wait(30000);
      cy.reload();
      cy.wait(3000);
      this.waitForStatement(5, "//td[contains(text(),'0451')]/ancestor::tr//div[contains(text(),'Approval Team')]", siteId);
      this.clickOnStatementDropdown();
      this.clickViewStatementButton();

      // Use the correct XPath for the randomOption
      const amount = FixedFeedata[randomOption]
      const formattedAmount = new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: 'USD',
      }).format(amount);

      const combinedXPath = `//*[text()='${randomOption}']/following-sibling::*[text()='${formattedAmount}']`;
      cy.xpath(combinedXPath).should('be.visible');
    } else {
      cy.log('No options available to select.');
    }
    conD.locators.invoiceCloseButton().click()
    this.clickCustomersButton()
    this.insertSiteId(siteId)
    this.clickOnViewDetailButton()
    this.clickOnContractDetailButton()
    this.clickOnEditButton()
    this.expandFixedFee()
    //this.turnOnFixedFeeToggleButton()
    conD.fixedFeeLocator.fixedFeeDeleteButton().click({ multiple: true })
    conD.fixedFeeLocator.fixedFeeToggleOnButton().click()
    this.clickOnSaveButton()
    cy.wait(1000)
    this.clickOnEditButton()
    cy.wait(1000)
    this.clickOnSaveButton()
  }

  clickOnViewDetailButton() {
    custLoc.locator.viewdetailbtn().should("be.visible").click()
    cy.wait(1000);
  }

  clickOnContractDetailButton() {
    conD.locators.contractdetailsButton().should("be.visible").click()
    cy.wait(1000);
  }

  clickEditButton() {
    conD.locators.editButton().should("be.visible").click()
  }

  insertDeviationPercentageAndAmount() {
    conD.locators.deviationpercentage().should("be.visible").click().clear().type(FixedFeedata.deviation_percentage)
    conD.locators.deviationamount().should("be.visible").click().clear().type(FixedFeedata.deviation_amount)
  }

  clickFixedFeeToExpand() {
    conD.fixedFeeLocator.fixedFeeExpandButton().click()
  }

  turnOnFixedFeeToggleButton() {
    conD.fixedFeeLocator.togglebutton()
      .then($button => {
        const status = $button.attr("data-state");
        if (status === "unchecked") {
          cy.log('Toggle button is OFF, turning it ON');
          cy.wrap($button).click();
        }
        else {
          cy.log('Toggle button is already ON');
        }
      });
    this.checkAndDeleteAllFixedFeeServices()
  }

  selectAndInsertFixedFeeOptionAndAmount() {
    const opt = FixedFeedata.options;
    opt.forEach(option => {
      conD.fixedFeeLocator.fixedfeeaddbutton().click();
      cy.xpath(`//*[@class='h-full w-full rounded-[inherit]']//*[text()="${option}"]`).wait(500).click()
      cy.wait(500);
      conD.fixedFeeLocator.fixedfeemultipleoption(option).click().clear().type(FixedFeedata[option] / 10);
    });
  }

  clickOnSaveButton() {
    conD.locators.saveButton().should("be.visible").click()
    cy.wait(2000)
  }

  verifySuccessMsgPopup() {
    conD.locators.successmsgpopup().should("be.visible")
  }

  verifySuccessFullySavedPopup() {
    conD.locators.successfulltsavedpopup().should("be.visible")
  }

  clickCustomersButton() {
    conD.locators.customersbuttonfromtopbar().should("be.visible").click()
    cy.wait(2000)
  }

  deleteEntriesIfPresent() {
    conD.fixedFeeLocator.fixedFeeDeleteButton()
      .then(($deleteButton) => {
        if ($deleteButton.is(':visible')) {
          cy.log($deleteButton.length);
          cy.wrap($deleteButton).click({ multiple: true });
        } else {
          cy.log("All clear...");
        }
      });
  }

  insertAllDetailsForFixedFee() {

    this.insertSiteId(FixedFeedata.site_id)
    this.clickOnViewDetailButton()
    this.clickOnContractDetailButton()
    this.clickEditButton()
    this.insertDeviationPercentageAndAmount()
    this.clickFixedFeeToExpand()
    this.turnOnFixedFeeToggleButton()
    // this.deleteEntriesIfPresent()
    this.selectAndInsertFixedFeeOptionAndAmount();
    cy.wait(2000)
    this.clickOnSaveButton()
    cy.wait(2000)
    this.clickEditButton()
    this.clickOnSaveButton()
    cy.wait(2000)
    this.verifySuccessMsgPopup()
    this.verifySuccessFullySavedPopup()
    cy.wait(5000)
    this.clickCustomersButton()
  }

  insertAmountForAllOptions() {
    const opt = FixedFeedata.options;
    opt.forEach(option => {
      cy.xpath(`//*[text()='${option}']/ancestor::div[@class='flex items-start justify-between space-x-2']//input[@placeholder='Fee']`).wait(500).click().clear().type(FixedFeedata[option] / 10);
    });
  }

  clickFixedFeeAddButton() {
    conD.fixedFeeLocator.fixedfeeaddbutton().click();
  }

  insertAmountsForFixedFeeOptions() {
    this.insertSiteId(FixedFeedata.amount_site_id)
    this.clickOnViewDetailButton()
    this.clickOnContractDetailButton()
    this.clickEditButton()
    this.insertDeviationPercentageAndAmount()
    this.expandFixedFee()
    this.turnOnFixedFeeToggleButton()
    //conD.fixedFeeLocator.fixedFeeDeleteButton().click({multiple:true})
    //this.checkAndDeleteAllFixedFeeServices()
    this.clickFixedFeeAddButton()
    this.insertAmountForAllOptions()
    this.clickOnSaveButton()
    cy.wait(2000)
    this.clickEditButton()
    this.clickOnSaveButton()
    cy.wait(2000)
    this.verifySuccessMsgPopup()
    this.verifySuccessFullySavedPopup()
    cy.wait(5000)
    this.clickCustomersButton()
  }

  clickGenerateStatementButton() {
    custLoc.locator.generateStatementButton().should("be.visible").click()
  }

  clickAdminProceedButtonModalComponent() {
    cy.wait(2000)
    custLoc.locator.adminProceedButtonFromModel().click()
    cy.wait(5000)
  }

  verifyRequestAcceptedPopupDisplayed() {
    custLoc.locator.requestAcceptedPopup().should("be.visible")
  }

  verifySuccessfullPopupDisplayed() {
    custLoc.locator.successfullPopup().should("be.visible")
  }

  iGenerateStatementOfTheSiteId(siteId) {
    this.insertSiteId(siteId)
    this.clickGenerateStatementButton()
    this.clickAdminProceedButtonModalComponent()
    this.verifyRequestAcceptedPopupDisplayed()
    this.verifySuccessfullPopupDisplayed()
  }

  clickOnContractDetailTab() {
    conD.locators.contractDetailsTab().click()
  }

  clickOnEditButton() {
    conD.locators.editButton().click()
    cy.wait(2000)
  }

  clickOnCancelButton() {
    conD.locators.cancelButton().click()
  }

  expandFixedFee() {
    conD.fixedFeeLocator.fixedFeeExpandButton().click()
    cy.wait(2000)
  }

  // enableFixedFee() {
  //   conD.fixedFeeLocator.enableFixedFeeButton().last().invoke('attr', 'aria-checked').then(($btn) => {
  //     if ($btn === 'false') {
  //       cy.log('Toggle is OFF, turning it ON...');
  //       cy.get('button[id=":r2uv:-form-item"]').click();
  //     } else {
  //       cy.log('Toggle is already ON');
  //     }
  //   })
  // }

  enterFixedFee() {
    conD.fixedFeeLocator.displayNameTextBox().clear().type('Auto Test')
    cy.wait(2000)
    conD.fixedFeeLocator.feeTextBox().clear().type('600')
    cy.wait(2000)
  }


  clickStatementFromTopBar() {
    custLoc.locator.statementsButtonFromTopBar().click()
  }

  // clickOnStatementDropdown()
  // {
  // custLoc.locator.statementDropDown().last().click()
  // }

  clickOnStatementDropdown() {
    stateMultiple.locators.expandStatement(FixedFeedata.site_id).click({ force: true })
    cy.wait(1000)
  }

  clickViewStatementButton() {
    custLoc.locator.viewStatementButton().click()
  }

  clickOnApproveStatementButton() {
    stateMultiple.locators.approveStatementButton(FixedFeedata.site_id).click()
  }

  clickOnYesApproveButton() {
    stateMultiple.locators.yesApproveButton().click()
  }


  verifyDetailsOfInvoice() {
    const opt = FixedFeedata.options;
    const amt = FixedFeedata.amount;

    // Calculate and format total
    const total = amt.reduce((sum, amount) => {
      const numericAmount = parseFloat(amount.replace(/[$,]/g, ''));
      return sum + numericAmount;
    }, 0);

    const formattedTotal = `$${total.toLocaleString('en-US', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    })}`;

    console.log(`Expected Total: ${formattedTotal}`);

    // Verify individual items
    opt.forEach((opt, index) => {
      const combinedXPath = `//*[text()='${opt}']/following-sibling::*[text()='${amt[index]}']`;
      cy.wait(500);
      cy.xpath(combinedXPath).scrollIntoView({ offset: { top: -100, left: 0 } }).should('be.visible');
    });

    // Verify total if it exists on the page
    cy.xpath(`//*[span[contains(text(), "${formattedTotal}")] and span[contains(text(), "Total Amount Due")]]`).should('be.visible')
    cy.wait(1000)

  }

  verifyFixedFeeDetailsFromInvoice(siteId) {
    cy.wait(30000)
    cy.reload()
    cy.wait(3000)
    this.clickStatementFromTopBar()
    this.waitForStatement(10, `//td[contains(text(),'${siteId}')]/ancestor::tr//div[contains(text(),'Approval Team')]`, siteId)
    this.clickOnStatementDropdown()
    this.clickViewStatementButton()
    this.verifyDetailsOfInvoice()
    conD.locators.invoiceCloseButton().click()
    this.clickOnApproveStatementButton()
    this.clickOnYesApproveButton()
    cy.wait(2000)
    this.clickCustomersButton()
  }

  closeInvoiceButton() {
    conD.locators.invoiceCloseButton().click()
  }

  verifyFixedFeeAmountFromInvoice(siteId) {
    cy.wait(30000)
    cy.reload()
    cy.wait(3000)
    this.clickStatementFromTopBar()
    this.waitForStatement(15, `//td[contains(text(),'${siteId}')]/ancestor::tr//div[contains(text(),'Approval Team')]`, siteId)
    this.clickOnStatementDropdown()
    this.clickViewStatementButton()
    this.verifyDetailsOfInvoice()
    this.closeInvoiceButton()
    this.clickCustomersButton()
  }

  clickOnToggleOnButtonAndVerify() {
    this.enableFixedFee()
    // conD.fixedFeeLocator.fixedFeeToggleButton().should("be.visible").click()
    // conD.fixedFeeLocator.fixedFeeToggleOnButton().should("be.visible")
    this.clickOnSaveButton()
    cy.contains('Contract updated successfully!').should('be.visible')
    conD.fixedFeeLocator.fixedFeeToggleButton().should("have.attr","data-state","checked")
    cy.wait(1000)
  }

  enableFixedFee() {
        conD.fixedFeeLocator.enableFixedFeeButton().last().invoke('attr', 'aria-checked').then(($btn) => {
            if ($btn === 'false') {
                cy.log('Toggle is OFF, turning it ON...');
                cy.xpath("(//button[contains(text(),'Fixed Fee')]/ancestor::h3/following-sibling::div//button)[1]").click();
            } else {
                cy.log('Toggle is already ON');
            }
        })
    }

    

  clickOnToggleOffButtonAndVerify() {
    conD.fixedFeeLocator.fixedFeeToggleButton().should("be.visible").click()
    // conD.fixedFeeLocator.fixedFeeToggleOnButton().should("be.visible")
    this.clickOnSaveButton()
    cy.contains('Contract updated successfully!').should('be.visible')
    
    conD.fixedFeeLocator.fixedFeeToggleButton().should("have.attr","data-state","unchecked")
    cy.wait(1000)

  }

  iNevigateToContractDetailsPage() {
    this.insertSiteId(FixedFeedata.site_id_for_other_validations)
    this.clickOnViewDetailButton()
    this.clickOnContractDetailButton()
  }

  iNevigateToContractDetailsPageForRandomOption() {
    this.insertSiteId(FixedFeedata.amount_site_id)
    this.clickOnViewDetailButton()
    this.clickOnContractDetailButton()
  }

  iClickOnContractDetailsPageEditButton() {
    this.clickEditButton()
  }

  verifyToggleButtonIsGettingOnAndOffSuccessfully() {
    this.clickEditButton()
    this.expandFixedFee()
    this.clickOnToggleOnButtonAndVerify()
    this.clickEditButton()
    this.clickOnToggleOffButtonAndVerify()
    // this.clickOnEditButton()
    // conD.fixedFeeLocator.fixedFeeToggleButton().should("be.visible").click()
    // this.clickOnSaveButton()
    // cy.contains('Contract updated successfully!').should('be.visible')
  }

  verifyAddButtonIsEnable() {
    conD.fixedFeeLocator.fixedfeeaddbutton().should("be.enabled")
  }

  verifyAddButtonIsNotEnable() {
    conD.fixedFeeLocator.fixedfeeaddbutton().should("not.be.enabled")
  }

  verifyAddButtonGetsEnableadDisableWhenToggleButtonGetsOnAndOff() {
    this.expandFixedFee()
    conD.fixedFeeLocator.fixedFeeToggleButton().click()
    conD.fixedFeeLocator.fixedfeeaddbutton().should("be.visible")
    conD.fixedFeeLocator.fixedFeeToggleButton().click()
    conD.fixedFeeLocator.fixedfeeaddbutton().should("be.visible")
  }




  waitForStatement(maxRetries, xpath, siteId) {
    let retryCount = 0;
    const statementXPath = xpath

    function checkStatement() {
      cy.wait(1000); // Brief wait for UI updates
      commonStatement.getSearchBar(siteId); // Perform search

      // Manually evaluate XPath without failing Cypress command
      cy.document().then((doc) => {
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
          cy.reload();
          cy.wait(30000).then(checkStatement); // Wait after reload and retry
        } else {
          cy.log('❌ Max retries reached. Statement not found.');
          throw new Error('Statement not found after maximum retries'); // Optionally fail the test
        }
      });
    }

    cy.wrap(null).then(checkStatement); // Start the retry loop
  }


  viewStamentforsite(siteId) {
    cy.xpath(`//td[contains(text(),'${siteId}')]/ancestor::tr//div[@class="flex items-center space-x-2"]`).first().click({ force: true })
    cy.xpath('//span[contains(text(),"View Invoice")]').click({ force: true })
    cy.wait(2000)

    cy.xpath('//td[contains(text(),"Auto Test")]').should('exist')

    cy.xpath('//tr/td[contains(text(),"$6,000.00")]').should('exist')
  }

  checkAndDeleteAllFixedFeeServices() {

    const servicesSectionXPath = '//label[contains(text(),"Enable Fixed Fee")]/ancestor::div[@class="grid gap-4 p-4 border rounded-lg"]//label[contains(text(),"Services")]';
    const deleteButtonXPath = '//button[contains(@data-qa-id, "button-removeFixedFeeService")]';

    cy.log('Attempting to check and delete all fixed fee services...');

    // Use cy.document() to get the native `document` object.
    cy.document().then(document => {
      // --- 1. Check if the Services Section Exists ---
      // `document.evaluate()` is a NATIVE BROWSER API for executing XPath expressions.
      const servicesSectionNode = document.evaluate(
        servicesSectionXPath, // Using the predefined XPath
        document,
        null,
        XPathResult.FIRST_ORDERED_NODE_TYPE,
        null
      ).singleNodeValue;

      // If services section doesn't exist, log it and exit the function.
      if (!servicesSectionNode) {
        cy.log(`Services section not found (using XPath: "${servicesSectionXPath}"), continuing with test.`);
        return; // Nothing to delete if the section isn't there.
      }

      // If the section exists, log it and proceed to check for delete buttons.
      cy.log(`Services section found (using XPath: "${servicesSectionXPath}"). Proceeding to check for delete buttons.`);

      // --- 2. Define a Recursive Function to Delete Services ---
      // This function will repeatedly find and click delete buttons.
      const deleteAllServicesRecursively = () => {
        // It's important to re-query the document inside the recursive call,
        // as the DOM structure changes after each deletion.
        cy.document().then(currentDoc => {
          // `document.evaluate()` is used again for the delete button.
          const deleteButtonNode = currentDoc.evaluate(
            deleteButtonXPath, // Using the predefined XPath
            currentDoc,
            null,
            XPathResult.FIRST_ORDERED_NODE_TYPE,
            null
          ).singleNodeValue;

          // If a delete button DOM node is found:
          if (deleteButtonNode) {
            cy.log(`Delete button found (using XPath: "${deleteButtonXPath}"). Clicking it.`);

            // `cy.wrap(deleteButtonNode)` takes the NATIVE DOM element
            // and wraps it in a Cypress object. This allows us to use
            // Cypress commands like `.click()` on it.
            cy.wrap(deleteButtonNode).click();

            // Wait for a short period to allow the DOM to update.
            // Consider more robust waits if this proves flaky (e.g., waiting for the button to disappear).
            cy.wait(300); // ms

            // Recursive call: run the function again to check for more delete buttons.
            deleteAllServicesRecursively();
          } else {
            // Base case for the recursion: no more delete buttons are found.
            cy.log(`No more delete buttons found (using XPath: "${deleteButtonXPath}"). Deletion process complete or no services were present initially.`);
          }
        });
      };

      // Start the recursive deletion process.
      deleteAllServicesRecursively();
    });
  }

}
