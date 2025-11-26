Feature: MultipleInvoices

    Scenario: Verify that when Multiple Invoice is enabled it is correctly reflected in the generated statement
        Given Log into application
        When Go to the view detail for a site
        Then Go to Contract details
        And Enter the details for Multipe invoice
        And Enter the details for Fixed fee and select the invoice and save it
        And Genrate a Statement for the site
        And Verify that the generated statement includes all General Info and reflects Multiple Invoices

    Scenario: Verify Adding and Deleting Multiple Invoice
        Given Log into application
        When Go to the view detail for a site
        Then Go to Contract details
        And Add new Row for Multiple Invoice and verfiy it is showing in dropdown
        And Disabled the Invoice and Enabled the Invoice
        And After enabling the Invoice option, verify that the default options is displayed correctly