-- Create Database
CREATE DATABASE XRayDiagnosticDB;
GO

USE XRayDiagnosticDB;
GO

-- Users Table
CREATE TABLE Users (
    UserID INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Password NVARCHAR(255) NOT NULL, -- In a real app, hash this
    FullName NVARCHAR(100),
    Role NVARCHAR(20) CHECK (Role IN ('Admin', 'Doctor', 'Radiologist')),
    CreatedAt DATETIME DEFAULT GETDATE()
);

-- Patients Table
CREATE TABLE Patients (
    PatientID INT PRIMARY KEY IDENTITY(1,1),
    FullName NVARCHAR(100) NOT NULL,
    Age INT,
    Gender NVARCHAR(10),
    ContactNumber NVARCHAR(15),
    Address NVARCHAR(255),
    CreatedAt DATETIME DEFAULT GETDATE()
);

-- XRays Table
CREATE TABLE XRays (
    XRayID INT PRIMARY KEY IDENTITY(1,1),
    PatientID INT FOREIGN KEY REFERENCES Patients(PatientID),
    ImagePath NVARCHAR(255) NOT NULL,
    UploadDate DATETIME DEFAULT GETDATE(),
    TechnicianNotes NVARCHAR(MAX),
    Status NVARCHAR(20) DEFAULT 'Pending'
);

-- Observations Table (Finding tags for each X-ray)
CREATE TABLE Observations (
    ObservationID INT PRIMARY KEY IDENTITY(1,1),
    XRayID INT FOREIGN KEY REFERENCES XRays(XRayID),
    Tag NVARCHAR(50) NOT NULL, -- e.g., 'Cloudy', 'Fracture', 'Enlarged'
    XCoordinate INT, -- Optional: Marker on image
    YCoordinate INT
);

-- Diagnosis Rules Table
CREATE TABLE DiagnosisRules (
    RuleID INT PRIMARY KEY IDENTITY(1,1),
    ConditionName NVARCHAR(100) NOT NULL,
    RequiredTags NVARCHAR(255) NOT NULL, -- Comma-separated tags e.g., 'Cloudy,Fluid'
    Description NVARCHAR(MAX)
);

-- Reports Table
CREATE TABLE Reports (
    ReportID INT PRIMARY KEY IDENTITY(1,1),
    XRayID INT FOREIGN KEY REFERENCES XRays(XRayID),
    DiagnosisResult NVARCHAR(MAX),
    DoctorComments NVARCHAR(MAX),
    GeneratedDate DATETIME DEFAULT GETDATE()
);

-- Seed Data
INSERT INTO Users (Username, Password, FullName, Role) VALUES 
('admin', 'admin123', 'System Administrator', 'Admin'),
('doctor1', 'doc123', 'Dr. Smith', 'Doctor'),
('radio1', 'radio123', 'John Doe (Radiologist)', 'Radiologist');

INSERT INTO DiagnosisRules (ConditionName, RequiredTags, Description) VALUES
('Potential Pneumonia', 'Cloudy,Fluid', 'Findings suggest fluid or opacity in lungs often associated with pneumonia.'),
('Bone Fracture', 'Displacement', 'Visible break or displacement in the bone structure.'),
('Normal Chest', 'Clear', 'No significant abnormalities detected in the chest cavity.');
