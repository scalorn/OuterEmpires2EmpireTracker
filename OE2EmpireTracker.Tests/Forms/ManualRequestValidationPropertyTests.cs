// <copyright file="ManualRequestValidationPropertyTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

// Feature: api-manual-requests, Property 2: Invalid ID Rejection
// Feature: api-manual-requests, Property 3: Successful Response Formatting

using System.Linq;
using FsCheck;
using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Forms.GameApiStatus;

namespace OE2EmpireTracker.Tests.Forms
{
    /// <summary>
    /// Property-based tests for ManualRequestValidation input rejection.
    /// Feature: api-manual-requests
    /// </summary>
    [TestFixture]
    public class ManualRequestValidationPropertyTests
    {
        // ---------------------------------------------------------------
        // Property 2: Invalid ID Rejection
        // For any SingleId endpoint and any non-positive-integer string,
        // validation returns IsValid=false with "Error: ID must be a number"
        // **Validates: Requirements 3.1, 3.2**
        // ---------------------------------------------------------------

        private static Gen<ManualRequestEndpoint> SingleIdEndpointGen()
        {
            var singleIdEndpoints = ManualRequestEndpoint.All
                .Where(e => e.Category == EndpointCategory.SingleId)
                .ToList();
            return from index in Gen.Choose(0, singleIdEndpoints.Count - 1)
                   select singleIdEndpoints[index];
        }

        private static Gen<string> InvalidIdGen()
        {
            return Gen.Frequency(
                System.Tuple.Create(1, Gen.Constant(string.Empty)),
                System.Tuple.Create(1, Gen.Constant((string)null)),
                System.Tuple.Create(1, Gen.Constant("   ")),
                System.Tuple.Create(1, Gen.Constant("\t")),
                System.Tuple.Create(2, Gen.Elements("abc", "hello", "ID", "xyz", "test", "NaN")),
                System.Tuple.Create(1, Gen.Elements("1.5", "2.7", "0.1", "3.14", "99.9")),
                System.Tuple.Create(1, Gen.Elements("-1", "-5", "-100", "-999")),
                System.Tuple.Create(1, Gen.Constant("0")),
                System.Tuple.Create(1, Gen.Elements("12abc", "4 5", "1,000", "1_000", "5.")));
        }

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property InvalidId_AlwaysRejected()
        {
            var inputGen = from endpoint in SingleIdEndpointGen()
                           from invalidId in InvalidIdGen()
                           select new { Endpoint = endpoint, InvalidId = invalidId };

            return Prop.ForAll(inputGen.ToArbitrary(), data =>
            {
                var result = ManualRequestValidation.ValidateInputs(
                    data.Endpoint,
                    data.InvalidId,
                    null,
                    null);

                var isInvalid = !result.IsValid;
                var hasCorrectMessage = result.ErrorMessage == "Error: ID must be a number";

                return isInvalid
                    .Label($"IsValid should be false for input \"{data.InvalidId}\" on endpoint \"{data.Endpoint.DisplayName}\"")
                    .And(hasCorrectMessage)
                    .Label($"ErrorMessage should be \"Error: ID must be a number\" but was \"{result.ErrorMessage}\"");
            });
        }

        // ---------------------------------------------------------------
        // Property 3: Successful Response Formatting
        // For any valid JSON string with success=true, output starts with
        // "HTTP 200 OK" followed by blank line followed by indented JSON
        // **Validates: Requirements 5.1, 5.2**
        // ---------------------------------------------------------------

        private static Gen<string> ValidJsonGen()
        {
            return Gen.Frequency(
                System.Tuple.Create(3, SimpleObjectGen()),
                System.Tuple.Create(2, SimpleArrayGen()),
                System.Tuple.Create(1, NestedObjectGen()));
        }

        private static Gen<string> SimpleObjectGen()
        {
            return from key in Gen.Elements("id", "name", "value", "status", "count")
                   from intVal in Gen.Choose(0, 10000)
                   from strVal in Gen.Elements("alpha", "beta", "gamma", "delta")
                   from useBool in Gen.Elements(true, false)
                   select useBool
                       ? "{\"" + key + "\":" + intVal + ",\"active\":true}"
                       : "{\"" + key + "\":\"" + strVal + "\",\"count\":" + intVal + "}";
        }

        private static Gen<string> SimpleArrayGen()
        {
            return from a in Gen.Choose(1, 100)
                   from b in Gen.Choose(1, 100)
                   from c in Gen.Choose(1, 100)
                   select "[" + a + "," + b + "," + c + "]";
        }

        private static Gen<string> NestedObjectGen()
        {
            return from outerKey in Gen.Elements("data", "result", "payload")
                   from innerKey in Gen.Elements("id", "type", "level")
                   from val in Gen.Choose(1, 999)
                   select "{\"" + outerKey + "\":{\"" + innerKey + "\":" + val + "}}";
        }

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property SuccessResponse_AlwaysFormatted()
        {
            return Prop.ForAll(ValidJsonGen().ToArbitrary(), json =>
            {
                string result = ManualRequestFormatter.FormatResponse(true, 200, json);

                string expectedHeader = "HTTP 200 OK\r\n\r\n";
                bool startsWithHeader = result.StartsWith(expectedHeader);

                string body = result.Substring(expectedHeader.Length);
                object parsedInput = JsonConvert.DeserializeObject(json);
                object parsedOutput = JsonConvert.DeserializeObject(body);
                string normalizedInput = JsonConvert.SerializeObject(parsedInput);
                string normalizedOutput = JsonConvert.SerializeObject(parsedOutput);
                bool jsonEquals = normalizedInput == normalizedOutput;

                bool isIndented = body.Contains("\n") && body.Contains("  ");

                return startsWithHeader
                    .Label($"Result should start with \"HTTP 200 OK\\r\\n\\r\\n\" but was \"{result.Substring(0, System.Math.Min(30, result.Length))}...\"")
                    .And(jsonEquals)
                    .Label($"Parsed output JSON should equal parsed input JSON")
                    .And(isIndented)
                    .Label("Output body should be indented (contain newlines and spaces)");
            });
        }
    }
}
