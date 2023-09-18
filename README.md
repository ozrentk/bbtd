# Bulk Barcode Test Driver

This is a test-driver setup for testing barcode reading operations in bulk.

Note that web server and smartphone should connect to the same local network.

## Database setup

SQL Server database was manually created on localhost SQL Server using name `BBTDDB`. It was set up using `BBTDDatabase\Person.sql` file.

## Database model

Database model class was reverse engineered using `dotnet ef` tool:

```
dotnet ef dbcontext scaffold "server=.;Database=BBTDDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true" Microsoft.EntityFrameworkCore.SqlServer -o Models
```

## Logging

Graylog is used for logging.

You can use an existing Graylog server, but if that is not configured I recommend using Docker Compose to start local server:

```
cd Graylog
docker compose up -d
```

Use the following to monitor Graylog startup:

```
docker compose logs -f -t
```

Use the following to open Graylog web interface (user: *admin*, password: *admin*):

```
http://localhost:9000/search
```

When in web interface go to `System/Inputs` menu and add a default GELF UDP input to record incoming messages (named e.g. *GELF input*). Go to `Search` menu to see log messages.

Also, add the following rule `System/Pipelines/Manage Rules` (add it to the newly created pipeline).

```
rule "Extract Timestamp"
when
    has_field("exact_ts")
then
    let exact_ts = parse_date(to_string($message.exact_ts), "yyyy-MM-dd'T'HH:mm:ss.SSS'Z'");
    set_field("timestamp", exact_ts); 
end
```
