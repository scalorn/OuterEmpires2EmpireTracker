// <copyright file="FormBankingTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using OE2EmpireTracker.Forms.Banking;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Forms
{
    /// <summary>
    /// Unit tests verifying FormBanking import buttons are removed and
    /// BankingDataChanged subscription remains active.
    ///
    /// **Validates: Requirements 3.1, 3.2, 3.4**
    /// </summary>
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class FormBankingTests
    {
        [SetUp]
        public void SetUp()
        {
            TestHelper.ResetWithCachedData();
        }

        [TearDown]
        public void TearDown()
        {
            EmpireContext.Reset();
        }

        /// <summary>
        /// Verifies that no control named "btnImportTransactions" exists in the
        /// FormBanking controls collection (including nested containers).
        ///
        /// **Validates: Requirements 3.1**
        /// </summary>
        [Test]
        public void FormBanking_DoesNotContain_BtnImportTransactions()
        {
            using (var form = new FormBanking())
            {
                var match = FindControlByName(form, "btnImportTransactions");
                Assert.That(match, Is.Null, "btnImportTransactions should not exist in FormBanking");
            }
        }

        /// <summary>
        /// Verifies that no control named "btnImportBalance" exists in the
        /// FormBanking controls collection (including nested containers).
        ///
        /// **Validates: Requirements 3.2**
        /// </summary>
        [Test]
        public void FormBanking_DoesNotContain_BtnImportBalance()
        {
            using (var form = new FormBanking())
            {
                var match = FindControlByName(form, "btnImportBalance");
                Assert.That(match, Is.Null, "btnImportBalance should not exist in FormBanking");
            }
        }

        /// <summary>
        /// Verifies that firing BankingDataChanged on PlayerContext causes
        /// FormBanking to refresh its balance display.
        ///
        /// **Validates: Requirements 3.4**
        /// </summary>
        [Test]
        public void FormBanking_RefreshesOnBankingDataChanged()
        {
            var playerContext = EmpireContext.PlayerContext;

            using (var form = new FormBanking())
            {
                // Set a known balance and fire the event
                playerContext.BankingBalance = 12345.67m;
                playerContext.OnBankingDataChanged();

                // The form subscribes to BankingDataChanged and calls RefreshBalanceDisplay
                // which updates lblBalance. Since we're on the STA thread, invoke is synchronous.
                var lblBalance = FindControlByName(form, "lblBalance") as Label;
                Assert.That(lblBalance, Is.Not.Null, "lblBalance should exist in FormBanking");
                Assert.That(
                    lblBalance.Text,
                    Does.Contain("12,345.67").Or.Contain("12345.67"),
                    "Balance label should reflect the updated BankingBalance value after event fires");
            }
        }

        /// <summary>
        /// Recursively searches all controls in the form's control tree for a
        /// control with the specified name.
        /// </summary>
        private static Control FindControlByName(Control parent, string name)
        {
            foreach (Control child in parent.Controls)
            {
                if (string.Equals(child.Name, name, StringComparison.Ordinal))
                {
                    return child;
                }

                var found = FindControlByName(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
