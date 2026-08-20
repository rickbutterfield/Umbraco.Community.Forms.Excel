# Test site

A minimal Umbraco 17 + Forms 17 site for manually exercising the package. References the `Umbraco.Community.Forms.Excel` project directly, so local changes take effect on the next run.

```bash
dotnet run --project tests/Umbraco.Community.Forms.Excel.TestSite
```

Unattended install is configured for a local SQLite database — no setup wizard needed. Login: `admin@example.com` / `SecurePass1234!`.
