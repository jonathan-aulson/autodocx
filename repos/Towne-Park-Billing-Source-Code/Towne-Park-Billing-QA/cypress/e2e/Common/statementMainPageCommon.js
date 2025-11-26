/// <reference types="cypress"/>

export default class StatementMainPageCommon {

    locators = {
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

    getSearchBar(siteId) {
        this.locators.searchBar().type(siteId);
    }
}