// <copyright file="BankingServiceImportTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for BankingService.ImportTransactionsAsync.
    /// Uses HttpListener to serve controlled API responses to a real GameApiClient.
    /// Requirements: 3.1, 3.2, 3.3, 3.4, 3.7.
    /// </summary>
    [TestFixture]
    public class BankingServiceImportTests
    {
        private HttpListener _listener;
        private string _baseUrl;
        private GameApiClient _client;
        private PlayerContext _playerContext;

        /// <summary>
        /// Sets up HttpListener, GameApiClient, and PlayerContext for each test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _baseUrl = "http://localhost:" + GetAvailablePort() + "/";
            _listener = new HttpListener();
            _listener.Prefixes.Add(_baseUrl);
            _listener.Start();

            _client = new GameApiClient(_baseUrl.TrimEnd('/'));

            PlayerContext.FilePath = string.Empty;
            _playerContext = new PlayerContext(new PlayerRoot());
        }

        /// <summary>
        /// Tears down all test resources.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            _client?.Dispose();

            if (_listener != null && _listener.IsListening)
            {
                _listener.Stop();
                _listener.Close();
            }
        }

        // -------------------------------------------------------------------
        // Test: Successful multi-page import
        // Validates: Req 3.1 (pagination), Req 3.2 (stop on partial page), Req 3.7
        // -------------------------------------------------------------------

        /// <summary>
        /// Importing two pages of transactions (first page full, second partial)
        /// imports all records and reports correct counts.
        /// </summary>
        [Test]
        public async Task ImportTransactionsAsync_MultiPage_ImportsAllRecords()
        {
            SetupMultipleResponses(context =>
            {
                string url = context.Request.Url.Query;
                if (url.Contains("offset=0"))
                {
                    // First page: 100 records (full page triggers next request)
                    var transactions = BuildTransactionArray(100, 0);
                    string json = BuildEnvelopeJson(transactions);
                    return (HttpStatusCode.OK, json);
                }
                else
                {
                    // Second page: 30 records (partial page stops pagination)
                    var transactions = BuildTransactionArray(30, 100);
                    string json = BuildEnvelopeJson(transactions);
                    return (HttpStatusCode.OK, json);
                }
            });

            var result = await BankingService.ImportTransactionsAsync(_client, _playerContext);

            Assert.That(result.Success, Is.True);
            Assert.That(result.TransactionsImported, Is.EqualTo(130));
            Assert.That(result.PagesCompleted, Is.EqualTo(2));
            Assert.That(result.DuplicatesSkipped, Is.EqualTo(0));
            Assert.That(_playerContext.BankingTransactionList.Count, Is.EqualTo(130));
        }

        // -------------------------------------------------------------------
        // Test: Deduplication skips existing transactions
        // Validates: Req 3.3 (composite key deduplication)
        // -------------------------------------------------------------------

        /// <summary>
        /// When existing transactions match the composite key of incoming records,
        /// those records are skipped and DuplicatesSkipped is incremented.
        /// </summary>
        [Test]
        public async Task ImportTransactionsAsync_Deduplication_SkipsExistingTransactions()
        {
            // Build the same transactions that BuildTransactionArray(5, 0) will produce
            // so we can pre-populate matching composite keys.
            // Composite key = TransactionDateTime|CreditChange|Detail
            var page = BuildTransactionArray(5, 0);
            string pageJson = JsonConvert.SerializeObject(page);
            var pageArray = JArray.Parse(pageJson);

            // Pre-populate with 2 transactions matching the first 2 from the page
            for (int i = 0; i < 2; i++)
            {
                var item = pageArray[i];
                _playerContext.AddBankingTransaction(new BankingTransaction
                {
                    UUID = Guid.NewGuid().ToString(),
                    TransactionDateTime = item.Value<string>("transactionDT"),
                    CreditChange = item.Value<decimal>("creditChange"),
                    Detail = item.Value<string>("detail"),
                });
            }

            SetupMultipleResponses(context =>
            {
                // Return 5 transactions, 2 of which match existing records
                var transactions = BuildTransactionArray(5, 0);
                string json = BuildEnvelopeJson(transactions);
                return (HttpStatusCode.OK, json);
            });

            var result = await BankingService.ImportTransactionsAsync(_client, _playerContext);

            Assert.That(result.Success, Is.True);
            Assert.That(result.DuplicatesSkipped, Is.EqualTo(2));
            Assert.That(result.TransactionsImported, Is.EqualTo(3));
            Assert.That(_playerContext.BankingTransactionList.Count, Is.EqualTo(5));
        }

        // -------------------------------------------------------------------
        // Test: Partial failure retains prior pages
        // Validates: Req 3.4 (retain prior pages on error)
        // -------------------------------------------------------------------

        /// <summary>
        /// When the API fails on page 2, transactions from page 1 are retained
        /// and the result indicates partial success.
        /// </summary>
        [Test]
        public async Task ImportTransactionsAsync_PartialFailure_RetainsPriorPages()
        {
            SetupMultipleResponses(context =>
            {
                string url = context.Request.Url.Query;
                if (url.Contains("offset=0"))
                {
                    // First page succeeds with 100 records
                    var transactions = BuildTransactionArray(100, 0);
                    string json = BuildEnvelopeJson(transactions);
                    return (HttpStatusCode.OK, json);
                }
                else
                {
                    // Second page fails with 403 (non-transient, no retries)
                    return (HttpStatusCode.Forbidden, string.Empty);
                }
            });

            var result = await BankingService.ImportTransactionsAsync(_client, _playerContext);

            Assert.That(result.Success, Is.False);
            Assert.That(result.FailedAtPage, Is.EqualTo(2));
            Assert.That(result.TransactionsImported, Is.EqualTo(100));
            Assert.That(result.PagesCompleted, Is.EqualTo(1));
            Assert.That(_playerContext.BankingTransactionList.Count, Is.EqualTo(100));
        }

        // -------------------------------------------------------------------
        // Test: API error sets Success=false with FailedAtPage
        // Validates: Req 3.4 (error reporting with FailedAtPage)
        // -------------------------------------------------------------------

        /// <summary>
        /// When the API returns an error on the first page, Success is false
        /// and FailedAtPage indicates the failing page number.
        /// </summary>
        [Test]
        public async Task ImportTransactionsAsync_ApiError_SetsFailedAtPage()
        {
            SetupMultipleResponses(context =>
            {
                // Return 403 (non-transient, no retries)
                return (HttpStatusCode.Forbidden, string.Empty);
            });

            var result = await BankingService.ImportTransactionsAsync(_client, _playerContext);

            Assert.That(result.Success, Is.False);
            Assert.That(result.FailedAtPage, Is.EqualTo(1));
            Assert.That(result.TransactionsImported, Is.EqualTo(0));
            Assert.That(result.PagesCompleted, Is.EqualTo(0));
            Assert.That(_playerContext.BankingTransactionList.Count, Is.EqualTo(0));
        }

        // -------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------

        private static int GetAvailablePort()
        {
            var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private static string BuildEnvelopeJson(object data)
        {
            var envelope = new
            {
                success = true,
                returnCode = 0,
                returnString = string.Empty,
                data = data,
            };
            return JsonConvert.SerializeObject(envelope);
        }

        private static object[] BuildTransactionArray(int count, int startIndex)
        {
            var transactions = new object[count];
            for (int i = 0; i < count; i++)
            {
                int idx = startIndex + i;
                int hours = idx / 3600;
                int minutes = (idx % 3600) / 60;
                int seconds = idx % 60;
                transactions[i] = new
                {
                    transactionDT = string.Format(
                        "2025-01-01T{0:D2}:{1:D2}:{2:D2}Z",
                        hours,
                        minutes,
                        seconds),
                    creditChange = 100.00m,
                    oldBalance = 1000.00m + (idx * 100m),
                    newBalance = 1100.00m + (idx * 100m),
                    transactionType = 2,
                    detail = "Txn-" + idx,
                    characterId = 1,
                    systemObjectId = 10,
                    systemId = 5,
                };
            }

            return transactions;
        }

        private void SetupMultipleResponses(
            Func<HttpListenerContext, (HttpStatusCode StatusCode, string Body)> handler)
        {
            Task.Run(async () =>
            {
                try
                {
                    while (_listener.IsListening)
                    {
                        var context = await _listener.GetContextAsync();
                        var result = handler(context);
                        context.Response.StatusCode = (int)result.StatusCode;
                        if (!string.IsNullOrEmpty(result.Body))
                        {
                            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(result.Body);
                            context.Response.ContentLength64 = buffer.Length;
                            context.Response.ContentType = "application/json";
                            await context.Response.OutputStream.WriteAsync(
                                buffer, 0, buffer.Length);
                        }

                        context.Response.Close();
                    }
                }
                catch (ObjectDisposedException)
                {
                }
                catch (HttpListenerException)
                {
                }
            });
        }
    }
}
