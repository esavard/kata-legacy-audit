-- schema.sql - run against an empty "warranty_claims" Postgres database.
-- There are no EF Core migrations in this repo - see Data/WarrantyContext.cs for why.

-- ---------------------------------------------------------------------------
-- manufacturers: the lookup table behind the "multi-manufacturer" dropdown.
-- Ford is the only one with real rules anywhere else in the app - see
-- Services/ManufacturerRulesService.cs. Honda/Toyota/GM exist here and in the
-- UI, and nowhere else that matters.
-- ---------------------------------------------------------------------------
CREATE TABLE manufacturers (
    id SERIAL PRIMARY KEY,
    code VARCHAR(20) NOT NULL UNIQUE,
    display_name VARCHAR(100) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true
);

-- ---------------------------------------------------------------------------
-- dealers: real and normalized, because the app genuinely needed to support
-- several actual Ford dealerships. This is the one part of "multi-tenant"
-- that's actually true today.
-- ---------------------------------------------------------------------------
CREATE TABLE dealers (
    id SERIAL PRIMARY KEY,
    name VARCHAR(150) NOT NULL,
    address VARCHAR(255),
    phone VARCHAR(30),
    manufacturer_id INT NOT NULL REFERENCES manufacturers(id),
    created_at TIMESTAMP NOT NULL DEFAULT now()
);

-- ---------------------------------------------------------------------------
-- users: dealer staff AND manufacturer staff, same table, told apart by `role`
-- (free text, not an enum/check constraint).
-- ---------------------------------------------------------------------------
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(50) NOT NULL UNIQUE,
    password_hash VARCHAR(40) NOT NULL,   -- SHA-1 hex, unsalted - see AuthController.cs
    role VARCHAR(20) NOT NULL,            -- 'dealer' / 'manufacturer' / 'admin'
    full_name VARCHAR(100),
    dealer_id INT REFERENCES dealers(id), -- null for manufacturer/admin accounts
    created_at TIMESTAMP NOT NULL DEFAULT now()
);

-- ---------------------------------------------------------------------------
-- claims: THE table. Dealer got pulled out into its own normalized table (see
-- above) because the app actually needed that, but Vehicle, Customer, the
-- parts list, the invoice and the approval are all still flattened into this
-- one row, unchanged since the WinForms desktop version that talked directly
-- to a local database file. Every "just one more field" request since 2019
-- got solved with ALTER TABLE ADD COLUMN.
-- ---------------------------------------------------------------------------
CREATE TABLE claims (
    id SERIAL PRIMARY KEY,
    claim_number VARCHAR(30),

    dealer_id INT NOT NULL REFERENCES dealers(id),
    manufacturer_code VARCHAR(20),   -- copied from dealers.manufacturer_id at creation time, never reconciled again - see Models/Claim.cs

    customer_name VARCHAR(150),
    customer_phone VARCHAR(30),
    customer_email VARCHAR(150),

    vin VARCHAR(20),
    vehicle_model VARCHAR(60),
    vehicle_year VARCHAR(10),        -- text, not int - WinForms text field, never tightened
    vehicle_mileage_km VARCHAR(20),
    vehicle_color VARCHAR(40),

    problem_description TEXT,

    -- parts - 5 repeating slots, straight out of the WinForms form's five hardcoded
    -- "Part N" group boxes.
    part1_name VARCHAR(100), part1_number VARCHAR(40), part1_qty INT, part1_unit_price NUMERIC(10,2),
    part2_name VARCHAR(100), part2_number VARCHAR(40), part2_qty INT, part2_unit_price NUMERIC(10,2),
    part3_name VARCHAR(100), part3_number VARCHAR(40), part3_qty INT, part3_unit_price NUMERIC(10,2),
    part4_name VARCHAR(100), part4_number VARCHAR(40), part4_qty INT, part4_unit_price NUMERIC(10,2),
    part5_name VARCHAR(100), part5_number VARCHAR(40), part5_qty INT, part5_unit_price NUMERIC(10,2),

    labor_hours NUMERIC(6,2),
    labor_rate NUMERIC(8,2),

    status VARCHAR(30) NOT NULL DEFAULT 'draft',  -- see kata-solution for the full undocumented state list, incl. dirty data
    rejection_reason VARCHAR(255),
    approved_by_name VARCHAR(100),                 -- free text, not a user_id FK

    estimate_pdf_path VARCHAR(255),
    invoice_pdf_path VARCHAR(255),
    photo_path VARCHAR(255),

    invoice_total NUMERIC(10,2),
    tax_rate_used VARCHAR(20),

    -- dates as free text, same story as vehicle_year
    submitted_at VARCHAR(30),
    approved_at VARCHAR(30),
    completed_at VARCHAR(30),
    invoiced_at VARCHAR(30),
    paid_at VARCHAR(30),

    internal_notes TEXT,
    extra_fields_json TEXT,    -- see ClaimsController.ImportExtraFields() - insecure deserialization surface

    created_by_user_id INT,
    updated_by_user_id INT,
    is_deleted BOOLEAN NOT NULL DEFAULT false,

    created_at TIMESTAMP NOT NULL DEFAULT now(),
    updated_at TIMESTAMP NOT NULL DEFAULT now()
);
