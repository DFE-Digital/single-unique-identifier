using Reqnroll;

namespace System.E2ETests.Steps;

[Binding]
public class EducationSteps
{

    [Given("(.*) school exists")]
    public void GivenSchoolSchoolExists(string schoolName)
    {
        throw new PendingStepException();
    }

    [Given("(.*) exists with role (.*)")]
    public void GivenExistsWithRole(string name, string roleName)
    {
        // Check if user exists with role. Create if not.
        throw new PendingStepException();
    }

    [Given("(.*) local authority exists")]
    public void GivenLocalAuthorityExists(string laName)
    {
        throw new PendingStepException();
    }

    [Given("there was an enrollment for (.*) at (.*)")]
    public void GivenThereWasAnEnrollmentForChildAtSchool(string child, string school)
    {
        throw new PendingStepException();
    }

    [When("(.*) views the record for (.*) at (.*)")]
    public void WhenViewsTheRecordForAt(string socialWorkerName ,string child, string school)
    {
        throw new PendingStepException();
    }

    [Then("(.*) education record shows that he has been enrolled at (.*)")]
    public void ThenEducationRecordShowsThatHeHasBeenEnrolledAt(string child, string school)
    {
        throw new PendingStepException();
    }

    [Given("(.*) has NHS number (.*)")]
    public void GivenHasNhsNumber(string child, int nhsNumber)
    {
        throw new PendingStepException();
    }

    [Given("(.*) is in the single view cohort for (.*)")]
    public void GivenIsInTheSingleViewCohortFor()
    {
        throw new PendingStepException();
    }
}