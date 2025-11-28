namespace SUI.Transfer.Domain;

public record AddressV1(
    string? Line1,
    string? Line2,
    string? TownOrCity,
    string? County,
    string? Postcode
);

public enum HealthSettingV1
{
    Other,
    Hospital,
    GP,
    Community,
}

public record ChildPersonalDetailsRecordV1
{
    /// <summary>
    /// The child's first name.
    /// </summary>
    /// <example>Sarah</example>
    public string? FirstName { get; set; }

    /// <summary>
    /// The child's last name.
    /// </summary>
    /// <example>Smith</example>
    public string? LastName { get; set; }

    /// <summary>
    /// The child's date of birth.
    /// </summary>
    /// <example>2011-09-25</example>
    public DateOnly? DateOfBirth { get; set; }

    /// <summary>
    /// The latest known address of the child's main residence.
    /// </summary>
    /// <example>12 Burton Street, London, SW1A 0AA</example>
    public AddressV1? Address { get; set; }

    /// <summary>
    /// The full names of the other people known to be residing at the child's main address.
    /// </summary>
    /// <example>James Smith, Henry Smith, Thomas Smith, Jason Archer, Sarah Flint-Smith</example>
    public string[]? NamesOfIndividualsResidingAtMainAddress { get; set; }

    /// <summary>
    /// Birth assigned sex
    /// </summary>
    /// <example>Female</example>
    public string? BirthAssignedSex { get; set; }

    /// <summary>
    /// Pronouns
    /// </summary>
    /// <example>She/her</example>
    public string? Pronouns { get; set; }

    /// <summary>
    /// Ethnicity
    /// </summary>
    /// <example>Irish Traveller</example>
    public string? Ethnicity { get; set; }

    /// <summary>
    /// First language
    /// </summary>
    /// <example>English</example>
    public string? FirstLanguage { get; set; }

    /// <summary>
    /// Designated Local Authority
    /// </summary>
    /// <example>Bromley</example>
    public string? DesignatedLocalAuthority { get; set; }

    /// <summary>
    /// Communication need: English as additional language (EAL)
    /// </summary>
    public bool? EnglishAsAdditionalLanguage { get; set; }

    /// <summary>
    /// Communication need: Braille needed
    /// </summary>
    public bool? Braille { get; set; }

    /// <summary>
    /// Communication need: Sign language
    /// </summary>
    public bool? SignLanguage { get; set; }

    /// <summary>
    /// Communication need: Makaton needed
    /// </summary>
    public bool? Makaton { get; set; }

    /// <summary>
    /// Communication need: Interpreter needed
    /// </summary>
    public bool? Interpreter { get; set; }

    /// <summary>
    /// The people known to be related to the child.
    /// </summary>
    public RelatedPersonV1[]? RelatedPeople { get; set; }
}

public record ChildSocialCareDetailsRecordV1
{
    /// <summary>
    /// CSC - Key worker
    /// </summary>
    /// <example>Alex Patel</example>
    public string? KeyWorker { get; set; }

    /// <summary>
    /// CSC - Duty contact details
    /// </summary>
    /// <example>csc@bromley.gov.uk, 08792675387</example>
    public string[]? DutyContactDetails { get; set; }

    /// <summary>
    /// CSC - Team involvement
    /// </summary>
    /// <example>Disabled Childrens Team, Neighbourhood Team</example>
    public string[]? TeamInvolvement { get; set; }

    /// <summary>
    /// Child Social Care - Active statuses and plans
    /// </summary>
    public CSCActiveStatusAndPlanV1[]? CSCActiveStatusesAndPlans { get; set; }

    /// <summary>
    /// Child Social Care Referrals
    /// </summary>
    public CSCReferralV1[]? CSCReferrals { get; set; }
}

public record EducationDetailsRecordV1
{
    /// <summary>
    /// Education setting - Name
    /// </summary>
    /// <example>Redwood Academy</example>
    public string? EducationSettingName { get; set; }

    /// <summary>
    /// Education setting - Address
    /// </summary>
    /// <example>Redrood Drive, Maltby, London, SE6 8DL</example>
    public AddressV1? EducationSettingAddress { get; set; }

    /// <summary>
    /// Education setting - Telephone
    /// </summary>
    /// <example>0123456789</example>
    public string? EducationSettingTelephone { get; set; }

    /// <summary>
    /// Education - Active statuses and plans
    /// </summary>
    public EducationActiveStatusAndPlanV1[]? EducationActiveStatusesAndPlans { get; set; }

    /// <summary>
    /// Education Attendances
    /// </summary>
    public EducationAttendanceV1[]? EducationAttendances { get; set; }
}

public record ChildHealthDataRecordV1
{
    /// <summary>
    /// Registered GP
    /// </summary>
    /// <example>Dr E Green</example>
    public string? RegisteredGP { get; set; }

    /// <summary>
    /// GP Address
    /// </summary>
    /// <example>Duke Medical Centre, 28 Talbot Road, Sheffield, S2 2TD</example>
    public AddressV1? GPAddress { get; set; }

    /// <summary>
    /// GP Telephone
    /// </summary>
    /// <example>0114 272 2100</example>
    public string? GPTelephone { get; set; }

    /// <summary>
    /// Child and Adolescent Mental Health Services - Contact address
    /// </summary>
    public AddressV1? CAMHSContactAddress { get; set; }

    /// <summary>
    /// Child and Adolescent Mental Health Services - Contact telephone
    /// </summary>
    /// <example>01422 345926</example>
    public string? CAMHSContactTelephone { get; set; }

    /// <summary>
    /// Health - Active statuses and plans
    /// </summary>
    public HealthActiveStatusAndPlanV1[]? HealthActiveStatusesAndPlans { get; set; }

    /// <summary>
    /// Missed Healthcare Appointments
    /// </summary>
    public HealthMissedAppointmentV1[]? MissedAppointments { get; set; }

    /// <summary>
    /// Emergency (A&amp;E) Department Attendances
    /// </summary>
    public EmergencyDepartmentAttendanceV1[]? EmergencyDepartmentAttendances { get; set; }
}

public record ChildLinkedCrimeDataRecordV1
{
    /// <summary>
    /// Police marker details
    /// </summary>
    /// <example>Individuals at the address may resort to violent behaviour</example>
    public string? PoliceMarkerDetails { get; set; }

    /// <summary>
    /// Crime - Services known to
    /// </summary>
    /// <example>Youth justice service (YJS), Police</example>
    public string[]? ServicesKnownTo { get; set; }

    /// <summary>
    /// Last Police Protection Power event
    /// </summary>
    public string? LastPoliceProtectionPowerEvent { get; set; }

    /// <summary>
    /// Police - Missing Episodes
    /// </summary>
    public CrimeMissingEpisodeV1[]? MissingEpisodes { get; set; }

    /// <summary>
    /// Crime - Risk - Sexual Exploitation
    /// </summary>
    public bool? RiskSexualExploitation { get; set; }

    /// <summary>
    /// Crime - Risk - Criminal Exploitation
    /// </summary>
    public bool? RiskCriminalExploitation { get; set; }

    /// <summary>
    /// Crime - Risk - Radicalisation
    /// </summary>
    public bool? RiskRadicalisation { get; set; }

    /// <summary>
    /// Crime - Risk - Modern Slavery and Trafficking
    /// </summary>
    public bool? RiskModernSlaveryAndTrafficking { get; set; }

    /// <summary>
    /// Crime - Risk - Gangs and Youth Violence
    /// </summary>
    public bool? RiskGangsAndYouthViolence { get; set; }

    /// <summary>
    /// Child Linked Crime - Active statuses and plans
    /// </summary>
    public CrimeActiveStatusAndPlanV1[]? CrimeActiveStatusesAndPlans { get; set; }
}

public record RelatedPersonV1
{
    /// <summary>
    /// Related Person - Relationship to the child
    /// </summary>
    /// <example>Father</example>
    public string? RelationshipToTheChild { get; set; }

    /// <summary>
    /// Related Person - Full Name
    /// </summary>
    /// <example>James Smith</example>
    public string? Name { get; set; }

    /// <summary>
    /// Related Person - Date of birth
    /// </summary>
    /// <example>1978-11-01</example>
    public DateOnly? DOB { get; set; }

    /// <summary>
    /// Risks posed by the Related Person
    /// </summary>
    /// <example>Individual may possess firearms</example>
    public string[]? Risk { get; set; }

    /// <summary>
    /// Related Person - Services known to
    /// </summary>
    /// <example>Police, Probation, Mental Health</example>
    public string[]? ServicesKnownTo { get; set; }
}

public record CSCActiveStatusAndPlanV1
{
    /// <summary>
    /// CSC - Active statuses and plans - Status
    /// </summary>
    /// <example>Child In Need</example>
    public string? Status { get; set; }

    /// <summary>
    /// CSC - Active statuses and plans - Plan
    /// </summary>
    /// <example>Child in need plan</example>
    public string? Plan { get; set; }
}

public record CSCReferralV1
{
    /// <summary>
    /// CSC - Referral history - Date
    /// </summary>
    /// <example>2025-08-16</example>
    public DateOnly? Date { get; set; }

    /// <summary>
    /// CSC - Referral history - Type
    /// </summary>
    /// <example>Early Help</example>
    public string? Type { get; set; }
}

public record EducationActiveStatusAndPlanV1
{
    /// <summary>
    /// Education - Active statuses and plans - Status
    /// </summary>
    /// <example>SEND</example>
    public string? Status { get; set; }

    /// <summary>
    /// Education - Active statuses and plans - Plan
    /// </summary>
    /// <example>Education, Health and Care (EHC) plan</example>
    public string? Plan { get; set; }
}

public record EducationAttendanceV1
{
    /// <summary>
    /// Education attendance history - Academic Term Year Start
    /// </summary>
    /// <example>2024</example>
    public int? AcademicTermYearStart { get; set; }

    /// <summary>
    /// Education attendance history - Academic Term Year End
    /// </summary>
    /// <example>2025</example>
    public int? AcademicTermYearEnd { get; set; }

    /// <summary>
    /// Education attendance history - Attendance Percentage
    /// </summary>
    /// <example>70</example>
    public float? AttendancePercentage { get; set; }

    /// <summary>
    /// Education attendance history - Unauthorised Absence Percentage
    /// </summary>
    /// <example>2</example>
    public float? UnauthorisedAbsencePercentage { get; set; }

    /// <summary>
    /// Education attendance history - Suspensions
    /// </summary>
    /// <example>1</example>
    public int? Suspensions { get; set; }

    /// <summary>
    /// Education attendance history - Exclusions
    /// </summary>
    /// <example>0</example>
    public int? Exclusions { get; set; }

    /// <summary>
    /// Education attendance history - School moves non transitional
    /// </summary>
    /// <example>0</example>
    public int? SchoolMovesNonTransitional { get; set; }
}

public record HealthActiveStatusAndPlanV1
{
    /// <summary>
    /// Health - Active statuses and plans - Status
    /// </summary>
    /// <example>Open to CAMHS</example>
    public string? Status { get; set; }

    /// <summary>
    /// Health - Active statuses and plans - Plan
    /// </summary>
    /// <example>CAMHS plan</example>
    public string? Plan { get; set; }
}

public record HealthMissedAppointmentV1
{
    /// <summary>
    /// Health - Missed appointments history - Date
    /// </summary>
    /// <example>2025-07-24</example>
    public DateOnly? Date { get; set; }

    /// <summary>
    /// Health - Missed appointments history - Setting
    /// </summary>
    /// <example>GP</example>
    public HealthSettingV1? Setting { get; set; }
}

public record EmergencyDepartmentAttendanceV1
{
    /// <summary>
    /// Health - Emergency department attendance - Date
    /// </summary>
    /// <example>2025-06-12</example>
    public DateOnly? Date { get; set; }
}

public record CrimeMissingEpisodeV1
{
    /// <summary>
    /// Missing episode date
    /// </summary>
    public DateOnly? Date { get; set; }
}

public record CrimeActiveStatusAndPlanV1
{
    /// <summary>
    /// Crime - Active statuses and plans - Status
    /// </summary>
    /// <example>Open to Youth Justice Service</example>
    public string? Status { get; set; }

    /// <summary>
    /// Crime - Active statuses and plans - Plan
    /// </summary>
    public string? Plan { get; set; }
}
