/// <reference types="cypress"/>

import GeneralInfoLoctors from '../../Locator/Customers/GeneralInfoLocators'

const gInfo = new GeneralInfoLoctors()


export default class GeneralInfoPageObject{

    clickOnGeneralInfoTab(){
        gInfo.locators.generalInfoTab().click({force:true})
        cy.wait(2000)
    }

    clickOnEditButton(){
        gInfo.locators.editButton().invoke('click')
        cy.wait(2000)
        // gInfo.locators.editButton().click()
    }

    clickOnSaveButton(){
        gInfo.locators.saveButton().click()
        cy.wait(2000)
    }
    
    clickOnCancelButton(){
        gInfo.locators.cancelButton().click()
    }

    enterVendorId(venId){
        gInfo.locators.vendorIdTextBox().clear().type(venId)
    }

    enterSiteName(sName){
        gInfo.locators.siteNameTextBox().clear().type(sName)
    }

    enterAddress(addr){
        gInfo.locators.addressTextBox().clear().type(addr)
    }

    enterRoomsAvaible(roomA){
        gInfo.locators.totalRoomsAvaibleTextBox().clear().type(roomA)
    }

    enterParkingAvaible(parkA){
        gInfo.locators.totalParkingAvaibleTextBox().clear().type(parkA)
    }

}