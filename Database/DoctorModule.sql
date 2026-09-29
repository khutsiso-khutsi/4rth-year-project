/* ============================================================================
   NMB-HLabSys — Doctor Module database script
   ----------------------------------------------------------------------------
   This script is ADDITIVE and defensive: every CREATE TABLE / CREATE PROC is
   guarded with an existence check so it is safe to run against the shared
   team database even if some of these objects already exist in a slightly
   different shape.

   IMPORTANT — read before running:
   There is no schema (.sql) file anywhere else in this repository, so the
   table/column names below are inferred from how the existing C# data-access
   code (Patient/DataAccess/UserDataAccess.cs) already queries the database
   (e.g. it expects Patients(PatientID, UserID, FirstName, LastName, IDNumber,
   DateOfBirth, CellphoneNumber, HomeAddress), Conditions/Allergies/Medications
   master tables + PatientConditions/PatientAllergies/PatientMedications
   junction tables, a Doctors(DoctorID, DoctorName, Email) table, Roles,
   Users). If your actual column names differ, adjust the procs below to
   match — the C# side (DoctorDataAccess.cs) reads columns strictly by name.
   Run this against a COPY of the dev database first.
   ============================================================================ */

SET NOCOUNT ON;
GO

/* ---------------------------------------------------------------------------
   1. Supporting tables (created only if missing)
--------------------------------------------------------------------------- */

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TestCategories')
BEGIN
    CREATE TABLE TestCategories (
        CategoryID   INT IDENTITY(1,1) PRIMARY KEY,
        CategoryName NVARCHAR(100) NOT NULL UNIQUE
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TestTypes')
BEGIN
    CREATE TABLE TestTypes (
        TestTypeID      INT IDENTITY(1,1) PRIMARY KEY,
        TestName        NVARCHAR(150) NOT NULL,
        CategoryID      INT NOT NULL REFERENCES TestCategories(CategoryID),
        UnitName        NVARCHAR(50)  NULL,
        NormalRangeMin  DECIMAL(10,2) NULL,
        NormalRangeMax  DECIMAL(10,2) NULL,
        SampleTypeName  NVARCHAR(100) NULL      -- e.g. "EDTA Whole Blood", "Serum"
    );
END
GO

-- Doctors table: add a UserID link if the table exists without one, so a
-- logged-in doctor (Users/Session) can be resolved to a DoctorID.
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Doctors')
BEGIN
    CREATE TABLE Doctors (
        DoctorID     INT IDENTITY(1,1) PRIMARY KEY,
        UserID       INT NULL UNIQUE REFERENCES Users(UserID),
        DoctorName   NVARCHAR(200) NOT NULL,
        Email        NVARCHAR(256) NOT NULL
    );
END
ELSE IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Doctors') AND name = 'UserID')
BEGIN
    ALTER TABLE Doctors ADD UserID INT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TestRequests')
BEGIN
    CREATE TABLE TestRequests (
        RequestID       INT IDENTITY(1,1) PRIMARY KEY,
        RequestNumber   NVARCHAR(30) NOT NULL UNIQUE,
        PatientID       INT NOT NULL REFERENCES Patients(PatientID),
        DoctorID        INT NOT NULL REFERENCES Doctors(DoctorID),
        RequestDate     DATETIME NOT NULL DEFAULT GETDATE(),
        Urgency         NVARCHAR(20) NOT NULL,          -- Routine / Urgent / STAT
        RequestStatus   NVARCHAR(30) NOT NULL,          -- see status vocabulary below
        ClinicalNotes   NVARCHAR(MAX) NULL,
        ReleaseNotes    NVARCHAR(MAX) NULL,
        ReleasedDate    DATETIME NULL,
        CancelReason    NVARCHAR(MAX) NULL,
        CancelledBy     NVARCHAR(30) NULL               -- 'Doctor' or 'LabTechnician'
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TestRequestItems')
BEGIN
    CREATE TABLE TestRequestItems (
        RequestItemID       INT IDENTITY(1,1) PRIMARY KEY,
        RequestID           INT NOT NULL REFERENCES TestRequests(RequestID),
        TestTypeID          INT NOT NULL REFERENCES TestTypes(TestTypeID),
        ItemStatus          NVARCHAR(30) NOT NULL,      -- mirrors RequestStatus vocabulary
        ResultValue         DECIMAL(10,2) NULL,
        ResultNotes         NVARCHAR(MAX) NULL,
        IsAbnormal          BIT NOT NULL DEFAULT 0,
        CompletionDateTime  DATETIME NULL,
        VerificationDateTime DATETIME NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TestRequestSamples')
BEGIN
    CREATE TABLE TestRequestSamples (
        SampleID        INT IDENTITY(1,1) PRIMARY KEY,
        RequestID       INT NOT NULL REFERENCES TestRequests(RequestID),
        BarcodeValue    NVARCHAR(60) NOT NULL,
        CollectedDate   DATETIME NULL,
        ReceivedDate    DATETIME NULL                    -- set by lab technician ("Sample(s) received")
    );
END
GO

/* Status vocabulary constant reference (kept as a comment for the team,
   both RequestStatus and ItemStatus must always be one of):
     Submitted
     Sample(s) received
     In progress
     Completed
     Released by doctor
     Cancelled
*/

/* ---------------------------------------------------------------------------
   2. Stored procedures
--------------------------------------------------------------------------- */

-- Resolve the DoctorID for the currently logged-in user (Session UserID).
IF OBJECT_ID('sp_GetDoctorIdByUserId') IS NOT NULL DROP PROCEDURE sp_GetDoctorIdByUserId;
GO
CREATE PROCEDURE sp_GetDoctorIdByUserId
    @UserID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 DoctorID, DoctorName, Email
    FROM Doctors
    WHERE UserID = @UserID;
END
GO

-- Search / list patients for the "Manage Patient Records" screen.
IF OBJECT_ID('sp_SearchPatients') IS NOT NULL DROP PROCEDURE sp_SearchPatients;
GO
CREATE PROCEDURE sp_SearchPatients
    @SearchTerm NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.PatientID, p.FirstName, p.LastName, p.IDNumber, p.DateOfBirth,
           p.CellphoneNumber, u.Email
    FROM Patients p
    JOIN Users u ON u.UserID = p.UserID
    WHERE @SearchTerm IS NULL OR @SearchTerm = ''
       OR p.FirstName LIKE '%' + @SearchTerm + '%'
       OR p.LastName  LIKE '%' + @SearchTerm + '%'
       OR p.IDNumber  LIKE '%' + @SearchTerm + '%'
       OR u.Email     LIKE '%' + @SearchTerm + '%'
    ORDER BY p.LastName, p.FirstName;
END
GO

-- SA ID uniqueness check used before registering a new patient.
IF OBJECT_ID('sp_CheckPatientIdNumberExists') IS NOT NULL DROP PROCEDURE sp_CheckPatientIdNumberExists;
GO
CREATE PROCEDURE sp_CheckPatientIdNumberExists
    @IDNumber NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) AS ExistsCount FROM Patients WHERE IDNumber = @IDNumber;
END
GO

-- All active test types, for the "Create Test Request" checklist.
IF OBJECT_ID('sp_GetAllTestTypes') IS NOT NULL DROP PROCEDURE sp_GetAllTestTypes;
GO
CREATE PROCEDURE sp_GetAllTestTypes
AS
BEGIN
    SET NOCOUNT ON;
    SELECT tt.TestTypeID, tt.TestName, tc.CategoryName, tt.UnitName,
           tt.NormalRangeMin, tt.NormalRangeMax, tt.SampleTypeName
    FROM TestTypes tt
    JOIN TestCategories tc ON tc.CategoryID = tt.CategoryID
    ORDER BY tc.CategoryName, tt.TestName;
END
GO

-- Create a test request (header + items + sample barcodes) in one transaction.
-- Sample barcodes are passed as a delimited string ("BC001|BC002") because this
-- project does not use table-valued parameters elsewhere; the C# side splits it.
IF OBJECT_ID('sp_CreateTestRequest') IS NOT NULL DROP PROCEDURE sp_CreateTestRequest;
GO
CREATE PROCEDURE sp_CreateTestRequest
    @PatientID          INT,
    @DoctorID           INT,
    @Urgency             NVARCHAR(20),
    @ClinicalNotes       NVARCHAR(MAX) = NULL,
    @TestTypeIdsCsv      NVARCHAR(MAX),   -- e.g. "3,7,12"
    @BarcodesCsv         NVARCHAR(MAX)    -- e.g. "BC-2025-001,BC-2025-002"
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @RequestNumber NVARCHAR(30) =
        'REQ-' + CONVERT(NVARCHAR(4), YEAR(GETDATE())) + '-' +
        RIGHT('00000' + CONVERT(NVARCHAR(10), NEXT VALUE FOR seq_TestRequestNumber), 5);

    INSERT INTO TestRequests (RequestNumber, PatientID, DoctorID, RequestDate, Urgency, RequestStatus, ClinicalNotes)
    VALUES (@RequestNumber, @PatientID, @DoctorID, GETDATE(), @Urgency, N'Submitted', @ClinicalNotes);

    DECLARE @RequestID INT = SCOPE_IDENTITY();

    INSERT INTO TestRequestItems (RequestID, TestTypeID, ItemStatus)
    SELECT @RequestID, CAST(value AS INT), N'Submitted'
    FROM STRING_SPLIT(@TestTypeIdsCsv, ',');

    INSERT INTO TestRequestSamples (RequestID, BarcodeValue)
    SELECT @RequestID, LTRIM(RTRIM(value))
    FROM STRING_SPLIT(@BarcodesCsv, ',')
    WHERE LTRIM(RTRIM(value)) <> '';

    COMMIT TRANSACTION;

    SELECT @RequestID AS RequestID, @RequestNumber AS RequestNumber;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = 'seq_TestRequestNumber')
    CREATE SEQUENCE seq_TestRequestNumber AS INT START WITH 1 INCREMENT BY 1;
GO

-- All test requests submitted by a doctor (dashboard + tracking list), with an
-- optional date range for the Reports feature.
IF OBJECT_ID('sp_GetDoctorTestRequests') IS NOT NULL DROP PROCEDURE sp_GetDoctorTestRequests;
GO
CREATE PROCEDURE sp_GetDoctorTestRequests
    @DoctorID  INT,
    @FromDate  DATETIME = NULL,
    @ToDate    DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT r.RequestID, r.RequestNumber, r.RequestDate, r.Urgency, r.RequestStatus,
           r.ClinicalNotes, r.ReleaseNotes, r.ReleasedDate, r.CancelReason, r.CancelledBy,
           p.PatientID, p.FirstName + ' ' + p.LastName AS PatientName, u.Email AS PatientEmail
    FROM TestRequests r
    JOIN Patients p ON p.PatientID = r.PatientID
    JOIN Users u ON u.UserID = p.UserID
    WHERE r.DoctorID = @DoctorID
      AND (@FromDate IS NULL OR r.RequestDate >= @FromDate)
      AND (@ToDate IS NULL OR r.RequestDate < DATEADD(DAY, 1, @ToDate))
    ORDER BY r.RequestDate DESC;
END
GO

-- Items + samples for a single request (for the detail/results view).
IF OBJECT_ID('sp_GetTestRequestItemsForDoctor') IS NOT NULL DROP PROCEDURE sp_GetTestRequestItemsForDoctor;
GO
CREATE PROCEDURE sp_GetTestRequestItemsForDoctor
    @RequestID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT i.RequestItemID, i.RequestID, tt.TestName, tc.CategoryName, i.ItemStatus,
           i.ResultValue, i.ResultNotes, i.IsAbnormal, i.CompletionDateTime,
           i.VerificationDateTime, tt.UnitName, tt.NormalRangeMin, tt.NormalRangeMax
    FROM TestRequestItems i
    JOIN TestTypes tt ON tt.TestTypeID = i.TestTypeID
    JOIN TestCategories tc ON tc.CategoryID = tt.CategoryID
    WHERE i.RequestID = @RequestID;

    SELECT SampleID, BarcodeValue, CollectedDate, ReceivedDate
    FROM TestRequestSamples
    WHERE RequestID = @RequestID;
END
GO

-- Cancel a request; only allowed while Submitted or "Sample(s) received".
IF OBJECT_ID('sp_CancelTestRequest') IS NOT NULL DROP PROCEDURE sp_CancelTestRequest;
GO
CREATE PROCEDURE sp_CancelTestRequest
    @RequestID INT,
    @Reason    NVARCHAR(MAX),
    @CancelledBy NVARCHAR(30)   -- 'Doctor' or 'LabTechnician'
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE TestRequests
    SET RequestStatus = N'Cancelled', CancelReason = @Reason, CancelledBy = @CancelledBy
    WHERE RequestID = @RequestID
      AND RequestStatus IN (N'Submitted', N'Sample(s) received');

    UPDATE TestRequestItems SET ItemStatus = N'Cancelled' WHERE RequestID = @RequestID;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- Release results to the patient with a note; only meaningful once Completed.
IF OBJECT_ID('sp_ReleaseTestRequestResults') IS NOT NULL DROP PROCEDURE sp_ReleaseTestRequestResults;
GO
CREATE PROCEDURE sp_ReleaseTestRequestResults
    @RequestID     INT,
    @ReleaseNotes  NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE TestRequests
    SET RequestStatus = N'Released by doctor', ReleaseNotes = @ReleaseNotes, ReleasedDate = GETDATE()
    WHERE RequestID = @RequestID;
END
GO

-- Abnormal-result alerts for a doctor within a date range (default: last 5 days).
IF OBJECT_ID('sp_GetDoctorAbnormalAlerts') IS NOT NULL DROP PROCEDURE sp_GetDoctorAbnormalAlerts;
GO
CREATE PROCEDURE sp_GetDoctorAbnormalAlerts
    @DoctorID  INT,
    @FromDate  DATETIME = NULL,
    @ToDate    DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @From DATETIME = ISNULL(@FromDate, DATEADD(DAY, -5, CAST(GETDATE() AS DATE)));
    DECLARE @To   DATETIME = ISNULL(@ToDate, GETDATE());

    SELECT i.RequestItemID, i.RequestID, r.RequestNumber, r.RequestDate,
           p.PatientID, p.FirstName + ' ' + p.LastName AS PatientName,
           tt.TestName, i.ResultValue, tt.UnitName, tt.NormalRangeMin, tt.NormalRangeMax,
           i.ResultNotes, i.CompletionDateTime
    FROM TestRequestItems i
    JOIN TestRequests r ON r.RequestID = i.RequestID
    JOIN Patients p ON p.PatientID = r.PatientID
    JOIN TestTypes tt ON tt.TestTypeID = i.TestTypeID
    WHERE r.DoctorID = @DoctorID
      AND i.IsAbnormal = 1
      AND i.CompletionDateTime BETWEEN @From AND DATEADD(DAY, 1, @To)
    ORDER BY i.CompletionDateTime DESC;
END
GO
