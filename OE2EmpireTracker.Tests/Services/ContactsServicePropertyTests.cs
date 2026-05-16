using System;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based tests for ContactsService CRUD operations.
    /// Feature: bl-121-contacts-readonly
    /// Validates: Requirements 8.1, 8.2, 8.3, 9.1, 9.2, 9.3
    /// </summary>
    [TestFixture]
    public class ContactsServicePropertyTests
    {
        private PlayerContext _ctx;
        private ContactsService _svc;

        [SetUp]
        public void SetUp()
        {
            PlayerContext.FilePath = string.Empty;
            _ctx = new PlayerContext(new PlayerRoot());
            _svc = new ContactsService(_ctx);
        }

        private static Gen<string> SafeStringGen()
        {
            return Gen.Elements("abcdefghijklmnopqrstuvwxyz0123456789 -_".ToCharArray())
                .ArrayOf()
                .Where(a => a.Length > 0)
                .Select(a => new string(a));
        }

        /// <summary>
        /// Property 5: Service.UpdateFaction Round-Trip.
        /// Validates: Requirements 8.1
        /// </summary>
        [Test]
        public void UpdateFaction_RoundTrip_PreservesAllFields()
        {
            Prop.ForAll(SafeStringGen().ToArbitrary(), SafeStringGen().ToArbitrary(), (name1, desc1) =>
            {
                var createReq = new FactionCreateRequest { Name = name1, Description = desc1 };
                var created = _svc.CreateFaction(createReq);

                var updateReq = new FactionUpdateRequest { Original = created, Name = desc1, Description = name1 };
                var updated = _svc.UpdateFaction(created.UUID, updateReq);

                return (updated.Name == desc1 && updated.Description == name1 && updated.UUID == created.UUID).ToProperty();
            }).QuickCheckThrowOnFailure();
        }

        /// <summary>
        /// Property 6: Service.CreateFaction Round-Trip.
        /// Validates: Requirements 8.2
        /// </summary>
        [Test]
        public void CreateFaction_RoundTrip_PreservesAllFields()
        {
            Prop.ForAll(SafeStringGen().ToArbitrary(), SafeStringGen().ToArbitrary(), (name, desc) =>
            {
                var req = new FactionCreateRequest { Name = name, Description = desc };
                var created = _svc.CreateFaction(req);

                return (created.Name == name && created.Description == desc && !string.IsNullOrEmpty(created.UUID)).ToProperty();
            }).QuickCheckThrowOnFailure();
        }

        /// <summary>
        /// Property 7: Service.DeleteFaction Removes Faction.
        /// Validates: Requirements 8.3
        /// </summary>
        [Test]
        public void DeleteFaction_RemovesFaction()
        {
            Prop.ForAll(SafeStringGen().ToArbitrary(), SafeStringGen().ToArbitrary(), (name, desc) =>
            {
                var req = new FactionCreateRequest { Name = name, Description = desc };
                var created = _svc.CreateFaction(req);
                _svc.DeleteFaction(created.UUID);

                var found = _ctx.FindMutableFaction(created.UUID);
                return (found == null).ToProperty();
            }).QuickCheckThrowOnFailure();
        }
    }
}
