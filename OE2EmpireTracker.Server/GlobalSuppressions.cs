// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// These suppressions target false positives from StyleCop 1.1.118
// which does not understand C# 9+ top-level statements.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "StyleCop.CSharp.LayoutRules",
    "SA1516:ElementsShouldBeSeparatedByBlankLine",
    Justification = "False positive: StyleCop 1.1.118 cannot locate this warning in top-level statement files.")]
