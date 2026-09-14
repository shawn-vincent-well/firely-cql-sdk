## Potentially Breaking

- **CQL-to-ELM translator:** a `Date`, `DateTime` or `Time` literal whose components are individually
  well-formed but out of range is now a translation error instead of being accepted. `@T24:59:59.999`,
  `@T23:60:59.999`, `@T23:59:60.999` and `@2023-02-30` previously translated; they now report
  `Invalid time literal '...'` / `Invalid date literal '...'` with the ranges each component allows.
  A library containing such a literal used to translate and evaluate, so this moves CQL evaluation
  results for that library from a wrong answer to a refusal to translate. Out-of-range literals in a
  *valid* library do not exist by definition, so nothing that translates today changes.

  The two error messages this replaces (`Unparseable date literal '...'`, `Unparseable date/time
  literal '...'`, `Unparseable time literal '...'`) are gone, and the reported `errorType` for a
  rejected literal moves from `syntax` to `semantic`: the lexer has already checked the literal's
  shape, so what is left to reject is a component that names no real date or time. Hosts matching on
  those message strings need to update.

## Fixes

- **CQL-to-ELM translator:** an out-of-range time literal is no longer silently wrapped into a
  different time. `@T24:59:59.999` translated to `00:59:59.999`, `@T23:60:59.999` to `00:00:59.999`
  and `@T23:59:60.999` to `00:00:00.999`, with no `CqlToElmError` and no `errorSeverity` anywhere in
  the emitted ELM — a consumer received a plausible value with no signal that it was not the one
  written. The range validation this needed already existed in `Hl7.Cql.Iso8601`, behind the
  constructors' `strict` parameter, but `TryParse` never asked for it.

- **CQL-to-ELM translator:** an out-of-range date or date/time literal no longer escapes translation
  as an unhandled `ArgumentOutOfRangeException`. `@2023-13-01` and `@2023-01-01T24:00:00` reached
  `System.DateTimeOffset`'s constructor by way of `TryParse` and threw out of the visitor; they are
  now reported as translation errors like any other bad literal.

- **`Hl7.Cql.Iso8601`:** `DateIso8601.TryParse`, `DateTimeIso8601.TryParse` and `TimeIso8601.TryParse`
  no longer throw. A value the underlying constructor rejects now returns `false`, which is what these
  methods' contract already documented. No input that succeeds today changes its result. This also
  reaches `CqlDate.TryParse`, `CqlDateTime.TryParse` and `CqlTime.TryParse`, which previously
  propagated the exception rather than returning `false`.

- **`Hl7.Cql.Iso8601`:** `DateIso8601`'s strict validation rejected every legal February date.
  Its February day checks were chained as `else if` arms of the same chain that assigned `Precision`,
  so a valid February date fell through with `Precision` left `Unknown` and was rejected by the guard
  below it — `new DateIso8601(2024, 2, 29, strict: true)` threw. The checks are now guards and
  `Precision` is assigned unconditionally. Relatedly, February's upper bound in a leap year was
  written as `day > 30` in both `DateIso8601` and `DateTimeIso8601` and is now `day > 29`.

## Features

- **`Hl7.Cql.Iso8601`:** `DateIso8601`, `DateTimeIso8601` and `TimeIso8601` each gain a
  `TryParse(string, bool strict, out T?)` overload that validates every parsed component against the
  range ISO 8601 allows for it. The existing two-argument `TryParse` is unchanged and still does no
  range checking, so only callers that opt in see the stricter behaviour.
