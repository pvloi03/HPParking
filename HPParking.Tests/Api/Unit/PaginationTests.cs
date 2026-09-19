using System.Collections.Generic;
using FluentAssertions;
using HPParking.Api.DTOs.Common;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class PaginationTests
    {
        [Theory]
        [InlineData(0, 1)]
        [InlineData(-5, 1)]
        [InlineData(3, 3)]
        public void PaginationQuery_PageIndex_ShouldNeverBeLessThanOne(int input, int expected)
        {
            var query = new PaginationQuery { PageIndex = input };
            query.PageIndex.Should().Be(expected);
        }

        [Theory]
        [InlineData(0, 20)]      // defaults to 20 when < 1
        [InlineData(-10, 20)]    // defaults to 20 when < 1
        [InlineData(50, 50)]     // valid range
        [InlineData(150, 100)]   // clamped to MaxPageSize = 100
        public void PaginationQuery_PageSize_ShouldBeClampedBetweenMinAndMax(int input, int expected)
        {
            var query = new PaginationQuery { PageSize = input };
            query.PageSize.Should().Be(expected);
        }

        [Fact]
        public void PaginationQuery_Skip_ShouldCalculateCorrectly()
        {
            var query = new PaginationQuery { PageIndex = 3, PageSize = 25 };
            query.Skip.Should().Be(50); // (3 - 1) * 25
        }

        [Theory]
        [InlineData(100, 20, 5)]
        [InlineData(95, 20, 5)]
        [InlineData(105, 20, 6)]
        [InlineData(0, 20, 0)]
        public void PaginationMetadata_TotalPages_ShouldCalculateCorrectly(long totalCount, int pageSize, int expectedPages)
        {
            var metadata = new PaginationMetadata(1, pageSize, totalCount);
            metadata.TotalPages.Should().Be(expectedPages);
        }

        [Fact]
        public void PaginationMetadata_HasNextAndPreviousPage_ShouldBeAccurate()
        {
            // Page 1 of 5
            var p1 = new PaginationMetadata(1, 20, 100);
            p1.HasPreviousPage.Should().BeFalse();
            p1.HasNextPage.Should().BeTrue();

            // Page 3 of 5
            var p3 = new PaginationMetadata(3, 20, 100);
            p3.HasPreviousPage.Should().BeTrue();
            p3.HasNextPage.Should().BeTrue();

            // Page 5 of 5
            var p5 = new PaginationMetadata(5, 20, 100);
            p5.HasPreviousPage.Should().BeTrue();
            p5.HasNextPage.Should().BeFalse();
        }

        [Fact]
        public void PagedResult_ShouldWrapItemsAndPagination()
        {
            var items = new List<string> { "item1", "item2" };
            var result = new PagedResult<string>(items, 1, 10, 2);

            result.Items.Should().BeEquivalentTo(items);
            result.Pagination.TotalCount.Should().Be(2);
            result.Pagination.PageIndex.Should().Be(1);
            result.Pagination.PageSize.Should().Be(10);
            result.Pagination.TotalPages.Should().Be(1);
        }
    }
}
