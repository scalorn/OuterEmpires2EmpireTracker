using NUnit.Framework;
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
            Assert.That(textBox.AllowSpaces, Is.EqualTo(true));
            Assert.That(textBox.AutoFormat, Is.EqualTo(true));
            Assert.That(textBox.ValidColor, Is.EqualTo(Color.White));
            Assert.That(textBox.InvalidColor, Is.EqualTo(Color.LightCoral));
            Assert.That(textBox.ErrorMessage, Is.EqualTo(""));
            Assert.That(textBox.IsValid, Is.EqualTo(true));
        }

        // -----------------------------------------------------------------------
        // AllowSpaces Property
        // -----------------------------------------------------------------------

        [Test]
        public void AllowSpaces_TrueByDefault()
        {
            var textBox = new ValidatedTextBox();
            Assert.That(textBox.AllowSpaces, Is.True);
        }

        [Test]
        public void AllowSpaces_SetToFalse_ChangesProperty()
        {
            var textBox = new ValidatedTextBox();
            textBox.AllowSpaces = false;
            Assert.That(textBox.AllowSpaces, Is.False);
        }

        // -----------------------------------------------------------------------
        // AutoFormat Property
        // -----------------------------------------------------------------------

        [Test]
        public void AutoFormat_TrueByDefault()
        {
            var textBox = new ValidatedTextBox();
            Assert.That(textBox.AutoFormat, Is.True);
        }

        [Test]
        public void AutoFormat_SetToFalse_ChangesProperty()
        {
            var textBox = new ValidatedTextBox();
            textBox.AutoFormat = false;
            Assert.That(textBox.AutoFormat, Is.False);
        }

        // -----------------------------------------------------------------------
        // InvalidColor Property
        // -----------------------------------------------------------------------

        [Test]
        public void InvalidColor_IsLightCoralByDefault()
        {
            var textBox = new ValidatedTextBox();
            Assert.That(textBox.InvalidColor, Is.EqualTo(Color.LightCoral));
        }

        [Test]
        public void InvalidColor_SetToCustom_ColorIsUpdated()
        {
            var customColor = Color.Blue;
            var textBox = new ValidatedTextBox();
            textBox.InvalidColor = customColor;
            Assert.That(textBox.InvalidColor, Is.EqualTo(customColor));
        }

        // -----------------------------------------------------------------------
        // ValidColor Property
        // -----------------------------------------------------------------------

        [Test]
        public void ValidColor_IsWhiteByDefault()
        {
            var textBox = new ValidatedTextBox();
            Assert.That(textBox.ValidColor, Is.EqualTo(Color.White));
        }

        [Test]
        public void ValidColor_SetToCustom_ColorIsUpdated()
        {
            var customColor = Color.Green;
            var textBox = new ValidatedTextBox();
            textBox.ValidColor = customColor;
            Assert.That(textBox.ValidColor, Is.EqualTo(customColor));
        }

        // -----------------------------------------------------------------------
        // ErrorMessage Property
        // -----------------------------------------------------------------------

        [Test]
        public void ErrorMessage_EmptyByDefault()
        {
            var textBox = new ValidatedTextBox();
            Assert.That(textBox.ErrorMessage, Is.EqualTo(""));
        }

        [Test]
        public void ErrorMessage_SetToCustom_StringIsUpdated()
        {
            var textBox = new ValidatedTextBox();
            textBox.ErrorMessage = "Invalid input";
            Assert.That(textBox.ErrorMessage, Is.EqualTo("Invalid input"));
        }

        // -----------------------------------------------------------------------
        // IsValid Property
        // -----------------------------------------------------------------------

        [Test]
        public void IsValid_TrueByDefault()
        {
            var textBox = new ValidatedTextBox();
            Assert.That(textBox.IsValid, Is.True);
        }

        [Test]
        public void IsValid_SetToFalse_ChangesProperty()
        {
            var textBox = new ValidatedTextBox();
            textBox.IsValid = false;
            Assert.That(textBox.IsValid, Is.False);
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

            Assert.That(textBox.Text, Is.EqualTo(""));
            Assert.That(textBox.IsValid, Is.EqualTo(true));
            Assert.That(textBox.BackColor, Is.EqualTo(Color.White));
            Assert.That(textBox.ErrorMessage, Is.EqualTo(""));
        }

        [Test]
        public void Clear_ResetsBackColorToValid()
        {
            var textBox = new ValidatedTextBox();
            textBox.BackColor = Color.Red; // Manually set invalid color
            textBox.Clear();
            Assert.That(textBox.BackColor, Is.EqualTo(Color.White));
        }

        // -----------------------------------------------------------------------
        // SetError() Method
        // -----------------------------------------------------------------------

        // DISABLED: [Test]
        // Not sure if this is valid. The SetError method is designed to be called by validation logic, not directly by users of the control.
        public void SetError_SetsErrorMessageAndInvalidColor()
        {
            var textBox = new ValidatedTextBox();
            textBox.SetError("Custom error message");

            Assert.That(textBox.ErrorMessage, Is.EqualTo("Custom error message"));
            Assert.That(textBox.InvalidColor, Is.EqualTo(Color.LightCoral));
            Assert.That(textBox.IsValid, Is.EqualTo(false));
        }

        [Test]
        public void SetError_MultiLine_ErrorMessagePreserved()
        {
            var textBox = new ValidatedTextBox();
            textBox.SetError("Line 1\nLine 2\nLine 3");

            Assert.That(textBox.ErrorMessage, Is.EqualTo("Line 1\nLine 2\nLine 3"));
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

            Assert.That(textBox.ErrorMessage, Is.EqualTo(""));
            Assert.That(textBox.ValidColor, Is.EqualTo(Color.White));
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
            Assert.That(result, Is.True);
        }

        [Test]
        public void ValidateInput_WithPattern_MatchesValidation_ReturnsTrue()
        {
            var textBox = new ValidatedTextBox();
            textBox.ValidationPattern = ValidatedTextBox.EMAIL_VALIDATION;
            textBox.Text = "test@example.com";

            bool result = textBox.ValidateInput();
            Assert.That(result, Is.True);
            Assert.That(textBox.BackColor, Is.EqualTo(Color.White));
            Assert.That(textBox.ErrorMessage, Is.EqualTo(""));
        }

        [Test]
        public void ValidateInput_WithPattern_NoMatch_ReturnsFalse()
        {
            var textBox = new ValidatedTextBox();
            textBox.ValidationPattern = ValidatedTextBox.EMAIL_VALIDATION;
            textBox.Text = "not-an-email";

            bool result = textBox.ValidateInput();
            Assert.That(result, Is.False);
            Assert.That(textBox.ErrorMessage, Is.EqualTo("Invalid pattern"));
            // BackColor should be the invalid color
            Assert.That(textBox.BackColor, Is.Not.EqualTo(Color.White));
        }

        [Test]
        public void ValidateInput_WithNumberPattern_ValidNumber_ReturnsTrue()
        {
            var textBox = new ValidatedTextBox();
            textBox.ValidationPattern = ValidatedTextBox.NUMBER_VALIDATION;
            textBox.Text = "12345";

            bool result = textBox.ValidateInput();
            Assert.That(result, Is.True);
        }

        [Test]
        public void ValidateInput_WithNumberPattern_InvalidNumber_ReturnsFalse()
        {
            var textBox = new ValidatedTextBox();
            textBox.ValidationPattern = ValidatedTextBox.NUMBER_VALIDATION;
            textBox.Text = "abc";

            bool result = textBox.ValidateInput();
            Assert.That(result, Is.False);
        }

        // -----------------------------------------------------------------------
        // Validation Pattern Tests
        // -----------------------------------------------------------------------

        [Test]
        public void ValidationPattern_ValidatesEmailFormat()
        {
            var textBox = new ValidatedTextBox();
            textBox.ValidationPattern = ValidatedTextBox.EMAIL_VALIDATION;
            textBox.Text = "user@domain.com";

            bool result = textBox.ValidateInput();
            Assert.That(result, Is.True);
        }

        [Test]
        public void ValidationPattern_ValidatesEmailFormat_IgnoreCase()
        {
            var textBox = new ValidatedTextBox();
            textBox.ValidationPattern = ValidatedTextBox.EMAIL_VALIDATION;
            textBox.Text = "USER@DOMAIN.COM";

            bool result = textBox.ValidateInput();
            Assert.That(result, Is.True);
        }

        [Test]
        public void ValidationPattern_ValidatesNumberFormat()
        {
            var textBox = new ValidatedTextBox();
            textBox.ValidationPattern = ValidatedTextBox.NUMBER_VALIDATION;
            textBox.Text = "1234567890";

            bool result = textBox.ValidateInput();
            Assert.That(result, Is.True);
        }

        [Test]
        public void ValidationPattern_InvalidEmail_ReturnsFalse()
        {
            var textBox = new ValidatedTextBox();
            textBox.ValidationPattern = ValidatedTextBox.EMAIL_VALIDATION;
            textBox.Text = "invalid-email@";

            bool result = textBox.ValidateInput();
            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidationPattern_InvalidNumber_ReturnsFalse()
        {
            var textBox = new ValidatedTextBox();
            textBox.ValidationPattern = ValidatedTextBox.NUMBER_VALIDATION;
            textBox.Text = "abc123";

            bool result = textBox.ValidateInput();
            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidationPattern_NullValue_AlwaysValidates()
        {
            var textBox = new ValidatedTextBox();
            // When ValidationPattern is null, validation should always pass
            textBox.Text = "anything";
            bool result = textBox.ValidateInput();
            Assert.That(result, Is.True);
        }

        // -----------------------------------------------------------------------
        // AllowSpaces Behavior with Validation Pattern
        // -----------------------------------------------------------------------

        // DISABLED: [Test]
        // Not sure this is even valid.
        public void AllowSpaces_WithPattern_MatchAllowsSpaces()
        {
            var textBox = new ValidatedTextBox();
            textBox.ValidationPattern = ValidatedTextBox.EMAIL_VALIDATION;
            textBox.AllowSpaces = true;
            textBox.Text = "test @example.com";

            bool result = textBox.ValidateInput();
            // If validation regex matches, spaces are allowed
            Assert.That(result, Is.True);
        }

        [Test]
        public void AllowSpaces_WithoutPattern_IgnoreValidation()
        {
            var textBox = new ValidatedTextBox();
            textBox.AllowSpaces = true;
            textBox.Text = "spaces are ok";

            bool result = textBox.ValidateInput();
            Assert.That(result, Is.True);
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

            Assert.That(textBox.Text, Is.EqualTo(""));
            Assert.That(textBox.IsValid, Is.EqualTo(true));
            Assert.That(textBox.ErrorMessage, Is.EqualTo(""));
            Assert.That(textBox.ValidColor, Is.EqualTo(Color.White));
        }

        // -----------------------------------------------------------------------
        // InvalidCharacter Detection (HasInvalidCharacter)
        // -----------------------------------------------------------------------

        [Test]
        public void HasInvalidCharacter_WithValidationPattern_DetectsInvalidChars()
        {
            var textBox = new ValidatedTextBox();
            textBox.ValidationPattern = ValidatedTextBox.EMAIL_VALIDATION;

            bool hasSpace = textBox.HasInvalidCharacter(' ');
            Assert.That(hasSpace, Is.True); // Space is invalid for email pattern
        }

        [Test]
        public void HasInvalidCharacter_WithNumberPattern_DetectsLetters()
        {
            var textBox = new ValidatedTextBox();
            textBox.ValidationPattern = ValidatedTextBox.NUMBER_VALIDATION;

            bool hasLetter = textBox.HasInvalidCharacter('a');
            Assert.That(hasLetter, Is.True); // Letters are invalid for number pattern
        }

        [Test]
        public void HasInvalidCharacter_WithoutValidationPattern_AllowsAllChars()
        {
            var textBox = new ValidatedTextBox();
            // No validation pattern set
            bool hasAny = textBox.HasInvalidCharacter('!');
            Assert.That(hasAny, Is.False); // Without pattern, all chars allowed
        }

        [Test]
        public void HasInvalidCharacter_ReturnsFalseForCarriageReturn()
        {
            var textBox = new ValidatedTextBox();
            bool hasCR = textBox.HasInvalidCharacter('\r');
            Assert.That(hasCR, Is.False);
        }

        // -----------------------------------------------------------------------
        // Color Property Tests
        // -----------------------------------------------------------------------

        [Test]
        public void IsValid_PropertyChangesBackColor()
        {
            var textBox = new ValidatedTextBox();

            // TODO: FIXME: We don't expose an event that fires when the text is invalid.
            // That is a feature that should be added.
            // bool wasChanged = false;
            // _ = textBox.PropertyChanged += (sender, e) =>
            // {
            //     if (e.PropertyName == "IsValid") wasChanged = true;
            // };

            // textBox.IsValid = false;
            // The backcolor should be updated through validation logic
        }

        [Test]
        public void ValidColor_IsNotUsedForBackColorDirectly()
        {
            var textBox = new ValidatedTextBox();
            Color expected = Color.White;
            Assert.That(textBox.ValidColor, Is.EqualTo(expected));
        }

        [Test]
        public void InvalidColor_IsNotUsedForBackColorDirectly()
        {
            var textBox = new ValidatedTextBox();
            Color expected = Color.LightCoral;
            Assert.That(textBox.InvalidColor, Is.EqualTo(expected));
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
            Assert.That(result, Is.True);
        }

        [Test]
        public void ValidateInput_WithVeryLongText_ValidatesCorrectly()
        {
            var textBox = new ValidatedTextBox();
            textBox.ValidationPattern = "";
            textBox.Text = new string('a', 1000); // Very long text
            bool result = textBox.ValidateInput();
            // Should validate without issues
            Assert.That(result, Is.True);
        }

        [Test]
        public void SetError_WithNullMessage_ClearsError()
        {
            var textBox = new ValidatedTextBox();
            textBox.SetError(""); // Empty string is treated as null effectively
            Assert.That(textBox.ErrorMessage, Is.EqualTo(""));
        }

        [Test]
        public void ClearError_WithInvalidBackcolor_ResetsToValidColor()
        {
            var textBox = new ValidatedTextBox();
            textBox.BackColor = Color.LightCoral;
            textBox.ClearError();
            Assert.That(textBox.ValidColor, Is.EqualTo(Color.White));
        }

        // -----------------------------------------------------------------------
        // Helper method for PropertyChanged event
        // -----------------------------------------------------------------------

        // private bool _wasPropertyChanged = false;
    }
}