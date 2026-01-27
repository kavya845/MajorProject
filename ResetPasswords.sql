-- Reset Passwords to Plain-Text
USE XRayAuthDB;
GO

PRINT 'Resetting all administrator passwords to admin123...';
UPDATE hospital.Admins SET Password = 'admin123';

PRINT 'Resetting all patient passwords to password123...';
UPDATE hospital.Patients SET Password = 'password123';

PRINT '✓ All passwords reset to plain-text.';
GO
