import { RevenueReviewThreshold_Loctors } from "../Locator/RevenueReviewThreshold.locators";
import FakerHelper from "../../../support/FakerHelper";

const loctors = new RevenueReviewThreshold_Loctors();

let RevenueReviewThresholdData;
beforeEach(() => {
    
    RevenueReviewThresholdData = FakerHelper.generateRevenueReviewThresholdData();
    expect(RevenueReviewThresholdData).to.have.property('DEVIATIONPERCENTAGE').that.is.a('number');
    expect(RevenueReviewThresholdData).to.have.property('DEVIATIONAMOUNT').that.is.a('string');
    
});

export class RevenueReviewThreshold{
    ClickOnMenubar() {
        cy.get(loctors.MENUBAR).should('be.visible').and('be.enabled').click().log("Menubar button is present and clickable");
    }
    VerifyNavigattiontoRevenueReviewThresholdPage()
    {
        this.ClickOnMenubar()
        cy.get(loctors.MENUBAR_BUTTONS).contains('Revenue Review Threshold').click()
        cy.get(loctors.REVENUE_REVIEW_THRESHOLD_PAGE).contains('Revenue Review Threshold').should('be.visible').log("Revenue Review Threshold is visible")
      }
    ClickOnSignInWithMicrosoftButton() {
        cy.get(loctors.SIGN_IN_WITH_MICROSOFT_BUTTON).contains("Sign in with Microsoft").should('be.visible').click();
        cy.loginWithSSO();
    }
    VerifyAllStringsOnRevenueReviewThresholdPage()
    {
        const stringsToCheck = [
            'Revenue Review Threshold',
            'Items per page:', 
            'Customer Name',
            'Site ID',
            'Deviation %',
            'Deviation $',
            'Actions',
            'Select All',
            'Bulk Edit',
            'Previous',
            'Next',
            
            
        ];
        cy.wait(1000)
        cy.get(loctors.REVENUE_REVIEW_THRESHOLD_PAGE).invoke('text').then((pageText) => {
            stringsToCheck.forEach((str) => {
              expect(pageText).to.include(str);
              cy.contains(str).should('be.visible').log(`"${str}" is present and visible on the page`);
          });
        });
    }
    ClickOnSelectAllCustomerCheckBox() {
        cy.get(loctors.SELECT_ALL_CUSTOMERS_CHECKBOX).first().then(($checkbox) => {
            // Check the 'data-state' attribute to determine the current state
            if ($checkbox.attr('data-state') === 'checked') {
                // If it's checked, click to uncheck
                cy.wrap($checkbox).click({ force: true });
            } else if ($checkbox.attr('data-state') === 'unchecked') {
                // If it's not checked, click to check
                cy.wrap($checkbox).click({ force: true });
            } else {
                throw new Error('Unexpected state for the checkbox element.');
            }
        });
    }
    
    ClickONSelectAllCustomerButton()
    {
        cy.get(loctors.REVENUE_REVIEW_THRESHOLD_PAGE).contains("Select All").click()
    }
    ClickONUnSelectAllCustomerButton()
    {
        this.ClickONSelectAllCustomerButton()
        cy.get(loctors.REVENUE_REVIEW_THRESHOLD_PAGE).contains("Unselect All").click()
    }
    ClickOnUnselectAllCustomerCheckBox() {
        //this.ClickONSelectAllCustomerButton()
        cy.get(loctors.SELECT_ALL_CUSTOMERS_CHECKBOX).first().then(($checkbox) => {
            // Check the 'data-state' attribute to determine the current state
            if ($checkbox.attr('data-state') === 'checked') {
                // If it's checked, click to uncheck
                cy.wrap($checkbox).click({ force: true });
            } else if ($checkbox.attr('data-state') === 'unchecked') {
                // If it's already unchecked, do nothing
                cy.log('The checkbox is already unchecked.');
            } else {
                throw new Error('Unexpected state for the checkbox element.');
            }
        });
    }
    ClickOnPencilIconButton()
    {
        //cy.get(loctors.REVENUE_REVIEW_THRESHOLD_PAGE).contains('Deviation %').click()
        cy.get(loctors.PENCIL_ICON_BUTTON).first().click()
    }
    VerifyDeviationPercentageAndDollerTextbox()
    {
        cy.get(loctors.DEVIATION_PERCENTAGE_TEXT_BOX).should('be.visible').and('be.enabled').log('Deviation percentage text box is present and editable')
        cy.get(loctors.DEVIATION_DOLLER_TEXT_BOX).should('be.visible').and('be.enabled').log('Deviation Amount text box is present and editable')
    }
    VerifyDeviationPercentageAndDollerTextboxOnBulkEditWindow()
    {
        cy.get(loctors.DEVIATION_PERCENTAGE_TEXT_BOX_ON_BULK_EDIT_WINDOW).should('be.visible').and('be.enabled').log('Deviation percentage text box is present and editable on bulk edit window')
        cy.get(loctors.DEVIATION_DOLLER_TEXT_BOX_ON_BULK_EDIT_WINDOW).should('be.visible').and('be.enabled').log('Deviation Amount text box is present and editable on bulk edit window')
    }
    VerifyXIconButtonisVisibleAndEnable()
    {
        cy.get(loctors.X_ICON_BUTTON).first().should('be.visible').log('X icon button is visible and clickable')
    }
    VerifyRightIconButtonisVisibleAndEnable()
    {
        cy.get(loctors.RIGHT_ICON_BUTTON).first().should('be.visible').log('Right icon button is visible and clickable')
    }
    ClickONRightIconButton()
    {
        cy.get(loctors.RIGHT_ICON_BUTTON).first().should('be.visible').click({force:true})
    }
    VerifySuccessPopUpAppeared()
    {
        cy.get(loctors.SUCCESS_POP_UP).should('be.visible').log('Success pop-up is appeared')
    }
    ClickOnBulkEditButton()
    {
        cy.get(loctors.REVENUE_REVIEW_THRESHOLD_PAGE).contains('Bulk Edit').click()
    }
    verifyBulkEditWindowAppeared()
    {
        cy.get(loctors.BULK_EDIT_WINDOW).should('be.visible').log('Bulk Edit window is appeared')
    }
    VerifyAllStringsOnBulkEditWindow()
    {
        const stringsToCheck = [
            'Bulk Edit',
            'You are about to make bulk edits to the selected customers. This action cannot be undone. Are you sure you want to proceed?', 
            'customers selected for bulk edit',
            'Cancel',
            'Deviation %',
            'Deviation $',
            'Save',
            
            
        ];
        cy.wait(1000)
        cy.get(loctors.BULK_EDIT_WINDOW).invoke('text').then((pageText) => {
            stringsToCheck.forEach((str) => {
              expect(pageText).to.include(str);
              cy.contains(str).should('be.visible').log(`"${str}" is present and visible on the page`);
          });
        });
    }
    VerifyXIconButtonisVisibleAndEnableOnBulkEditWindow()
    {
        cy.get(loctors.BULK_EDIT_WINDOW).should('be.visible').and('be.enabled').log('X icon button is visible and clickable on bulk edit window')
    }
    VerifyCancelButtonisVisibleAndEnableOnBulkEditWindow()
    {
        cy.get(loctors.BULK_EDIT_WINDOW).contains('Cancel').should('be.visible').and('be.enabled').log('Cancel button is visible and clickable on bulk edit window')
    }
    VerifySaveButtonisVisibleAndEnableOnBulkEditWindow()
    {
        cy.get(loctors.BULK_EDIT_WINDOW).contains('Save').should('be.visible').and('be.enabled').log('Save button is visible and clickable on bulk edit window')
    }
    ClickOnSaveButtononBulkEditWindow()
    {
        cy.get(loctors.BULK_EDIT_WINDOW).contains('Save').click()
    }
    verifyConfirmBulkEditWindowAppeared()
    {
        cy.get(loctors.BULK_EDIT_WINDOW).contains('Confirm Bulk Edit').should('be.visible').log('Confirm Bulk Edit window is appeared')
    }
    VerifyAllStringsOnConfirmBulkEditWindow()
    {
        const stringsToCheck = [
            'Confirm Bulk Edit',
            'You are about to make bulk edits to the selected customers. This action cannot be undone. Are you sure you want to proceed?', 
            'Cancel',
            'Save',
            'customers selected for bulk edit'
            
            
        ];
        cy.wait(1000)
        cy.get(loctors.BULK_EDIT_WINDOW).invoke('text').then((pageText) => {
            stringsToCheck.forEach((str) => {
              expect(pageText).to.include(str);
              cy.contains(str).should('be.visible').log(`"${str}" is present and visible on the page`);
          });
        });
    }
    
    VerifyCancelButtonisVisibleAndEnableOnConfirmBulkEditWindow()
    {
        cy.get(loctors.BULK_EDIT_WINDOW).contains('Cancel').should('be.visible').and('be.enabled').log('Cancel button is visible and clickable on bulk edit window')
    }
    VerifySaveButtonisVisibleAndEnableOnConfirmBulkEditWindow()
    {
        cy.get(loctors.BULK_EDIT_WINDOW).contains('Save').should('be.visible').and('be.enabled').log('Save button is visible and clickable on bulk edit window')
    }
    EnterValuesInDeviationPercentageAndDollerTextBox()
    {
        cy.get(loctors.CUSTOMER_LIST_SEARCH_TEXT_BOX).type('2270')
        cy.get(loctors.PENCIL_ICON_BUTTON).first().click()
        cy.get(loctors.DEVIATION_DOLLER_TEXT_BOX).type(RevenueReviewThresholdData.DEVIATIONDOLLER)
        cy.get(loctors.DEVIATION_PERCENTAGE_TEXT_BOX).type(RevenueReviewThresholdData.DEVIATIONPERCENTAGE)
        
    }
}
