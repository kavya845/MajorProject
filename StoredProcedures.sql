-- Stored Procedures for hospital schema
USE XRayAuthDB;
GO

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

PRINT 'Stored procedures created successfully in hospital schema!';
