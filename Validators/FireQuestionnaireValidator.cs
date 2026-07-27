using FluentValidation;
using QuestDetails.Models;

namespace QuestDetails.Validators;

/// <summary>
/// Server-side validation rules for the full questionnaire. The rules are
/// deliberately explicit so changes to a section remain easy to review.
/// </summary>
public sealed class FireQuestionnaireValidator : AbstractValidator<FireQuestionnaireModel>
{
    public FireQuestionnaireValidator()
    {
        RuleFor(x => x.IgnitionAndSpread).SetValidator(new IgnitionAndSpreadValidator());
        RuleFor(x => x.Outages).SetValidator(new OutagesValidator());
        RuleFor(x => x.EquipmentAndPoles).SetValidator(new EquipmentAndPolesValidator());
        RuleFor(x => x.WaterUse).SetValidator(new WaterUseValidator());
        RuleFor(x => x.BrushClearance).SetValidator(new BrushClearanceValidator());
        RuleFor(x => x.OtherDefendantInfo).SetValidator(new OtherDefendantInfoValidator());
        RuleFor(x => x.DocumentProduction).SetValidator(new DocumentProductionValidator());
    }

    private sealed class IgnitionAndSpreadValidator : AbstractValidator<IgnitionAndSpreadSection>
    {
        public IgnitionAndSpreadValidator()
        {
            RuleFor(x => x.ObservedFlamesSmokeSmoldering).NotNull().WithMessage("Please select Yes or No.");
            RuleFor(x => x.ObserverNameOrMe).MaximumLength(500);
            RuleFor(x => x.ObservationDescription).MaximumLength(5000);
            RuleFor(x => x.ObservedHomesIgnite).NotNull().WithMessage("Please select Yes or No.");
            RuleFor(x => x.HomesIgniteDescription).MaximumLength(5000);
            RuleFor(x => x.ObservedFireFlamesExplosionsEmbers).NotNull().WithMessage("Please select Yes or No.");
            RuleFor(x => x.FireFlamesExplosionsEmbersDescription).MaximumLength(5000);
        }
    }

    private sealed class OutagesValidator : AbstractValidator<OutagesSection>
    {
        public OutagesValidator()
        {
            RuleFor(x => x.LostServiceDuringFire).NotNull().WithMessage("Please select Yes or No.");
            RuleFor(x => x.OutageDetails).MaximumLength(500);
        }
    }

    private sealed class EquipmentAndPolesValidator : AbstractValidator<EquipmentAndPolesSection>
    {
        public EquipmentAndPolesValidator()
        {
            RuleFor(x => x.AwareOfPoleIssuesBeforeFire).NotNull().WithMessage("Please select Yes or No.");
            RuleFor(x => x.PoleIssuesDescription).MaximumLength(5000);
            RuleFor(x => x.WitnessedBrokenPoleDownedLineSparking).NotNull().WithMessage("Please select Yes or No.");
            RuleFor(x => x.BrokenPoleDescription).MaximumLength(5000);
            RuleFor(x => x.MomentPoleBrokeDescription).MaximumLength(5000);
        }
    }

    private sealed class WaterUseValidator : AbstractValidator<WaterUseSection>
    {
        public WaterUseValidator()
        {
            RuleFor(x => x.WaterProvider).NotNull().WithMessage("Please select a water provider.");
            RuleFor(x => x.WaterProviderOtherText).MaximumLength(500);
            RuleFor(x => x.WaterInfrastructureDescription).MaximumLength(500);
            RuleFor(x => x.TurnedOnWaterToFightFire).NotNull().WithMessage("Please select Yes or No.");
            RuleFor(x => x.HydrantsLowPressureDescription).MaximumLength(5000);
            RuleFor(x => x.ObservedHydrantsLowPressure).NotNull().WithMessage("Please select Yes or No.");
        }
    }

    private sealed class BrushClearanceValidator : AbstractValidator<BrushClearanceSection>
    {
        public BrushClearanceValidator()
        {
            RuleFor(x => x.BrushLocationsAndEvidence).MaximumLength(5000);
            RuleFor(x => x.BrushReportedDetails).MaximumLength(5000);
        }
    }

    private sealed class OtherDefendantInfoValidator : AbstractValidator<OtherDefendantInfoSection>
    {
        public OtherDefendantInfoValidator()
        {
            RuleFor(x => x.AdditionalLiabilityInformation).MaximumLength(5000);
        }
    }

    private sealed class DocumentProductionValidator : AbstractValidator<DocumentProductionSection>
    {
        public DocumentProductionValidator()
        {
            RuleFor(x => x.Item1Response).NotNull().WithMessage("Please select a response for this document request.");
            RuleFor(x => x.Item1OtherText).MaximumLength(5000);
            RuleFor(x => x.Item2Response).NotNull().WithMessage("Please select a response for this document request.");
            RuleFor(x => x.Item2OtherText).MaximumLength(5000);
            RuleFor(x => x.Item3Response).NotNull().WithMessage("Please select a response for this document request.");
            RuleFor(x => x.Item3OtherText).MaximumLength(5000);
            RuleFor(x => x.Item4Response).NotNull().WithMessage("Please select a response for this document request.");
            RuleFor(x => x.Item4OtherText).MaximumLength(5000);
            RuleFor(x => x.Item5Response).NotNull().WithMessage("Please select a response for this document request.");
            RuleFor(x => x.Item5OtherText).MaximumLength(5000);
            RuleFor(x => x.Item6Response).NotNull().WithMessage("Please select a response for this document request.");
            RuleFor(x => x.Item6OtherText).MaximumLength(5000);
            RuleFor(x => x.Item7Response).NotNull().WithMessage("Please select a response for this document request.");
            RuleFor(x => x.Item7OtherText).MaximumLength(5000);
            RuleFor(x => x.Item8Response).NotNull().WithMessage("Please select a response for this document request.");
            RuleFor(x => x.Item8OtherText).MaximumLength(5000);
            RuleFor(x => x.Item9Response).NotNull().WithMessage("Please select a response for this document request.");
            RuleFor(x => x.Item9OtherText).MaximumLength(5000);
            RuleFor(x => x.Item10Response).NotNull().WithMessage("Please select a response for this document request.");
            RuleFor(x => x.Item10OtherText).MaximumLength(5000);
        }
    }
}
