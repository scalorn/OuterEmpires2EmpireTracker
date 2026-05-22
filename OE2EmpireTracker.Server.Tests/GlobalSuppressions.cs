// -----------------------------------------------------------------------
// <copyright file="GlobalSuppressions.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

// SA1009: False positive with null-forgiving operator (!) after closing parenthesis.
// StyleCop 1.1.118 does not handle this C# 8+ syntax correctly.
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1009:ClosingParenthesisMustBeSpacedCorrectly", Justification = "False positive with null-forgiving operator")]

// SA1507: Multiple blank lines in test files for readability between test methods.
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1507:CodeMustNotContainMultipleBlankLinesInARow", Justification = "Test file readability")]

// SA1000: False positive with target-typed new() expressions in C# 9+.
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1000:KeywordsMustBeSpacedCorrectly", Justification = "False positive with target-typed new()")]
