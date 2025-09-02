Feature: Education record data is available to social care practitioners
  
  As a social care practitioner
  I want to see the record for a child who has joined a new school
  So that I am more informed in my work with the child
  
  Background: 
    Given 'Bassingbourn Primary' school exists
    And 'Jenny' exists with role 'SocialWorker'
    And 'South Cambridgeshire DC' local authority exists

  Scenario Outline: A social worker is interested in a child that has joined a new school
    Given there was an enrollment for <child> at <school_name>
    And <child> has NHS number <nhs_no>
    And <school_name> is in the single view cohort for <la_name>
    When <social_care_user> views the record for <child> at <la_name>
    Then <child> education record shows that he has been enrolled at <school_name>

    Examples:
      | child | school_name          | nhs_no     | la_name                 | social_care_user |
      | Timmy | Bassingbourn Primary | 1234567890 | South Cambridgeshire DC | Jenny            |
