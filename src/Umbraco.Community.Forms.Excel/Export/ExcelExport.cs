using Umbraco.Forms.Core.Searchers;
using Umbraco.Forms.Core.Services;

namespace Umbraco.Community.Forms.Excel.Export;

/// <summary>
/// Exports all submitted values to an Excel (.xlsx) file.
/// </summary>
public class ExcelExport : ExcelExportTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelExport" /> class.
    /// </summary>
    /// <param name="formRecordSearcher">The form record searcher.</param>
    /// <param name="formService">The form service.</param>
    /// <param name="fieldTypeStorage">The field type storage.</param>
    /// <param name="prevalueSourceService">The prevalue source service.</param>
    /// <param name="fieldPreValueSourceTypeService">The field pre value source type service.</param>
    public ExcelExport(IFormRecordSearcher formRecordSearcher, IFormService formService, IFieldTypeStorage fieldTypeStorage, IPrevalueSourceService prevalueSourceService, IFieldPreValueSourceTypeService fieldPreValueSourceTypeService)
        : base(formRecordSearcher, formService, fieldTypeStorage, prevalueSourceService, fieldPreValueSourceTypeService)
    {
        Id = new Guid(Constants.ExportTypes.Excel);
        Alias = "excelFileSubmittedValues";
        Name = "Excel file (submitted values)";
        Description = "Exports all submitted values to an Excel file.";
    }
}
