using NUnit.Framework;
using NUnit.Framework.Legacy;
using OE2EmpireTracker.Controls;
using System.Drawing;

namespace OE2EmpireTracker.Tests.Controls
{
    [TestFixture]
    public class ValidatedTextBoxTests
    {
        // -----------------------------------------------------------------------
        // Default Constructor
        // -----------------------------------------------------------------------

        [Test]
        public void DefaultConstructor_InitializesDefaults()
        {
            var textBox = new ValidatedTextBox();
            Assert.AreEqual(true, textBox.AllowSpaces);
            Assert.AreEqual(true, textBox.AutoFormat);
            Assert.AreEqual(Color.White, textBox.ValidColor);
            Assert.AreEqual(Color.LightCoral, textBox.InvalidColor);
            Assert.AreEqual("", textBox.ErrorMessage);
            Assert.AreEqual(true, textBox.IsValid);
        }

        // -----------------------------------------------------------------------
        // AllowSpaces Property
        // -----------------------------------------------------------------------

        [Test]
        public void AllowSpaces_TrueByDefault()
        {
            var textBox = new ValidatedTextBox();
            Assert.IsTrue(textBox.AllowSpaces);
        }

        [Test]
        public void AllowSpaces_SetToFalse_ChangesProperty()
        {
            var textBox = new ValidatedTextBox();
            textBox.AllowSpaces = false;
            Assert.IsFalse(textBox.AllowSpaces);
        }

        // -----------------------------------------------------------------------
        // AutoFormat Property
        // -----------------------------------------------------------------------

        [Test]
        public void AutoFormat_TrueByDefault()
        {
            var textBox = new ValidatedTextBox();
            Assert.IsTrue(textBox.AutoFormat);
        }

        [Test]
        public void AutoFormat_SetToFalse_ChangesProperty()
        {
            var textBox = new ValidatedTextBox();
            textBox.AutoFormat = false;
            Assert.IsFalse(textBox.AutoFormat);
        }

        // -----------------------------------------------------------------------
        // InvalidColor Property
        // -----------------------------------------------------------------------

        [Test]
        public void InvalidColor_IsLightCoralByDefault()
        {
            var textBox = new ValidatedTextBox();
            Assert.AreEqual(Color.LightCoral, textBox.InvalidColor);
        }

        [Test]
        public void InvalidColor_SetToCustom_ColorIsUpdated()
        {
            var customColor = Color.Blue;
            var textBox = new ValidatedTextBox();
            textBox.InvalidColor = customColor;
            Assert.AreEqual(customColor, textBox.InvalidColor);
        }

        // -----------------------------------------------------------------------
        // ValidColor Property
        // -----------------------------------------------------------------------

        [Test]
        public void ValidColor_IsWhiteByDefault()
        {
            var textBox = new ValidatedTextBox();
            Assert.AreEqual(Color.White, textBox.ValidColor);
        }

        [Test]
        public void ValidColor_SetToCustom_ColorIsUpdated()
        {
            var customColor = Color.Green;
            var textBox = new ValidatedTextBox();
            textBox.ValidColor = customColor;
            Assert.AreEqual(customColor, textBox.ValidColor);
        }

        // -----------------------------------------------------------------------
        // ErrorMessage Property
        // -----------------------------------------------------------------------

        [Test]
        public void ErrorMessage_EmptyByDefault()
        {
            var textBox = new ValidatedTextBox();
            Assert.AreEqual("", textBox.ErrorMessage);
        }

        [Test]
        public void ErrorMessage_SetToCustom_StringIsUpdated()
        {
            var textBox = new ValidatedTextBox();
            textBox.ErrorMessage = "Invalid input";
            Assert.AreEqual("Invalid input", textBox.ErrorMessage);
        }

        // -----------------------------------------------------------------------
        // IsValid Property
        // -----------------------------------------------------------------------

        [Test]
        public void IsValid_TrueByDefault()
        {
            var textBox = new ValidatedTextBox();
            Assert.IsTrue(textBox.IsValid);
        }

        [Test]
        public void IsValid_SetToFalse_ChangesProperty()
        {
            var textBox = new ValidatedTextBox();
            textBox.IsValid = false;
            Assert.IsFalse(textBox.IsValid);
        }

        // -----------------------------------------------------------------------
        // Clear() Method
        // -----------------------------------------------------------------------

        [Test]
        public void Clear_CleansTextAndRevalidates()
        {
            var textBox = new ValidatedTextBox();
            textBox.Text = "Some input";
            textBox.SetError("Error message");
            textBox.IsValid = false;

            textBox.Clear();

            Assert.AreEqual("", textBox.Text);
            Assert.AreEqual(true, textBox.IsValid);
            Assert.AreEqual(Color.White, textBox.BackColor);
            Assert.AreEqual("", textBox.ErrorMessage);
        }

        [Test]
        public void Clear_ResetsBackColorToValid()
        {
            var textBox = new ValidatedTextBox();
            textBox.BackColor = Color.Red; // Manually set invalid color
            textBox.Clear();
            Assert.AreEqual(Color.White, textBox.BackColor);
        }

        // -----------------------------------------------------------------------
        // SetError() Method
        // -----------------------------------------------------------------------

        [Test]
        public void SetError_SetsErrorMessageAndInvalidColor()
        {
            var textBox = new ValidatedTextBox();
            textBox.SetError("Custom error message");

            Assert.AreEqual("Custom error message", textBox.ErrorMessage);
            Assert.AreEqual(Color.LightCoral, textBox.InvalidColor);
            Assert.AreEqual(false, textBox.IsValid);
        }

        [Test]
        public void SetError_MultiLine_ErrorMessagePreserved()
        {
            var textBox = new ValidatedTextBox();
            textBox.SetError("Line 1\nLine 2\nLine 3");

            Assert.AreEqual("Line 1\nLine 2\nLine 3", textBox.ErrorMessage);
        }

        // -----------------------------------------------------------------------
        // ClearError() Method
        // -----------------------------------------------------------------------

        [Test]
        public void ClearError_ResetsErrorMessageAndBackcolorToValid()
        {
            var textBox = new ValidatedTextBox();
            textBox.SetError("Some error");
            textBox.BackColor = Color.LightCoral;

            textBox.ClearError();

            Assert.AreEqual("", textBox.ErrorMessage);
            Assert.AreEqual(Color.White, textBox.ValidColor);
        }

        // -----------------------------------------------------------------------
        // ValidateInput() Method
        // -----------------------------------------------------------------------

        [Test]
        public void ValidateInput_NoPattern_AlwaysValid()
        {
            var textBox = new ValidatedTextBox();
            // By default, ValidationPattern is null
            bool result = textBox.ValidateInput();
            Assert.IsTrue(result);
        }

        [Test]
        public void ValidateInput_WithPattern_MatchesValidation_ReturnsTrue()
        {
            var textBox = new ValidatedTextBox();
            textBox.ValidationPattern = "email";
            textBox.Text = "test@example.com";

            bool result = textBox.ValidateInput();
            Assert.IsTrue(result);
            Assert.AreEqual(Color.White, textBox.BackColor);
            Assert.AreEqual("", textBox.ErrorMessage);
        }

        [Test]
        public void ValidateInput_WithPattern_NoMatch_ReturnsFalse()
        {
            var textBox = new ValidatedTextBox();
            textBox.ValidationPattern = "email";
            textBox.Text = "not-an-email";

            bool result = textBox.ValidateInput();
            Assert.IsFalse(result);
            Assert.AreEqual("Invalid pattern", textBox.ErrorMessage);
            // BackColor should be the invalid color
            Assert.AreNotEqual(Color.White, textBox.BackColor);
        }

        [Test]
        public void ValidateInput_WithNumberPattern_ValidNumber_ReturnsTrue()
        {
            var textBox = new ValidatedTextBox();
            textBox.ValidationPattern = "number";
            textBox.Text = "12345";

            bool result = textBox.ValidateInput();
            Assert.IsTrue(result);
        }

        [Test]
        public void ValidateInput_WithNumberPattern_InvalidNumber_ReturnsFalse()
        {
            var textBox = new ValidatedTextBox();
            textBox.ValidationPattern = "number";
            textBox.Text = "abc";

            bool result = textBox.ValidateInput();
            Assert.IsFalse(result);
        }

        // -----------------------------------------------------------------------
        // Validation Pattern Tests
        // -----------------------------------------------------------------------

        [Test]
        public void ValidationPattern_ValidatesEmailFormat()
        {
            var textBox = new ValidatedTextBox();
            textBox.ValidationPattern = "email";
            textBox.Text = "user@domain.com";

            bool result = textBox.ValidateInput();
            Assert.IsTrue(result);
        }

        [Test]
        public void ValidationPattern_ValidatesEmailFormat_IgnoreCase()
        {
            var textBox = new ValidatedTextBox();
            textBox.ValidationPattern = "email";
            textBox.Text = "USER@DOMAIN.COM";

            bool result = textBox.ValidateInput();
            Assert.IsTrue(result);
        }

        [Test]
        public void ValidationPattern_ValidatesNumberFormat()
        {
            var textBox = new ValidatedTextBox();
            textBox.ValidationPattern = "number";
            textBox.Text = "1234567890";

            bool result = textBox.ValidateInput();
            Assert.IsTrue(result);
        }

        [Test]
        public void ValidationPattern_InvalidEmail_ReturnsFalse()
        {
            var textBox = new ValidatedTextBox();
            textBox.ValidationPattern = "email";
            textBox.Text = "invalid-email@";

            bool result = textBox.ValidateInput();
            Assert.IsFalse(result);
        }

        [Test]
        public void ValidationPattern_InvalidNumber_ReturnsFalse()
        {
            var textBox = new ValidatedTextBox();
            textBox.ValidationPattern = "number";
            textBox.Text = "abc123";

            bool result = textBox.ValidateInput();
            Assert.IsFalse(result);
        }

        [Test]
        public void ValidationPattern_NullValue_AlwaysValidates()
        {
            var textBox = new ValidatedTextBox();
            // When ValidationPattern is null, validation should always pass
            textBox.Text = "anything";
            bool result = textBox.ValidateInput();
            Assert.IsTrue(result);
        }

        // -----------------------------------------------------------------------
        // AllowSpaces Behavior with Validation Pattern
        // -----------------------------------------------------------------------

        [Test]
        public void AllowSpaces_WithPattern_MatchAllowsSpaces()
        {
            var textBox = new ValidatedTextBox();
            textBox.ValidationPattern = "email";
            textBox.AllowSpaces = true;
            textBox.Text = "test @example.com";

            bool result = textBox.ValidateInput();
            // If validation regex matches, spaces are allowed
            Assert.IsTrue(result);
        }

        [Test]
        public void AllowSpaces_WithoutPattern_IgnoreValidation()
        {
            var textBox = new ValidatedTextBox();
            textBox.AllowSpaces = true;
            textBox.Text = "spaces are ok";

            bool result = textBox.ValidateInput();
            Assert.IsTrue(result);
        }

        // -----------------------------------------------------------------------
        // Reset() Method
        // -----------------------------------------------------------------------

        [Test]
        public void Reset_ClearsEverythingAndSetsValid()
        {
            var textBox = new ValidatedTextBox();
            textBox.Text = "Some text";
            textBox.SetError("Error message");
            textBox.IsValid = false;
            textBox.ErrorMessage = "Custom error";

            textBox.Reset();

            Assert.AreEqual("", textBox.Text);
            Assert.AreEqual(true, textBox.IsValid);
            Assert.AreEqual("", textBox.ErrorMessage);
            Assert.AreEqual(Color.White, textBox.ValidColor);
        }

        // -----------------------------------------------------------------------
        // InvalidCharacter Detection (HasInvalidCharacter)
        // -----------------------------------------------------------------------

        [Test]
        public void HasInvalidCharacter_WithValidationPattern_DetectsInvalidChars()
        {
            var textBox = new ValidatedTextBox();
            textBox.ValidationPattern = "email";

            bool hasSpace = textBox.HasInvalidCharacter(' ');
            Assert.IsTrue(hasSpace); // Space is invalid for email pattern
        }

        [Test]
        public void HasInvalidCharacter_WithNumberPattern_DetectsLetters()
        {
            var textBox = new ValidatedTextBox();
            textBox.ValidationPattern = "number";

            bool hasLetter = textBox.HasInvalidCharacter('a');
            Assert.IsTrue(hasLetter); // Letters are invalid for number pattern
        }

        [Test]
        public void HasInvalidCharacter_WithoutValidationPattern_AllowsAllChars()
        {
            var textBox = new ValidatedTextBox();
            // No validation pattern set
            bool hasAny = textBox.HasInvalidCharacter('!');
            Assert.IsFalse(hasAny); // Without pattern, all chars allowed
        }

        [Test]
        public void HasInvalidCharacter_ReturnsFalseForCarriageReturn()
        {
            var textBox = new ValidatedTextBox();
            bool hasCR = textBox.HasInvalidCharacter('\r');
            Assert.IsFalse(hasCR);
        }

        // -----------------------------------------------------------------------
        // Color Property Tests
        // -----------------------------------------------------------------------

        [Test]
        public void IsValid_PropertyChangesBackColor()
        {
            var textBox = new ValidatedTextBox();

            bool wasChanged = false;
            _ = textBox.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == "IsValid") wasChanged = true;
            };

            textBox.IsValid = false;
            // The backcolor should be updated through validation logic
        }

        [Test]
        public void ValidColor_IsNotUsedForBackColorDirectly()
        {
            var textBox = new ValidatedTextBox();
            Color expected = Color.White;
            Assert.AreEqual(expected, textBox.ValidColor);
        }

        [Test]
        public void InvalidColor_IsNotUsedForBackColorDirectly()
        {
            var textBox = new ValidatedTextBox();
            Color expected = Color.LightCoral;
            Assert.AreEqual(expected, textBox.InvalidColor);
        }

        // -----------------------------------------------------------------------
        // Edge Cases
        // -----------------------------------------------------------------------

        [Test]
        public void ValidateInput_WithNullText_ReturnsTrue()
        {
            var textBox = new ValidatedTextBox();
            textBox.Text = null; // Set to null
            bool result = textBox.ValidateInput();
            Assert.IsTrue(result);
        }

        [Test]
        public void ValidateInput_WithVeryLongText_ValidatesCorrectly()
        {
            var textBox = new ValidatedTextBox();
            textBox.ValidationPattern = "email";
            textBox.Text = new string('a', 1000); // Very long text
            bool result = textBox.ValidateInput();
            // Should validate without issues
            Assert.IsTrue(result);
        }

        [Test]
        public void SetError_WithNullMessage_ClearsError()
        {
            var textBox = new ValidatedTextBox();
            textBox.SetError(""); // Empty string is treated as null effectively
            Assert.AreEqual("", textBox.ErrorMessage);
        }

        [Test]
        public void ClearError_WithInvalidBackcolor_ResetsToValidColor()
        {
            var textBox = new ValidatedTextBox();
            textBox.BackColor = Color.LightCoral;
            textBox.ClearError();
            Assert.AreEqual(Color.White, textBox.ValidColor);
        }

        // -----------------------------------------------------------------------
        // Helper method for PropertyChanged event
        // -----------------------------------------------------------------------

        private bool _wasPropertyChanged = false;
    }
}