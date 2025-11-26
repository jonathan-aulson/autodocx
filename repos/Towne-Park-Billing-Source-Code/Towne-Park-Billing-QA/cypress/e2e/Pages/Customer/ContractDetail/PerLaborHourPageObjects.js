/// <reference types="cypress"/>

import ContractDetailsLocators from '../../../Locator/Customers/ContractDetailsLocators'
import StatementMainPageCommon from '../../../Common/statementMainPageCommon'
import ViewDetailsStatementLocators from '../../../Locator/Statements/ViewDetailsStatementLocators'
import CustomersLoctors from '../../../Locator/Customers/CustomersLocators'
import FixedFeePageObjects from './FixedFeePageObject'
// import { should } from 'chai'

const conD = new ContractDetailsLocators()
const commonStatement = new StatementMainPageCommon()
const stateMultiple = new ViewDetailsStatementLocators()
const custLoc = new CustomersLoctors()
const PerLaborHourData = require('../../../../fixtures/PerLaborHour/PerLaborHour.json').PerLaborHour;

export default class PerLaborHourPageObjects {

  insertSiteId(site_id) {
    custLoc.locator.siteidsearchbox().click().clear().type(site_id);
    cy.wait(3000);
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
    conD.locators.deviationpercentage().should("be.visible").click().clear().type(PerLaborHourData.deviation_percentage)
    conD.locators.deviationamount().should("be.visible").click().clear().type(PerLaborHourData.deviation_amount)
  }

  clickPerLaborHourToExpand() {
    conD.perLaborHour.perLaborHourExpandButton().should("be.visible").click()
  }

  turnOnPerLaborHourToggleButton() {
    conD.perLaborHour.perLaborHourToggleOnButton()
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
  }

  turnOffPerLaborHourToggleButton() {
    conD.perLaborHour.perLaborHourToggleOnButton()
      .then($button => {
        const status = $button.attr("data-state");
        if (status === "checked") {
          cy.log('Toggle button is ON, turning it OFF');
          cy.wrap($button).click();
        }
        else {
          cy.log('Toggle button is already OFF');
        }
      });
  }

  clickAddButton() {
    conD.perLaborHour.addButton().click()
  }

  addOption() {
    conD.perLaborHour.multipleOption(PerLaborHourData.multipleOption).click()
  }

  selectAndInsertPerLaborHourOptionAndAmount() {
    const opt = PerLaborHourData.multipleOptions;
    opt.forEach(options => {
      conD.perLaborHour.addButton().click()
      cy.wait(90);
      cy.xpath(`//*[@class='h-full w-full rounded-[inherit]']//*[text()="${options}"]`).click()
      cy.wait(90);
      conD.perLaborHour.standardRateBox(options).click().clear().type(PerLaborHourData[options].standardRate / 10)
      conD.perLaborHour.overtimeRateBox(options).click().clear().type(PerLaborHourData[options].overtimeRate / 10)
      conD.perLaborHour.jobCodeBox(options).click().clear().type(PerLaborHourData[options].jobCode)
    });
  }

  waitForSeconds(sec) {
    cy.wait(sec * 1000)
  }

  clickOnSaveButton() {
    conD.locators.saveButton().should("be.visible").click()
    cy.wait(2000)
  }

  clickCustomersButton() {
    conD.locators.customersbuttonfromtopbar().should("be.visible").click()
    cy.wait(2000)
  }

  configurePerLaborHourForTheSite() {
    this.insertSiteId(PerLaborHourData.site_id)
    this.clickOnViewDetailButton()
    this.clickOnContractDetailButton()
    this.clickEditButton()
    this.insertDeviationPercentageAndAmount()
    this.clickPerLaborHourToExpand()
    this.turnOnPerLaborHourToggleButton()
    this.checkAndDeleteAllPerLaborHourEntries()
    this.selectAndInsertPerLaborHourOptionAndAmount()
    this.waitForSeconds(2)
    this.clickOnSaveButton()
    this.waitForSeconds(2)
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

  generateStatementForLaborPerHour() {
    this.insertSiteId(PerLaborHourData.site_id)
    this.clickGenerateStatementButton()
    this.clickAdminProceedButtonModalComponent()
    this.verifyRequestAcceptedPopupDisplayed()
    this.verifySuccessfullPopupDisplayed()
  }

  clickStatementFromTopBar() {
    custLoc.locator.statementsButtonFromTopBar().click()
  }

  // clickOnStatementDropdown()
  // {
  //   custLoc.locator.statementDropDown().last().click()
  // }

  clickOnStatementDropdown() {
    stateMultiple.locators.expandStatement(PerLaborHourData.site_id).click({ force: true })
    cy.wait(1000)
  }

  clickViewStatementButton() {
    custLoc.locator.viewStatementButton().click()
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

  // verifyPerLaborHourFromInvoice() {
  //   const opt = PerLaborHourData.multipleOptions;
  //   opt.forEach(option => {
  //     // Total Hours
  //     conD.perLaborHour.totalHoursWorked(option)
  //       .invoke('text')
  //       .then((text) => {
  //         const totalHours = parseFloat(text.match(/(\d+\.\d+)/)[0]);
  //         cy.log('Total Hours: ' + totalHours);

  //         // Service Fee Per Hour

  //         const serviceFeePerHour = PerLaborHourData[option].standardRate
  //         cy.log(serviceFeePerHour)

  //         cy.xpath(`//*[@class='flex flex-col gap-6 mx-auto max-w-10xl p-6 md:p-8 lg:p-10 relative']/div[@class='rounded-lg border bg-card text-card-foreground shadow-sm max-w-full']/div[@class='p-6 pt-0']//table//*[text()="${option}"]/following-sibling::td[@class='p-4 align-middle [&:has([role=checkbox])]:pr-0 text-right']`)
  //           .first().invoke('text')
  //           .then((text) => {
  //             const cleanText = text.replace(/,/g, '');
  //             const expectedRate = parseFloat(cleanText.replace('$', ''));
  //             cy.log(expectedRate)

  //             expect(expectedRate.toString()).to.contain(
  //               ((totalHours * serviceFeePerHour)).toString());
  //           })


  //       });
  //   });
  // }

  verifyPerLaborHourFromInvoice() {
    const opt = PerLaborHourData.multipleOptions;
    let totalAmount = 0;

    // Process each option and calculate total
    cy.wrap(opt).each((option) => {
      // Get total hours
      return conD.perLaborHour.totalHoursWorked(option)
        .invoke('text')
        .then((text) => {
          const totalHours = parseFloat(text.match(/(\d+\.\d+)/)[0]);
          const serviceFeePerHour = PerLaborHourData[option].standardRate;

          // Calculate amount for this option
          const calculatedAmount = totalHours * serviceFeePerHour;
          totalAmount += calculatedAmount;

          cy.log(`${option}: ${totalHours} hours × $${serviceFeePerHour} = $${calculatedAmount}`);

          // Verify individual amount on page
          return cy.xpath(`//*[@class='flex flex-col gap-6 mx-auto max-w-10xl p-6 md:p-8 lg:p-10 relative']/div[@class='rounded-lg border bg-card text-card-foreground shadow-sm max-w-full']/div[@class='p-6 pt-0']//table//*[text()="${option}"]/following-sibling::td[@class='p-4 align-middle [&:has([role=checkbox])]:pr-0 text-right']`)
            .first()
            .invoke('text')
            .then((text) => {
              const expectedRate = parseFloat(text.replace(/[$,]/g, ''));
              expect(expectedRate).to.equal(calculatedAmount);
            });
        });
    }).then(() => {
      // Format total as currency
      const formattedTotal = `$${totalAmount.toLocaleString('en-US', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
      })}`;

      cy.log(`Total Amount: ${formattedTotal}`);

      // Store for later use
      cy.wrap(formattedTotal).as('totalAmount');

      // Verify total on page
      // cy.contains(formattedTotal).should('be.visible');
      // cy.contains('Total Amount Due').should('be.visible');
      cy.xpath(`//*[span[contains(text(), "${formattedTotal}")] and span[contains(text(), "Total Amount Due")]]`).should('be.visible')
      cy.wait(1000)
    });
  }

  closeInvoiceButton() {
    conD.locators.invoiceCloseButton().click()
  }

  clickPerLaborHourDeleteButton() {
    conD.perLaborHour.perLaborHourDeleteButton().click({ multiple: true })
  }

  clickOnApproveStatementButton() {
    stateMultiple.locators.approveStatementButton(PerLaborHourData.site_id).click()
  }

  clickOnYesApproveButton() {
    stateMultiple.locators.yesApproveButton().click()
  }

  verifyPerLaborEntriesFromInvoice(siteId) {
    cy.wait(3000)
    this.waitForSeconds(40)
    cy.reload()
    cy.wait(3000)
    this.clickStatementFromTopBar()
    this.waitForStatement(15, `//td[contains(text(),'${siteId}')]/ancestor::tr//div[contains(text(),'Approval Team')]`, siteId)
    this.clickOnStatementDropdown()
    this.clickViewStatementButton()
    this.verifyPerLaborHourFromInvoice()
    this.closeInvoiceButton()
    this.clickOnApproveStatementButton()
    this.clickOnYesApproveButton()

    cy.wait(2000)
    this.clickCustomersButton()
  }

  nevigateToContractDetailsPage() {
    this.insertSiteId(PerLaborHourData.site_id2)
    this.clickOnViewDetailButton()
    this.clickOnContractDetailButton()
  }

  verifyCorrectStringPresent() {
    const opt = PerLaborHourData.multipleString;
    opt.forEach(options => {
      conD.perLaborHour.stringOfPerLaborHour(options).should("be.visible")
    });
  }

  verifyCorrectStringPresentForPerLaborHour() {
    this.clickEditButton()
    this.clickPerLaborHourToExpand()
    this.turnOnPerLaborHourToggleButton()
    this.verifyCorrectStringPresent()
  }

  verifyAddButtonEnableWhenPerLaborHourToggleButtonGetsOn() {
    this.clickEditButton()
    this.clickPerLaborHourToExpand()
    // this.turnOnPerLaborHourToggleButton()
    cy.xpath('//button[@data-qa-id="switch-component-perLaborHourEnabled"]/span')
      .then($el => {
        if ($el.attr('data-state') !== 'checked') {
          // If not checked, click to enable
          cy.xpath('//button[@data-qa-id="switch-component-perLaborHourEnabled"]').click();
        } else {
          // Already checked, no action needed
          cy.log('Switch is already enabled');
        }
      });
    
    this.clickOnSaveButton()
    cy.contains('Contract updated successfully!').should('be.visible')
    cy.xpath('//button[@data-qa-id="switch-component-perLaborHourEnabled"]/span').should('have.attr','data-state','checked')
    conD.perLaborHour.addButton().should("have.attr","disabled")
    this.clickEditButton()
    this.turnOffPerLaborHourToggleButton()
    this.clickOnSaveButton()
    cy.contains('Contract updated successfully!').should('be.visible')
    cy.xpath('//button[@data-qa-id="switch-component-perLaborHourEnabled"]/span').should('have.attr','data-state','unchecked')

  }

  checkAndDeleteAllPerLaborHourEntries() {

    const servicesSectionXPath = '//label[contains(text(),"Per Labor Hour")]/ancestor::div[@class="grid gap-4 p-4 border rounded-lg"]//label[contains(text(),"Jobs / Services")]';
    const deleteButtonXPath = '//button[contains(@data-qa-id, "button-removePerLaborHourJob")]';

    cy.log('Attempting to check and delete all per labor hour services...');

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