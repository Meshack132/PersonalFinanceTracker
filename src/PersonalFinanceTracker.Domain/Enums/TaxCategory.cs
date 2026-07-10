namespace PersonalFinanceTracker.Domain.Enums;

/// <summary>
/// SARS-recognized categories for individual taxpayers.
/// Grouped by whether they affect taxable income or claimable credits.
/// Reference: Income Tax Act 58 of 1962, Sections 11(k), 18A, 6A, 6B.
/// </summary>
public enum TaxCategory
{
    /// <summary>Default — no tax implication.</summary>
    NotApplicable = 0,

    /// <summary>Section 11F — retirement fund contributions (deductible up to 27.5% of income, capped at R350,000/year).</summary>
    RetirementContribution = 1,

    /// <summary>Section 6A — medical scheme fees tax credit (fixed monthly amount per member).</summary>
    MedicalSchemeFees = 2,

    /// <summary>Section 6B — additional medical expenses tax credit (qualifying out-of-pocket medical costs).</summary>
    MedicalOutOfPocket = 3,

    /// <summary>Section 18A — donations to approved Public Benefit Organisations (deductible up to 10% of taxable income).</summary>
    CharitableDonation = 4,

    /// <summary>Home office expenses for salaried employees working from home (subject to strict requirements).</summary>
    HomeOffice = 5,

    /// <summary>Business travel by employees using their own vehicle (with logbook).</summary>
    BusinessTravel = 6,

    /// <summary>Solar/renewable energy tax incentive (Section 12BA — where applicable).</summary>
    SolarInstallation = 7,

    /// <summary>Personal expense — not deductible, but useful to categorize for budgeting.</summary>
    PersonalExpense = 99
}
