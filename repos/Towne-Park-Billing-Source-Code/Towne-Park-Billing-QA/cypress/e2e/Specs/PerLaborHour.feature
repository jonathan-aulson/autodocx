Feature: PerLaborHour

    Scenario: verify calculation for per labor hour
        Given Log into application
        When I configure per labor hour for the site
        When I generate statement for per labor hour
        Then I should get entries in the invoice

    Scenario: verify string present in correct format on contract details
        Given Log into application
        When I nevigate to contract details
        Then I verify correct string present for per labor hour
    
    Scenario: verify add button enable or disable according to toggle on off button
        Given Log into application
        When I nevigate to contract details
        Then I verify add button enable when per labor hour toggle button gets on
        