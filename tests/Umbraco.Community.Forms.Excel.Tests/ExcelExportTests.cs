using MiniExcelLibs;
using Moq;
using NUnit.Framework;
using Umbraco.Community.Forms.Excel.Export;
using Umbraco.Forms.Core.Models;
using Umbraco.Forms.Core.Searchers;
using Umbraco.Forms.Core.Services;

namespace Umbraco.Community.Forms.Excel.Tests;

[TestFixture]
internal class ExcelExportTests
{
    private Mock<IFormRecordSearcher> _formRecordSearcherMock = null!;
    private ExcelExport _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _formRecordSearcherMock = new Mock<IFormRecordSearcher>();
        _sut = new ExcelExport(
            _formRecordSearcherMock.Object,
            Mock.Of<IFormService>(),
            Mock.Of<IFieldTypeStorage>(),
            Mock.Of<IPrevalueSourceService>(),
            Mock.Of<IFieldPreValueSourceTypeService>());
    }

    [Test]
    public async Task ExportToFile_WritesHeaderRowAndTypedValues()
    {
        var created = new DateTime(2026, 1, 2, 9, 30, 0);
        _formRecordSearcherMock
            .Setup(x => x.QueryDataBase(It.IsAny<Guid>(), It.IsAny<RecordFilter>()))
            .Returns(BuildCollection(
                ["Name", "Age", "Created"],
                ["Alice", 30, created],
                ["Bob", 25, created]));

        var rows = await ExportAndReadBack(_sut);

        Assert.That(rows, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(rows[0]["Name"], Is.EqualTo("Alice"));
            Assert.That(Convert.ToInt32(rows[0]["Age"]), Is.EqualTo(30));
            Assert.That(rows[0]["Created"], Is.EqualTo(created), "DateTime values should round-trip as typed cells, not strings.");
            Assert.That(rows[1]["Name"], Is.EqualTo("Bob"));
        });
    }

    [Test]
    public async Task ExportToFile_DisambiguatesDuplicateColumnHeaders()
    {
        _formRecordSearcherMock
            .Setup(x => x.QueryDataBase(It.IsAny<Guid>(), It.IsAny<RecordFilter>()))
            .Returns(BuildCollection(
                ["Field", "Field"],
                ["first", "second"]));

        var rows = await ExportAndReadBack(_sut);

        Assert.That(rows, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(rows[0]["Field"], Is.EqualTo("first"));
            Assert.That(rows[0]["Field (2)"], Is.EqualTo("second"), "Duplicate schema names must be suffixed so no column is lost.");
        });
    }

    [Test]
    public async Task ExportToFile_WithNoResults_StillWritesHeaderRow()
    {
        _formRecordSearcherMock
            .Setup(x => x.QueryDataBase(It.IsAny<Guid>(), It.IsAny<RecordFilter>()))
            .Returns(BuildCollection(["Name", "Age"]));

        // Read without treating the first row as a header, so we can assert the header row
        // itself was written even though there are no data rows (parity with the CSV export).
        var raw = await ExportAndReadBack(_sut, useHeaderRow: false);

        Assert.That(raw, Has.Count.EqualTo(1), "The header row should be written even with no submissions.");
        Assert.Multiple(() =>
        {
            Assert.That(raw[0]["A"], Is.EqualTo("Name"));
            Assert.That(raw[0]["B"], Is.EqualTo("Age"));
        });

        // And with the header consumed there should be zero data rows.
        var rows = await ExportAndReadBack(_sut);
        Assert.That(rows, Is.Empty);
    }

    [Test]
    public void SubmittedValuesExport_HasExpectedMetadata()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_sut.Id, Is.EqualTo(new Guid(Constants.ExportTypes.Excel)));
            Assert.That(_sut.Alias, Is.EqualTo("excelFileSubmittedValues"));
            Assert.That(_sut.FileExtension, Is.EqualTo("xlsx"));
            Assert.That(_sut.MimeType, Is.EqualTo("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
        });
    }

    [Test]
    public void DisplayValuesExport_HasExpectedMetadata()
    {
        var sut = CreateSut<ExcelDisplayValuesExport>();

        Assert.Multiple(() =>
        {
            Assert.That(sut.Id, Is.EqualTo(new Guid(Constants.ExportTypes.ExcelDisplayValues)));
            Assert.That(sut.Alias, Is.EqualTo("excelFileDisplayValues"));
            Assert.That(sut.FileExtension, Is.EqualTo("xlsx"));
        });
    }

    private static T CreateSut<T>()
        where T : ExcelExportTypeBase
        => (T)Activator.CreateInstance(
            typeof(T),
            Mock.Of<IFormRecordSearcher>(),
            Mock.Of<IFormService>(),
            Mock.Of<IFieldTypeStorage>(),
            Mock.Of<IPrevalueSourceService>(),
            Mock.Of<IFieldPreValueSourceTypeService>())!;

    private static async Task<List<IDictionary<string, object>>> ExportAndReadBack(ExcelExport sut, bool useHeaderRow = true)
    {
        var path = Path.Combine(Path.GetTempPath(), $"forms-excel-test-{Guid.NewGuid():N}.xlsx");
        try
        {
            await sut.ExportToFileAsync(Guid.NewGuid(), new RecordExportFilter(), path);
            return MiniExcel.Query(path, useHeaderRow: useHeaderRow)
                .Cast<IDictionary<string, object>>()
                .ToList();
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private static EntrySearchResultCollection BuildCollection(string[] headers, params object?[][] rows)
        => new()
        {
            Schema = headers.Select(h => new EntrySearchResultSchema { Name = h }).ToList(),
            Results = rows.Select(r => new EntrySearchResult
            {
                Fields = r.Select(v => new EntrySearchResult.FieldData { Value = v }).ToList(),
            }).ToList(),
        };
}
