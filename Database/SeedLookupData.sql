-- ============================================================================
-- Seed data for NMB-HLabSys lookup/master tables (GRP-04-05-Haematology)
--
-- Your SSMS "schema only" migration brought across every table, constraint
-- and stored procedure, but zero rows. Roles was already seeded separately.
-- This script seeds the remaining lookup tables the Doctor module (and the
-- rest of the app) needs actual rows in before you can register patients
-- with conditions/allergies/medications, or create test requests:
--   - TestCategories, SampleTypes, UnitsOfMeasurement, TestTypes
--   - AllergyCategories, Allergies
--   - MedicalConditionCategories, MedicalConditions
--   - MedicationCategories, Medications
--
-- Safe to re-run: every INSERT is guarded with a NOT EXISTS check, so running
-- this twice won't create duplicates or violate the UNIQUE constraints on
-- each table's name column.
--
-- Run this in SSMS with the query window pointed at GRP-04-05-Haematology.
-- ============================================================================
USE [GRP-04-05-Haematology]
GO

-- ---- SampleTypes -----------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.SampleTypes WHERE SampleTypeName = 'Whole Blood (EDTA)')
    INSERT INTO dbo.SampleTypes (SampleTypeName, Description) VALUES ('Whole Blood (EDTA)', 'EDTA anticoagulated whole blood, used for FBC and related tests');
IF NOT EXISTS (SELECT 1 FROM dbo.SampleTypes WHERE SampleTypeName = 'Serum')
    INSERT INTO dbo.SampleTypes (SampleTypeName, Description) VALUES ('Serum', 'Clotted blood, centrifuged to separate serum');
IF NOT EXISTS (SELECT 1 FROM dbo.SampleTypes WHERE SampleTypeName = 'Plasma (Citrate)')
    INSERT INTO dbo.SampleTypes (SampleTypeName, Description) VALUES ('Plasma (Citrate)', 'Citrated plasma, used for coagulation studies');
GO

-- ---- UnitsOfMeasurement -----------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.UnitsOfMeasurement WHERE UnitSymbol = 'g/dL')
    INSERT INTO dbo.UnitsOfMeasurement (UnitSymbol, UnitName) VALUES ('g/dL', 'Grams per decilitre');
IF NOT EXISTS (SELECT 1 FROM dbo.UnitsOfMeasurement WHERE UnitSymbol = 'x10^9/L')
    INSERT INTO dbo.UnitsOfMeasurement (UnitSymbol, UnitName) VALUES ('x10^9/L', 'Times ten to the ninth per litre');
IF NOT EXISTS (SELECT 1 FROM dbo.UnitsOfMeasurement WHERE UnitSymbol = 'x10^12/L')
    INSERT INTO dbo.UnitsOfMeasurement (UnitSymbol, UnitName) VALUES ('x10^12/L', 'Times ten to the twelfth per litre');
IF NOT EXISTS (SELECT 1 FROM dbo.UnitsOfMeasurement WHERE UnitSymbol = '%')
    INSERT INTO dbo.UnitsOfMeasurement (UnitSymbol, UnitName) VALUES ('%', 'Percent');
IF NOT EXISTS (SELECT 1 FROM dbo.UnitsOfMeasurement WHERE UnitSymbol = 'fL')
    INSERT INTO dbo.UnitsOfMeasurement (UnitSymbol, UnitName) VALUES ('fL', 'Femtolitre');
IF NOT EXISTS (SELECT 1 FROM dbo.UnitsOfMeasurement WHERE UnitSymbol = 'pg')
    INSERT INTO dbo.UnitsOfMeasurement (UnitSymbol, UnitName) VALUES ('pg', 'Picogram');
IF NOT EXISTS (SELECT 1 FROM dbo.UnitsOfMeasurement WHERE UnitSymbol = 'sec')
    INSERT INTO dbo.UnitsOfMeasurement (UnitSymbol, UnitName) VALUES ('sec', 'Seconds');
GO

-- ---- TestCategories ----------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.TestCategories WHERE CategoryName = 'Full Blood Count')
    INSERT INTO dbo.TestCategories (CategoryName, Description) VALUES ('Full Blood Count', 'Haematology screening panel');
IF NOT EXISTS (SELECT 1 FROM dbo.TestCategories WHERE CategoryName = 'Coagulation')
    INSERT INTO dbo.TestCategories (CategoryName, Description) VALUES ('Coagulation', 'Clotting-time studies');
IF NOT EXISTS (SELECT 1 FROM dbo.TestCategories WHERE CategoryName = 'Iron Studies')
    INSERT INTO dbo.TestCategories (CategoryName, Description) VALUES ('Iron Studies', 'Ferritin and iron-panel tests');
GO

-- ---- TestTypes (references TestCategories / SampleTypes / UnitsOfMeasurement) ----
INSERT INTO dbo.TestTypes (TestName, CategoryID, SampleTypeID, UnitID, NormalRangeMin, NormalRangeMax, TurnaroundTimeMinutes, IsActive)
SELECT v.TestName, c.CategoryID, s.SampleTypeID, u.UnitID, v.NormalRangeMin, v.NormalRangeMax, v.TurnaroundTimeMinutes, 1
FROM (VALUES
    ('Haemoglobin',        'Full Blood Count', 'Whole Blood (EDTA)', 'g/dL',    12.0, 16.0, 60),
    ('White Cell Count',   'Full Blood Count', 'Whole Blood (EDTA)', 'x10^9/L',  4.0, 11.0, 60),
    ('Platelet Count',     'Full Blood Count', 'Whole Blood (EDTA)', 'x10^9/L', 150.0, 450.0, 60),
    ('Red Cell Count',     'Full Blood Count', 'Whole Blood (EDTA)', 'x10^12/L', 4.2, 5.9, 60),
    ('Haematocrit',        'Full Blood Count', 'Whole Blood (EDTA)', '%',       36.0, 50.0, 60),
    ('MCV',                'Full Blood Count', 'Whole Blood (EDTA)', 'fL',      80.0, 100.0, 60),
    ('MCH',                'Full Blood Count', 'Whole Blood (EDTA)', 'pg',      27.0, 33.0, 60),
    ('Prothrombin Time',   'Coagulation',       'Plasma (Citrate)',   'sec',     11.0, 13.5, 120),
    ('APTT',               'Coagulation',       'Plasma (Citrate)',   'sec',     25.0, 35.0, 120),
    ('Serum Ferritin',     'Iron Studies',      'Serum',              'pg',      20.0, 250.0, 240)
) AS v(TestName, CategoryName, SampleTypeName, UnitSymbol, NormalRangeMin, NormalRangeMax, TurnaroundTimeMinutes)
INNER JOIN dbo.TestCategories c ON c.CategoryName = v.CategoryName
INNER JOIN dbo.SampleTypes s ON s.SampleTypeName = v.SampleTypeName
INNER JOIN dbo.UnitsOfMeasurement u ON u.UnitSymbol = v.UnitSymbol
WHERE NOT EXISTS (SELECT 1 FROM dbo.TestTypes tt WHERE tt.TestName = v.TestName);
GO

-- ---- AllergyCategories / Allergies -------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.AllergyCategories WHERE CategoryName = 'Medication')
    INSERT INTO dbo.AllergyCategories (CategoryName, Description, IsActive) VALUES ('Medication', 'Drug allergies', 1);
IF NOT EXISTS (SELECT 1 FROM dbo.AllergyCategories WHERE CategoryName = 'Food')
    INSERT INTO dbo.AllergyCategories (CategoryName, Description, IsActive) VALUES ('Food', 'Food allergies', 1);
IF NOT EXISTS (SELECT 1 FROM dbo.AllergyCategories WHERE CategoryName = 'Environmental')
    INSERT INTO dbo.AllergyCategories (CategoryName, Description, IsActive) VALUES ('Environmental', 'Environmental/contact allergies', 1);
GO

INSERT INTO dbo.Allergies (AllergyCategoryID, AllergyName, Description, IsActive)
SELECT c.AllergyCategoryID, v.AllergyName, v.Description, 1
FROM (VALUES
    ('Penicillin',   'Medication', 'Penicillin and related beta-lactam antibiotics'),
    ('Sulfa Drugs',  'Medication', 'Sulfonamide antibiotics'),
    ('Aspirin',      'Medication', 'Acetylsalicylic acid / NSAIDs'),
    ('Latex',        'Environmental', 'Natural rubber latex'),
    ('Peanuts',      'Food', 'Peanut / tree nut allergy')
) AS v(AllergyName, CategoryName, Description)
INNER JOIN dbo.AllergyCategories c ON c.CategoryName = v.CategoryName
WHERE NOT EXISTS (SELECT 1 FROM dbo.Allergies a WHERE a.AllergyName = v.AllergyName);
GO

-- ---- MedicalConditionCategories / MedicalConditions --------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.MedicalConditionCategories WHERE CategoryName = 'Chronic')
    INSERT INTO dbo.MedicalConditionCategories (CategoryName, Description, IsActive) VALUES ('Chronic', 'Long-term chronic conditions', 1);
IF NOT EXISTS (SELECT 1 FROM dbo.MedicalConditionCategories WHERE CategoryName = 'Haematological')
    INSERT INTO dbo.MedicalConditionCategories (CategoryName, Description, IsActive) VALUES ('Haematological', 'Blood-related conditions', 1);
GO

INSERT INTO dbo.MedicalConditions (ConditionCategoryID, ConditionName, Description, IsActive)
SELECT c.ConditionCategoryID, v.ConditionName, v.Description, 1
FROM (VALUES
    ('Diabetes Mellitus Type 2', 'Chronic', 'Type 2 diabetes'),
    ('Hypertension',             'Chronic', 'High blood pressure'),
    ('Anaemia',                  'Haematological', 'Low haemoglobin / red cell count'),
    ('Sickle Cell Disease',      'Haematological', 'Inherited haemoglobin disorder'),
    ('Haemophilia',              'Haematological', 'Inherited clotting factor deficiency')
) AS v(ConditionName, CategoryName, Description)
INNER JOIN dbo.MedicalConditionCategories c ON c.CategoryName = v.CategoryName
WHERE NOT EXISTS (SELECT 1 FROM dbo.MedicalConditions mc WHERE mc.ConditionName = v.ConditionName);
GO

-- ---- MedicationCategories / Medications --------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.MedicationCategories WHERE CategoryName = 'Anticoagulant')
    INSERT INTO dbo.MedicationCategories (CategoryName, Description, IsActive) VALUES ('Anticoagulant', 'Blood thinners', 1);
IF NOT EXISTS (SELECT 1 FROM dbo.MedicationCategories WHERE CategoryName = 'Analgesic')
    INSERT INTO dbo.MedicationCategories (CategoryName, Description, IsActive) VALUES ('Analgesic', 'Pain relief medication', 1);
IF NOT EXISTS (SELECT 1 FROM dbo.MedicationCategories WHERE CategoryName = 'Antibiotic')
    INSERT INTO dbo.MedicationCategories (CategoryName, Description, IsActive) VALUES ('Antibiotic', 'Antibiotic medication', 1);
GO

INSERT INTO dbo.Medications (MedicationCategoryID, MedicationName, Description, IsActive)
SELECT c.MedicationCategoryID, v.MedicationName, v.Description, 1
FROM (VALUES
    ('Warfarin',    'Anticoagulant', 'Vitamin K antagonist anticoagulant'),
    ('Aspirin',     'Analgesic',     'Pain relief / antiplatelet'),
    ('Paracetamol', 'Analgesic',     'Pain relief / antipyretic'),
    ('Amoxicillin', 'Antibiotic',    'Broad-spectrum antibiotic')
) AS v(MedicationName, CategoryName, Description)
INNER JOIN dbo.MedicationCategories c ON c.CategoryName = v.CategoryName
WHERE NOT EXISTS (SELECT 1 FROM dbo.Medications m WHERE m.MedicationName = v.MedicationName);
GO

-- ---- Quick sanity check ------------------------------------------------------
SELECT 'TestTypes' AS TableName, COUNT(*) AS RowCount FROM dbo.TestTypes
UNION ALL SELECT 'SampleTypes', COUNT(*) FROM dbo.SampleTypes
UNION ALL SELECT 'Allergies', COUNT(*) FROM dbo.Allergies
UNION ALL SELECT 'MedicalConditions', COUNT(*) FROM dbo.MedicalConditions
UNION ALL SELECT 'Medications', COUNT(*) FROM dbo.Medications;
GO
