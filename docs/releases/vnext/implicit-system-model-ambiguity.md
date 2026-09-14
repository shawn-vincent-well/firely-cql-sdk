## Fixes

- **CQL-to-ELM translator:** an unqualified type name that exists in both the System model and the
  one model a library declares -- `Quantity`, `Code`, `Concept` in a library that writes only
  `using FHIR version '4.0.1'` -- now resolves to the declared model's type, as the specification
  requires: "when the System model declaration is implicit, it is not considered as part of
  determining ambiguity ... The identifier Quantity in this function declaration resolves to
  FHIR.Quantity unambiguously because only the FHIR model is explicitly declared" (Developer's
  Guide, Multiple Data Models). It was reported as `The type Quantity is ambiguous between
  System.Quantity, FHIR.Quantity.` under the default `AmbiguousTypeBehavior.Error`, which made
  `(O.value as Quantity).value` -- the ordinary way to read an Observation's numeric value -- a
  translation error in every library that did not spell out `FHIR.Quantity`, and left every
  expression built on it typed `Any`.

  A library that writes `using System` itself puts the two models on equal footing, and the name
  is then ambiguous exactly as before, with `AmbiguousTypeBehavior` deciding. `PreferSystem` keeps
  its documented meaning in both cases. An explicit `using System` is now accepted and replaces
  the declaration the translator makes on the library's behalf; it was rejected as
  `Duplicate identifier System in scope.`

  **CQL translation results change:** libraries that previously failed with the ambiguity error
  now translate, and a bare `Quantity` in them is `FHIR.Quantity`.
