# MySQL setup for reports and promotions

The report and promotion data sources use the tables/views present in the shared MySQL schema. The repository intentionally does not contain an Aiven password.

In PowerShell, set the connection string for the current terminal and start the web project:

```powershell
$env:ConnectionStrings__DefaultConnection = 'Server=<AIVEN_HOST>;Port=<AIVEN_PORT>;Database=cuahang_codefirst_dev;UserID=<AIVEN_USER>;Password=<AIVEN_PASSWORD>;SslMode=Required;'
dotnet run --project .\src\ConvenienceStore.Web
```

Replace the four placeholders with the values from Aiven. Do not commit the completed connection string or put the password in appsettings files.

The registered MySQL adapters cover BC-01 through BC-08 and KM-01 through KM-04. BC-12 exports the report response and does not need a separate database adapter. Report timestamps and date boundaries use UTC+7.

The schema dump does not contain an image URL column on `chuong_trinh_khuyen_mai`; promotion image URLs are therefore not persisted by this adapter. Add and apply an approved schema migration before enabling persistent promotion images.

This setup note does not prove a live database round trip. Verify from the machine that can resolve the Aiven hostname by starting the app, signing in, and opening a report/promotion page.
