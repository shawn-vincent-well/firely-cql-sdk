/*
 * Copyright (c) 2026, Firely, NCQA and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-cql-sdk/main/LICENSE
 */

#nullable enable

using Hl7.Cql.Iso8601;

namespace CoreTests;

/// <summary>
/// Covers the <c>strict</c> overloads of the ISO 8601 types' <c>TryParse</c>, which reject a string whose
/// components are individually well-formed but out of range. Without them, such a string either silently
/// produced a different instant than it named (time) or escaped <c>TryParse</c> as an exception (date and
/// date time), both of which leave the caller with no usable signal.
/// </summary>
[TestClass]
[TestCategory("UnitTest")]
public class Iso8601StrictParseTests
{
    [DataTestMethod]
    [DataRow("24:59:59.999")]
    [DataRow("23:60:59.999")]
    [DataRow("23:59:60.999")]
    [DataRow("10:30:00+15:00")]
    public void TimeIso8601_StrictParse_RejectsOutOfRangeComponents(string value)
    {
        Assert.IsFalse(TimeIso8601.TryParse(value, strict: true, out var time));
        Assert.IsNull(time);
    }

    [DataTestMethod]
    [DataRow("00:00:00.000")]
    [DataRow("23:59:59.999")]
    [DataRow("23")]
    [DataRow("23:59")]
    [DataRow("10:30:00-05:30")]
    [DataRow("10:30:00Z")]
    public void TimeIso8601_StrictParse_AcceptsInRangeComponents(string value)
    {
        Assert.IsTrue(TimeIso8601.TryParse(value, strict: true, out var time));
        Assert.AreEqual(value, time!.ToString());
    }

    [TestMethod]
    public void TimeIso8601_NonStrictParse_StillWrapsOutOfRangeComponents()
    {
        // The default overload's behaviour is deliberately unchanged: callers that were relying on it
        // keep what they had, and only callers that ask for strict parsing see the components checked.
        Assert.IsTrue(TimeIso8601.TryParse("24:59:59.999", out var time));
        Assert.AreEqual(24, time!.Hour);
    }

    [DataTestMethod]
    [DataRow("2023-13-01")]
    [DataRow("2023-00-01")]
    [DataRow("2023-02-30")]
    [DataRow("2023-02-29")]
    [DataRow("2023-04-31")]
    public void DateIso8601_StrictParse_RejectsOutOfRangeComponents(string value)
    {
        Assert.IsFalse(DateIso8601.TryParse(value, strict: true, out var date));
        Assert.IsNull(date);
    }

    [DataTestMethod]
    [DataRow("2023")]
    [DataRow("2023-02")]
    [DataRow("2023-02-28")]
    [DataRow("2024-02-29")]
    [DataRow("2023-12-31")]
    public void DateIso8601_StrictParse_AcceptsInRangeComponents(string value)
    {
        Assert.IsTrue(DateIso8601.TryParse(value, strict: true, out var date));
        Assert.AreEqual(value, date!.ToString());
    }

    [DataTestMethod]
    [DataRow("2023-13-01T10:30:00")]
    [DataRow("2023-01-01T24:00:00")]
    [DataRow("2023-01-01T10:60:00")]
    [DataRow("2023-01-01T10:30:60")]
    public void DateTimeIso8601_StrictParse_RejectsOutOfRangeComponents(string value)
    {
        Assert.IsFalse(DateTimeIso8601.TryParse(value, strict: true, out var dateTime));
        Assert.IsNull(dateTime);
    }

    [DataTestMethod]
    [DataRow("2023-12-31T23:59:59.999")]
    [DataRow("2024-02-29T00:00:00.000")]
    [DataRow("2023-01-01T10:30:00-05:30")]
    public void DateTimeIso8601_StrictParse_AcceptsInRangeComponents(string value)
    {
        Assert.IsTrue(DateTimeIso8601.TryParse(value, strict: true, out var dateTime));
        Assert.AreEqual(value, dateTime!.ToString());
    }

    [TestMethod]
    public void TryParse_ReportsFailureInsteadOfThrowing()
    {
        // An out-of-range month reached System.DateTimeOffset's constructor and threw out of TryParse,
        // whose own contract says it returns false rather than throwing.
        Assert.IsFalse(DateIso8601.TryParse("2023-13-01", out var date));
        Assert.IsNull(date);
        Assert.IsFalse(DateTimeIso8601.TryParse("2023-13-01T10:30:00", out var dateTime));
        Assert.IsNull(dateTime);
    }
}
