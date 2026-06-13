// <copyright file="ManualRequestValidation.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

namespace OE2EmpireTracker.Forms.GameApiStatus
{
    /// <summary>
    /// Provides input validation for manual API request parameters.
    /// </summary>
    public static class ManualRequestValidation
    {
        /// <summary>
        /// Validates user inputs based on the selected endpoint's category.
        /// </summary>
        /// <param name="endpoint">The selected endpoint.</param>
        /// <param name="idText">The ID text input value.</param>
        /// <param name="viewText">The View text input value.</param>
        /// <param name="marketIdsText">The Market IDs text input value.</param>
        /// <returns>A tuple indicating whether inputs are valid and an error message if not.</returns>
        public static (bool IsValid, string ErrorMessage) ValidateInputs(
            ManualRequestEndpoint endpoint,
            string idText,
            string viewText,
            string marketIdsText)
        {
            switch (endpoint.Category)
            {
                case EndpointCategory.SingleId:
                case EndpointCategory.LocationDetail:
                    if (!IsValidPositiveInteger(idText))
                    {
                        return (false, "Error: ID must be a number");
                    }

                    break;

                case EndpointCategory.MarketView:
                    if (string.IsNullOrWhiteSpace(viewText))
                    {
                        return (false, "Error: View is required");
                    }

                    break;

                case EndpointCategory.MarketCompetitors:
                    if (string.IsNullOrWhiteSpace(marketIdsText))
                    {
                        return (false, "Error: Market IDs are required");
                    }

                    break;

                case EndpointCategory.Parameterless:
                default:
                    break;
            }

            return (true, null);
        }

        private static bool IsValidPositiveInteger(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            return int.TryParse(text, out int value) && value > 0;
        }
    }
}
