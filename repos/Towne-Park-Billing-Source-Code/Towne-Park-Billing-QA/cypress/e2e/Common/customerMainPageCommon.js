/// <reference types="cypress"/>

export default class CustomerMainPageCommon {

    locators = {
        signInWithMicrosoftButton: () => cy.get('button[type="submit"]'),
        mainCustomerButton: ()=> cy.xpath('(//a[@href="/billing/customers"][contains(text(),"Customers")])'),
        mainStatementsButton: ()=> cy.xpath('(//a[@href="/billing/statements"][contains(text(),"Statements")])'),
        searchBar: ()=> cy.get('input[placeholder="Search..."]'),

    }


    getMainCustomerButton() {
        this.locators.mainCustomerButton().click();
    }

    getMainStatementsButton() {
        this.locators.mainStatementsButton().click();
    }

    getSearchBar() {
        this.locators.searchBar().type();
    }

    ClickOnSignInWithMicrosoftButton()
    {
       this.locators.signInWithMicrosoftButton().contains("Sign in with Microsoft").should('be.visible').and('not.be.disabled').log('Sign in with Microsoft button is present and clickable')
        
    }
}
