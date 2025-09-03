Feature: Education information consolidation
  
  As a social care practitioner (SCP)
  I want to see the education details of a given child
  So that I am more informed in my work with the child
  
  Scenario: An SCP receives a contact referral and looks up which school they attend
    Given there is a school named 'Bassingbourn Primary'
    And there is a local authority named 'South Cambridgeshire District Council'
    And there is a social care practitioner named 'Jenny'
    And there is a child named 'Timmy' with the NHS number '1234567890'
    And 'Bassingbourn Primary' is in the single view cohort for 'South Cambridgeshire District Council'
    When 'Timmy' enrolls as a pupil at 'Bassingbourn Primary'
    And a contact referral is made about 'Timmy' to 'South Cambridgeshire District Council'
    And 'Jenny' finds and views consolidated records for 'Timmy' at 'South Cambridgeshire District Council'
    Then 'Jenny' sees that 'Timmy' is enrolled at 'Bassingbourn Primary'
