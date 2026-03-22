using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace OE2EmpireTracker.Controls
{
    [ToolboxItem(true)]
    [DefaultEvent("TextChanged")]
    public class ValidatedTextBox : TextBox
    {
        #region Properties

        private bool _allowSpaces = false;
        private bool _autoFormat = true;
        private Regex _validationRegex;
        private string _validationErrorPattern;
        private string _errorMessage = "";
        private bool _isValid = true;
        private Color _validColor = Color.White;
        private Color _invalidColor = Color.LightCoral;
        private Timer _debounceTimer;

        [Category("Validation")]
        public string ValidationPattern
        {
            get => _validationRegex?.ToString();
            set
            {
                _validationErrorPattern = value ?? "";
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

        [Browsable(false)]
        public bool IsValid
        {
            get => _isValid;
            set => _isValid = value;
        }

        #endregion

        #region Constructor & Initialization

        public ValidatedTextBox()
        {
            Font = new Font(FindForm().Font.Name, 10);
            Enabled = true;
            TabIndex = 1;
            //SizeMode = Mode.Single;
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            ValidateInput();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (IsValid && _autoFormat && !e.KeyCode.ToString().StartsWith("Back"))
            {
                FormatInput(e.KeyValue);
            }
            else
            {
                base.OnKeyDown(e);
            }
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (e.KeyChar != '\r' && IsValid && _autoFormat)
            {
                FormatInput(e.KeyChar);
            }
            else
            {
                base.OnKeyPress(e);
            }
        }

        public void Reset()
        {
            Clear();
            IsValid = true;
            _errorMessage = "";
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            ValidateInput();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            if (!IsValid && ErrorMessage != "")
            {
                Focus();
            }
        }

        #endregion

        #region Methods

        public bool ValidateInput()
        {
            var currentText = Text;
            var match = _validationRegex?.Match(currentText);

            if (AllowSpaces && match.Success)
            {
                IsValid = true;
                ErrorMessage = "";
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

        private void DebounceTick(object sender, EventArgs e)
        {
            ValidateInput();
            _debounceTimer.Stop();
        }

        private void FormatInput(int keyChar)
        {
            string input = Text;
            if (AllowSpaces && !Regex.IsMatch(input, @"^[\d\s]+$")) return;

            var validChars = Regex.Matches(ValidationPattern ?? "", @"[^\\s]").Cast<char>().ToList();
            if (validChars.Count <= input.Length) return;

            for (int i = 0; i < input.Length; i++)
            {
                if (i >= validChars.Count) break;

                char expected = validChars[i];

                if (i == 0 && keyChar != '\r')
                {
                    Text = expected.ToString();
                }
                else
                {
                    char current = input[i];
                    int currentIndex = i > 0 ? i - 1 : -1;
                    if (currentIndex >= validChars.Count) break;

                    if (current != '\r')
                    {
                        int matchIndex = Array.IndexOf(input.ToArray(), expected);

                        if (matchIndex != currentIndex && matchIndex >= 0)
                            Text = InputWithReplaced(expected).ToString();
                    }
                }
            }
        }

        private string InputWithReplaced(char replacement)
        {
            var newText = "";
            for (int i = 0; i < Text.Length; i++)
            {
                if (Text[i].Equals(replacement)) newText += replacement;
                else newText += '\u201F';
            }
            return newText;
        }

        #endregion

        public new void Clear()
        {
            base.Clear();
            Text = "";
            IsValid = true;
            BackColor = ValidColor;
            ErrorMessage = "";
        }

        public void SetError(string message)
        {
            _errorMessage = message;
            BackColor = InvalidColor;
            Invalidate();
        }

        public void ClearError()
        {
            _errorMessage = "";
            BackColor = ValidColor;
            Invalidate();
        }

        private bool HasInvalidCharacter(char c)
        {
            if (c == '\r') return false;

            var allowedChars = ValidationPattern ?? string.Empty;
            return !allowedChars.Contains(c);
        }
    }
}
