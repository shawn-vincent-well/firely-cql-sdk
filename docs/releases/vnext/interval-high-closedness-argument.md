## Fixes

- The `Interval` constructor no longer applies the **low** boundary's inclusivity expression to the
  **high** boundary. When the third and fourth arguments are non-literal expressions, CQL-to-ELM
  populated both `Interval.lowClosedExpression` and `Interval.highClosedExpression` from the third
  argument, so an interval's upper bound inclusivity was governed by its lower bound's expression.
  The result was silently wrong, with no error or warning.

  Only the constructor reached through a quoted identifier — `"Interval"(low, high, lowClosed,
  highClosed)` — can supply non-literal inclusivity arguments. The `Interval[...]` selector syntax
  always emits Boolean literals for those two positions and was never affected.

  This changes CQL evaluation results for the affected expressions, which is an input to the version
  level for this release.
