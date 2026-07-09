using System.Data;
using MiniExcelLibs;
using Umbraco.Forms.Core;
using Umbraco.Forms.Core.Extensions;
using Umbraco.Forms.Core.Models;
using Umbraco.Forms.Core.Providers;
using Umbraco.Forms.Core.Searchers;
using Umbraco.Forms.Core.Services;

namespace Umbraco.Community.Forms.Excel.Export;

/// <summary>
/// Base class for Excel (.xlsx) exports.
/// </summary>
public abstract class ExcelExportTypeBase : ExportType
{
    private readonly IFormRecordSearcher _formRecordSearcher;
    private readonly IFormService _formService;
    private readonly IFieldTypeStorage _fieldTypeStorage;
    private readonly IPrevalueSourceService _prevalueSourceService;
    private readonly IFieldPreValueSourceTypeService _fieldPreValueSourceTypeService;

    /// <summary>
    /// Gets or sets a value indicating whether to replace prevalue captions.
    /// </summary>
    /// <value>
    ///   <c>true</c> if prevalue captions are replaced; otherwise, <c>false</c>.
    /// </value>
    protected bool ReplacePrevalueCaptions { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelExportTypeBase" /> class.
    /// </summary>
    /// <param name="formRecordSearcher">The form record searcher.</param>
    /// <param name="formService">The form service.</param>
    /// <param name="fieldTypeStorage">The field type storage.</param>
    /// <param name="prevalueSourceService">The prevalue source service.</param>
    /// <param name="fieldPreValueSourceTypeService">The field pre value source type service.</param>
    protected ExcelExportTypeBase(IFormRecordSearcher formRecordSearcher, IFormService formService, IFieldTypeStorage fieldTypeStorage, IPrevalueSourceService prevalueSourceService, IFieldPreValueSourceTypeService fieldPreValueSourceTypeService)
    {
        _formRecordSearcher = formRecordSearcher;
        _formService = formService;
        _fieldTypeStorage = fieldTypeStorage;
        _prevalueSourceService = prevalueSourceService;
        _fieldPreValueSourceTypeService = fieldPreValueSourceTypeService;

        FileExtension = "xlsx";
        Icon = "icon-document-spreadsheet";
        MimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    }

    /// <inheritdoc />
    public sealed override Task<string> ExportRecordsAsync(Guid formId, RecordExportFilter filter)
        => throw new NotImplementedException("Only use ExportToSteamAsync().");

    /// <inheritdoc />
    protected override async Task ExportToSteamAsync(Guid formId, RecordExportFilter filter, Stream stream)
    {
        EntrySearchResultCollection submissions = _formRecordSearcher.QueryDataBase(formId, filter);

        // Get the prevalue mapping from value to caption for replacement in results
        Dictionary<Guid, Dictionary<string, string?>>? prevalueMaps = await GetPrevalueMaps(formId).ConfigureAwait(false);

        // Build ordered, unique column keys from the schema. MiniExcel keys rows by header name, so
        // duplicate schema names would collide - suffix them to keep every column addressable.
        var columnKeys = BuildColumnKeys(submissions.Schema);

        // Build a DataTable so the header row is always written - even when there are no
        // submissions in range - matching the CSV export. (MiniExcel's dictionary mode only
        // emits a header row when at least one data row exists.)
        using var table = new DataTable("Submissions");
        foreach (var columnKey in columnKeys)
        {
            table.Columns.Add(columnKey, typeof(object));
        }

        foreach (EntrySearchResult record in submissions.Results)
        {
            var fields = record.Fields as IList<EntrySearchResult.FieldData> ?? record.Fields.ToList();
            DataRow row = table.NewRow();

            for (var i = 0; i < columnKeys.Count; i++)
            {
                EntrySearchResult.FieldData? field = i < fields.Count ? fields[i] : null;
                row[i] = GetCellValue(field, prevalueMaps) ?? (object)DBNull.Value;
            }

            table.Rows.Add(row);
        }

        await stream.SaveAsAsync(table, printHeader: true, sheetName: "Submissions").ConfigureAwait(false);
    }

    private static List<string> BuildColumnKeys(IEnumerable<EntrySearchResultSchema> schema)
    {
        var keys = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (EntrySearchResultSchema column in schema)
        {
            var name = column.Name;
            var candidate = name;
            var suffix = 1;
            while (seen.Add(candidate) is false)
            {
                candidate = $"{name} ({++suffix})";
            }

            keys.Add(candidate);
        }

        return keys;
    }

    private object? GetCellValue(EntrySearchResult.FieldData? field, Dictionary<Guid, Dictionary<string, string?>>? prevalueMaps)
    {
        if (field is null)
        {
            return null;
        }

        // Replace prevalue values with their captions where configured; otherwise pass the raw value
        // through so MiniExcel can write typed cells (dates, numbers) rather than plain strings.
        if (prevalueMaps is not null &&
            field.Value is string fieldValue &&
            Guid.TryParse(field.FieldId, out Guid fieldId))
        {
            return fieldValue.ApplyPrevalueCaptions(fieldId, prevalueMaps);
        }

        return field.Value;
    }

    private async Task<Dictionary<Guid, Dictionary<string, string?>>?> GetPrevalueMaps(Guid formId)
    {
        if (ReplacePrevalueCaptions is false ||
            _formService.Get(formId) is not Form form)
        {
            return null;
        }

        var maps = new Dictionary<Guid, Dictionary<string, string?>>();

        foreach (Field field in form.AllFields)
        {
            FieldType? fieldType = _fieldTypeStorage.GetFieldTypeByField(field);
            if (fieldType is null || fieldType.SupportsPreValues is false)
            {
                continue;
            }

            List<PreValue> prevalues = await GetPrevaluesForFormField(field, form).ConfigureAwait(false);

            // Ensure we group before attempting to create a dictionary. Prevalue values should be unique, or they wouldn't make much sense, but there's
            // nothing that enforces them to be.
            // So if we do have duplicates, take the first one and its associated caption only.
            var prevaluesMap = prevalues
                .GroupBy(x => x.Value)
                .Select(x => x.First())
                .ToDictionary(x => x.Value, x => x.Caption);
            maps[field.Id] = prevaluesMap;
        }

        return maps;
    }

    private async Task<List<PreValue>> GetPrevaluesForFormField(Field formField, Form form)
    {
        // If a prevalue source is defined, retrieve the prevalues from there.
        if (formField.PreValueSourceId != Guid.Empty)
        {
            FieldPreValueSource? prevalueSource = _prevalueSourceService.Get(formField.PreValueSourceId);
            if (prevalueSource is not null)
            {
                FieldPreValueSourceType? prevalueSourceType = _fieldPreValueSourceTypeService.GetById(prevalueSource.FieldPreValueSourceTypeId);
                if (prevalueSourceType is not null)
                {
                    prevalueSourceType.LoadSettings(prevalueSource);
                    return await prevalueSourceType.GetPreValuesAsync(formField, form).ConfigureAwait(false);
                }
            }

            return [];
        }

        // Otherwise get any defined directly on the field.
        return formField.PreValues
            .Select(x => new PreValue
            {
                Value = x.Value,
                Caption = x.Caption,
            })
            .ToList();
    }
}
