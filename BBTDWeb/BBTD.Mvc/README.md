# Bulk Barcode Test Driver

This is a test-driver setup for testing barcode reading operations in bulk.

## Database setup

SQL Server database was manually created on localhost SQL Server using name `BBTDDB`. It was set up using `BBTDDatabase\Person.sql` file.

## Database model

Database model class was reverse engineered using `dotnet ef` tool:

```
dotnet ef dbcontext scaffold "server=.;Database=BBTDDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true" Microsoft.EntityFrameworkCore.SqlServer -o Models
```

