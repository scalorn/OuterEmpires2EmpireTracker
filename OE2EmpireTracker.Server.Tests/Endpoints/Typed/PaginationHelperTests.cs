// -----------------------------------------------------------------------
// <copyright file="PaginationHelperTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Server.Endpoints.Typed;

namespace OE2EmpireTracker.Server.Tests.Endpoints.Typed;

/// <summary>
/// Unit tests for <see cref="PaginationHelper"/>.
/// Validates: Requirements 29.1, 29.2, 29.3, 29.4, 29.5, 29.6.
/// </summary>
[TestFixture]
public class PaginationHelperTests
{
    private static readonly List<string> SampleItems = Enumerable.Range(1, 50)
        .Select(i => $"item-{i}")
        .ToList();

    /// <summary>
    /// When no limit or offset is provided, returns the raw array (backward compatible).
    /// Validates: Requirement 29.2.
    /// </summary>
    [Test]
    public void ApplyPagination_NoParams_ReturnsRawArray()
    {
        var result = PaginationHelper.ApplyPagination<string>(SampleItems, null, null);

        var okResult = result as Ok<IReadOnlyList<string>>;
        Assert.That(okResult, Is.Not.Null, "Expected Ok<IReadOnlyList<string>> result");
        Assert.That(okResult!.Value, Is.EqualTo(SampleItems));
    }

    /// <summary>
    /// When limit and offset are provided, returns a PaginatedResponse with correct metadata.
    /// Validates: Requirements 29.1, 29.3, 29.4.
    /// </summary>
    [Test]
    public void ApplyPagination_WithLimitOffset_ReturnsPaginatedResponse()
    {
        var result = PaginationHelper.ApplyPagination<string>(SampleItems, 10, 5);

        var okResult = result as Ok<PaginatedResponse<string>>;
        Assert.That(okResult, Is.Not.Null, "Expected Ok<PaginatedResponse<string>> result");

        var response = okResult!.Value!;
        Assert.That(response.Items, Has.Count.EqualTo(10));
        Assert.That(response.Total, Is.EqualTo(50));
        Assert.That(response.Limit, Is.EqualTo(10));
        Assert.That(response.Offset, Is.EqualTo(5));
        Assert.That(response.Items[0], Is.EqualTo("item-6"));
    }

    /// <summary>
    /// When limit exceeds MaxLimit (500), it is capped at 500 without error.
    /// Validates: Requirement 29.6.
    /// </summary>
    [Test]
    public void ApplyPagination_LimitExceedsMax_CapsAt500()
    {
        var largeCollection = Enumerable.Range(1, 1000).Select(i => i).ToList();

        var result = PaginationHelper.ApplyPagination<int>(largeCollection, 999, 0);

        var okResult = result as Ok<PaginatedResponse<int>>;
        Assert.That(okResult, Is.Not.Null, "Expected Ok<PaginatedResponse<int>> result");

        var response = okResult!.Value!;
        Assert.That(response.Limit, Is.EqualTo(500));
        Assert.That(response.Items, Has.Count.EqualTo(500));
        Assert.That(response.Total, Is.EqualTo(1000));
    }

    /// <summary>
    /// When offset is negative, returns 400 Bad Request.
    /// Validates: Requirement 29.5.
    /// </summary>
    [Test]
    public void ApplyPagination_NegativeOffset_Returns400()
    {
        var result = PaginationHelper.ApplyPagination<string>(SampleItems, 10, -1);

        // BadRequest<T> where T is an anonymous type — verify via StatusCode property
        var statusCodeProp = result.GetType().GetProperty("StatusCode");
        Assert.That(statusCodeProp, Is.Not.Null, "Expected result to have StatusCode property");
        Assert.That(statusCodeProp!.GetValue(result), Is.EqualTo(400));
    }

    /// <summary>
    /// When limit is negative, returns 400 Bad Request.
    /// Validates: Requirement 29.5.
    /// </summary>
    [Test]
    public void ApplyPagination_NegativeLimit_Returns400()
    {
        var result = PaginationHelper.ApplyPagination<string>(SampleItems, -5, 0);

        // BadRequest<T> where T is an anonymous type — verify via StatusCode property
        var statusCodeProp = result.GetType().GetProperty("StatusCode");
        Assert.That(statusCodeProp, Is.Not.Null, "Expected result to have StatusCode property");
        Assert.That(statusCodeProp!.GetValue(result), Is.EqualTo(400));
    }

    /// <summary>
    /// When offset is beyond the collection size, returns empty items with correct total.
    /// Validates: Requirements 29.3, 29.4.
    /// </summary>
    [Test]
    public void ApplyPagination_OffsetBeyondCollection_ReturnsEmptyItemsWithCorrectTotal()
    {
        var result = PaginationHelper.ApplyPagination<string>(SampleItems, 10, 100);

        var okResult = result as Ok<PaginatedResponse<string>>;
        Assert.That(okResult, Is.Not.Null, "Expected Ok<PaginatedResponse<string>> result");

        var response = okResult!.Value!;
        Assert.That(response.Items, Is.Empty);
        Assert.That(response.Total, Is.EqualTo(50));
        Assert.That(response.Offset, Is.EqualTo(100));
    }

    /// <summary>
    /// When only limit is provided (no offset), offset defaults to 0.
    /// Validates: Requirement 29.3.
    /// </summary>
    [Test]
    public void ApplyPagination_OnlyLimit_OffsetDefaultsToZero()
    {
        var result = PaginationHelper.ApplyPagination<string>(SampleItems, 5, null);

        var okResult = result as Ok<PaginatedResponse<string>>;
        Assert.That(okResult, Is.Not.Null, "Expected Ok<PaginatedResponse<string>> result");

        var response = okResult!.Value!;
        Assert.That(response.Offset, Is.EqualTo(0));
        Assert.That(response.Items, Has.Count.EqualTo(5));
        Assert.That(response.Items[0], Is.EqualTo("item-1"));
    }

    /// <summary>
    /// When only offset is provided (no limit), limit defaults to MaxLimit.
    /// Validates: Requirements 29.3, 29.6.
    /// </summary>
    [Test]
    public void ApplyPagination_OnlyOffset_LimitDefaultsToMaxLimit()
    {
        var result = PaginationHelper.ApplyPagination<string>(SampleItems, null, 10);

        var okResult = result as Ok<PaginatedResponse<string>>;
        Assert.That(okResult, Is.Not.Null, "Expected Ok<PaginatedResponse<string>> result");

        var response = okResult!.Value!;
        Assert.That(response.Limit, Is.EqualTo(PaginationHelper.MaxLimit));
        Assert.That(response.Items, Has.Count.EqualTo(40));
    }
}
