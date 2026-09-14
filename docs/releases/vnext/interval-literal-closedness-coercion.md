## Fixes

- **CQL-to-ELM translator:** coercing the point type of an interval selector whose boundaries are
  both `null` — for example the `Interval(null, null)` in `Interval(null, null) overlaps
  Interval[1, 10]` — no longer produces an interval whose closedness cannot be evaluated. Coercion
  rebuilds the interval and lowered its closedness to `lowClosedExpression` / `highClosedExpression`
  property reads against the source expression. That is the right lowering when the source has a
  value to read from, but a selector with two null boundaries denotes a null interval, so the
  property reads could only evaluate to null — and because ELM gives the expression form precedence
  over the `lowClosed` / `highClosed` attributes, that null is what a consumer had to use, even
  though the selector's own brackets fix its closedness at translation time. Coercion now carries
  that closedness across as the attributes for this case. Intervals whose closedness is only known
  at run time are unaffected and still read it from the source expression, which keeps it consistent
  with the boundaries, read from the same value. **CQL evaluation results change:** an expression
  coercing the point type of a null-bounded interval selector previously offered its consumer a null
  closedness and now offers the declared one.
