namespace Umbraco.Community.Forms.Excel;

/// <summary>
/// Constants for the Excel export types.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Well-known IDs for the export types registered by this package.
    /// </summary>
    public static class ExportTypes
    {
        /// <summary>
        /// Exports all submitted values to an Excel (.xlsx) file.
        /// </summary>
        public const string Excel = "94ED105A-87B3-4e1f-97CB-9A320AEE2745";

        /// <summary>
        /// Exports all display values to an Excel (.xlsx) file. Captions are used for prevalue data where available.
        /// </summary>
        public const string ExcelDisplayValues = "688711A2-DC6F-4B51-B8D2-0BB177BB0499";
    }
}
