namespace SUI.Transfer.Domain;

public enum StatusFlag
{
    /// <summary>
    /// English as an additional language (EAL) flag
    /// </summary>
    IsEnglishAsAdditionalLanguage,

    /// <summary>
    /// Children's Social Care Status - open/closed
    /// </summary>
    IsChildrensSocialCareStatusOpen,

    /// <summary>
    /// Child in need (CIN) flag
    /// </summary>
    IsChildInNeed,

    /// <summary>
    /// Child looked after (CLA) flag
    /// </summary>
    IsChildLookedAfter,

    /// <summary>
    /// Child protection (CP) flag
    /// </summary>
    IsChildProtection,

    /// <summary>
    /// Not in Education, Employment or Training (NEET)
    /// </summary>
    IsNotInEducationEmploymentOrTraining,

    /// <summary>
    /// Child has Pupil Premium funding
    /// </summary>
    HasPupilPremium,

    /// <summary>
    /// Child has Free school meals (FSM)
    /// </summary>
    HasFreeSchoolMeals,

    /// <summary>
    /// Electively Home Educated (EHE) flag
    /// </summary>
    IsElectivelyHomeEducated,

    /// <summary>
    /// Special educational needs and disabilities (SEND) flag
    /// </summary>
    HasSpecialEducationalNeedsAndDisabilities,

    /// <summary>
    /// Open to Child and Adolescent Mental Health Services (CAMHS) flag
    /// </summary>
    IsOpenToCAMHS,

    /// <summary>
    /// Open to Youth Justice Service flag
    /// </summary>
    IsOpenToYouthJusticeService,

    /// <summary>
    /// Risk of exploitation - sexual (CSE)
    /// </summary>
    RiskOfExploitationSexual,

    /// <summary>
    /// Risk of exploitation - crime (CCE)
    /// </summary>
    RiskOfExploitationCriminal,

    /// <summary>
    /// Risk of radicalisation
    /// </summary>
    RiskOfRadicalisation,

    /// <summary>
    /// Risk of modern slavery and trafficking
    /// </summary>
    RiskOfModernSlaveryAndTrafficking,

    /// <summary>
    /// Risk of gangs and youth violence
    /// </summary>
    RiskOfGangsAndYouthViolence,

    /// <summary>
    /// Risk of FGM
    /// </summary>
    RiskOfFGM,

    /// <summary>
    /// Police Powers of Protection (PPP) flag
    /// </summary>
    HasPolicePowersOfProtection,
}
