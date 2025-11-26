Feature: Fixed Fee

    Scenario: Verify fixed fee is displayed successfully on the invoice
        Given Log into application
        When I configure side id for fixed fee
        Then I generate Statement for the site
        Then I verify fixed fee details from the invoice

    Scenario: Verify toggle button is getting on/off successfully
        Given Log into application
        When I negivate to contract details page
        Then I verify toggle button is getting on and off successfully

    # Scenario: Verify add button gets enable when we toggle on fixed fee button
    #     Given Log into application
    #     When I negivate to contract details page
    #     When I click on contract details page edit buttton
    #     Then I verify add button gets enable and disable when toggle button gets on and off

    # Scenario: Verify random option is selected and gets displayed on invoice
    #     Given Log into application
    #     When I negivate to contract details page of random option
    #     When I click on contract details page edit buttton
    #     Then I select random option for fixed fee to generate the statement and verify



