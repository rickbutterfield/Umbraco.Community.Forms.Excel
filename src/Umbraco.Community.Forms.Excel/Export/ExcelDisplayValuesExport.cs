using Umbraco.Forms.Core.Searchers;
using Umbraco.Forms.Core.Services;

namespace Umbraco.Community.Forms.Excel.Export;

/// <summary>
/// Exports all display values to an Excel (.xlsx) file. Captions are used for prevalue data where available.
/// </summary>
public class ExcelDisplayValuesExport : ExcelExportTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelDisplayValuesExport" /> class.
    /// </summary>
    /// <param name="formRecordSearcher">The form record searcher.</param>
    /// <param name="formService">The form service.</param>
    /// <param name="fieldTypeStorage">The field type storage.</param>
    /// <param name="prevalueSourceService">The prevalue source service.</param>
    /// <param name="fieldPreValueSourceTypeService">The field pre value source type service.</param>
    public ExcelDisplayValuesExport(IFormRecordSearcher formRecordSearcher, IFormService formService, IFieldTypeStorage fieldTypeStorage, IPrevalueSourceService prevalueSourceService, IFieldPreValueSourceTypeService fieldPreValueSourceTypeService)
        : base(formRecordSearcher, formService, fieldTypeStorage, prevalueSourceService, fieldPreValueSourceTypeService)
    {
        Id = new Guid(Constants.ExportTypes.ExcelDisplayValues);
        Alias = "excelFileDisplayValues";
        Name = "Excel file (display values)";
        Description = "Exports all display values to an Excel file. Captions are used for prevalue data where available.";
        ReplacePrevalueCaptions = true;
    }
}
