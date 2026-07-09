using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Community.Forms.Excel.Export;
using Umbraco.Forms.Core.Providers;

namespace Umbraco.Community.Forms.Excel;

/// <summary>
/// Registers the Excel export types with Umbraco Forms on startup.
/// </summary>
public class ExcelExportComposer : IComposer
{
    /// <inheritdoc />
    public void Compose(IUmbracoBuilder builder)
        => builder.WithCollectionBuilder<ExportCollectionBuilder>()
            .Add<ExcelExport>()
            .Add<ExcelDisplayValuesExport>();
}
