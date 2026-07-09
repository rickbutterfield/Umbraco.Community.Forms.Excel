# Umbraco.Community.Forms.Excel

Adds Excel (`.xlsx`) export types to [Umbraco Forms](https://umbraco.com/products/add-ons/forms/), using [MiniExcel](https://github.com/mini-software/MiniExcel).

## Features

Registers two additional export types on the Forms record list, alongside the built-in CSV exports:

- **Excel file (submitted values)** — exports the raw submitted values for a form's entries to `.xlsx`.
- **Excel file (display values)** — exports display values, using prevalue captions where available (e.g. dropdown/checkbox list labels instead of raw stored values).

## Installation

```bash
dotnet add package Umbraco.Community.Forms.Excel
```

No further configuration is required — the export types register automatically on startup and appear in the form export dropdown in the Umbraco Forms backoffice.

## Compatibility

| Package version | Umbraco Forms |
|---|---|
| 1.x | 17.x, 18.x |

## License

[MIT](LICENSE)
