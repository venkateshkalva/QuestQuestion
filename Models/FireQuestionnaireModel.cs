using System.ComponentModel.DataAnnotations;

namespace QuestDetails.Models
{
    public class FireQuestionnaireModel
    {
        public string SessionId { get; set; } = Guid.NewGuid().ToString();

        // -1 indicates that the questionnaire has not yet been created by
        // the downstream system. Once returned, this ID is reused on saves.
        public long LqId { get; set; } = -1;

        public IgnitionAndSpreadSection IgnitionAndSpread { get; set; } = new();
        public OutagesSection Outages { get; set; } = new();
        public EquipmentAndPolesSection EquipmentAndPoles { get; set; } = new();
        public WaterUseSection WaterUse { get; set; } = new();
        public BrushClearanceSection BrushClearance { get; set; } = new();
        public OtherDefendantInfoSection OtherDefendantInfo { get; set; } = new();
        public DocumentProductionSection DocumentProduction { get; set; } = new();
    }

    // NOTE: two/three-value enums with NO "unset" member. Required fields
    // are typed as the nullable enum (YesNo?) so an unanswered radio group
    // binds to null and [Required] actually fires. A non-nullable enum
    // with a "NotSelected = 0" sentinel never fails [Required] because
    // Required only checks for null, and value types are never null.
    public enum YesNo
    {
        Yes = 1,
        No = 2
    }

    public enum YesNoDontKnow
    {
        Yes = 1,
        No = 2,
        [Display(Name = "Don't Know")]
        DontKnow = 3
    }

    public enum DocumentAvailability
    {
        [Display(Name = "Have Documents / Will Produce")]
        HaveDocumentsWillProduce = 1,
        [Display(Name = "Do Not Have Documents")]
        DoNotHaveDocuments = 2,
        Other = 3
    }

    public class IgnitionAndSpreadSection
    {
        [Required(ErrorMessage = "Please select Yes or No.")]
        public YesNo? ObservedFlamesSmokeSmoldering { get; set; }

        [StringLength(500, ErrorMessage = "Must be 500 characters or fewer.")]
        public string? ObserverNameOrMe { get; set; }

        [StringLength(5000, ErrorMessage = "Must be 5000 characters or fewer.")]
        public string? ObservationDescription { get; set; }

        [Required(ErrorMessage = "Please select Yes or No.")]
        public YesNo? ObservedHomesIgnite { get; set; }

        [StringLength(5000)]
        public string? HomesIgniteDescription { get; set; }

        [Required(ErrorMessage = "Please select Yes or No.")]
        public YesNo? ObservedFireFlamesExplosionsEmbers { get; set; }

        [StringLength(5000)]
        public string? FireFlamesExplosionsEmbersDescription { get; set; }
    }

    public class OutagesSection
    {
        [Required(ErrorMessage = "Please select Yes or No.")]
        public YesNo? LostServiceDuringFire { get; set; }

        [StringLength(500, ErrorMessage = "Must be 500 characters or fewer.")]
        public string? OutageDetails { get; set; }
    }

    public class EquipmentAndPolesSection
    {
        [Required(ErrorMessage = "Please select Yes or No.")]
        public YesNo? AwareOfPoleIssuesBeforeFire { get; set; }

        [StringLength(5000)]
        public string? PoleIssuesDescription { get; set; }

        [Required(ErrorMessage = "Please select Yes or No.")]
        public YesNo? WitnessedBrokenPoleDownedLineSparking { get; set; }

        [StringLength(5000)]
        public string? BrokenPoleDescription { get; set; }

        public YesNo? WitnessedMomentPoleBroke { get; set; }

        [StringLength(5000)]
        public string? MomentPoleBrokeDescription { get; set; }
    }

    public class WaterUseSection
    {
        public enum WaterProviderOption
        {
            LADWP,
            [Display(Name = "District 29")]
            District29,
            [Display(Name = "Las Virgenes")]
            LasVirgenes,
            Other
        }

        [Required(ErrorMessage = "Please select a water provider.")]
        public WaterProviderOption? WaterProvider { get; set; }

        [StringLength(500)]
        public string? WaterProviderOtherText { get; set; }

        [StringLength(500, ErrorMessage = "Must be 500 characters or fewer.")]
        public string? WaterInfrastructureDescription { get; set; }

        [Required(ErrorMessage = "Please select Yes or No.")]
        public YesNo? TurnedOnWaterToFightFire { get; set; }

        public YesNoDontKnow? LostWaterPressure { get; set; }
        public YesNo? WaterRunningWhenLeft { get; set; }

        [Required(ErrorMessage = "Please select Yes or No.")]
        public YesNo? ObservedHydrantsLowPressure { get; set; }

        [StringLength(5000)]
        public string? HydrantsLowPressureDescription { get; set; }
    }

    public class BrushClearanceSection
    {
        public YesNo? OvergrownBrushCityOfLA { get; set; }
        public YesNo? OvergrownBrushStateOfCA { get; set; }
        public YesNo? OvergrownBrushMRCA { get; set; }
        public YesNo? OvergrownBrushGetty { get; set; }
        public YesNo? OvergrownBrushPalisadesBowl { get; set; }
        public YesNo? OvergrownBrushOtherOwners { get; set; }

        [StringLength(5000)]
        public string? BrushLocationsAndEvidence { get; set; }

        [StringLength(5000)]
        public string? BrushReportedDetails { get; set; }
    }

    public class OtherDefendantInfoSection
    {
        [StringLength(5000, ErrorMessage = "Must be 5000 characters or fewer.")]
        public string? AdditionalLiabilityInformation { get; set; }
    }

    public class DocumentProductionSection
    {
        [Required(ErrorMessage = "Please select a response for this document request.")]
        public DocumentAvailability? Item1Response { get; set; }
        [StringLength(5000, ErrorMessage = "Must be 5000 characters or fewer.")]
        public string? Item1OtherText { get; set; }

        [Required(ErrorMessage = "Please select a response for this document request.")]
        public DocumentAvailability? Item2Response { get; set; }
        [StringLength(5000, ErrorMessage = "Must be 5000 characters or fewer.")]
        public string? Item2OtherText { get; set; }

        [Required(ErrorMessage = "Please select a response for this document request.")]
        public DocumentAvailability? Item3Response { get; set; }
        [StringLength(5000, ErrorMessage = "Must be 5000 characters or fewer.")]
        public string? Item3OtherText { get; set; }

        [Required(ErrorMessage = "Please select a response for this document request.")]
        public DocumentAvailability? Item4Response { get; set; }
        [StringLength(5000, ErrorMessage = "Must be 5000 characters or fewer.")]
        public string? Item4OtherText { get; set; }

        [Required(ErrorMessage = "Please select a response for this document request.")]
        public DocumentAvailability? Item5Response { get; set; }
        [StringLength(5000, ErrorMessage = "Must be 5000 characters or fewer.")]
        public string? Item5OtherText { get; set; }

        [Required(ErrorMessage = "Please select a response for this document request.")]
        public DocumentAvailability? Item6Response { get; set; }
        [StringLength(5000, ErrorMessage = "Must be 5000 characters or fewer.")]
        public string? Item6OtherText { get; set; }

        [Required(ErrorMessage = "Please select a response for this document request.")]
        public DocumentAvailability? Item7Response { get; set; }
        [StringLength(5000, ErrorMessage = "Must be 5000 characters or fewer.")]
        public string? Item7OtherText { get; set; }

        [Required(ErrorMessage = "Please select a response for this document request.")]
        public DocumentAvailability? Item8Response { get; set; }
        [StringLength(5000, ErrorMessage = "Must be 5000 characters or fewer.")]
        public string? Item8OtherText { get; set; }

        [Required(ErrorMessage = "Please select a response for this document request.")]
        public DocumentAvailability? Item9Response { get; set; }
        [StringLength(5000, ErrorMessage = "Must be 5000 characters or fewer.")]
        public string? Item9OtherText { get; set; }

        [Required(ErrorMessage = "Please select a response for this document request.")]
        public DocumentAvailability? Item10Response { get; set; }
        [StringLength(5000, ErrorMessage = "Must be 5000 characters or fewer.")]
        public string? Item10OtherText { get; set; }
    }
}
