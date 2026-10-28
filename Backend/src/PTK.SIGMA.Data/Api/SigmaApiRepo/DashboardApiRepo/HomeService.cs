using System;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTK.SIGMA.Domain.SigmaService;
using PTK.SIGMA.Data.Context;

namespace PTK.SIGMA.Data.Api.SigmaApiRepo;

public sealed class HomeService : IHomeService
{
    private readonly AppDbContext _context;

    public HomeService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<object> GetSummaryAsync(
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        // The summary deliberately selects only the small fields needed by the
        // KPI cards/charts. Images, serial numbers, notes and navigation
        // entities are not loaded for every inventory row.
        var kategoriList = await _context.Kategoris
            .AsNoTracking()
            .OrderBy(k => k.NamaKategori)
            .Select(k => k.NamaKategori)
            .ToListAsync(cancellationToken);

        var rawLogs = await (
            from log in _context.ActivityLogs.AsNoTracking()
            join user in _context.Users.AsNoTracking()
                on log.Username equals user.Username into userGroup
            from user in userGroup.DefaultIfEmpty()
            orderby log.Waktu descending
            select new
            {
                log.Id,
                log.Username,
                UserRole = user == null ? null : user.Role.ToString(),
                NamaLengkap = user == null ? null : user.NamaLengkap,
                log.Aksi,
                log.Target,
                log.Detail,
                log.Waktu,
                log.IsRead
            })
            .Take(10)
            .ToListAsync(cancellationToken);

        var logs = rawLogs.Select(l =>
        {
            var actorDisplay = l.UserRole == "Admin"
                ? "Admin"
                : !string.IsNullOrWhiteSpace(l.NamaLengkap)
                    ? l.NamaLengkap
                    : (l.Username ?? "System");

            var cleanedDetail = l.Detail ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(l.Username))
            {
                cleanedDetail = cleanedDetail.Replace("Staff " + l.Username, actorDisplay);
            }

            return new
            {
                l.Id,
                l.Username,
                ActorDisplay = actorDisplay,
                Role = l.UserRole,
                l.Aksi,
                l.Target,
                Detail = cleanedDetail,
                l.Waktu,
                l.IsRead
            };
        }).ToList();

        var allActiveInventory = await _context.DataPerangkat
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .Select(p => new
            {
                p.Kategori,
                p.Kondisi,
                p.Jumlah,
                p.CreatedAt,
                p.IsVerified
            })
            .ToListAsync(cancellationToken);

        var verifiedInventory = allActiveInventory.Where(p => p.IsVerified).ToList();
        var totalInventoryUnits = verifiedInventory.Sum(p => p.Jumlah);
        var totalInventoryCategories = verifiedInventory
            .Select(p => string.IsNullOrEmpty(p.Kategori) ? "Perangkat ICT Operation" : p.Kategori)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var criticalInventoryItems = verifiedInventory.Count(p => IsDamaged(p.Kondisi));

        var tonerItems = verifiedInventory
            .Where(p => p.Kategori == "Tinta / Toner / Cartridge")
            .ToList();
        var totalTonerUnits = tonerItems.Sum(p => p.Jumlah);
        var totalJenisToner = tonerItems.Count;
        var tonerKritis = tonerItems.Count(p => p.Jumlah < 5 || IsDamaged(p.Kondisi));

        var goodUnits = allActiveInventory.Where(p => IsGood(p.Kondisi)).Sum(p => p.Jumlah);
        var damagedUnits = allActiveInventory.Where(p => IsDamaged(p.Kondisi)).Sum(p => p.Jumlah);
        var pendingVerificationUnits = allActiveInventory.Where(p => !p.IsVerified).Sum(p => p.Jumlah);

        var activeLoans = await _context.Peminjamans
            .AsNoTracking()
            .Where(p => p.Status == "Disetujui" &&
                        (p.StatusPeminjaman == "Dipinjam" || p.StatusPeminjaman == "Dikeluarkan"))
            .Select(p => p.Jumlah)
            .ToListAsync(cancellationToken);
        var activeBorrowedTransactions = activeLoans.Count;
        var borrowedUnits = activeLoans.Sum();

        var categoryDistribution = verifiedInventory
            .GroupBy(p => string.IsNullOrWhiteSpace(p.Kategori) ? "Perangkat ICT Operation" : p.Kategori)
            .Select(g => new
            {
                name = g.Key,
                total = g.Sum(p => p.Jumlah)
            })
            .OrderByDescending(x => x.total)
            .ThenBy(x => x.name)
            .ToList();

        var trendStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-11);
        var trendSource = verifiedInventory
            .Where(p => p.CreatedAt >= trendStart)
            .ToList();
        var monthlyTrend = Enumerable.Range(0, 12)
            .Select(offset =>
            {
                var month = trendStart.AddMonths(offset);
                var rows = trendSource.Where(x => x.CreatedAt.Year == month.Year && x.CreatedAt.Month == month.Month);
                return new
                {
                    period = month.ToString("yyyy-MM"),
                    label = month.ToString("MMM", CultureInfo.GetCultureInfo("id-ID")),
                    year = month.Year,
                    total = rows.Sum(x => x.Jumlah),
                    good = rows.Where(x => IsGood(x.Kondisi)).Sum(x => x.Jumlah),
                    damaged = rows.Where(x => IsDamaged(x.Kondisi)).Sum(x => x.Jumlah)
                };
            })
            .ToList();

        return new
        {
            isAdmin,
            totalUnits = totalInventoryUnits,
            totalCategories = totalInventoryCategories,
            criticalItems = criticalInventoryItems,
            tonerUnits = totalTonerUnits,
            tonerItems = totalJenisToner,
            tonerCritical = tonerKritis,
            totalToner = totalTonerUnits,
            lowToner = tonerKritis,
            tonerStats = new
            {
                total = totalTonerUnits,
                itemTypes = totalJenisToner,
                lowStock = tonerKritis,
                threshold = 5
            },
            borrowedUnits,
            statusStats = new
            {
                goodUnits,
                damagedUnits,
                borrowedUnits,
                borrowedTransactions = activeBorrowedTransactions,
                pendingVerificationUnits
            },
            categoryDistribution,
            monthlyTrend,
            kategoriList,
            logs
        };
    }

    public async Task<object> GetDashboardAsync(
        string? search,
        string? category,
        string? filter,
        int page,
        int pageSize,
        string? sort,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        search ??= string.Empty;
        category ??= string.Empty;
        filter ??= string.Empty;
        sort = string.IsNullOrWhiteSpace(sort) ? "terbaru" : sort.Trim().ToLowerInvariant();
        page = page < 1 ? 1 : page;
        pageSize = pageSize is 8 or 16 or 24 ? pageSize : 8;

        var kategoriList = await _context.Kategoris
            .AsNoTracking()
            .Select(k => k.NamaKategori)
            .ToListAsync(cancellationToken);


        var rawLogs = await (
            from log in _context.ActivityLogs.AsNoTracking()
            join user in _context.Users.AsNoTracking()
            on log.Username equals user.Username into userGroup
            from user in userGroup.DefaultIfEmpty()
            orderby log.Waktu descending
            select new
            {
                log.Id,
                log.Username,
                UserRole = user == null ? null : user.Role.ToString(),
                NamaLengkap = user == null ? null : user.NamaLengkap,
                log.Aksi,
                log.Target,
                log.Detail,
                log.Waktu,
                log.IsRead
            }
        )
        .Take(10)
        .ToListAsync(cancellationToken);


        var logs = rawLogs.Select(l =>
        {
            var actorDisplay = l.UserRole == "Admin"
                ? "Admin"
                : !string.IsNullOrWhiteSpace(l.NamaLengkap)
                    ? l.NamaLengkap
                    : (l.Username ?? "System");

            var cleanedDetail = l.Detail ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(l.Username))
            {
                cleanedDetail = cleanedDetail.Replace("Staff " + l.Username, actorDisplay);
            }

            return new
            {
                Id = l.Id,
                Username = l.Username,
                ActorDisplay = actorDisplay,
                Role = l.UserRole,
                Aksi = l.Aksi,
                Target = l.Target,
                Detail = cleanedDetail,
                Waktu = l.Waktu,
                IsRead = l.IsRead
            };
        }).ToList();

        // Load the small inventory set once and derive dashboard aggregates in memory.
        // This avoids repeating a long sequence of database round-trips on refresh.
        var allActiveInventory = await _context.DataPerangkat
            .AsNoTracking()
            .Include(p => p.Vendor)
            .Include(p => p.Lokasi)
            .Where(p => !p.IsDeleted)
            .ToListAsync(cancellationToken);

        var verifiedInventory = allActiveInventory.Where(p => p.IsVerified).ToList();
        var totalInventoryUnits = verifiedInventory.Sum(p => p.Jumlah);
        var totalInventoryCategories = verifiedInventory
            .Select(p => string.IsNullOrEmpty(p.Kategori) ? "Perangkat ICT Operation" : p.Kategori)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var criticalInventoryItems = verifiedInventory.Count(p => IsDamaged(p.Kondisi));

        var tonerItems = verifiedInventory
            .Where(p => p.Kategori == "Tinta / Toner / Cartridge")
            .ToList();
        var totalTonerUnits = tonerItems.Sum(p => p.Jumlah);
        var totalJenisToner = tonerItems.Count;
        var tonerKritis = tonerItems.Count(p => p.Jumlah < 5 || IsDamaged(p.Kondisi));

        var goodUnits = allActiveInventory.Where(p => IsGood(p.Kondisi)).Sum(p => p.Jumlah);
        var damagedUnits = allActiveInventory.Where(p => IsDamaged(p.Kondisi)).Sum(p => p.Jumlah);
        var pendingVerificationUnits = allActiveInventory.Where(p => !p.IsVerified).Sum(p => p.Jumlah);

        var activeLoans = await _context.Peminjamans
            .AsNoTracking()
            .Where(p => p.Status == "Disetujui" &&
                        (p.StatusPeminjaman == "Dipinjam" || p.StatusPeminjaman == "Dikeluarkan"))
            .Select(p => p.Jumlah)
            .ToListAsync(cancellationToken);
        var activeBorrowedTransactions = activeLoans.Count;
        var borrowedUnits = activeLoans.Sum();

        var categoryDistribution = verifiedInventory
            .GroupBy(p => string.IsNullOrWhiteSpace(p.Kategori) ? "Perangkat ICT Operation" : p.Kategori)
            .Select(g => new
            {
                name = g.Key,
                total = g.Sum(p => p.Jumlah)
            })
            .OrderByDescending(x => x.total)
            .ThenBy(x => x.name)
            .ToList();

        var trendStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-11);
        var trendSource = verifiedInventory
            .Where(p => p.CreatedAt >= trendStart)
            .Select(p => new { p.CreatedAt, p.Jumlah, p.Kondisi })
            .ToList();

        var monthlyTrend = Enumerable.Range(0, 12)
            .Select(offset =>
            {
                var month = trendStart.AddMonths(offset);
                var rows = trendSource.Where(x => x.CreatedAt.Year == month.Year && x.CreatedAt.Month == month.Month);
                var total = rows.Sum(x => x.Jumlah);
                var good = rows
                    .Where(x => !string.IsNullOrWhiteSpace(x.Kondisi) && x.Kondisi.Contains("baik", StringComparison.OrdinalIgnoreCase) && !x.Kondisi.Contains("tidak", StringComparison.OrdinalIgnoreCase))
                    .Sum(x => x.Jumlah);
                var damaged = rows
                    .Where(x => !string.IsNullOrWhiteSpace(x.Kondisi) && x.Kondisi.Contains("rusak", StringComparison.OrdinalIgnoreCase))
                    .Sum(x => x.Jumlah);

                return new
                {
                    period = month.ToString("yyyy-MM"),
                    label = month.ToString("MMM", CultureInfo.GetCultureInfo("id-ID")),
                    year = month.Year,
                    total,
                    good,
                    damaged
                };
            })
            .ToList();

        var perangkatQuery = verifiedInventory.AsEnumerable();

        if (!string.IsNullOrEmpty(filter))
        {
            if (filter.Equals("rusak", StringComparison.OrdinalIgnoreCase))
                perangkatQuery = perangkatQuery.Where(p => IsDamaged(p.Kondisi));
            else if (filter.Equals("stok_menipis", StringComparison.OrdinalIgnoreCase) ||
                     filter.Equals("low-stock", StringComparison.OrdinalIgnoreCase))
                perangkatQuery = perangkatQuery.Where(p => p.Kategori == "Tinta / Toner / Cartridge" && p.Jumlah < 5);
        }

        if (!string.IsNullOrEmpty(category))
        {
            perangkatQuery = category == "Perangkat ICT Operation"
                ? perangkatQuery.Where(p => string.IsNullOrEmpty(p.Kategori) || p.Kategori == category)
                : perangkatQuery.Where(p => p.Kategori == category);
        }

        if (!string.IsNullOrEmpty(search))
        {
            perangkatQuery = perangkatQuery.Where(p =>
                (!string.IsNullOrEmpty(p.Nama) && p.Nama.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(p.SnLaptop) && p.SnLaptop.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(p.Keterangan) && p.Keterangan.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (p.Lokasi != null && !string.IsNullOrEmpty(p.Lokasi.NamaLokasi) && p.Lokasi.NamaLokasi.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        var totalItems = perangkatQuery.Count();

        var orderedQuery = sort switch
        {
            "terlama" => perangkatQuery.OrderBy(p => p.CreatedAt).ThenBy(p => p.Id),
            "nama-az" => perangkatQuery.OrderBy(p => p.Nama).ThenByDescending(p => p.Id),
            "nama-za" => perangkatQuery.OrderByDescending(p => p.Nama).ThenByDescending(p => p.Id),
            "stok-tertinggi" => perangkatQuery.OrderByDescending(p => p.Jumlah).ThenByDescending(p => p.Id),
            "stok-terendah" => perangkatQuery.OrderBy(p => p.Jumlah).ThenByDescending(p => p.Id),
            _ => perangkatQuery.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
        };

        var items = orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var itemDtos = items.Select(p => new PerangkatResponseDto
        {
            Id = p.Id,
            ProductId = p.ProductId, // ✨ DITAMBAHKAN ✨
            Nama = p.Nama,
            Kategori = p.Kategori,
            StatusSewa = p.StatusSewa,
            TypeLaptop = p.TypeLaptop,
            Kondisi = p.Kondisi,
            SnLaptop = p.SnLaptop,
            Jumlah = p.Jumlah,
            LokasiRak = p.LokasiRak, // ✨ DITAMBAHKAN ✨
            LokasiId = p.LokasiId,
            NamaLokasi = p.Lokasi?.NamaLokasi ?? "-",
            VendorId = p.VendorId,
            NamaVendor = p.Vendor?.NamaVendor ?? "-",
            Dokumentasi = p.Dokumentasi,
            Keterangan = p.Keterangan,
            IsVerified = p.IsVerified,
            IsDeleted = p.IsDeleted,
            CreatedAt = p.CreatedAt,
            CreatedBy = p.CreatedBy,
            UpdatedAt = p.UpdatedAt,
            UpdatedBy = p.UpdatedBy
        }).ToList();

        return new
        {
            isAdmin,
            totalUnits = totalInventoryUnits,
            totalCategories = totalInventoryCategories,
            criticalItems = criticalInventoryItems,
            // Data status toner adalah hasil agregasi langsung dari DataPerangkat
            // dengan kategori Tinta / Toner / Cartridge yang sudah terverifikasi.
            tonerUnits = totalTonerUnits,
            tonerItems = totalJenisToner,
            tonerCritical = tonerKritis,
            totalToner = totalTonerUnits,
            lowToner = tonerKritis,
            tonerStats = new
            {
                total = totalTonerUnits,
                itemTypes = totalJenisToner,
                lowStock = tonerKritis,
                threshold = 5
            },
            borrowedUnits,
            statusStats = new
            {
                goodUnits,
                damagedUnits,
                borrowedUnits,
                borrowedTransactions = activeBorrowedTransactions,
                pendingVerificationUnits
            },
            categoryDistribution,
            monthlyTrend,
            kategoriList,
            logs,
            pagination = new
            {
                currentPage = page,
                totalPages = (int)Math.Ceiling(decimal.Divide(totalItems, pageSize)),
                totalItems,
                pageSize,
                sort
            },
            data = itemDtos
        };
    }

    public async Task<object> GetInventoryAsync(
        string? search,
        string? category,
        string? filter,
        int page,
        int pageSize,
        string? sort,
        CancellationToken cancellationToken = default)
    {
        search = search?.Trim() ?? string.Empty;
        category = category?.Trim() ?? string.Empty;
        filter = filter?.Trim() ?? string.Empty;
        sort = string.IsNullOrWhiteSpace(sort) ? "terbaru" : sort.Trim().ToLowerInvariant();
        page = Math.Max(1, page);
        pageSize = pageSize is 8 or 16 or 24 ? pageSize : 8;

        var query = _context.DataPerangkat
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.IsVerified);

        if (filter.Equals("rusak", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(p => p.Kondisi != null &&
                                     p.Kondisi.ToLower().Contains("rusak") &&
                                     !p.Kondisi.ToLower().Contains("baik"));
        }
        else if (filter.Equals("stok_menipis", StringComparison.OrdinalIgnoreCase) ||
                 filter.Equals("low-stock", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(p => p.Kategori == "Tinta / Toner / Cartridge" && p.Jumlah < 5);
        }

        if (!string.IsNullOrEmpty(category))
        {
            query = category == "Perangkat ICT Operation"
                ? query.Where(p => string.IsNullOrEmpty(p.Kategori) || p.Kategori == category)
                : query.Where(p => p.Kategori == category);
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(p =>
                (!string.IsNullOrEmpty(p.Nama) && p.Nama.Contains(search)) ||
                (!string.IsNullOrEmpty(p.SnLaptop) && p.SnLaptop.Contains(search)) ||
                (!string.IsNullOrEmpty(p.Keterangan) && p.Keterangan.Contains(search)) ||
                (p.Lokasi != null && !string.IsNullOrEmpty(p.Lokasi.NamaLokasi) && p.Lokasi.NamaLokasi.Contains(search)));
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(decimal.Divide(totalItems, pageSize)));
        page = Math.Min(page, totalPages);

        var orderedQuery = sort switch
        {
            "terlama" => query.OrderBy(p => p.CreatedAt).ThenBy(p => p.Id),
            "nama-az" => query.OrderBy(p => p.Nama).ThenByDescending(p => p.Id),
            "nama-za" => query.OrderByDescending(p => p.Nama).ThenByDescending(p => p.Id),
            "stok-tertinggi" => query.OrderByDescending(p => p.Jumlah).ThenByDescending(p => p.Id),
            "stok-terendah" => query.OrderBy(p => p.Jumlah).ThenByDescending(p => p.Id),
            _ => query.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
        };

        var items = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PerangkatResponseDto
            {
                Id = p.Id,
                ProductId = p.ProductId,
                Nama = p.Nama,
                Kategori = p.Kategori,
                StatusSewa = p.StatusSewa,
                TypeLaptop = p.TypeLaptop,
                Kondisi = p.Kondisi,
                SnLaptop = p.SnLaptop,
                Jumlah = p.Jumlah,
                LokasiRak = p.LokasiRak,
                LokasiId = p.LokasiId,
                NamaLokasi = p.Lokasi != null ? p.Lokasi.NamaLokasi ?? "-" : "-",
                VendorId = p.VendorId,
                NamaVendor = p.Vendor != null ? p.Vendor.NamaVendor ?? "-" : "-",
                Dokumentasi = p.Dokumentasi,
                Keterangan = p.Keterangan,
                IsVerified = p.IsVerified,
                IsDeleted = p.IsDeleted,
                CreatedAt = p.CreatedAt,
                CreatedBy = p.CreatedBy,
                UpdatedAt = p.UpdatedAt,
                UpdatedBy = p.UpdatedBy
            })
            .ToListAsync(cancellationToken);

        return new
        {
            pagination = new
            {
                currentPage = page,
                totalPages,
                totalItems,
                pageSize,
                sort
            },
            data = items
        };
    }

    private static bool IsDamaged(string? condition)
        => !string.IsNullOrWhiteSpace(condition) &&
           condition.Contains("rusak", StringComparison.OrdinalIgnoreCase) &&
           !condition.Contains("baik", StringComparison.OrdinalIgnoreCase);

    private static bool IsGood(string? condition)
        => !string.IsNullOrWhiteSpace(condition) &&
           condition.Contains("baik", StringComparison.OrdinalIgnoreCase) &&
           !condition.Contains("tidak", StringComparison.OrdinalIgnoreCase);
}
