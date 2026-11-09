#nullable enable
using System;

namespace Survival.Domain.Kids
{
    public enum AgeEntryError
    {
        None,
        MissingYear,
        MissingMonth,
        YearOutOfRange,
        InFuture,
    }

    /// <summary>
    /// The neutral first-launch age question: birth month and year, no default answer and no hint that one answer unlocks
    /// more. Only the resulting band is ever stored; the month and year are dropped once classified.
    /// </summary>
    public static class AgeGate
    {
        /// <summary>Whole years between a birth month/year and today (birthday assumed on the 1st of the month).</summary>
        public static int AgeInYears(int birthYear, int birthMonth, DateTime today)
        {
            var age = today.Year - birthYear;
            if (today.Month < birthMonth)
            {
                age--;
            }

            return age;
        }

        public static AgeEntryError Validate(int? birthYear, int? birthMonth, DateTime today, ChildSafetyConfig config)
        {
            if (birthYear == null)
            {
                return AgeEntryError.MissingYear;
            }

            if (birthMonth == null || birthMonth < 1 || birthMonth > 12)
            {
                return AgeEntryError.MissingMonth;
            }

            if (birthYear < today.Year - config.OldestBirthYearSpan || birthYear > today.Year)
            {
                return AgeEntryError.YearOutOfRange;
            }

            if (birthYear == today.Year && birthMonth > today.Month)
            {
                return AgeEntryError.InFuture;
            }

            return AgeEntryError.None;
        }

        /// <summary>The band for a valid entry. Throws on an invalid one; call <see cref="Validate"/> first.</summary>
        public static AgeBandDef Classify(int birthYear, int birthMonth, DateTime today, ChildSafetyConfig config)
        {
            if (Validate(birthYear, birthMonth, today, config) != AgeEntryError.None)
            {
                throw new ArgumentOutOfRangeException(nameof(birthYear), "invalid age entry");
            }

            return config.BandForAge(Math.Max(0, AgeInYears(birthYear, birthMonth, today)));
        }
    }
}
