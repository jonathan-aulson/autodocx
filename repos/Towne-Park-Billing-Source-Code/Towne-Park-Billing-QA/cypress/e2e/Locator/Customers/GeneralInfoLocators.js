/// <reference types="cypress"/>

export default class GeneralInfoLoctors {

    locators = {

        generalInfoTab: ()=>  cy.xpath('//button[contains(text(),"General Info")]'),
        editButton: ()=> cy.xpath('//button[@class="inline-flex items-center justify-center whitespace-nowrap rounded-md text-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:pointer-events-none disabled:opacity-50 bg-primary text-primary-foreground shadow hover:bg-primary/90 h-9 px-4 py-2"]'),
        saveButton: ()=> cy.xpath('//button[contains(text(),"Save")]'),
        cancelButton: ()=> cy.xpath('//button[contains(text(),"Cancel")]'),
        vendorIdTextBox: ()=> cy.get('input[id="vendor-id"]'),
        siteNameTextBox: ()=> cy.get('input[id="site-name-id"]'),
        addressTextBox: ()=> cy.get('input[id="address-id"]'),
        totalRoomsAvaibleTextBox: ()=> cy.get('input[id="total-rooms"]'),
        totalParkingAvaibleTextBox: ()=> cy.get('input[id="total-parking"]'),
        

    }
    
}