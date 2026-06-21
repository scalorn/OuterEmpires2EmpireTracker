// -----------------------------------------------------------------------
// <copyright file="AuthorizationHelperTests.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;
using OE2EmpireTracker.Server.Endpoints;
using OE2EmpireTracker.Server.Storage;
using SharingRule = OE2EmpireTracker.Common.Models.SharingRule;
using SharingTargetType = OE2EmpireTracker.Common.Models.SharingTargetType;
using TokenRole = OE2EmpireTracker.Common.Models.TokenRole;

namespace OE2EmpireTracker.Server.Tests.Endpoints;

/// <summary>
/// Unit tests for <see cref="AuthorizationHelper"/>.
/// Validates: Req 14 Criterion 7; Req 15 Criterion 8.
/// </summary>
[TestFixture]
public class AuthorizationHelperTests
{
    private const string CallerUUID = "caller-char-uuid";
    private const string TargetUUID = "target-char-uuid";
    private const string FactionUUID = "faction-uuid";

    /// <summary>
    /// When the caller's CharacterUUID matches the target, access is granted.
    /// </summary>
    [Test]
    public async Task CanAccessCharacterData_OwnerOfCharacter_ReturnsTrue()
    {
        var httpContext = CreateHttpContext(CallerUUID);
        var storage = new StubStorageBackend();

        var result = await AuthorizationHelper.CanAccessCharacterData(
            httpContext, CallerUUID, storage);

        Assert.That(result, Is.True);
    }

    /// <summary>
    /// When the caller is listed as a direct grantee (GranteeType=Character), access is granted.
    /// </summary>
    [Test]
    public async Task CanAccessCharacterData_DirectGrantee_ReturnsTrue()
    {
        var httpContext = CreateHttpContext(CallerUUID);
        var storage = new StubStorageBackend();
        storage.Grantees[TargetUUID] = new List<CharacterGranteePermissions>
        {
            new CharacterGranteePermissions
            {
                OwnerCharacterUUID = TargetUUID,
                GranteeType = GranteeType.Character,
                GranteeUUID = CallerUUID,
            },
        };

        var result = await AuthorizationHelper.CanAccessCharacterData(
            httpContext, TargetUUID, storage);

        Assert.That(result, Is.True);
    }

    /// <summary>
    /// When the caller is a member of a faction listed as a grantee (GranteeType=Faction), access is granted.
    /// </summary>
    [Test]
    public async Task CanAccessCharacterData_FactionGrantee_ReturnsTrue()
    {
        var httpContext = CreateHttpContext(CallerUUID);
        var storage = new StubStorageBackend();
        storage.Grantees[TargetUUID] = new List<CharacterGranteePermissions>
        {
            new CharacterGranteePermissions
            {
                OwnerCharacterUUID = TargetUUID,
                GranteeType = GranteeType.Faction,
                GranteeUUID = FactionUUID,
            },
        };
        storage.FactionMembers[(FactionUUID, CallerUUID)] = new FactionMemberPermissions
        {
            FactionUUID = FactionUUID,
            CharacterUUID = CallerUUID,
            ClearanceLevelUUID = "level-1",
        };

        var result = await AuthorizationHelper.CanAccessCharacterData(
            httpContext, TargetUUID, storage);

        Assert.That(result, Is.True);
    }

    /// <summary>
    /// When the target has a SharingRule with TargetType=Character targeting the caller, access is granted.
    /// </summary>
    [Test]
    public async Task CanAccessCharacterData_SharingRuleCharacter_ReturnsTrue()
    {
        var httpContext = CreateHttpContext(CallerUUID);
        var storage = new StubStorageBackend();
        storage.SharingRules[TargetUUID] = new List<SharingRule>
        {
            new SharingRule
            {
                Id = "rule-1",
                OwnerCharacterUUID = TargetUUID,
                TargetType = SharingTargetType.Character,
                TargetUUID = CallerUUID,
            },
        };

        var result = await AuthorizationHelper.CanAccessCharacterData(
            httpContext, TargetUUID, storage);

        Assert.That(result, Is.True);
    }

    /// <summary>
    /// When the target has a SharingRule with TargetType=Faction targeting a faction the caller belongs to, access is granted.
    /// </summary>
    [Test]
    public async Task CanAccessCharacterData_SharingRuleFaction_ReturnsTrue()
    {
        var httpContext = CreateHttpContext(CallerUUID);
        var storage = new StubStorageBackend();
        storage.SharingRules[TargetUUID] = new List<SharingRule>
        {
            new SharingRule
            {
                Id = "rule-2",
                OwnerCharacterUUID = TargetUUID,
                TargetType = SharingTargetType.Faction,
                TargetUUID = FactionUUID,
            },
        };
        storage.FactionMembers[(FactionUUID, CallerUUID)] = new FactionMemberPermissions
        {
            FactionUUID = FactionUUID,
            CharacterUUID = CallerUUID,
            ClearanceLevelUUID = "level-1",
        };

        var result = await AuthorizationHelper.CanAccessCharacterData(
            httpContext, TargetUUID, storage);

        Assert.That(result, Is.True);
    }

    /// <summary>
    /// When the caller has the Owner role, access is granted regardless of other relationships.
    /// </summary>
    [Test]
    public async Task CanAccessCharacterData_OwnerRole_ReturnsTrue()
    {
        var httpContext = CreateHttpContext("unrelated-uuid", isOwner: true);
        var storage = new StubStorageBackend();

        var result = await AuthorizationHelper.CanAccessCharacterData(
            httpContext, TargetUUID, storage);

        Assert.That(result, Is.True);
    }

    /// <summary>
    /// When no access path exists (not owner, not grantee, no sharing rule, not Owner role), access is denied.
    /// </summary>
    [Test]
    public async Task CanAccessCharacterData_NoRelationship_ReturnsFalse()
    {
        var httpContext = CreateHttpContext(CallerUUID);
        var storage = new StubStorageBackend();

        var result = await AuthorizationHelper.CanAccessCharacterData(
            httpContext, TargetUUID, storage);

        Assert.That(result, Is.False);
    }

    /// <summary>
    /// IsFactionMember returns true when a membership record exists.
    /// </summary>
    [Test]
    public async Task IsFactionMember_MemberExists_ReturnsTrue()
    {
        var storage = new StubStorageBackend();
        storage.FactionMembers[(FactionUUID, CallerUUID)] = new FactionMemberPermissions
        {
            FactionUUID = FactionUUID,
            CharacterUUID = CallerUUID,
            ClearanceLevelUUID = "level-1",
        };

        var result = await AuthorizationHelper.IsFactionMember(
            CallerUUID, FactionUUID, storage);

        Assert.That(result, Is.True);
    }

    /// <summary>
    /// IsFactionMember returns false when no membership record exists.
    /// </summary>
    [Test]
    public async Task IsFactionMember_NotMember_ReturnsFalse()
    {
        var storage = new StubStorageBackend();

        var result = await AuthorizationHelper.IsFactionMember(
            CallerUUID, FactionUUID, storage);

        Assert.That(result, Is.False);
    }

    /// <summary>
    /// IsOwner returns true when the caller has the Owner role.
    /// </summary>
    [Test]
    public void IsOwner_OwnerRole_ReturnsTrue()
    {
        var httpContext = CreateHttpContext(CallerUUID, isOwner: true);

        var result = AuthorizationHelper.IsOwner(httpContext);

        Assert.That(result, Is.True);
    }

    /// <summary>
    /// IsOwner returns false when the caller does not have the Owner role.
    /// </summary>
    [Test]
    public void IsOwner_NonOwner_ReturnsFalse()
    {
        var httpContext = CreateHttpContext(CallerUUID);

        var result = AuthorizationHelper.IsOwner(httpContext);

        Assert.That(result, Is.False);
    }

    /// <summary>
    /// GetCallerCharacterUUID returns the claim value when present.
    /// </summary>
    [Test]
    public void GetCallerCharacterUUID_HasClaim_ReturnsValue()
    {
        var httpContext = CreateHttpContext(CallerUUID);

        var result = AuthorizationHelper.GetCallerCharacterUUID(httpContext);

        Assert.That(result, Is.EqualTo(CallerUUID));
    }

    /// <summary>
    /// GetCallerCharacterUUID returns null when the claim is not present.
    /// </summary>
    [Test]
    public void GetCallerCharacterUUID_NoClaim_ReturnsNull()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity()),
        };

        var result = AuthorizationHelper.GetCallerCharacterUUID(httpContext);

        Assert.That(result, Is.Null);
    }

    private static HttpContext CreateHttpContext(
        string? characterUUID = null,
        bool isOwner = false)
    {
        var claims = new List<Claim>();

        if (characterUUID != null)
        {
            claims.Add(new Claim("CharacterUUID", characterUUID));
        }

        if (isOwner)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Owner"));
        }

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        return new DefaultHttpContext
        {
            User = principal,
        };
    }
}
