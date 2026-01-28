-- ============================================================================
-- Clear All Project Data Script
-- ============================================================================
-- This script deletes all records from Reports, XRays, Patients, and AuditLogs.
-- Admins table is preserved.
-- ============================================================================

USE XRayAuthDB;
GO

-- 1. Disable the immutable trigger on AuditLogs
IF OBJECT_ID('hospital.trg_PreventAuditModification', 'TR') IS NOT NULL
BEGIN
    DISABLE TRIGGER hospital.trg_PreventAuditModification ON hospital.AuditLogs;
    PRINT 'Trigger hospital.trg_PreventAuditModification disabled.';
END
GO

-- 2. Delete data in correct order (FK constraints)
PRINT 'Deleting Reports...';
DELETE FROM hospital.Reports;

PRINT 'Deleting XRays...';
DELETE FROM hospital.XRays;

PRINT 'Deleting Patients...';
DELETE FROM hospital.Patients;

PRINT 'Deleting AuditLogs...';
DELETE FROM hospital.AuditLogs;
GO

-- 3. Reset Identity seeds
DBCC CHECKIDENT ('hospital.Reports', RESEED, 0);
DBCC CHECKIDENT ('hospital.XRays', RESEED, 0);
DBCC CHECKIDENT ('hospital.Patients', RESEED, 0);
DBCC CHECKIDENT ('hospital.AuditLogs', RESEED, 0);
GO

-- 4. Re-enable the trigger
IF OBJECT_ID('hospital.trg_PreventAuditModification', 'TR') IS NOT NULL
BEGIN
    ENABLE TRIGGER hospital.trg_PreventAuditModification ON hospital.AuditLogs;
    PRINT 'Trigger hospital.trg_PreventAuditModification enabled.';
END
GO

PRINT '========================================';
PRINT 'All transaction data cleared successfully.';
PRINT 'Admins table preserved.';
PRINT '========================================';
