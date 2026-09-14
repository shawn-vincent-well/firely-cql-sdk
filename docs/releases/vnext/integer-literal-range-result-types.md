## Fixes

- **CQL-to-ELM translator:** an integer literal outside the range of `Integer` (for example
  `2147483648`, or `Floor(2147483648)`) is still rejected, but it no longer costs the library its
  type information. `ParseNumberLiteral` returned the offending `Literal` straight from its error
  path, which was the one path that skipped assigning a result type, so the emitted node had no
  `valueType`, no `resultTypeName` and no `resultTypeSpecifier`. That omission did not stay local:
  operator resolution over an untyped argument fails in turn, and in the observed cases a single
  out-of-range literal left the whole library with no result types anywhere, which a consumer
  cannot tell apart from a library that simply carries none. Such a literal is now typed `Integer`,
  the type its syntax names whether or not the value fits, with the translation error reported on
  the node: the same treatment an out-of-range long literal has always had, typed `Long` and
  reported alongside. The expression is still an error and the library still fails to translate;
  every other definition in it now annotates normally. Behaviour is unchanged when literal
  validation is disabled, and no generated C# changes.
