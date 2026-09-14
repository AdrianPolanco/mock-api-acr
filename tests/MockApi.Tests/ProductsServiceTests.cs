using FluentAssertions;
using MockApi.Api.Contracts;
using MockApi.Api.Data;
using MockApi.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MockApi.Tests;

public class ProductsServiceTests
{
    private static ProductsService CreateSut(out MockApiDbContext dbContext)
    {
        var options = new DbContextOptionsBuilder<MockApiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        dbContext = new MockApiDbContext(options);
        return new ProductsService(dbContext);
    }

    [Fact]
    public async Task GetAllAsync_NoProducts_ReturnsEmptyList()
    {
        var sut = CreateSut(out _);

        var result = await sut.GetAllAsync(CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsSuccessAndPersistsProduct()
    {
        var sut = CreateSut(out var dbContext);
        var request = new CreateProductRequest("Widget", 9.99m, 10);

        var result = await sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Widget");
        dbContext.Products.Should().ContainSingle(p => p.Name == "Widget");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_NameMissing_ReturnsFailure(string invalidName)
    {
        var sut = CreateSut(out _);
        var request = new CreateProductRequest(invalidName, 9.99m, 10);

        var result = await sut.CreateAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingProduct_ReturnsSuccess()
    {
        var sut = CreateSut(out _);
        var created = await sut.CreateAsync(new CreateProductRequest("Widget", 9.99m, 10), CancellationToken.None);

        var result = await sut.GetByIdAsync(created.Value!.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(created.Value.Id);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsFailure()
    {
        var sut = CreateSut(out _);

        var result = await sut.GetByIdAsync(999, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_ExistingProduct_UpdatesFields()
    {
        var sut = CreateSut(out _);
        var created = await sut.CreateAsync(new CreateProductRequest("Widget", 9.99m, 10), CancellationToken.None);
        var update = new UpdateProductRequest("Widget v2", 14.99m, 5);

        var result = await sut.UpdateAsync(created.Value!.Id, update, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Widget v2");
        result.Value.Price.Should().Be(14.99m);
        result.Value.Stock.Should().Be(5);
    }

    [Fact]
    public async Task UpdateAsync_UnknownId_ReturnsFailure()
    {
        var sut = CreateSut(out _);

        var result = await sut.UpdateAsync(999, new UpdateProductRequest("Widget", 9.99m, 10), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ExistingProduct_RemovesIt()
    {
        var sut = CreateSut(out var dbContext);
        var created = await sut.CreateAsync(new CreateProductRequest("Widget", 9.99m, 10), CancellationToken.None);

        var result = await sut.DeleteAsync(created.Value!.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        dbContext.Products.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_UnknownId_ReturnsFailure()
    {
        var sut = CreateSut(out _);

        var result = await sut.DeleteAsync(999, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
