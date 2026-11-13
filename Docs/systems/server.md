# Server

Part of the project notes. The index, the house rules, the gotchas, the Initialize chain and the
events are in `CLAUDE.md` at the repository root. This file describes how the area is, not a
changelog: edit it in place, in the same session as the change.

- **Server client** (B2.2 of the refactor, 2026-10-22), `_project/Scripts/Server/`
  (`KitchenDesign.Server`); the old static `ProFitWeb` is gone. `ServerClient` (constructed by
  `AppInitializer` before the catalog data and the session, which get it in their constructors) owns:
  - `ServerConnection`: the server address from the profile, `UseHttps` (starts true;
    `ProbeCoroutine` flips it once if the first request to the host fails, then tries again),
    `IsTestServer`, the domains the WebGL build may be embedded in, and the editor-only
    `SaveFallbackJsonResource`: in the editor every successful catalog request (manufacturers,
    categories, groups, products, brand catalogs, cabinets, tags, compatibilities) rewrites
    `Resources/<App>/FallbackJSON`, which is how the offline data stays current (and why a sign-in
    changes those files; see Gotchas).
  - One request class per server resource: `AccountRequests`, `ClientRequests`,
    `QuoteRequests`, `CatalogRequests`, `AnalyticsRequests`. An offline user (`UserType.Offline`)
    gets simulated sign-in and profile responses and reads products and cabinets from the fallback
    JSON; analytics are skipped for it.
  - Static, pure parsers next to them: `AccountJson`, `ClientJson`, `QuoteJson`, `CatalogJson`
    (the JSON key strings in them are the frozen format, R-DATA-3), `ServerJson`
    (`ValidateJsonResponse`: a JSON `error`, an `errors` map (its keys are shown only in debug
    builds), parser errors or a status other than 200 fail the request), `CatalogLookups`
    (lookups in a given list), `FallbackJsonFiles` (the fallback file names).
  - The product and cabinet parsers take the product groups, the products already loaded and the
    `IAppRules` as arguments. `CatalogRequests.GetProductList`/`GetCabinetList` take them as
    `Func<…>` providers, read when the response is parsed: that is when the old code read them,
    and the server refresh can replace the lists in between. Why the cabinet parser needs the loaded
    products: Stoll's cabinets come from the fallback JSON without server ids, and each gets an id
    below the smallest one in use. Manufacturers without an `order` (a server bug for newly added
    ones) sort as -1.

- **Server data types** (B2.1 of the refactor, 2026-10-22): `_project/Scripts/Server/Data/`
  (`KitchenDesign.Server`): `User`, `UserProfile`, `UserClient`, `UserClientQuote`,
  `ManufacturerInfo` (+ `ManufacturerDoorStyleInfo`), `ProductGroupInfo`, `ProductTagInfo`,
  `CabinetCompatibilityInfo`, `BrandCatalogInfo`, `CabinetSectionCategoryInfo`; `ResponseCode`
  and `MeasurementUnitExtensions` in `Server/`. Enums `ServerType`, `UserType`, `UserFilter`,
  `Currency`, `MeasurementUnit` in `_projectShared/Enums/`, values as before (the first three are
  serialized as ints in `AppProfile`/`AppOptions`). None of these is serialized by name: the
  parsers read the server's JSON by key, so **the JSON key strings in the parsers are the frozen
  format (R-DATA-3), not the C# names**. `User.userType` is set by the app, not sent by the
  server. `UserProfile`: `dealerSelectedManufacturerIds` is the JSON `manufacturers`,
  `adminSelectedManufacturerIds` is `whitelisted_manufacturers`; `whiteLabelReturnUrl` is where a
  white label user goes after saving a design.
