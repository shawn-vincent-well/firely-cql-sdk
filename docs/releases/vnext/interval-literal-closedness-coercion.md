## Fixes

- **CQL-to-ELM translator:** coercing the point type of an interval selector whose boundaries are
  both `null` — for example the `Interval(null, null)` in `Interval(null, null) overlaps
  Interval[1, 10]` — now emits a `Null` typed as the target interval type. Such a selector denotes a
  null interval: it has no point type, so there is no range for either boundary to stand for, and
  coercing a null interval yields a null interval. Coercion previously rebuilt it as an interval
  whose boundaries and closedness were property reads against the selector itself, a shape that
  evaluates only by null propagation through the closedness expressions; carrying the selector's
  closedness across as attributes instead, which was the first version of this fix, would have been
  worse, because the specification reads a closed null boundary as the beginning or end of the point
  type's range, so a null interval would have quietly become one spanning the whole of the target
  type. `Interval[null as T, null as T]` is unaffected: its boundaries are casts rather than nulls,
  it has a point type, and it spans the whole of `T` as the specification says. **CQL evaluation
  results change** for consumers that did not already propagate the null closedness: an expression
  coercing a null-bounded interval selector is now null rather than unbounded.
