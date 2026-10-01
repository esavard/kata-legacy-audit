# Warranty Claims

Internal tool for warranty claim submission between dealers and the manufacturer.
Originally a desktop app, ported to Azure in 2019.

## Setup

1. Create a Postgres database called `warranty_claims`, then run `db/schema.sql`
   followed by `db/seed.sql` against it.
2. Update the connection string in `backend/WarrantyClaims.Api/appsettings.json` if your
   local Postgres isn't on the default port/credentials.
3. `cd backend/WarrantyClaims.Api && dotnet restore && dotnet run` - API runs on
   `http://localhost:5000` by default.
4. Open `frontend/index.html` directly in a browser, or serve the `frontend/` folder
   with any static file server. If you change the API port, update `API_BASE` at the
   top of `frontend/js/app.js`.
5. Login: `jmartin` / `dealer123` (Riverside Ford), `sleblanc` / `dealer123` (Capital
   Ford), or `fordwarr` / `mfg123` (manufacturer side).

## Deploying

This runs on an Azure App Service (API) + Azure Static Web App or Blob Storage static
site (frontend) + Azure Database for PostgreSQL, set up manually through the Azure
portal back in 2019. There is no deployment pipeline - whoever is deploying needs the
publish profile (not in this repo, ask around) and Visual Studio's right-click Publish.
If you don't have the publish profile, you probably can't deploy this. Sorry.

## Known issues

- The "Honda"/"Toyota"/"GM" options in the manufacturer dropdown don't really do
  anything differently from Ford yet - that integration was never finished. Every real
  dealer so far is a Ford dealer so this hasn't mattered.
- Invoice totals on the PDF sometimes don't match what the claim screen showed a moment
  earlier. Don't worry about it, finance reconciles it manually.
- The search box on the claims list is slow with a lot of results.
- Tokens are valid for a year, so logging out doesn't do a whole lot server-side. Clear
  your browser storage if that bothers you.

## TODO

- Move secrets out of appsettings.json (noted since 2019)
- Actually implement manufacturer-specific rules before onboarding a non-Ford dealer
- Set up CI/CD so deploys don't depend on one person's laptop having the right
  publish profile

---
*Last meaningfully touched 2021. The original developer has a full-time job elsewhere
now and can't commit much time to this - if something's broken, your best bet is to
leave a message and wait.*
