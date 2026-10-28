using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PTK.SIGMA.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialSqlServer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Aksi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Target = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Detail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Waktu = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Kategoris",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NamaKategori = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kategoris", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lokasis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NamaLokasi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Keterangan = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lokasis", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Raks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NamaRak = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Raks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NamaLengkap = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vendors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NamaVendor = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vendors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataPerangkat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nama = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Kategori = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TypeLaptop = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProductId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SnLaptop = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StatusSewa = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VendorId = table.Column<int>(type: "int", nullable: true),
                    LokasiId = table.Column<int>(type: "int", nullable: true),
                    Jumlah = table.Column<int>(type: "int", nullable: false),
                    Keterangan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Kondisi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Dokumentasi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LokasiRak = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LokasiBarang = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataPerangkat", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataPerangkat_Lokasis_LokasiId",
                        column: x => x.LokasiId,
                        principalTable: "Lokasis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DataPerangkat_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "BarangKeluars",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TanggalKeluar = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PerangkatId = table.Column<int>(type: "int", nullable: false),
                    Jumlah = table.Column<int>(type: "int", nullable: false),
                    Penerima = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Keterangan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BarangKeluars", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BarangKeluars_DataPerangkat_PerangkatId",
                        column: x => x.PerangkatId,
                        principalTable: "DataPerangkat",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BarangMasuks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TanggalMasuk = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PerangkatId = table.Column<int>(type: "int", nullable: false),
                    Jumlah = table.Column<int>(type: "int", nullable: false),
                    VendorId = table.Column<int>(type: "int", nullable: true),
                    Keterangan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BarangMasuks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BarangMasuks_DataPerangkat_PerangkatId",
                        column: x => x.PerangkatId,
                        principalTable: "DataPerangkat",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BarangMasuks_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Peminjamans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NamaPeminjam = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Jabatan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Fungsi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PerangkatId = table.Column<int>(type: "int", nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Jumlah = table.Column<int>(type: "int", nullable: false),
                    LokasiId = table.Column<int>(type: "int", nullable: true),
                    TanggalPinjam = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TanggalKembali = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StatusPeminjaman = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Keterangan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NoBa = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Peminjamans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Peminjamans_DataPerangkat_PerangkatId",
                        column: x => x.PerangkatId,
                        principalTable: "DataPerangkat",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Peminjamans_Lokasis_LokasiId",
                        column: x => x.LokasiId,
                        principalTable: "Lokasis",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StockOpnames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PerangkatId = table.Column<int>(type: "int", nullable: false),
                    JumlahSistem = table.Column<int>(type: "int", nullable: false),
                    JumlahFisik = table.Column<int>(type: "int", nullable: false),
                    KondisiFisik = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Keterangan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FotoOpname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TanggalOpname = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockOpnames", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockOpnames_DataPerangkat_PerangkatId",
                        column: x => x.PerangkatId,
                        principalTable: "DataPerangkat",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Kategoris",
                columns: new[] { "Id", "NamaKategori" },
                values: new object[,]
                {
                    { 1, "Laptop SM5" },
                    { 2, "Laptop Non SM5" },
                    { 3, "Tinta / Toner / Cartridge" },
                    { 4, "Perangkat ICT Operation" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BarangKeluars_PerangkatId",
                table: "BarangKeluars",
                column: "PerangkatId");

            migrationBuilder.CreateIndex(
                name: "IX_BarangMasuks_PerangkatId",
                table: "BarangMasuks",
                column: "PerangkatId");

            migrationBuilder.CreateIndex(
                name: "IX_BarangMasuks_VendorId",
                table: "BarangMasuks",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_DataPerangkat_LokasiId",
                table: "DataPerangkat",
                column: "LokasiId");

            migrationBuilder.CreateIndex(
                name: "IX_DataPerangkat_VendorId",
                table: "DataPerangkat",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_Peminjamans_LokasiId",
                table: "Peminjamans",
                column: "LokasiId");

            migrationBuilder.CreateIndex(
                name: "IX_Peminjamans_PerangkatId",
                table: "Peminjamans",
                column: "PerangkatId");

            migrationBuilder.CreateIndex(
                name: "IX_StockOpnames_PerangkatId",
                table: "StockOpnames",
                column: "PerangkatId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityLogs");

            migrationBuilder.DropTable(
                name: "BarangKeluars");

            migrationBuilder.DropTable(
                name: "BarangMasuks");

            migrationBuilder.DropTable(
                name: "Kategoris");

            migrationBuilder.DropTable(
                name: "Peminjamans");

            migrationBuilder.DropTable(
                name: "Raks");

            migrationBuilder.DropTable(
                name: "StockOpnames");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "DataPerangkat");

            migrationBuilder.DropTable(
                name: "Lokasis");

            migrationBuilder.DropTable(
                name: "Vendors");
        }
    }
}
