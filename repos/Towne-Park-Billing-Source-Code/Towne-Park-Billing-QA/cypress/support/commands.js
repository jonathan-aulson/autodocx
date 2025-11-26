// ***********************************************
// This example commands.js shows you how to
// create various custom commands and overwrite
// existing commands.
//
// For more comprehensive examples of custom
// commands please read more here:
// https://on.cypress.io/custom-commands
// ***********************************************
//

import 'cypress-xpath';


// cypress/support/commands.js
Cypress.Commands.add('loginWithSSO', () => {
  cy.fixture('Credentials.json').then((credentials) => {
    cy.session('sso-session', () => {
      // Visit the initial page to trigger the SSO login flow using the base URL from environment variables
      cy.visit(Cypress.env('baseUrl'));

      // Click the login button to start the SSO process
      cy.get('button[type="submit"]').click(); // Adjust the selector as needed

      // Handle Microsoft login with cy.origin and pass credentials as args
      cy.origin('https://login.microsoftonline.com', { args: credentials }, ({ email, password }) => {
        cy.get('input[type="email"]', { timeout: 10000 }).should('be.visible');

        cy.on('uncaught:exception', (e) => {
          if (
            e.message.includes('Failed to load external resource') ||
            e.message.includes('reloading from fallback CDN endpoint') ||
            e.message.includes('Loading chunk') ||
            e.message.includes('Things went bad') ||
            e.message.includes("Cannot read properties of null") 
          ) {
            return false;
          }
          console.error('Unhandled Error:', e.message);
          return true;
        });

        cy.get('input[type="email"]').type(email,{force:true});
        cy.get('input[type="submit"]').click();
        cy.log('Clicked submit after email');
        cy.wait(2000)
        cy.get('input[type="password"]', { timeout: 6000 }).should('be.visible').type(password,{force:true});
        cy.wait(3000)
        cy.get('input[type="submit"]').click().wait(1000);

        cy.get('body').then(($body) => {
          if ($body.find('input[id="idSIButton9"]').length > 0) {
            cy.get('input[id="idSIButton9"]').click(); // Click "Yes"
          }
        });
        cy.wait(1000);
      });
    });

    // Visit the billing customers page using the base URL from environment variables
    cy.visit(`${Cypress.env('baseUrl')}billing/customers`);
  });
});

Cypress.Commands.add('getYearMonth', (offset = 0) => {
  const currentDate = new Date();
  currentDate.setMonth(currentDate.getMonth() + offset); // Move to previous or future month
  
  const year = currentDate.getFullYear();
  const month = String(currentDate.getMonth() + 1).padStart(2, '0'); // Ensures two-digit format

  return `${year}${month}`; // Format: YYYY-MM



  // cy.getYearMonth(-1).then((prevMonth) => {
  //   cy.get(`[data-date="${prevMonth}"]`).click(); for previous month 
  // });


  // cy.getYearMonth(0).then((currentMonth) => {
  //   cy.get(`[data-date="${currentMonth}"]`).click(); for current  month
  // });

  // cy.getYearMonth(1).then((nextMonth) => {
  //   cy.get(`[data-date="${nextMonth}"]`).click(); for next month
  // });

});

Cypress.Commands.add('monthAndYear', (offset = 0) => {
  const currentDate = new Date();
  currentDate.setMonth(currentDate.getMonth() + offset); // Move to previous or future month
  
  const year = currentDate.getFullYear();
  const monthName = currentDate.toLocaleString('default', { month: 'long' }); // Get full month name

  return `${monthName} ${year}`; // Format: month full name and year
});

// -- This is a parent command --
// Cypress.Commands.add('login', (email, password) => { ... })
//
//
// -- This is a child command --
// Cypress.Commands.add('drag', { prevSubject: 'element'}, (subject, options) => { ... })
//
//
// -- This is a dual command --
// Cypress.Commands.add('dismiss', { prevSubject: 'optional'}, (subject, options) => { ... })
//
//
// -- This will overwrite an existing command --
// Cypress.Commands.overwrite('visit', (originalFn, url, options) => { ... })