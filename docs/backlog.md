# Backlog

Tech-debt items captured during planning that don't have a phase number yet.
Reference these from a phase doc when promoting them into scheduled work.

## Standardize PropertyType / TransactionType enum serialization

The API returns these enums as PascalCase strings (`"Apartment"`, `"Sale"`).
The frontend's `useProperties.buildParams` re-PascalCases lowercase inputs as
a defensive no-op:

```ts
if (filter.type) params.type = filter.type.charAt(0).toUpperCase() + filter.type.slice(1);
```

That shim is safe today because the new `usePropertyTypeOptions` API also
returns PascalCase values which the search nav passes through unchanged.

Pick one canonical convention and remove the conversion shim. Recommendation:
**lowercase URL params on the wire, server converts on input**. Lowercase is
friendlier in URLs (`?type=apartment` vs `?type=Apartment`) and matches the
existing internal frontend representation in `Property.propertyType` /
`Property.transactionType` (already lowercase). Server already exposes
`Estoria.Domain.Enums.PropertyType` via `[FromQuery]` — `Enum.TryParse` with
`ignoreCase: true` handles either form.

Touch points when this lands:
- `src/Estoria.Application/Services/PropertyService.cs` — confirm enum binding
  is case-insensitive
- `src/Estoria.Application/Services/PublicLookupService.cs` — change the
  `value` field returned by `GetTypeOptionsAsync` to lowercase
- `frontend/src/hooks/api/useProperties.ts` — drop the PascalCase shim in
  `buildParams`
- `frontend/src/hooks/api/usePublic.ts` — update `DEMO_TYPE_OPTIONS_BY_LANG`
  to use lowercase `value`s

Schedule alongside any other property-filter work (P2.x).
