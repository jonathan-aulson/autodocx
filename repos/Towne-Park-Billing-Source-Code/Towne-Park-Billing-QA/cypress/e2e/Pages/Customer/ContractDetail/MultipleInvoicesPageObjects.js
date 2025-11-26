/// <reference types="cypress"/>

import ContractDetailsLocators from '../../../Locator/Customers/ContractDetailsLocators'
import ViewDetailsStatementLocators from '../../../Locator/Statements/ViewDetailsStatementLocators'
import StatementMainPageCommon from '../../../Common/statementMainPageCommon'
import CustomerMainPageCommon from '../../../Common/customerMainPageCommon'
import Helper from '../../../../support/Helper'


const conD = new ContractDetailsLocators()
const stateMultiple = new ViewDetailsStatementLocators()
const commonStatement = new StatementMainPageCommon()
const commonMain = new CustomerMainPageCommon()
const multipleInvoiceData = require('../../../../fixtures/MultipleInvoices/MultipleInvoices.json').MultipleInvoices;


const siteIDData = multipleInvoiceData.SiteId[0]
const title1Data = multipleInvoiceData.title1Invoice[0]
const title2Data = multipleInvoiceData.title2Invoice[0]
const description1Data = multipleInvoiceData.Description1Invoice[0]
const description2Data = multipleInvoiceData.Description2Invoice[0]
// const FixedFeeOpt = multipleInvoiceData.FixedFeeOptions

let storedText;
let storedText2;

export default class FixedFeePageObjects {

    clickOnMainPage() {
        commonMain.getMainCustomerButton()
    }

    clickOnGeneralInfoTab() {
        conD.locators.contractDetailsTab().click()
    }

    clickOnContractDetailTab() {
        conD.locators.contractDetailsTab().click()
    }

    clickOnEditButton() {
        conD.locators.editButton().click()
    }

    clickOnSaveButton() {
        conD.locators.saveButton().should('be.enabled').wait(500).click()
    }

    getSearchBar() {
        conD.locators.searchBar().type(siteIDData)
    }

    viewSiteDetails() {
        conD.mulitipleInvoicesLocator.viewDetailforSite(multipleInvoiceData.SiteId).click({ force: true })
    }

    // clickOnCancelButton() {
    //     conD.locators.cancelButton().click()
    // }


    expandMultipleInvoices() {
        conD.mulitipleInvoicesLocator.multipleInvioveExpandButton().click()
    }

    enableMultipleInvoice() {
        conD.mulitipleInvoicesLocator.enableMulipleInvoice().invoke('attr', 'aria-checked').then(($btn) => {
            if ($btn === 'false') {
                cy.log('Toggle is OFF, turning it ON...');
                cy.xpath("(//button[contains(text(),'Multiple Invoices')]/ancestor::h3/following-sibling::div//button)[1]").click();
            } else {
                cy.log('Toggle is already ON');
            }
        })
    }

    enterInvoiceInfo() {
        conD.mulitipleInvoicesLocator.invoiceTitle1TextBox().clear().type(title1Data)
        conD.mulitipleInvoicesLocator.invoiceDescription1TextBox().clear().type(description1Data)
        cy.wait(1000)
        conD.mulitipleInvoicesLocator.invoiceTitle2TextBox().clear().type(title2Data)
        conD.mulitipleInvoicesLocator.invoiceDescription2TextBox().clear().type(description2Data)
        cy.wait(1000)
    }

    expandFixedFee() {
        conD.fixedFeeLocator.fixedFeeExpandButton().click()
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

    disableFixedFee() {
        conD.fixedFeeLocator.enableFixedFeeButton().last().click()
    }

    selectAndInsertFixedFeeOptionAndAmount() {

        conD.fixedFeeLocator.fixedfeeaddbutton().click()
        cy.wait(500)
        cy.contains('Bell').click({ force: true })
        cy.wait(500)
        conD.fixedFeeLocator.feeFixedInput('Bell').focus().clear().type('1500', { force: true }).type('{backspace}')
        cy.wait(2000)
        conD.fixedFeeLocator.fixedfeeaddbutton().click()
        cy.wait(500)
        cy.contains('Parking Management').click({ force: true })
        cy.wait(500)
        conD.fixedFeeLocator.feeFixedInput('Parking Management').focus().clear().type('3000', { force: true }).type('{backspace}')
    }

    fetchAndStoreText1(text1) {
        cy.xpath(`(//p[contains(text(),'${text1}')]/ancestor::div[@class="flex items-start justify-between space-x-2"]//input)[3]`)
            .invoke('val')
            .then((text) => {
                storedText = text; // Store value in a variable
                cy.log('Stored Text:', storedText);
            });
    }

    fetchAndStoreText2(text2) {
        cy.xpath(`(//p[contains(text(),'${text2}')]/ancestor::div[@class="flex items-start justify-between space-x-2"]//input)[3]`)
            .invoke('val')
            .then((text) => {
                storedText2 = text; // Store value in a variable
                cy.log('Stored Text:', storedText2);
            });
    }

    enterFixedFee() {
        conD.fixedFeeLocator.selectInvoiceOption1('Bell', '1')
        this.fetchAndStoreText1('Bell')
        cy.wait(1000)
        conD.fixedFeeLocator.selectInvoiceOption2('Parking Management', '2')
        this.fetchAndStoreText2('Parking Management')
        cy.wait(1000)

    }

    generateStatement() {
        conD.locators.generateStatementforSite('0245').click()
        cy.wait(1000)
        conD.locators.adminProcessforStatementButton().click()
    }

    waitForStatement(maxRetries) {
        let retryCount = 0;
        const statementXPath = `//td[contains(text(),'${siteIDData}')]/ancestor::tr//div[contains(text(),'Approval Team')]`;

        function checkStatement() {
            cy.wait(1000); // Brief wait for UI updates
            conD.locators.searchBar().type(siteIDData)// Perform search

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
                    cy.wait(20000).then(checkStatement); // Wait after reload and retry
                } else {
                    cy.log('❌ Max retries reached. Statement not found.');
                    throw new Error('Statement not found after maximum retries'); // Optionally fail the test
                }
            });
        }

        cy.wrap(null).then(checkStatement); // Start the retry loop
    }


    expandStamentForSite() {
        stateMultiple.locators.expandStatement(siteIDData).click({ force: true })
        cy.wait(1000)
    }

    viewStamentforsite1(invoiceNum) {
        cy.getYearMonth(-1).then((yearMonth) => {  // Get the previous month
            stateMultiple.locators.viewStatementForSiteInvoice1(siteIDData, yearMonth, invoiceNum).click();
        });
        cy.wait(2000)
    }

    viewStamentforsite2(invoiceNum) {
        cy.getYearMonth(-1).then((yearMonth) => {  // Get the previous month
            stateMultiple.locators.mulViewStatementForSiteInvoice2(siteIDData, yearMonth, invoiceNum).click();
        });
        cy.wait(2000)
    }

    closeInvocieStatement() {
        stateMultiple.locators.closeStatement().click()
    }

    clickOnApproveStatementButton() {
        stateMultiple.locators.approveStatementButton(siteIDData).click()
    }

    clickOnYesApproveButton() {
        stateMultiple.locators.yesApproveButton().click()
    }

    verifyFirstInvoice() {
        cy.contains(title1Data).should('exist')
        cy.contains(description1Data).should('exist')
        cy.wrap(null).then(() => { // Ensures Cypress waits
            cy.contains(storedText).should('be.visible'); // Use storedText in cy.contains
        });
    }


    verifyFirstInvoiceAmount() {
        // cy.contains(title1Data).should('exist')
        // cy.contains(description1Data).should('exist')
        // cy.wrap(null).then(() => { // Ensures Cypress waits
        //     cy.contains(storedText).should('be.visible'); // Use storedText in cy.contains
        // });


        cy.xpath('//tr[@data-qa-id="row-lineItem-0-invoice"]/td[contains(text(),"$1,500.00")]').should('be.visible')
        cy.xpath('//*[span[contains(text(), "$1,500.00")] and span[contains(text(), "Total Amount Due")]]').should('be.visible')


    }

    verifySecondInvoice() {
        cy.contains(title2Data).should('exist')
        cy.contains(description2Data).should('exist')
        cy.wrap(null).then(() => { // Ensures Cypress waits
            cy.contains(storedText2).should('be.visible'); // Use storedText in cy.contains
        });
    }

    verifySecondInvoiceAmount() {
        // cy.contains(title1Data).should('exist')
        // cy.contains(description1Data).should('exist')
        // cy.wrap(null).then(() => { // Ensures Cypress waits
        //     cy.contains(storedText).should('be.visible'); // Use storedText in cy.contains
        // });


        cy.xpath('//tr[@data-qa-id="row-lineItem-0-invoice"]/td[contains(text(),"$3,000.00")]').should('be.visible')
        cy.xpath('//*[span[contains(text(), "$3,000.00")] and span[contains(text(), "Total Amount Due")]]').should('be.visible')


    }

    clearContactDetail() {
        this.clickOnMainPage()
        this.getSearchBar()
        cy.wait(1000)
        this.viewSiteDetails()
        cy.wait(1000)
        this.clickOnContractDetailTab()
        this.clickOnEditButton()
        cy.wait(2000)
        this.expandMultipleInvoices()
        cy.wait(1000)
        this.disableMultipleInvoice()
        this.expandFixedFee()
        const xpathSelector = '//button[@class="inline-flex items-center justify-center whitespace-nowrap rounded-md text-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:pointer-events-none disabled:opacity-50 hover:bg-accent hover:text-accent-foreground h-9 px-4 py-2"]';

        cy.xpath(xpathSelector).then((elements) => {
            // Check if elements exist
            if (elements.length > 0) {
                // Iterate through each element and click
                cy.wrap(elements).each((element) => {
                    cy.wrap(element).click();
                });
            } else {
                // Log if no elements are found
                cy.log('No matching elements found');
            }
        });

        this.disableFixedFee()
        cy.wait(1000)

        this.clickOnSaveButton()
        cy.contains('Contract updated successfully!').should('exist')
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



    clickOnAddInvoice() {
        conD.mulitipleInvoicesLocator.addInvoiceButton().click()
    }

    clickOnCancelButton() {
        conD.locators.cancelButton().click({ force: true })
        conD.locators.cancelYesButton().click({ force: true })
    }

    // addFixedFeeToVerify() {

    // }

    disableMultipleInvoice() {
        conD.mulitipleInvoicesLocator.enableMulipleInvoice().click()
        cy.wait(1000)
        conD.mulitipleInvoicesLocator.disableConfirmButton().should('be.visible').click()
    }

    verifyAfterNewInvoiceIsAdded() {
        cy.get('input[placeholder="Title for Invoice 3"]').should('be.visible')
        cy.get('input[placeholder="Description for Invoice 3"]').should('be.visible')
        cy.xpath('//div[@class="space-y-4 mt-2"]//select/option[@value="3"]').should('exist')
    }

    verifyAfterReEnablingInvoice() {
        cy.get('input[placeholder="Title for Invoice 3"]').should('not.exist')
        cy.get('input[placeholder="Description for Invoice 3"]').should('not.exist')
        cy.xpath('//div[@class="space-y-4 mt-2"]//select/option[@value="3"]').should('not.exist')
    }



}
