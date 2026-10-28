using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PTK.SIGMA.Api.Controllers;
using PTK.SIGMA.Data.Context;
using PTK.SIGMA.Domain.Entities;
using PTK.SIGMA.Domain.SigmaService;
using Xunit;

namespace PTK.SIGMA.Api.Tests;

public sealed class BusinessRegressionTests
{
    [Theory]
    [InlineData(typeof(PeminjamanController), nameof(PeminjamanController.Edit))]
    [InlineData(typeof(PeminjamanController), nameof(PeminjamanController.Setujui))]
    [InlineData(typeof(PeminjamanController), nameof(PeminjamanController.Tolak))]
    [InlineData(typeof(PeminjamanController), nameof(PeminjamanController.DeleteConfirmed))]
    [InlineData(typeof(PerangkatController), nameof(PerangkatController.DeleteConfirmed))]
    [InlineData(typeof(PerangkatController), nameof(PerangkatController.Restore))]
    [InlineData(typeof(PerangkatController), nameof(PerangkatController.PermanentDelete))]
    [InlineData(typeof(PerangkatController), nameof(PerangkatController.SetujuiValidasi))]
    [InlineData(typeof(PerangkatController), nameof(PerangkatController.TolakValidasi))]
    [InlineData(typeof(VendorController), nameof(VendorController.Create))]
    [InlineData(typeof(VendorController), nameof(VendorController.Edit))]
    [InlineData(typeof(VendorController), nameof(VendorController.Delete))]
    [InlineData(typeof(OpnameController), nameof(OpnameController.DeleteConfirmed))]
    public void MutatingEndpointsRequireAdmin(Type controllerType, string actionName)
    {
        var action = controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(method => method.Name == actionName);

        var roles = action.GetCustomAttributes<AuthorizeAttribute>()
            .SelectMany(attribute => (attribute.Roles ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        Assert.Contains("Admin", roles, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void NewLoanStartsPendingAndDoesNotPretendToBeBorrowed()
    {
        var loan = new Peminjaman();

        Assert.Equal("Pending", loan.Status);
        Assert.Equal("Menunggu Verifikasi", loan.StatusPeminjaman);
    }

    [Theory]
    [InlineData("0001-01-01", "2026-10-04")]
    [InlineData("2026-10-05", "2026-10-04")]
    public async Task ExcelReportRejectsInvalidDateRanges(string startValue, string endValue)
    {
        var service = new RecordingPeminjamanService();
        var controller = new PeminjamanController(service);

        var result = await controller.DownloadExcel(
            DateTime.Parse(startValue, System.Globalization.CultureInfo.InvariantCulture),
            DateTime.Parse(endValue, System.Globalization.CultureInfo.InvariantCulture));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, service.ExcelGenerationCalls);
    }

    [Fact]
    public void InventorySchemaKeepsSearchColumnsBoundedAndIndexed()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=SigmaModelOnly;Trusted_Connection=True")
            .Options;
        using var context = new AppDbContext(options);
        var entity = context.Model.FindEntityType(typeof(Perangkat));

        Assert.NotNull(entity);
        Assert.Equal(256, entity!.FindProperty(nameof(Perangkat.Nama))!.GetMaxLength());
        Assert.Equal(128, entity.FindProperty(nameof(Perangkat.Kategori))!.GetMaxLength());
        Assert.Equal(256, entity.FindProperty(nameof(Perangkat.SnLaptop))!.GetMaxLength());
        Assert.Equal(2000, entity.FindProperty(nameof(Perangkat.Keterangan))!.GetMaxLength());

        var indexes = entity.GetIndexes()
            .Select(index => string.Join(",", index.Properties.Select(property => property.Name)))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("IsDeleted,IsVerified,CreatedAt", indexes);
        Assert.Contains("IsDeleted,IsVerified,Kategori", indexes);
        Assert.Contains("Nama", indexes);
        Assert.Contains("SnLaptop", indexes);
    }

    private sealed class RecordingPeminjamanService : IPeminjamanService
    {
        public int ExcelGenerationCalls { get; private set; }

        public Task<byte[]> GenerateExcelReportAsync(DateTime start, DateTime end)
        {
            ExcelGenerationCalls++;
            return Task.FromResult(Array.Empty<byte>());
        }

        public Task<object> GetIndexDataAsync(string? searchString, DateTime? startDate, DateTime? endDate, string? kategori, int? page) => throw new NotSupportedException();
        public Task<PeminjamanResponseDto?> GetByIdAsync(int id) => throw new NotSupportedException();
        public Task<object> GetCreateOptionsAsync() => throw new NotSupportedException();
        public Task<byte[]> GeneratePdfReportAsync(DateTime? startDate, DateTime? endDate) => throw new NotSupportedException();
        public Task<string> GenerateNextNoBaAsync(DateTime? tanggalPinjam = null) => throw new NotSupportedException();
        public Task<(bool Success, string Message, PeminjamanResponseDto? Data)> CreatePeminjamanAsync(PeminjamanCreateDto dto) => throw new NotSupportedException();
        public Task<(bool Success, string Message)> UpdatePeminjamanAsync(int id, PeminjamanCreateDto dto) => throw new NotSupportedException();
        public Task<bool> SetujuiAsync(int id) => throw new NotSupportedException();
        public Task<bool> TolakAsync(int id) => throw new NotSupportedException();
        public Task<bool> KembalikanAsync(int id) => throw new NotSupportedException();
        public Task<bool> DeleteAsync(int id) => throw new NotSupportedException();
    }
}
