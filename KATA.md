# Kata: Legacy Code Audit → Event Storming → DDD → Modernization Audit Report

This file is the real README for the exercise. `README.md` at the repo root is part of
the fiction - it's written as if it belonged to the legacy application itself. Everything
under `kata-solution/` is a facilitator's answer key; don't read it before attempting the
exercise.

## The scenario

**Warranty Claims** started life as a Windows desktop app (C#, WinForms) built for a
single Ford dealership. In 2019, a contact of one of the dealership's owners ported it
to Azure: C# backend (ASP.NET Core Web API), an AngularJS frontend, and a database that
moved along with it. It handles warranty repairs: a dealer creates an estimate, the
manufacturer approves or rejects it, the dealer orders parts and does the repair, then
invoices the manufacturer for reimbursement so the customer pays nothing.

Since the 2019 port, the application has genuinely grown: it now runs **several real
Ford dealerships**, not just the original one, and someone added a manufacturer dropdown
with Honda/Toyota/GM already listed - the early, unfinished shape of a plan to support
other manufacturers too. Look closely at what's actually implemented behind that
dropdown before assuming it works.

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
  non-Ford dealer signed up tomorrow?
- What's the actual shape of the data - is the database schema telling you the truth
  about the domain, or hiding it?
- What would you need before you could safely change anything (tests? something else?)

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
