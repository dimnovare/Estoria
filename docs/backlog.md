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

## Drag-and-drop kanban for deals (P2.5 follow-up)

`AdminDeals.tsx` currently drives stage changes through a click-to-open modal.
The visual layout is already a kanban; the natural interaction is to drag a
deal card from one column to another. Kept out of the P2.5 polish pass to
avoid scope creep — the modal is fine for shipping today.

Touch points when this lands:
- `frontend/src/pages/admin/AdminDeals.tsx` — wire a drag library (e.g.
  `@dnd-kit/core`) to call `useChangeStage` on drop
- For Won/Lost drops, still surface the existing modal so the user can fill in
  `actualValue` / `lossReason` (validation rules already enforced server-side
  in `DealService.ChangeStageAsync`)
- Confirm on mobile: drag UX usually needs a long-press handler

## Activity composer attachments (P2.6)

The activity composer in `ContactDetail.tsx` and `DealDetail.tsx` lets users
log `Note`/`Call`/`Email`/`Meeting`/`Viewing`/`OfferMade` activities with
title + body + outcome + duration. There's no way to attach files (signed
offers, viewing photos, contracts). Backend has no file/blob storage wired
up yet — this lands together with document storage for deals.

Touch points when this lands:
- Backend: a `MediaAsset` table or reuse `Estoria.Domain.Entities.Property`'s
  media pattern; add an `AttachmentIds` collection to `Activity`
- `frontend/src/hooks/api/useCrm.ts` — extend `ActivityCreateDto` with
  `attachmentIds`
- Activity composer cards in both detail pages — add an upload button

## Deal participants management (Add/Remove)

The P2.4 frontend scaffold shipped Add/Remove buttons in
`DealDetail.tsx` → Participants tab, calling endpoints that do not yet exist
on the backend. `useCrm.ts` exports a `PARTICIPANTS_WRITE_ENABLED = false`
flag and the buttons are hidden behind it; the participant *list* still
renders read-only from the deal detail DTO.

When the backend lands, flip the flag to true. Backend additions needed:
- `POST /admin/deals/{id}/participants` accepting `{ contactId, role }`
- `DELETE /admin/deals/{id}/participants/{participantId}`
- `DealParticipantWriteDto` + service methods in `DealService.cs` with the
  usual `RequireOwnershipOrAdmin(deal.AssignedAgentId)` guard
- Audit log entries (`Deal.AddParticipant` / `Deal.RemoveParticipant`)
