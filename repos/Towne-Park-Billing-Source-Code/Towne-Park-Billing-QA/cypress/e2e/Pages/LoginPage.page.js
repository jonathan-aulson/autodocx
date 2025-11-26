
import { LoginPage_Loctors } from "../Locator/LoginPage.locator"


var loctors = new LoginPage_Loctors

export class LoginPage {
    VerifyLogoIsVisible()
    {
        cy.get(loctors.LOGO).should('be.visible').log("Town Park log is visible on login page")
    }
    
    ClickOnSignInWithMicrosoftButton()
    {
        cy.get(loctors.SIGN_IN_WITH_MICROSOFT_BUTTON).contains("Sign in with Microsoft").should('be.visible').and('not.be.disabled').log('Sign in with Microsoft button is present and clickable')
        
    }
    VerifyImageOnRightSide()
    {
        cy.get(loctors.RIGHT_SIDE_IMAGE).should('be.visible').log('Image is present on login page')
    }
    NavigateToLoginPage()
    {
        cy.url().should('include', 'net');
    }
    VerifyAllStringsOnLoginPage()
    {
        const stringsToCheck = [
            'Towne Park Billing',
            'Please sign in with your company credentials', 
            
        ];
        cy.get(loctors.ALL_STRINGS).invoke('text').then((pageText) => {
            stringsToCheck.forEach((str) => {
              expect(pageText).to.include(str);
              cy.contains(str).should('be.visible').log(`"${str}" is present and visible on the page`);
          });
        });
    }
    VerifyAbletoLoginWithValidCredentails(){
        this.ClickOnSignInWithMicrosoftButton()
        cy.loginWithSSO();
        cy.get(loctors.COSTOMERS_STRING).contains('Customers').should('be.visible').log("User is able to login with valid credentails")
        
    }
}    