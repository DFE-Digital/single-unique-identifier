using Reqnroll;

namespace System.E2ETests.Steps;

[Binding]
public class EducationSteps
{
    [Given("there was an enrollment for {string} at {string}")]
    public void GivenThereWasAnEnrollmentForChildAtSchool(string child, string school)
    {
        throw new PendingStepException();
    }

    [When("{string} views the record for {string} at {string}")]
    public void WhenViewsTheRecordForAt(string socialWorkerName ,string child, string school)
    {
        throw new PendingStepException();
    }

    [Then("{string} education record shows that he has been enrolled at {string}")]
    public void ThenEducationRecordShowsThatHeHasBeenEnrolledAt(string child, string school)
    {
        throw new PendingStepException();
    }

    [Given("{string} has NHS number {string}")]
    public void GivenHasNhsNumber(string child, int nhsNumber)
    {
        throw new PendingStepException();
    }

    [When("{string} finds and views consolidated records for {string} at {string}")]
    public void WhenFindAndViewsConsolidatedRecordsForAt(string userName, string childName, string laName)
    {
        throw new PendingStepException();
    }

    [When("a contact referral is made about {string} to {string}")]
    public void WhenAContactReferralIsMadeAboutTo(string childName, string laName)
    {
        throw new PendingStepException();
    }

    [Given("{string} is in the single view cohort for {string}")]
    public void GivenIsInTheSingleViewCohortFor(string orgName, string laName)
    {
        throw new PendingStepException();
    }

    [Given("there is a child named {string} with the NHS number {string}")]
    public void GivenThereIsAChildNamedWithTheNHSNumber(string childName, string nhsNumber)
    {
        throw new PendingStepException();
    }

    [Given("there is a social care practitioner named {string}")]
    public void GivenThereIsASocialCarePractitionerNamed(string scpName)
    {
        throw new PendingStepException();
    }

    [Given("there is a local authority named {string}")]
    public void GivenThereIsALocalAuthorityNamed(string laName)
    {
        throw new PendingStepException();
    }

    [Given("there is a school named {string}")]
    public void GivenThereIsASchoolNamed(string schoolName)
    {
        throw new PendingStepException();
    }

    [When("{string} enrolls as a pupil at {string}")]
    public void WhenEnrollsAsAPupilAt(string childName, string schoolName)
    {
        throw new PendingStepException();
    }

    [Then("{string} sees that {string} is enrolled at {string}")]
    public void ThenSeesThatIsEnrolledAt(string userName, string childName, string schoolName)
    {
        throw new PendingStepException();
    }
}