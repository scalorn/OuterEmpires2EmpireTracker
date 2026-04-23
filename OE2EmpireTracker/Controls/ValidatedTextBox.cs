using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OE2EmpireTracker.Controls
{
    [ToolboxItem(true)]
    [DefaultEvent("TextChanged")]
    public class ValidatedTextBox : TextBox
    {
        public static readonly string EmailValidation = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

        public static readonly string DecimalValidation = @"^[+-]?\d+\.\d{2}$";

        public static readonly string NumberValidation = @"^[+-]?\d+$";

        [Category("Validation")]
        public string ValidationPattern
        {
            get => _validationRegex?.ToString();
            set
            {
                _validationErrorPattern = value ?? string.Empty;
                var options = RegexOptions.IgnoreCase | RegexOptions.Compiled;
                _validationRegex = new Regex(_validationErrorPattern, options);

                if (_debounceTimer != null)
                {
                    _debounceTimer.Dispose();
                }

                _debounceTimer = new Timer();
                _debounceTimer.Interval = 300;
                _debounceTimer.Tick += DebounceTick;
            }
        }

        [Browsable(false)]
        public bool IsValid
        {
            get => _isValid;
            set => _isValid = value;
        }

        private bool _allowSpaces = true;

        private bool _autoFormat = true;

        private Regex _validationRegex;

        private string _validationErrorPattern;

        private string _errorMessage = string.Empty;

        private bool _isValid = true;

        private bool _hasExternalError = false;

        private Color _validColor = Color.White;

        private Color _invalidColor = Color.LightCoral;

        private Timer _debounceTimer;

        public ValidatedTextBox()
        {
            Form form = FindForm();
            if (form != null && form.Font != null)
            {
                Font = new Font(form.Font.Name, 10);
            }

            Enabled = true;
            TabIndex = 1;
            // SizeMode = Mode.Single;
        }

        [Category("Validation")]
        public bool AllowSpaces { get => _allowSpaces; set => _allowSpaces = value; }

        [Category("Format")]
        public bool AutoFormat { get => _autoFormat; set => _autoFormat = value; }

        [Category("Colors")]
        public Color InvalidColor { get => _invalidColor; set => _invalidColor = value; }

        [Category("Colors")]
        public Color ValidColor { get => _validColor; set => _validColor = value; }

        [Browsable(false)]
        public string ErrorMessage { get => _errorMessage; set => _errorMessage = value; }

        public void Reset()
        {
            Clear();
            IsValid = true;
            _errorMessage = string.Empty;
        }

        public bool ValidateInput()
        {
            var currentText = Text;
            var match = _validationRegex?.Match(currentText);

            if (AllowSpaces && (match != null && match.Success))
            {
                IsValid = true;
                ErrorMessage = string.Empty;
                BackColor = ValidColor;
            }
            else if (ValidationPattern != null)
            {
                IsValid = bool.TryParse(match.Success.ToString(), out var validResult) && validResult;
                ErrorMessage = !IsValid ? "Invalid pattern" : string.Empty;

                if (!IsValid)
                    BackColor = InvalidColor;
                else
                    BackColor = ValidColor;
            }
            else
            {
                IsValid = true;
                BackColor = ValidColor;
            }

            return IsValid;
        }

        public new void Clear()
        {
            base.Clear();
            Text = string.Empty;
            IsValid = true;
            BackColor = ValidColor;
            ErrorMessage = string.Empty;
        }

        public void SetError(string message)
        {
            _errorMessage = message;
            _isValid = false;
            _hasExternalError = true;
            BackColor = InvalidColor;
            Invalidate();
        }

        public void ClearError()
        {
            _errorMessage = string.Empty;
            _isValid = true;
            _hasExternalError = false;
            BackColor = ValidColor;
            Invalidate();
        }

        public bool HasInvalidCharacter(char c)
        {
            if (c == '\r') return false;

            var allowedChars = ValidationPattern ?? string.Empty;
            if (string.IsNullOrEmpty(allowedChars))
            {
                return false;
            }

            return !allowedChars.Contains(c);
        }

        protected override void OnTextChanged(EventArgs e)
        {
            _hasExternalError = false;
            base.OnTextChanged(e);
            if (!_hasExternalError)
            {
                ValidateInput();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            if (!_hasExternalError)
            {
                ValidateInput();
            }
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            if (_hasExternalError || (!IsValid && ErrorMessage != string.Empty))
            {
                Focus();
            }
        }

        private void DebounceTick(object sender, EventArgs e)
        {
            ValidateInput();
            _debounceTimer.Stop();
        }
    }
}
