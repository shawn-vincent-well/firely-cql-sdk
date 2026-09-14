## Fixes

- **CQL-to-ELM translator:** a library may now define an expression and a function with the same
  name, as the HL7 reference translator allows: a function is only ever referenced with an argument
  list, so `"Span"` and `Span(a, b)` are never ambiguous. Both definitions were previously keyed
  into one symbol table, so the second was reported as `Identifier Span is already in use in this
  library.` -- and then a bare reference to the expression's name resolved to the function, whose
  `ToRef` throws, so translation died with `NotSupportedException: Refs cannot be created until the
  overload is resolved.` rather than reporting anything. Functions now live in their own table per
  scope; a symbol lookup returns the expression where both exist, a function lookup returns the
  function, and a local function still shadows a parent scope's (a local `Add` over the System
  library's) exactly as before. Two expressions, or two functions with one signature, sharing a name
  are still rejected.

- A bare reference to a name that resolves only to a function -- `define "x": "Span"` where `Span`
  is a function -- is now a translation error, `Span is a function and must be invoked with an
  argument list; no expression named Span is defined.`, instead of the same `NotSupportedException`.
