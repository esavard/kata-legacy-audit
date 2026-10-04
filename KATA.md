# Kata: Legacy Code Audit → Event Storming → DDD → Modernization Audit Report

This file is the real README for the exercise. `README.md` at the repo root is part of
the fiction - it's written as if it belonged to the legacy application itself. Everything
under `kata-solution/` is a facilitator's answer key; don't read it before attempting the
exercise.

## The scenario

**Warranty Claims** started life as a Windows desktop app (C#, WinForms) built for a
single Bramwell dealership. In 2019, a contact of one of the dealership's owners ported it
to Azure: C# backend (ASP.NET Core Web API), an AngularJS frontend, and a database that
moved along with it. It handles warranty repairs: a dealer creates an estimate, the
manufacturer approves or rejects it, the dealer orders parts and does the repair, then
invoices the manufacturer for reimbursement so the customer pays nothing.

Since the 2019 port, the application has genuinely grown: it now runs **several real
Bramwell dealerships**, not just the original one, and someone added a manufacturer dropdown
with Kestrel/Solenne/Northgate already listed - the early, unfinished shape of a plan to support
other manufacturers too. Look closely at what's actually implemented behind that
dropdown before assuming it works.

One thing worth stating plainly, because it should shape how you read the rest of the
code: the person who originally built this deeply understood warranty-claims processing
- the tax math, the labor rates, the approval workflow, the parts/pricing conventions -
and the system has been computing and running that logic correctly in production since
2019-2020. Nobody at the dealerships is complaining that their invoices are wrong. The
domain knowledge encoded in this codebase is sound. What's a house of cards is the
*architecture* around that knowledge - how tangled, duplicated, and fragile to change it
all is - not the business rules themselves. Keep that distinction in mind in every step
below: the audit should find structural risk, not accuse the business logic of being
wrong just because the code holding it is ugly.

**The problem the company is living with right now:** the developer who did the 2019
port has a full-time job elsewhere and can't give this any real attention anymore. Worse,
nobody at the company knows where the source code is. The app runs on Azure and the
company can administer it there - restart it, look at logs, change settings - but
nobody can rebuild or redeploy it without that developer, who is effectively
unreachable. The source code in this repo represents a copy that was eventually tracked
down and recovered; it's the first real look anyone outside that one developer has had
at this codebase in years.

**The mandate:** the company wants to turn this into a real product - cloud-based,
multi-tenant, serving multiple dealerships and multiple manufacturers properly, instead
of depending on one person who's already effectively gone. It doesn't have the in-house
capacity to size that work itself, so it has retained an external firm to audit the
codebase and come back with an informed estimate of the effort and cost involved.
**You are that firm.** Nobody is asking you to write the new application, or to fix the
deployment situation today - they're asking you to tell them, with enough rigor to be
trusted, what's really in this codebase, and roughly what it will take to get from here
to a clean, well-architected (domain-driven, hexagonal), properly deployable (Bicep/CI-CD
is the eventual target - not in scope for this audit) multi-tenant SaaS.

## Objectives

Work through the following in order - each stage builds on what you found in the last one.

### 1. Security & code audit

Read the code and run whatever tooling you'd normally reach for (a SAST scan, a
dependency/CVE scanner - `dotnet list package --vulnerable` is a good start on the
backend side - manual review, your choice). Look for:

- Injection points (SQL, insecure deserialization, or otherwise)
- Broken or missing access control
- Secrets and credentials handling
- File upload / file handling risks
- Outdated dependencies and their known CVEs
- Anything else that would fail a real security review

Produce a short findings list (severity + location) as if you were reporting to the
company's leadership, who is not technical.

### 2. Architecture & code quality audit

Set security aside and look at how the code is organized:

- Where do business rules live, and how many places is the *same* rule implemented?
- The app already supports several real dealerships. Does it actually support more
  than one manufacturer, or does it just look like it does? What would break if a real
  non-Bramwell dealer signed up tomorrow?
- What's the actual shape of the data - is the database schema telling you the truth
  about the domain, or hiding it?
- What would you need before you could safely change anything (tests? something else?)
- Where the same rule shows up more than once, does it actually still agree everywhere
  today, or has it already started to drift? Either answer is useful: "it still agrees,
  but only because nobody's touched all four copies at once" is as real a risk as "it's
  already inconsistent."

### 3. Event storming

Ignore the code and the database for this step. From the *business process* described
above (and from what you observed the app actually doing), run an event-storming pass:

- What are the domain events? (Something happened, past tense - "Claim Submitted," not
  "Submit Claim.")
- Who or what triggers each event, and what data does it carry?
- Where are the natural boundaries - points where one team's concerns clearly end and
  another's begin?

### 4. Domain-Driven Design

Using what came out of the event storming:

- Propose bounded contexts. Is "getting the repair done" the same context as "getting
  reimbursed by the manufacturer"? Given the mandate to go properly multi-manufacturer,
  where does "manufacturer" need to become a first-class, rule-bearing concept instead
  of a dropdown value with no real behavior behind most of its options?
- Identify aggregates, their invariants, and their roots. What should *not* be editable
  independently of what?
- Sketch what a corrected data model would look like, and contrast it with the actual
  schema you found in step 2.

Remember the distinction from the scenario above: the goal here is to give the correct
business rules a home that isn't a house of cards, not to second-guess or rewrite the
domain knowledge itself. A good target architecture *extracts and preserves* what the
original developer got right, behind a structure that can survive someone other than
them touching it.

### 5. The deliverable: audit & modernization report

This is the actual point of the exercise - everything above was groundwork for it.
Produce a single written document, addressed to the mandating company's leadership
(technically literate, but not people who will read your code), that could realistically
be handed over at the end of a paid audit engagement. It should cover:

- **Executive summary** - current state in a few sentences, and the headline
  recommendation. The "one developer and no deployment pipeline" risk belongs here, not
  buried in an appendix.
- **Security findings** - summarized from step 1, with severity and business impact, not
  a raw vulnerability dump.
- **Architecture & code quality findings** - summarized from step 2: what's structurally
  wrong, and why it matters for the multi-tenant, multi-manufacturer goal specifically.
- **Target architecture** - informed by steps 3-4: proposed bounded contexts, a domain
  model sketch, and how a hexagonal/clean architecture would let the domain stay
  manufacturer- and tenant-agnostic while adapters handle the rest.
- **Roadmap & rough sizing** - a phased plan (e.g. stabilize → extract domain →
  introduce real multi-manufacturer support → cloud deployment pipeline), with a rough
  effort estimate per phase (t-shirt sizes or ballpark person-weeks are fine - real
  estimation needs context you don't have, but you should be able to say which phases
  are bigger than others and why).
- **Risks & assumptions** - what could blow the estimate up (no tests, no deployment
  pipeline, dirty production data, nobody at the company who actually knows all the
  business rules buried in the code, the original developer being unreachable, etc.).

Given that this is (as far as the company knows) a live system that real dealerships
depend on, with no tests and a deployment process nobody but one unreachable person can
run, the roadmap should assume incremental delivery, not a big-bang rewrite - say what
you'd tackle first and what you'd deliberately leave alone for now, and why.

### 6. (Optional) Actually do some of the refactoring

The report is the deliverable; this step is just for practice. Pick one piece of your
roadmap - usually the smallest, cleanest first slice (e.g. pulling the tax/total math
into one place, or extracting `Vehicle`/`Customer`/the parts list out of the `claims`
God table behind a repository) - and actually implement it against this codebase. Write
characterization tests first if you can, so you have something to prove you didn't
change behavior. The point is to practice untangling spaghetti code hands-on, not to
finish the whole migration - stop once you've proven the approach on one slice.

## Suggested tools

Nothing here is required - the exercise is doable with just a code editor and a brain -
but these can speed up or sharpen each step for this particular stack.

**Security & dependency audit (step 1)**
- `dotnet list package --vulnerable --include-transitive` from `backend/WarrantyClaims.Api`
  once `dotnet restore` has pulled real packages - flags the log4net/Newtonsoft/
  iTextSharp advisories directly.
- GitHub Dependabot (already enabled on this repo) and/or Snyk for a second opinion and
  for the frontend side.
- [retire.js](https://retirejs.github.io/retire.js/) against `frontend/` for the
  outdated AngularJS/jQuery/Bootstrap CDN references.
- Roslyn analyzers - `Microsoft.CodeAnalysis.NetAnalyzers` (ships with the SDK) and
  [Security Code Scan](https://security-code-scan.github.io/) for .NET-specific
  injection/deserialization/crypto findings (it's built to catch exactly the
  `FromSqlRaw`-with-interpolation and `TypeNameHandling.All` patterns in
  `ClaimsController.cs`).
- [OWASP ZAP](https://www.zaproxy.org/) if you want to actually probe the running API
  (SQLi, auth, CORS) rather than only read the code for it.

**Architecture & code quality audit (step 2)**
- [SonarQube Community Edition](https://www.sonarsource.com/products/sonarqube/) or
  SonarCloud - duplication detection and cyclomatic complexity scoring will surface the
  four-copies-of-the-same-calculation problem and the God-controller size objectively,
  not just anecdotally.
- [NDepend](https://www.ndepend.com/) (commercial, free trial) if you want a real
  dependency-graph/"architecture smell" view of the C# side - it's specifically built
  for diagnosing the "house of cards" kind of finding this audit is after.
- `dotnet-depends` or a quick C4-model sketch (Structurizr, or just Mermaid/PlantUML) to
  draw what you find - useful both for your own thinking and as a figure in the final
  report.

**Event storming & DDD (steps 3-4)**
- A physical or virtual sticky-note board (Miro, FigJam, or literal paper) - event
  storming is a modeling technique, not a tool-dependent one.
- [Context Mapper](https://contextmapper.org/) if you want to formalize the bounded
  contexts and their relationships (a DSL + diagrams) rather than leave them as sketches.

**Evaluating the AngularJS → Angular migration effort specifically**

This app's frontend is a good real-world case for this question, independent of the
rest of the audit: AngularJS has had no security updates since it went end-of-life in
2021, so "stay on AngularJS" is not a real long-term option.

- [ngMigration Assistant](https://github.com/ellamaolson/ngMigration-Assistant) - a
  CLI built for exactly this: it scans an AngularJS codebase, measures size (SLOC),
  flags migration-blocking antipatterns, and recommends a path (full rewrite vs.
  incremental hybrid upgrade vs. "you're basically already there"). A good first pass
  to run against `frontend/` before estimating anything by hand.
- [`@angular/upgrade`](https://angular.dev/guide/upgrade) (ngUpgrade) - the official
  hybrid-app mechanism for running AngularJS and Angular side by side and migrating
  module-by-module instead of as a big-bang rewrite. There's no mature automated
  codemod from AngularJS syntax to Angular (the frameworks are too different), so
  ngUpgrade's incremental path is the realistic option to size, not a one-shot
  conversion tool.
- Plain size/complexity metrics as a sizing input - `cloc` or `scc` over `frontend/js`,
  plus a manual tally of controllers/directives/services and which ones touch the DOM
  directly (`jQuery`, `$compile`, custom directives) versus simple data-binding - the
  former migrate much slower than the latter, and that ratio is a better effort signal
  than raw line count.
- ESLint with Angular-aware rules (or just grep) to flag `$scope`, two-way `ng-model`
  binding, and jQuery usage specifically - these are the patterns that make a component
  "hard" to port and are worth calling out by name in the sizing section of your report.

## Ground rules

- Don't open `kata-solution/` until you've done your own pass - it lists every issue
  that was deliberately seeded, and will short-circuit the exercise if you read it first.
- The bugs, dirty data, and inconsistencies you'll find are intentional. So is the fact
  that some of them contradict each other (e.g. the total is computed differently in
  four different places). That's the point.
- Feel free to actually run the app locally - see `README.md` for setup (Postgres +
  `dotnet run` for the API, any static server for the AngularJS frontend) - seeing the
  bad behavior firsthand is part of the exercise, not just reading the code.
- NuGet package restore and the frontend's CDN-hosted libraries need real internet
  access to actually run the app. If you're doing this exercise somewhere offline or
  heavily sandboxed, reading the code is still the main event.
