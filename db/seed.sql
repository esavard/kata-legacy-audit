-- seed.sql - demo data. Run after schema.sql.

INSERT INTO manufacturers (code, display_name, is_active) VALUES
('FORD',   'Ford',   true),
('HONDA',  'Honda',  true),   -- dropdown option only - no real rules behind it, see ManufacturerRulesService.cs
('TOYOTA', 'Toyota', true),   -- same
('GM',     'General Motors', true); -- same

-- several REAL Ford dealers - this part of "multi-tenant" genuinely works today
INSERT INTO dealers (name, address, phone, manufacturer_id) VALUES
('Riverside Ford',      '4800 Riverside Dr, Windsor, ON',   '519-555-0101', (SELECT id FROM manufacturers WHERE code = 'FORD')),
('Capital Ford',        '900 Bank St, Ottawa, ON',          '613-555-0199', (SELECT id FROM manufacturers WHERE code = 'FORD')),
('Lakeshore Ford',      '220 Lakeshore Blvd, Toronto, ON',  '416-555-0150', (SELECT id FROM manufacturers WHERE code = 'FORD'));

-- password_hash values below are SHA-1 hex of the plaintext in the comment, unsalted -
-- see AuthController.Sha1Unsalted(). same weak hashing choice, same lack of per-user salt.
INSERT INTO users (username, password_hash, role, full_name, dealer_id) VALUES
('jmartin',   '92bf07a4c0cda37afbfebfc3bf12dd9f09911d7f', 'dealer', 'Jamie Martin',      (SELECT id FROM dealers WHERE name = 'Riverside Ford')), -- password: dealer123
('sleblanc',  '92bf07a4c0cda37afbfebfc3bf12dd9f09911d7f', 'dealer', 'Sophie Leblanc',    (SELECT id FROM dealers WHERE name = 'Capital Ford')),   -- password: dealer123
('rpatel',    '92bf07a4c0cda37afbfebfc3bf12dd9f09911d7f', 'dealer', 'Raj Patel',         (SELECT id FROM dealers WHERE name = 'Lakeshore Ford')), -- password: dealer123
('fordwarr',  'db3a8cfb7bb81c3fe7d25ef9ab905cc9a4f56a6a', 'manufacturer', 'Ford Warranty Claims Dept.', NULL),  -- password: mfg123
('admin',     'f865b53623b121fd34ee5426c792e5c33af8c227', 'admin', 'System Administrator', NULL);              -- password: admin123

-- a spread of claims across the (informal, undocumented) state machine, across THREE
-- different real dealers now instead of one - which is exactly what makes the missing
-- ownership check on GET /api/claims/{id} actually dangerous (any dealer account can
-- read any other dealer's customer data by id).

INSERT INTO claims
(claim_number, dealer_id, manufacturer_code, customer_name, customer_phone, customer_email,
 vin, vehicle_model, vehicle_year, vehicle_mileage_km, vehicle_color,
 problem_description,
 part1_name, part1_number, part1_qty, part1_unit_price,
 part2_name, part2_number, part2_qty, part2_unit_price,
 labor_hours, labor_rate,
 status, approved_by_name, invoice_total, tax_rate_used,
 submitted_at, approved_at, internal_notes, created_by_user_id, is_deleted)
VALUES
('RIV-240103-551', (SELECT id FROM dealers WHERE name = 'Riverside Ford'), 'FORD',
 'Robert Chen', '519-555-9911', 'r.chen@example.com',
 '1FA6P8TH1J5123456', 'Escape', '2022', '34500', 'Gray',
 'AC compressor failure, no cold air since last week. Replace cabin filter while in there.',
 'AC Compressor', 'FT4Z-19V703-A', 1, 612.40,
 'Cabin Air Filter', 'CV6Z-19N619-A', 1, 22.10,
 2.50, 110.00,
 'submitted', NULL, NULL, NULL,
 '2024-01-03', NULL, NULL, 1, false),

('CAP-231118-118', (SELECT id FROM dealers WHERE name = 'Capital Ford'), 'FORD',
 'Marie Tremblay', '613-555-4477', NULL,
 '3FA6P0HD1ER123789', 'Fusion', '2021', '58210', 'White',
 'Transmission shifting harshly between 2nd and 3rd gear. TSB applies per tech.',
 'Transmission Fluid Kit', 'XT-10-QLVC', 1, 89.95,
 NULL, NULL, NULL, NULL,
 3.00, 110.00,
 'aproved', 'Steve (phone, no email on file)', 654.32, '13% HST',  -- typo status from a 2021 bulk import, kept for "backward compatibility"
 '11/18/2023', '11/22/2023', 'approved_verbally:yes;callback:2023-12-01', 2, false),

('RIV-231005-902', (SELECT id FROM dealers WHERE name = 'Riverside Ford'), 'FORD',
 'Alain Fortin', '519-555-3300', 'alain.fortin@example.com',
 '1FMCU9H61MUA12345', 'Edge', '2023', '12040', 'Blue',
 'Power liftgate motor stopped working. Warranty parts + labor per bulletin.',
 'Liftgate Motor', 'FT4Z-78133-A', 1, 388.00,
 NULL, NULL, NULL, NULL,
 1.80, 110.00,
 'paid', 'Ford Warranty Claims Dept.', 596.11, '13% HST',
 '2023-10-05', '2023-10-09', NULL, 1, false),

('LAK-231220-330', (SELECT id FROM dealers WHERE name = 'Lakeshore Ford'), 'FORD',
 'Nathalie Côté', '416-555-7788', 'n.cote@example.com',
 '1FTEW1EP1NKE12345', 'F-150', '2022', '21980', 'Red',
 'Check engine light, code P0420 catalyst efficiency below threshold.',
 'Catalytic Converter', 'JL3Z-5E212-A', 1, 1180.00,
 'O2 Sensor', 'AT4Z-9F472-A', 2, 145.50,
 4.20, 110.00,
 'rejected', 'Ford Warranty Claims Dept.', NULL, NULL,
 '2023-12-20', '2023-12-27', 'rejected: mileage exceeds emissions warranty limit, see attached TSB', 3, false),

('RIV-240210-004', (SELECT id FROM dealers WHERE name = 'Riverside Ford'), 'FORD',
 'Test Customer DO NOT USE', '000-000-0000', NULL,
 'TESTVIN000000001', 'Escape', '2020', '99999', 'Black',
 'test claim from a training session, forgot to delete',
 NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
 0.00, 110.00,
 'draft', NULL, NULL, NULL,
 NULL, NULL, 'training/test row', 1, true),

('CAP-240301-777', (SELECT id FROM dealers WHERE name = 'Capital Ford'), 'FORD',
 'Denis Lavoie', '613-555-6622', 'denis.lavoie@example.com',
 '1FA6P8CF1N5123456', 'Mustang', '2024', '3200', 'Silver',
 'Infotainment screen freezes randomly, requires battery disconnect to reset.',
 'Infotainment Module', 'KS7T-14F645-A', 1, 940.00,
 NULL, NULL, NULL, NULL,
 1.00, 110.00,
 'parts_ordered', NULL, NULL, NULL,
 '2024-03-01', '2024-03-03', NULL, 2, false);
