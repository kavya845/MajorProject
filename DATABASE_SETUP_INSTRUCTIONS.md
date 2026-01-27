# Database Setup Instructions

## Quick Setup (Recommended)

Follow these steps to set up your database with the hospital schema:

### Option 1: Using SQL Server Management Studio (SSMS)

1. **Open SQL Server Management Studio**
2. **Connect to your local SQL Server instance** (usually `localhost` or `.`)
3. **Open the setup script**:
   - Click `File` → `Open` → `File...`
   - Navigate to: `c:\Users\Admin\source\repos\MajorProject\DatabaseSetup.sql`
4. **Execute the script**:
   - Click the `Execute` button (or press F5)
   - Wait for the script to complete
5. **Verify success**:
   - You should see messages indicating tables, triggers, and stored procedures were created
   - Expand `Databases` → `XRayAuthDB` → `Schemas` → `hospital` to see all objects

### Option 2: Using Command Line (sqlcmd)

Open PowerShell or Command Prompt and run:

```powershell
sqlcmd -S localhost -E -i "c:\Users\Admin\source\repos\MajorProject\DatabaseSetup.sql"
```

### Option 3: Using Azure Data Studio

1. **Open Azure Data Studio**
2. **Connect to your local SQL Server**
3. **Open the setup script**: `DatabaseSetup.sql`
4. **Click Run** (or press F5)

---

## Verification

After running the script, verify the setup:

1. **Check the database exists**:
   ```sql
   SELECT name FROM sys.databases WHERE name = 'XRayAuthDB';
   ```

2. **Check the hospital schema exists**:
   ```sql
   USE XRayAuthDB;
   SELECT name FROM sys.schemas WHERE name = 'hospital';
   ```

3. **Check all tables exist**:
   ```sql
   SELECT TABLE_SCHEMA, TABLE_NAME 
   FROM INFORMATION_SCHEMA.TABLES 
   WHERE TABLE_SCHEMA = 'hospital'
   ORDER BY TABLE_NAME;
   ```
   
   You should see:
   - hospital.Admins
   - hospital.AuditLogs
   - hospital.Patients
   - hospital.Reports
   - hospital.XRays

4. **Check admin user exists**:
   ```sql
   SELECT AdminID, Username FROM hospital.Admins;
   ```
   
   You should see one row with username `admin`

---

## Troubleshooting

### Error: "Cannot open database 'XRayAuthDB'"
- The database doesn't exist yet. Run the `DatabaseSetup.sql` script.

### Error: "Invalid object name 'hospital.Admins'"
- The hospital schema hasn't been created. Run the `DatabaseSetup.sql` script.

### Error: "Login failed for user"
- Make sure you're using Windows Authentication or have the correct SQL Server credentials
- Check your connection string in `appsettings.json`

### Connection String
Your `appsettings.json` should have:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=XRayAuthDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

---

## What This Script Does

1. ✅ Creates `XRayAuthDB` database (if it doesn't exist)
2. ✅ Creates `hospital` schema
3. ✅ Creates 5 tables with proper relationships
4. ✅ Creates 1 trigger for audit log immutability
5. ✅ Creates 3 stored procedures for patient CRUD operations
6. ✅ Seeds admin user (username: `admin`, password: `admin123`)

After running this script, your application should work without any "Invalid object name" errors!
