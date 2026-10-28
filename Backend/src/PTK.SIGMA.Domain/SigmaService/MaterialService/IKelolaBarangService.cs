using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PTK.SIGMA.Domain.SigmaService;
using PTK.SIGMA.Domain.Entities;

namespace PTK.SIGMA.Domain.SigmaService;

public interface IKelolaBarangService
{
    Task<List<TransaksiHistoryItemResponseDto>> GetIndexTransactionsAsync();
    Task<List<Kategori>> GetKategorisAsync();
    Task<Kategori?> GetKategoriByIdAsync(int id);
    Task AddKategoriAsync(Kategori model);
    Task UpdateKategoriAsync(Kategori model);
    Task DeleteKategoriAsync(Kategori kategori);
    Task<List<Lokasi>> GetLokasisAsync();
    Task<Lokasi?> GetLokasiByIdAsync(int id);
    Task<(bool Success, string Message)> TambahLokasiAsync(string namaLokasi);
    Task HapusLokasiAsync(Lokasi lokasi);
    Task<List<Rak>> GetRaksAsync();
    Task<Rak?> GetRakByIdAsync(int id);
    Task<(bool Success, string Message)> TambahRakAsync(string namaRak);
    Task HapusRakAsync(Rak rak);
    Task<bool> ApproveMasukAsync(int id);
    Task<bool> TolakMasukAsync(int id);
    Task<(bool Success, string Message)> ApproveKeluarAsync(int id);
    Task<bool> TolakKeluarAsync(int id);
    Task SubmitMasukAsync(BarangMasuk model);
    Task SubmitKeluarAsync(BarangKeluar model);
    Task<object> GetTransactionOptionsAsync();
    byte[] GeneratePdfReport(DateTime start, DateTime end);
    byte[] GenerateExcelReport(DateTime start, DateTime end);
}
