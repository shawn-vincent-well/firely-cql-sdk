## Fixes

- **CQL-to-ELM translator:** a translation error whose message names the types of a call's arguments
  is now reported as an error, instead of being replaced by a `NullReferenceException`. An expression
  that fails validation keeps a null `resultTypeSpecifier` — the visitor that rejected it returns the
  partially built node with the error attached and never assigns a result type — and that untyped node
  still reaches operator resolution, which fails in turn. `MessageProvider` built the message for that
  second failure by dereferencing each argument's `resultTypeSpecifier`, so the attempt to describe the
  failure threw, and the caller lost both errors along with any others reported beside them. An
  unresolved type now renders as `(missing)`, matching how `FunctionDef.ToString` already renders an
  absent result type: `Floor(2147483648)`, whose literal is outside the range of the CQL `Integer` type,
  now reports `Unparseable numeric literal '2147483648'.` together with `Could not resolve call to
  operator Floor with signature ((missing)).` The same guard covers the ambiguous-call message and the
  `TypeSpecifier` overload, which has callers that pass an expression's result type straight through.
  Only messages that previously threw are affected; a message built entirely from resolved types is
  byte-for-byte unchanged. Why the result type is missing in the first place is unchanged and is a
  separate matter — this is the error path, not type resolution.
