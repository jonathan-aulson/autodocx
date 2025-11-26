/// <reference types="cypress"/>

import ContractDetailsLocators from '../../../Locator/Customers/ContractDetailsLocators'
import ViewDetailsStatementLocators from '../../../Locator/Statements/ViewDetailsStatementLocators'
import StatementMainPageCommon from '../../../Common/statementMainPageCommon'
import CustomerMainPageCommon from '../../../Common/customerMainPageCommon'


const conD = new ContractDetailsLocators()
const stateMultiple = new ViewDetailsStatementLocators()
const commonStatement = new StatementMainPageCommon()
const commonMain = new CustomerMainPageCommon()
const billingTypeData = require('../../../../fixtures/BillingType/BillingType.json').BillingType;


const siteIDData = billingTypeData.SiteId[0]
const siteIDAdvData = billingTypeData.SiteId[1]
// const FixedFeeOpt = multipleInvoiceData.FixedFeeOptions

let storedText;

export default class BillingTypePageObjects {

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

    getSearchBarAdv() {
        conD.locators.searchBar().type(siteIDAdvData)
    }

    viewSiteDetails() {
        conD.locators.viewDetailforSite(siteIDData).click({ force: true })
    }

    viewSiteDetailsAdv() {
        conD.locators.viewDetailforSite(siteIDAdvData).click({ force: true })
    }

    clickOnCancelButton() {
        conD.locators.cancelButton().click()
    }

    clickOnBillingTypeDropdown() {
        conD.generalSetupLocator.billingTypeDropdownButton().click()
    }

    selectArrearsOptionFromDropdown() {
        this.clickOnBillingTypeDropdown()
        conD.generalSetupLocator.arrearsOptionValueToBillingTypeDropdown()
        cy.wait(1000)
        cy.xpath('//label[contains(text(),"Billing Type")]/following-sibling::button/span').should('contain', 'Arrears')
    }

    selectAdvanceOptionFromDropdown() {
        this.clickOnBillingTypeDropdown()
        cy.wait(1000)
        conD.generalSetupLocator.advancedOptionValueToBillingTypeDropdown()
        cy.wait(1000)
        cy.xpath('//label[contains(text(),"Billing Type")]/following-sibling::button/span').should('contain', 'Advance')
    }



    expandFixedFee() {
        conD.fixedFeeLocator.fixedFeeExpandButton().click()
    }

    checkAndDeleteAllFixedFeeServices() {
        const deleteButtonSelector = 'button[data-qa-id*="button-removeFixedFeeService"]';

        cy.log('Checking for Services section...');

        cy.get('body').then($body => {
            // Check if Services section exists within Enable Fixed Fee containers
            let servicesExists = false;

            $body.find('label').each((index, label) => {
                const $label = Cypress.$(label);

                // If this is an Enable Fixed Fee label
                if ($label.text().includes('Enable Fixed Fee')) {
                    // Check its container for Services label
                    const $container = $label.closest('div.grid.gap-4.p-4.border.rounded-lg');
                    const $servicesInContainer = $container.find('label').filter((i, el) => {
                        return Cypress.$(el).text().includes('Services');
                    });

                    if ($servicesInContainer.length > 0) {
                        servicesExists = true;
                        return false; // Break the loop
                    }
                }
            });

            if (servicesExists) {
                cy.log('✅ Services section found! Starting deletion...');

                const deleteServices = () => {
                    cy.get('body').then($currentBody => {
                        const $deleteButtons = $currentBody.find(deleteButtonSelector);

                        if ($deleteButtons.length > 0) {
                            cy.log(`Deleting service... (${$deleteButtons.length} remaining)`);
                            cy.wrap($deleteButtons.first()).click();
                            cy.wait(300);
                            deleteServices();
                        } else {
                            cy.log('🎉 All services deleted successfully!');
                        }
                    });
                };

                deleteServices();
            } else {
                cy.log('❌ Services section not found, continuing with test...');
            }
        });
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
        cy.contains('Bell').click()
        conD.fixedFeeLocator.feeFixedInput('Bell').type('1000').type("{backspace}")

    }

    fetchAndStoreText1(text1) {
        cy.xpath(`(//p[contains(text(),'${text1}')]/ancestor::div[@class="flex items-start justify-between space-x-2"]//input)[3]`)
            .invoke('val')
            .then((text) => {
                storedText = text; // Store value in a variable
                cy.log('Stored Text:', storedText);
            });
    }



    enterFixedFee() {
        this.fetchAndStoreText1('Bell')
        cy.wait(1000)

    }

    generateStatement() {
        conD.locators.generateStatementforSite(siteIDData).click()
        cy.wait(1000)
        conD.locators.adminProcessforStatementButton().click()
    }

    generateStatementAdv() {
        conD.locators.generateStatementforSite(siteIDAdvData).click()
        cy.wait(1000)
        conD.locators.adminProcessforStatementButton().click()
    }

    waitForStatementArrears(maxRetries) {
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

    waitForStatementAdvanced(maxRetries) {
        let retryCount = 0;
        const statementXPath = `//td[contains(text(),'${siteIDAdvData}')]/ancestor::tr//div[contains(text(),'Approval Team')]`;

        function checkStatement() {
            cy.wait(1000); // Brief wait for UI updates
            conD.locators.searchBar().type(siteIDAdvData)// Perform search

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


    expandStamentForSiteWithReadyStatus() {
        stateMultiple.locators.expandStatementWithReadyStatus(siteIDAdvData).should('be.visible').click({ force: true })
        cy.wait(1000)
    }

    viewStamentforsite(invoiceNum) {
        cy.getYearMonth(-1).then((yearMonth) => {  // Get the previous month
            stateMultiple.locators.viewStatementForSiteInvoice1(siteIDData, yearMonth, invoiceNum).click();
        });
        cy.wait(2000)
    }

    viewStamentforsiteAvd(invoiceNum) {
        cy.getYearMonth(0).then((yearMonth) => {  // Get the next month
            stateMultiple.locators.viewStatementForSiteInvoice2(siteIDAdvData, yearMonth, invoiceNum).should('be.visible').click();
        });
        cy.wait(2000)
    }


    closeInvocieStatement() {
        stateMultiple.locators.closeStatement().click()
    }

    clickOnApproveStatementButton() {
        stateMultiple.locators.approveStatementButton(siteIDData).click()
    }

    clickOnApproveStatementButtonAvd() {
        stateMultiple.locators.approveStatementButton(siteIDAdvData).click()
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

    verifyArrearsMonth() {
        // Get the previous month and year (offset of -1)
        cy.monthAndYear(-1)
            .then((dateString) => {
                // Create dynamic XPath with the previous month and year
                const xpath = `//td[contains(text(),'2161')]/ancestor::tr//div[contains(text(),'Approval Team')]/ancestor::tr//div[contains(text(),'${dateString}')]`;

                // Use the XPath for verification
                cy.xpath(xpath).should('exist'); // Requires cypress-xpath plugin
            });
        cy.wait(1000)


    }

    verifyAdvancedMonth() {
        // Get the next month and year (offset of +1)
        cy.monthAndYear(+1)
            .then((dateString) => {
                // Create dynamic XPath with the next month and  current year
                const xpath1 = `//td[contains(text(),'0985')]/ancestor::tr//div[contains(text(),'Approval Team')]/ancestor::tr//div[contains(text(),'${dateString}')]`;


                // Use the XPath for verification
                cy.wait(1000)
                const veriXpath = cy.xpath(xpath1)

                cy.log(veriXpath)

                cy.xpath(xpath1).should('exist')

            });
        cy.wait(1000)


    }

    verifyStatement() {
        this.viewStamentforsite("01")
        cy.wrap(null).then(() => { // Ensures Cypress waits
            cy.contains(storedText).should('be.visible'); // Use storedText in cy.contains
        });
        cy.xpath(`//*[span[contains(text(), "${storedText}")] and span[contains(text(), "Total Amount Due")]]`).should('be.visible')
    }

    verifyStatementAdv() {
        this.viewStamentforsiteAvd("01")
        cy.wrap(null).then(() => { // Ensures Cypress waits
            cy.contains(storedText).should('be.visible'); // Use storedText in cy.contains
        });
        cy.xpath(`//*[span[contains(text(), "${storedText}")] and span[contains(text(), "Total Amount Due")]]`).should('be.visible')
    }

    // clearContactDetail() {
    //     this.clickOnMainPage()
    //     this.getSearchBar()
    //     cy.wait(1000)
    //     this.viewSiteDetails()
    //     cy.wait(1000)
    //     this.clickOnContractDetailTab()
    //     this.clickOnEditButton()
    //     cy.wait(1000)
    //     this.expandFixedFee()
    //     const xpathSelector = '//button[@class="inline-flex items-center justify-center whitespace-nowrap rounded-md text-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:pointer-events-none disabled:opacity-50 hover:bg-accent hover:text-accent-foreground h-9 px-4 py-2"]';

    //     cy.xpath(xpathSelector).then((elements) => {
    //         // Check if elements exist
    //         if (elements.length > 0) {
    //             // Iterate through each element and click
    //             cy.wrap(elements).each((element) => {
    //                 cy.wrap(element).click();
    //             });
    //         } else {
    //             // Log if no elements are found
    //             cy.log('No matching elements found');
    //         }
    //     });

    //     this.disableFixedFee()
    //     cy.wait(1000)

    //     this.clickOnSaveButton()
    //     cy.contains('Contract updated successfully!').should('exist')
    // }

    clearContactDetailAdv() {
        this.clickOnMainPage()
        this.getSearchBarAdv()
        cy.wait(1000)
        this.viewSiteDetailsAdv()
        cy.wait(1000)
        this.clickOnContractDetailTab()
        this.clickOnEditButton()
        cy.wait(1000)
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


}
