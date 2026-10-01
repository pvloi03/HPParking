using FluentAssertions;
using HPParking.Api.DTOs.Common;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class PagedResultTests
    {
        [Fact]
        public void PagedResult_ShouldSetMetadataCorrectly_WhenItemsAreEmpty()
        {
            // Arrange: 0 items, pageIndex = 1, pageSize = 15, totalCount = 0
            var emptyItems = new List<string>();
            int pageIndex = 1;
            int pageSize = 15;
            long totalCount = 0;

            // Act
            var pagedResult = new PagedResult<string>(emptyItems, pageIndex, pageSize, totalCount);

            // Assert
            pagedResult.Items.Should().BeEmpty();
            pagedResult.Pagination.PageIndex.Should().Be(1);
            pagedResult.Pagination.PageSize.Should().Be(15);
            pagedResult.Pagination.TotalCount.Should().Be(0);
            pagedResult.Pagination.TotalPages.Should().Be(0);
            pagedResult.Pagination.HasPreviousPage.Should().BeFalse();
            pagedResult.Pagination.HasNextPage.Should().BeFalse();
        }

        [Fact]
        public void PagedResult_ShouldCalculateTotalPages_WhenItemsExist()
        {
            // Arrange: 15 items, pageIndex = 1, pageSize = 15, totalCount = 42
            var items = new List<int> { 1, 2, 3 };
            int pageIndex = 1;
            int pageSize = 15;
            long totalCount = 42;

            // Act
            var pagedResult = new PagedResult<int>(items, pageIndex, pageSize, totalCount);

            // Assert
            pagedResult.Pagination.PageIndex.Should().Be(1);
            pagedResult.Pagination.PageSize.Should().Be(15);
            pagedResult.Pagination.TotalCount.Should().Be(42);
            pagedResult.Pagination.TotalPages.Should().Be(3); // Math.Ceiling(42 / 15) = 3
            pagedResult.Pagination.HasPreviousPage.Should().BeFalse();
            pagedResult.Pagination.HasNextPage.Should().BeTrue();
        }
    }
}
