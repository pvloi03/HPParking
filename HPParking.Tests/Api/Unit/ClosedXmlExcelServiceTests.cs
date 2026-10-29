using ClosedXML.Excel;
using FluentAssertions;
using HPParking.Api.Common.Excel;
using HPParking.Api.Services.Implementations;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class ClosedXmlExcelServiceTests
    {
        private readonly ClosedXmlExcelService _service = new(NullLogger<ClosedXmlExcelService>.Instance);

        private class SampleItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public decimal Amount { get; set; }
        }

        private class SampleProfile : ExcelProfile<SampleItem>
        {
            public SampleProfile()
            {
                Map(x => x.Id).ColumnName("ID").Order(1);
                Map(x => x.Name).ColumnName("Tên").Order(2);
                Map(x => x.Amount).ColumnName("Số tiền").Order(3).Format("#,##0");
            }
        }

        [Fact]
        public async Task WriteTwoSheetsAsync_CreatesWorkbookWithBothSheetsAndData()
        {
            // Arrange
            var items1 = new List<SampleItem>
            {
                new() { Id = 1, Name = "Item A", Amount = 10000 },
                new() { Id = 2, Name = "Item B", Amount = 20000 }
            };

            var items2 = new List<SampleItem>
            {
                new() { Id = 3, Name = "Item C", Amount = 30000 }
            };

            var profile = new SampleProfile();

            var sheet1 = new ExcelSheetData<SampleItem>("Sheet One", items1, profile, "BÁO CÁO 1");
            var sheet2 = new ExcelSheetData<SampleItem>("Sheet Two", items2, profile, "BÁO CÁO 2");

            // Act
            var bytes = await _service.WriteTwoSheetsAsync(sheet1, sheet2);

            // Assert
            bytes.Should().NotBeNull();
            bytes.Length.Should().BeGreaterThan(0);

            using var ms = new MemoryStream(bytes);
            using var workbook = new XLWorkbook(ms);

            workbook.Worksheets.Count.Should().Be(2);

            var ws1 = workbook.Worksheet("Sheet One");
            ws1.Should().NotBeNull();
            // Title banner starts at row 2 for title
            ws1.Cell(2, 1).GetString().Should().Be("BÁO CÁO 1");
            // Header at row 5
            ws1.Cell(5, 1).GetString().Should().Be("ID");
            ws1.Cell(5, 2).GetString().Should().Be("Tên");
            ws1.Cell(5, 3).GetString().Should().Be("Số tiền");
            // Data row 6 & 7
            ws1.Cell(6, 1).GetDouble().Should().Be(1);
            ws1.Cell(6, 2).GetString().Should().Be("Item A");
            ws1.Cell(7, 1).GetDouble().Should().Be(2);
            ws1.Cell(7, 2).GetString().Should().Be("Item B");

            var ws2 = workbook.Worksheet("Sheet Two");
            ws2.Should().NotBeNull();
            ws2.Cell(2, 1).GetString().Should().Be("BÁO CÁO 2");
            ws2.Cell(6, 1).GetDouble().Should().Be(3);
            ws2.Cell(6, 2).GetString().Should().Be("Item C");
        }

        [Fact]
        public async Task WriteTwoSheetsAsync_WhenCancelled_ThrowsOperationCanceled()
        {
            var profile = new SampleProfile();
            var sheet1 = new ExcelSheetData<SampleItem>("Sheet One", new List<SampleItem> { new() { Id = 1, Name = "A" } }, profile, "T1");
            var sheet2 = new ExcelSheetData<SampleItem>("Sheet Two", new List<SampleItem> { new() { Id = 2, Name = "B" } }, profile, "T2");

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var act = () => _service.WriteTwoSheetsAsync(sheet1, sheet2, cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task WriteAsync_WhenCancelled_ThrowsOperationCanceled()
        {
            var items = new List<SampleItem> { new() { Id = 1, Name = "A" } };

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var act = () => _service.WriteAsync(items, new SampleProfile(), cancellationToken: cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task ReadAsync_WhenCancelled_ThrowsOperationCanceled()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var act = () => _service.ReadAsync<SampleItem>(
                new MemoryStream(), new SampleProfile(), cancellationToken: cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }
    }
}
