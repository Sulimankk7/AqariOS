# Properties mobile frontend

This feature consumes the existing v1 APIs. Backend, Web, authorization policies,
database and OpenAPI are unchanged. No new endpoints or pagination parameters.

## Screens and routes

Properties landing → Buildings → Building details → Floors → Floor details →
Units → Unit details. Building details also opens units directly. Landing offers
all company units and a building chooser for floors (there is no global floors API).
Screens use ordinary MaterialPageRoute navigation within the existing app.

## API consumption

| Operation | Existing endpoint |
|---|---|
| Buildings list/create | GET/POST `/api/v1/buildings` |
| Building details/edit/archive | GET/PUT/DELETE `/api/v1/buildings/{id}` |
| Floors list/create | GET/POST `/api/v1/buildings/{buildingId}/floors` |
| Floor details/edit/archive | GET/PUT/DELETE `/api/v1/floors/{id}` |
| Units list | GET `/api/v1/apartments`, optional `buildingId`, `floorId` |
| Unit create | POST `/api/v1/floors/{floorId}/apartments` |
| Unit details/edit/archive | GET/PUT/DELETE `/api/v1/apartments/{id}` |

Lists are arrays, not envelopes. Search is explicitly labelled as searching the
loaded list, with a 200 ms debounce and no HTTP search request. It searches real
building name/code/address, floor label/number, or unit number/building name/status
text. Building-type and occupancy filters are local only. No page/load-more UI.
Repository order is preserved. Global units load building names once, not per row.

The existing apartment API prioritizes floorId when both filters are supplied.
Mobile only supplies both from the navigated building/floor hierarchy; it does not
claim the backend enforces an intersection. Company identity is never supplied in
request bodies or query strings by this feature.

## Writes and validation

Forms use explicit write models and effective API limits (including request
annotations, not only Web schemas). Building floor count and floor number are
create-only. Apartment editing sends only `baseRentAmount`/`baseRentCurrency`.
Company-owned units send null external-owner fields. Paired coordinates are
validated together; zero coordinates are preserved. Arabic digits are accepted in
numeric fields. Supported currencies: JOD, USD, EUR, AED, SAR.

Read/create/update/delete actions mirror each existing permission OR
`properties.manage`. Backend authorization is authoritative. Archive requires
confirmation and stays on the record on failure; no optimistic deletion.
RFC 7807 field keys map to nearby safe validation messages. Technical detail and
stack traces are not rendered. No token/credential logging or new auth storage.

## Lifecycle and performance

Route-owned ChangeNotifiers stop notifying after disposal. Lists render lazily.
The repository deduplicates in-flight reads and keeps a 60-second memory cache.
Successful writes invalidate it; returning through the hierarchy refreshes changed
ancestors. Pull-to-refresh bypasses cache. Pending reads cannot repopulate stale
cache after invalidation. Session/account-scope changes clear this feature cache
and return navigation to the authentication/root gate. Nothing is persisted offline.

## Known contract limits

- Unpaginated arrays remain unbounded at the API level. Rendering is lazy, but
  transfer/parsing/memory still scale with the full returned list.
- No server search, cursor pagination, availability query or lease/tenant details
  is invented. Occupancy and asking rent are shown only from ApartmentDto.
- The Web FloorType mirror differs from the backend. Mobile uses the actual
  backend values: Basement=0, Ground=1, Regular=2, Roof=3; neither source is edited.
- Reverse-geocoding and automatic identifier suggestions are not part of these
  forms; address and identifiers can be entered manually using existing CRUD.
- Tests use test-only fixtures. Live authenticated CRUD and physical-device QA
  require the user's environment; automated tests do not claim those validations.

## Validation

`dart analyze lib test` and `flutter test` are the intended checks. The Properties
tests cover response/request mapping, local search, hierarchy navigation, permissions,
loading/empty/error states, retry, refresh concurrency, single retry, cache invalidation,
form validation/submission, archive cancellation/conflict, RTL and large text.

Visual captures under `test/goldens/` use test fixtures and bundled fonts, not a
live account. Recreate explicitly with `flutter test test/properties_visual_test.dart
--dart-define=CAPTURE_PROPERTIES=true --update-goldens` (as a single command).
Shared components retain their tokens: selects scroll, field errors wrap, back
controls have semantics, Material directional icons do not double-mirror, LTR
values retain their surrounding alignment, and dark outlined/text actions use
the existing readable text token. No brand palette changes.

### Results for this implementation

- `dart analyze lib test`: no issues.
- Full Flutter suite: 36 tests passed (23 new Properties tests).
- `flutter build web --no-pub`: succeeded, including the Wasm dry run. The tool
  emitted a missing CupertinoIcons font-family warning; Properties uses Material
  icons, which were verified in the captured screenshots. No dependency was added
  solely to suppress that warning.
- Arabic light list and dark unit-detail screenshots inspected; large-text and
  small-phone layout tests passed.
- Backend/backend-test/Web tracked diffs and untracked-file content hashes match
  the pre-task baseline. No .NET, database, migration or Docker commands ran.
- No live authenticated reads/writes or Android-device execution was performed.
