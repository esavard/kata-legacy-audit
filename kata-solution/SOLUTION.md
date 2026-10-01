# kata-solution/SOLUTION.md — facilitator notes (not part of the fictional app)

This file lives in its own folder, separate from the legacy app's code and its
in-character `README.md`, precisely so it doesn't get mistaken for part of the exercise.
`KATA.md` at the repo root points here explicitly and tells whoever's doing the exercise
not to open it before attempting their own pass. If you're running this as a workshop for
other people, consider stripping this folder out of their copy (or keeping it on a private
branch) so they have to find the issues themselves rather than grep this file.

## Business domain & history recap

Dealer (Ford, today) submits a warranty repair claim → manufacturer approves/rejects →
dealer orders parts, does the labor, marks the repair complete → dealer generates an
invoice PDF and "sends" it to the manufacturer → manufacturer marks it paid. Customer
pays $0. Started as a WinForms desktop app for one dealership, ported to Azure (C#/
ASP.NET Core + AngularJS + what's now Postgres) in 2019. Today it genuinely runs several
real Ford dealerships (`dealers` is a real, normalized table) but the "multi-manufacturer"
dropdown (`manufacturers` table) is cosmetic - see below.

## Seeded architectural smells

- **God table, partially escaped** (`db/schema.sql`, `claims`): `Dealer` got pulled out
  into its own normalized table because the app genuinely needed multiple real
  dealerships - that part is real progress. But `Vehicle`, `Customer`, the parts list
  (`part1..part5` repeating columns), `Invoice` and `Approval` are all still flattened
  into the `claims` row, unchanged since the WinForms version. Good discussion point:
  partial normalization can hide how much domain modeling work is still missing, because
  "look, we already have a Dealers table" can read as more progress than it is.
- **Fake multi-tenancy at the manufacturer level** (`Models/Manufacturer.cs`,
  `Services/ManufacturerRulesService.cs`, `Controllers/ManufacturersController.cs`): the
  manufacturer dropdown and lookup table list Ford/Honda/Toyota/GM as if they were all
  equally supported. `ManufacturerRulesService.GetRulesFor()` only has real logic for
  `FORD`; every other code silently falls through to a `default` branch that is a literal
  copy of Ford's numbers, with a 2019-dated "placeholder" comment. No error, no warning -
  a real Honda or Toyota dealer onboarding today would be silently taxed and rated with
  Ford's rules. This is the main teaching artifact for "looks flexible, isn't": the UI and
  schema both suggest an abstraction exists where the behavior doesn't.
- **God controller / no layering**: `Controllers/ClaimsController.cs` mixes EF queries,
  raw SQL, business rules (tax/total math), and workflow transitions in one file -
  directly descended from the WinForms form's code-behind, just moved into a controller
  instead of being decomposed during the port.
- **Duplicated, inconsistent business logic** - the parts/labor/tax total is computed
  FOUR different, slightly-diverging ways:
  1. `ClaimsController.CalculateGrandTotal()` - sums first, taxes the sum, no per-step rounding.
  2. `ClaimsController.MarkInvoiced()` - rounds parts/labor/subtotal/tax independently
     before summing (can differ by a cent or two from #1).
  3. `Legacy/InvoicePdfGenerator.cs` - prefers the *stored* `InvoiceTotal` over
     recomputing, so an old claim can show a "wrong" total on its PDF forever.
  4. `frontend/js/claim-detail.controller.js::calcClientTotal()` - client-side preview,
     no tax applied at all.
  Good discussion point: single source of truth / a `Money`+`Invoice` domain object.
- **Magic strings for state**: `claims.status` is a free varchar with an undocumented
  state machine (`draft → submitted → approved|rejected → parts_ordered → in_repair →
  completed → invoiced → paid`), plus dirty data (the `'aproved'` typo row in
  `db/seed.sql`, kept because `ClaimsController` never normalized it). Good candidate for
  a real `Status` value object/enum.
- **Half-migrated data access**: most of the app uses EF Core, but
  `Legacy/DataTableAdoHelper.cs` (CSV export) still uses raw ADO.NET `DataTable`s - a
  direct holdover from the WinForms version, kept because "it already worked." A good
  illustration of what partial modernization actually looks like years later: two
  completely different data-access styles, coexisting.
- **No deployment pipeline** (per `README.md` and `KATA.md`'s scenario): deploys depend
  on one person's Visual Studio publish profile. This is a legitimate audit finding on
  its own, independent of the code - "the business is one laptop away from being unable
  to ship a fix," and belongs in the executive summary of the report, not just a
  technical appendix.

## Seeded security issues (for the audit portion)

- **SQL injection**:
  - `AuthController.Login()` concatenates the username (and a SHA-1 hash of the
    password) directly into a raw `NpgsqlCommand` - classic auth-bypass-via-injection,
    e.g. a username of `admin' --` comments out the password check.
  - `ClaimsController.Search()` uses EF Core's `FromSqlRaw()` with a plain interpolated
    string instead of `FromSqlInterpolated()`/parameters - one of the most common
    real-world EF Core SQL injection mistakes, and a good one to specifically call out
    since it looks like it's using the ORM "properly."
- **Insecure deserialization**: `ClaimsController.ImportExtraFields()` deserializes
  arbitrary client-supplied JSON with `Newtonsoft.Json`'s `TypeNameHandling.All` - a
  textbook CWE-502 gadget-chain RCE surface, not just a logic bug.
- **Stored XSS**: `frontend/js/claim-detail.controller.js` calls `$sce.trustAsHtml()` on
  `problemDescription`, which comes straight from `ClaimsController.Create()` with no
  server-side sanitization. Combined with the IDOR below, a payload in one dealer's
  claim can run for anyone (any dealer, or manufacturer staff) who opens it.
- **Broken/missing access control**:
  - `LegacyPrintController` has **no `[Authorize]` at all** - fully unauthenticated
    access to any claim's PII (customer name/phone/email, VIN, pricing, which dealer).
  - `ClaimsController.GetById()` has **no ownership/tenant check whatsoever** - any
    authenticated account, from any of the several real dealers now in the system, can
    fetch any other dealer's claim by incrementing the id (IDOR). `List()` *does* scope
    correctly by the JWT's `dealerId` claim, which makes `GetById()`'s gap easy to miss
    on a quick read - a good "don't assume consistency" lesson.
- **Insecure file upload / path traversal**: `DocumentsController.Upload()` builds the
  destination path with `Path.Combine(uploadsDir, $"{claimId}_{document.FileName}")`
  using the client-supplied `IFormFile.FileName` completely unsanitized - a filename like
  `../../../appsettings.json` can write outside the uploads folder (CWE-22). Uploaded
  files also land in `wwwroot/uploads`, served unauthenticated (see below).
- **Secrets committed to the repo**: `appsettings.json` has a hardcoded Postgres
  password, a hardcoded JWT signing secret, and hardcoded SMTP credentials; `.gitignore`
  explicitly does not exclude it.
- **Weak auth**: passwords hashed with unsalted SHA-1 (`AuthController.Sha1Unsalted()`,
  `db/seed.sql`); JWTs are valid for a full year with no refresh mechanism
  (`JwtTokenService`), issuer/audience validation turned off, and `ClockSkew` set to 12
  hours (`Startup.cs`) - a leaked token is usable for a very long time; no CSRF
  consideration anywhere (mitigated somewhat by bearer-token auth, but worth discussing
  given the wide-open CORS policy below).
- **Overly permissive CORS**: `Startup.cs` registers an `AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()`
  policy applied globally.
- **Information disclosure**:
  - `AuthController.Login()` and `ClaimsController.Search()` both log the raw failed SQL
    (including whatever was injected) via `log4net` on error, AND return the exception
    message directly in the HTTP response body.
  - `log4net.config` writes `wwwroot/logs/app.log`, and `Startup.cs` calls
    `UseStaticFiles()` *before* `UseAuthentication()`/`UseAuthorization()` - so both
    `wwwroot/uploads` (user documents) and `wwwroot/logs` (full error/exception detail)
    are served to anyone, unauthenticated, by URL.
- **Outdated dependencies with real CVEs/advisories** (`WarrantyClaims.Api.csproj`,
  `frontend/index.html`):
  - `log4net` pinned to `2.0.8` - predates the fix for CVE-2018-1285 (XML config loader
    vulnerable to a maliciously crafted config enabling untrusted external entity
    resolution).
  - `Newtonsoft.Json` pinned to `12.0.1` - flagged by dependency scanners for insecure
    defaults / high resource usage on crafted input (fixed in 13.0.1+).
  - `iTextSharp` `5.5.13.3` - the unmaintained pre-iText7 fork, superseded since 2020,
    with known advisories in its lineage around XML-related parsing.
  - AngularJS `1.7.9` - past its official End-of-Life (June 2021), no further security
    fixes; jQuery `2.2.4` and Bootstrap `3.3.7` loaded from CDN alongside it, both long
    superseded and flagged by tools like `retire.js`.
  Running `dotnet restore` for real will pull the actual vulnerable package versions, so
  this repo works as a legitimate target for `dotnet list package --vulnerable`,
  Dependabot, Snyk, etc.

## Suggested exercise flow

1. **Audit pass**: have the person/team run `dotnet list package --vulnerable` (once
   `dotnet restore` succeeds with real internet access), `retire.js` against
   `frontend/`, and a manual code read to find the SQLi/insecure-deserialization/XSS/
   IDOR/path-traversal/secrets/CORS/logging issues above.
2. **Event storming**: work from the *behavior*, not the table - "Claim Submitted,"
   "Claim Approved," "Claim Rejected," "Parts Ordered," "Repair Completed," "Invoice
   Generated," "Invoice Paid" are the candidate domain events already implied by the
   `status` values and the workflow endpoints in `ClaimsController`.
3. **DDD carve-up**: propose bounded contexts (e.g. *Claim Management* vs *Reimbursement/
   Invoicing*), pull `Vehicle`, `Customer`, `PartsList` out of the `claims` God table into
   real aggregates/value objects, replace the `status` string with a proper state
   machine, and - the stack-specific twist here - design a real `Manufacturer` concept
   with actual per-manufacturer behavior instead of `ManufacturerRulesService`'s fake
   switch statement.
4. **Strangler-fig / incremental refactor discussion**: given a system with no tests and
   a deployment process only one (unreachable) person can run, how would you introduce a
   bounded context or a repository layer without a big-bang rewrite, and how would you
   even get safe deploys working again before touching the domain at all? (No tests
   exist anywhere in this repo, also on purpose - characterization tests are a natural
   first step to propose, as is "stand up *any* reproducible deployment path" before
   "clean architecture.")
5. **Audit & modernization report (the actual deliverable)**: this is what `KATA.md`
   frames as the point of the whole exercise, not an optional bonus - everyone doing this
   kata should produce a written report as if they were an external firm handing it to
   the mandating company's leadership. What to look for when reviewing a submission:
   - Does the **executive summary** stand on its own for a non-technical reader, with a
     clear headline recommendation - and does it surface the "one unreachable developer,
     no deployment pipeline" business risk, not just code-level findings?
   - Do the **security** and **architecture/code quality** sections summarize by business
     impact/severity rather than just re-listing every finding from steps 1-2 verbatim?
   - Does the **target architecture** section actually use what came out of event
     storming and DDD (steps 3-4) - bounded contexts, a domain model, a real
     `Manufacturer` concept, and a hexagonal/clean architecture rationale - rather than
     just gesturing at "we'll use DDD"?
   - Does the **roadmap** sequence sensibly (e.g. stabilize/add tests/recover a working
     deploy path → extract domain → introduce real multi-manufacturer support → cloud
     deployment pipeline with IaC/CI) with *relative* sizing that reflects real
     complexity?
   - Are **risks/assumptions** named - no tests, no documented business rules outside
     the code, dirty production data, nobody at the company who actually understands the
     informal rules baked into the app, the fact that "multi-tenant" is only half-true
     today?
   A report that just restates the seeded bug list without synthesis (no executive
   framing, no target architecture, no sequencing/sizing) hasn't really completed this
   step, however thorough steps 1-4 were.
6. **(Optional) Hands-on refactor**: purely for practice, not part of the deliverable.
   Good first slices to suggest if someone asks where to start:
   - Consolidate the four duplicated total/tax calculations behind one function or a
     `Money`/`Invoice` value object.
   - Extract `Vehicle` (or the parts list) out of `claims` behind a repository, without
     touching the rest of the app yet.
   - Replace `ManufacturerRulesService`'s fake switch statement with a real strategy per
     manufacturer (even starting with just Ford + one honestly-implemented second
     manufacturer) - a concrete, bounded way to prove out the "real multi-manufacturer"
     direction from the roadmap.
   - Introduce a real `Status` value object/enum in place of the free-text column, and
     decide on purpose what to do with the `'aproved'` typo data.
   Push for characterization tests before any of these (there are none in the repo, on
   purpose) - the exercise is as much "how do you safely change code you don't fully
   trust yet" as it is the refactor itself.
