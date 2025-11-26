Feature: Billing Type

    Scenario: Verify that when Billing Type is Appears then the Statements should reflect the pervious month for that site
        Given Log into application
        When Go to the view detail for a site
        Then Go to Contract details
        And Select the Billing type as  Appears
        And Enter the details for Fixed Fee
        And Genrate a Statement for the site
        And Verify that the generated statement for pervious month and Fixed Fee amount

    Scenario: Verify that when Billing Type is Advanced then the Statements should reflect the next month for that site
        Given Log into application
        When Go to the view detail for a site that is Advanced Billing type
        Then Go to Contract details
        And Select the Billing type as Advanced
        And Enter the details for Fixed Fee
        And Genrate a Statement for the site that is Advanced Billing type
        And Verify that the generated statement for next month and Fixed Fee amount  