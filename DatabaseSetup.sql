-- ============================================================================
-- XRayAuthDB Database Setup Script
-- ============================================================================
-- This is the authoritative database setup script for the XRay Diagnostic System.
-- All tables use the 'hospital' schema.
-- Run this script to set up a fresh database.
-- ============================================================================

-- Step 1: Create the database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'XRayAuthDB')
BEGIN
    CREATE DATABASE XRayAuthDB;
    PRINT 'Database XRayAuthDB created successfully.';
END
ELSE
BEGIN
    PRINT 'Database XRayAuthDB already exists.';
END
GO

-- Step 2: Use the database
USE XRayAuthDB;
GO

-- Step 3: Create hospital schema
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'hospital')
BEGIN
    EXEC('CREATE SCHEMA hospital');
    PRINT 'Schema hospital created successfully.';
END
ELSE
BEGIN
    PRINT 'Schema hospital already exists.';
END
GO

-- ============================================================================
-- Step 4: Create Tables
-- ============================================================================

-- Admins Table
IF OBJECT_ID('hospital.Admins', 'U') IS NULL
BEGIN
    CREATE TABLE hospital.Admins (
        AdminID INT PRIMARY KEY IDENTITY(1,1),
        Username NVARCHAR(50) NOT NULL UNIQUE,
        Password NVARCHAR(255) NOT NULL
    );
    PRINT 'Table hospital.Admins created successfully.';
END
GO

-- Patients Table
IF OBJECT_ID('hospital.Patients', 'U') IS NULL
BEGIN
    CREATE TABLE hospital.Patients (
        PatientID INT PRIMARY KEY IDENTITY(1,1),
        FullName NVARCHAR(100) NOT NULL,
        Age INT,
        Gender NVARCHAR(10),
        ContactNumber NVARCHAR(15),
        Address NVARCHAR(255),
        CreatedAt DATETIME DEFAULT GETDATE()
    );
    PRINT 'Table hospital.Patients created successfully.';
END
GO

-- XRays Table
IF OBJECT_ID('hospital.XRays', 'U') IS NULL
BEGIN
    CREATE TABLE hospital.XRays (
        XRayID INT PRIMARY KEY IDENTITY(1,1),
        PatientID INT FOREIGN KEY REFERENCES hospital.Patients(PatientID),
        ImagePath NVARCHAR(255) NOT NULL,
        BodyPart NVARCHAR(50),
        UploadDate DATETIME DEFAULT GETDATE(),
        TechnicianNotes NVARCHAR(MAX),
        Status NVARCHAR(20) DEFAULT 'Pending'
    );
    PRINT 'Table hospital.XRays created successfully.';
END
GO

-- Reports Table
IF OBJECT_ID('hospital.Reports', 'U') IS NULL
BEGIN
    CREATE TABLE hospital.Reports (
        ReportID INT PRIMARY KEY IDENTITY(1,1),
        XRayID INT FOREIGN KEY REFERENCES hospital.XRays(XRayID),
        DiagnosisResult NVARCHAR(MAX),
        DoctorComments NVARCHAR(MAX),
        Confidence INT,
        Severity NVARCHAR(50),
        Recommendations NVARCHAR(MAX),
        GeneratedDate DATETIME DEFAULT GETDATE()
    );
    PRINT 'Table hospital.Reports created successfully.';
END
GO

-- AuditLogs Table
IF OBJECT_ID('hospital.AuditLogs', 'U') IS NULL
BEGIN
    CREATE TABLE hospital.AuditLogs (
        LogID INT PRIMARY KEY IDENTITY(1,1),
        Action NVARCHAR(50),
        TableName NVARCHAR(50),
        RecordID INT,
        Details NVARCHAR(MAX),
        UserRole NVARCHAR(50),
        Timestamp DATETIME DEFAULT GETDATE()
    );
    PRINT 'Table hospital.AuditLogs created successfully.';
END
GO

-- ============================================================================
-- Step 5: Create Trigger
-- ============================================================================

-- Immutable Trigger for AuditLogs
IF OBJECT_ID('hospital.trg_PreventAuditModification', 'TR') IS NOT NULL
BEGIN
    DROP TRIGGER hospital.trg_PreventAuditModification;
    PRINT 'Existing trigger hospital.trg_PreventAuditModification dropped.';
END
GO

CREATE TRIGGER hospital.trg_PreventAuditModification
ON hospital.AuditLogs
FOR UPDATE, DELETE
AS
BEGIN
    RAISERROR ('Audit logs are immutable and cannot be modified or deleted.', 16, 1);
    ROLLBACK TRANSACTION;
END;
GO
PRINT 'Trigger hospital.trg_PreventAuditModification created successfully.';
GO

-- ============================================================================
-- Step 6: Seed Data
-- ============================================================================

-- Seed Admin
IF NOT EXISTS (SELECT * FROM hospital.Admins WHERE Username='admin')
BEGIN
    INSERT INTO hospital.Admins (Username, Password) VALUES ('admin', 'admin123');
    PRINT 'Admin user seeded successfully.';
END
ELSE
BEGIN
    PRINT 'Admin user already exists.';
END
GO

-- ============================================================================
-- Step 7: Create Stored Procedures
-- ============================================================================

-- Create Patient Stored Procedure
IF OBJECT_ID('hospital.sp_CreatePatient', 'P') IS NOT NULL
    DROP PROCEDURE hospital.sp_CreatePatient;
GO

CREATE PROCEDURE hospital.sp_CreatePatient
    @FullName NVARCHAR(100),
    @Age INT,
    @Gender NVARCHAR(10),
    @ContactNumber NVARCHAR(15),
    @Address NVARCHAR(255),
    @NewID INT OUTPUT
AS
BEGIN
    INSERT INTO hospital.Patients (FullName, Age, Gender, ContactNumber, Address, CreatedAt)
    VALUES (@FullName, @Age, @Gender, @ContactNumber, @Address, GETDATE());
    
    SET @NewID = SCOPE_IDENTITY();
END;
GO
PRINT 'Stored procedure hospital.sp_CreatePatient created successfully.';
GO

-- Update Patient Stored Procedure
IF OBJECT_ID('hospital.sp_UpdatePatient', 'P') IS NOT NULL
    DROP PROCEDURE hospital.sp_UpdatePatient;
GO

CREATE PROCEDURE hospital.sp_UpdatePatient
    @PatientID INT,
    @FullName NVARCHAR(100),
    @Age INT,
    @Gender NVARCHAR(10),
    @ContactNumber NVARCHAR(15),
    @Address NVARCHAR(255)
AS
BEGIN
    UPDATE hospital.Patients
    SET FullName = @FullName,
        Age = @Age,
        Gender = @Gender,
        ContactNumber = @ContactNumber,
        Address = @Address
    WHERE PatientID = @PatientID;
END;
GO
PRINT 'Stored procedure hospital.sp_UpdatePatient created successfully.';
GO

-- Delete Patient Stored Procedure
IF OBJECT_ID('hospital.sp_DeletePatient', 'P') IS NOT NULL
    DROP PROCEDURE hospital.sp_DeletePatient;
GO

CREATE PROCEDURE hospital.sp_DeletePatient
    @PatientID INT
AS
BEGIN
    -- Delete related records first (cascade delete)
    DELETE FROM hospital.Reports WHERE XRayID IN (SELECT XRayID FROM hospital.XRays WHERE PatientID = @PatientID);
    DELETE FROM hospital.XRays WHERE PatientID = @PatientID;
    DELETE FROM hospital.Patients WHERE PatientID = @PatientID;
END;
GO
PRINT 'Stored procedure hospital.sp_DeletePatient created successfully.';
GO

PRINT '========================================';
PRINT 'Database setup complete with hospital schema!';
PRINT 'Database: XRayAuthDB';
PRINT 'Schema: hospital';
PRINT 'Admin credentials: admin / admin123';
PRINT '========================================';
