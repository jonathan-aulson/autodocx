Feature: AdditionalFeesAndLineItems

    Scenario: Verify that when Additional Fees And Line Items is enabled it is correctly reflected in the generated statement with proper info
        Given Log into application
        When I configure side id for additional fees and line items
        When I generate statement for additional fees or line items
        Then I verify additional fees or line items details from invoice

    # Scenario: Verify toggle button for all additional fees
    #     Given Log into application
    #     When I nevigate to contract detials page
    #     Then I verify all toggle button is working as expected

