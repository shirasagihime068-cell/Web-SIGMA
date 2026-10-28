(function () {
    'use strict';

    const exportedData = window.SIGMA_ACTUAL_DATA || {};
    const fallbackInventory = [
        { id: 1, nama: 'Laptop Dell Latitude 5440', kategori: 'Laptop', kondisi: 'Baik', jumlah: 7, snLaptop: 'DL-5440-001', namaLokasi: 'Rak A-01', keterangan: 'Perangkat operasional', createdAt: '2026-10-21T08:00:00Z' },
        { id: 2, nama: 'Laptop Lenovo ThinkPad E14', kategori: 'Laptop', kondisi: 'Baik', jumlah: 5, snLaptop: 'LN-E14-014', namaLokasi: 'Ruang ICT', keterangan: 'Unit kerja lapangan', createdAt: '2026-10-19T08:00:00Z' },
        { id: 3, nama: 'Monitor Dell P2422H', kategori: 'Monitor', kondisi: 'Baik', jumlah: 12, snLaptop: 'MON-P24-008', namaLokasi: 'Rak B-02', keterangan: 'Monitor 24 inci', createdAt: '2026-10-16T08:00:00Z' },
        { id: 4, nama: 'Printer Epson L5290', kategori: 'Printer', kondisi: 'Baik', jumlah: 4, snLaptop: 'PRN-L5290-04', namaLokasi: 'Ruang Administrasi', keterangan: 'Printer multifungsi', createdAt: '2026-10-13T08:00:00Z' },
        { id: 5, nama: 'Cisco Managed Switch 24 Port', kategori: 'Perangkat Jaringan', kondisi: 'Baik', jumlah: 8, snLaptop: 'NET-CS24-003', namaLokasi: 'Server Room', keterangan: 'Core network', createdAt: '2026-10-10T08:00:00Z' },
        { id: 6, nama: 'Access Point Ubiquiti U6', kategori: 'Perangkat Jaringan', kondisi: 'Baik', jumlah: 9, snLaptop: 'NET-U6-017', namaLokasi: 'Gedung Utama', keterangan: 'Akses jaringan nirkabel', createdAt: '2026-10-08T08:00:00Z' },
        { id: 7, nama: 'Toner HP 151A', kategori: 'Tinta / Toner / Cartridge', kondisi: 'Baik', jumlah: 20, snLaptop: 'TNR-151A', namaLokasi: 'Rak C-01', keterangan: 'Stok toner printer', createdAt: '2026-10-05T08:00:00Z' },
        { id: 8, nama: 'Printer HP LaserJet Pro', kategori: 'Printer', kondisi: 'Rusak', jumlah: 1, snLaptop: 'PRN-HP-009', namaLokasi: 'Ruang Perbaikan', keterangan: 'Menunggu perbaikan teknisi', createdAt: '2026-10-02T08:00:00Z' }
    ];
    const inventory = Array.isArray(exportedData.inventory) && exportedData.inventory.length
        ? exportedData.inventory
        : fallbackInventory;

    const categories = [...new Set(inventory.map(item => item.kategori))];
    const ok = data => new Response(JSON.stringify(data), {
        status: 200,
        headers: { 'Content-Type': 'application/json' }
    });

    function filterInventory(url) {
        const category = url.searchParams.get('category') || '';
        const search = (url.searchParams.get('search') || '').toLowerCase();
        const filter = url.searchParams.get('filter') || '';
        const pageSize = Number(url.searchParams.get('pageSize')) || 8;
        const currentPage = Math.max(1, Number(url.searchParams.get('page')) || 1);
        const sort = url.searchParams.get('sort') || 'terbaru';
        let rows = inventory.filter(item => {
            const matchesCategory = !category || item.kategori === category;
            const matchesSearch = !search || [item.nama, item.snLaptop, item.namaLokasi, item.keterangan]
                .some(value => String(value || '').toLowerCase().includes(search));
            const matchesFilter = filter === 'rusak'
                ? item.kondisi.toLowerCase().includes('rusak')
                : filter === 'low-stock'
                    ? item.jumlah <= 2
                    : true;
            return matchesCategory && matchesSearch && matchesFilter;
        });

        rows = [...rows].sort((left, right) => {
            if (sort === 'terlama') return new Date(left.createdAt || 0) - new Date(right.createdAt || 0);
            if (sort === 'nama-az') return String(left.nama || '').localeCompare(String(right.nama || ''), 'id');
            if (sort === 'nama-za') return String(right.nama || '').localeCompare(String(left.nama || ''), 'id');
            if (sort === 'stok-tertinggi') return Number(right.jumlah || 0) - Number(left.jumlah || 0);
            if (sort === 'stok-terendah') return Number(left.jumlah || 0) - Number(right.jumlah || 0);
            return new Date(right.createdAt || 0) - new Date(left.createdAt || 0);
        });

        const totalPages = Math.max(1, Math.ceil(rows.length / pageSize));
        const safePage = Math.min(currentPage, totalPages);
        const start = (safePage - 1) * pageSize;
        return {
            success: true,
            data: {
                data: rows.slice(start, start + pageSize).map(item => ({
                    ...item,
                    dokumentasi: item.dokumentasi
                        ? new URL(item.dokumentasi, window.location.href).href
                        : ''
                })),
                pagination: { currentPage: safePage, pageSize, totalItems: rows.length, totalPages }
            }
        };
    }

    function dashboardSummary() {
        if (exportedData.summary && typeof exportedData.summary === 'object') {
            return { success: true, data: exportedData.summary };
        }
        return {
            success: true,
            data: {
                totalUnits: 77,
                totalCategories: categories.length,
                criticalItems: 5,
                borrowedUnits: 9,
                tonerUnits: 20,
                tonerCritical: 2,
                tonerItems: 3,
                isAdmin: true,
                kategoriList: categories,
                categoryDistribution: [
                    { name: 'Laptop', total: 22 },
                    { name: 'Monitor', total: 18 },
                    { name: 'Printer', total: 14 },
                    { name: 'Perangkat Jaringan', total: 13 },
                    { name: 'Tinta / Toner / Cartridge', total: 10 }
                ],
                monthlyTrend: [
                    { label: 'Mei', year: 2026, total: 58, good: 51, damaged: 3 },
                    { label: 'Jun', year: 2026, total: 62, good: 54, damaged: 4 },
                    { label: 'Jul', year: 2026, total: 66, good: 57, damaged: 4 },
                    { label: 'Agu', year: 2026, total: 69, good: 59, damaged: 5 },
                    { label: 'Sep', year: 2026, total: 73, good: 61, damaged: 5 },
                    { label: 'Okt', year: 2026, total: 77, good: 63, damaged: 5 }
                ],
                logs: [
                    { waktu: '2026-10-28T08:30:00Z', actorDisplay: 'Administrator Demo', aksi: 'Memperbarui', target: ' inventaris', detail: 'Data perangkat berhasil diperbarui.' },
                    { waktu: '2026-10-27T04:20:00Z', actorDisplay: 'Petugas Gudang', aksi: 'Menambahkan', target: ' barang', detail: 'Barang baru dicatat ke inventaris.' },
                    { waktu: '2026-10-26T09:15:00Z', actorDisplay: 'Administrator Demo', aksi: 'Menyetujui', target: ' peminjaman', detail: 'Pengajuan peminjaman disetujui.' }
                ]
            }
        };
    }

    function route(url, init) {
        const path = url.pathname.toLowerCase();
        const method = String(init?.method || 'GET').toUpperCase();

        if (path.endsWith('/api/auth/login')) return { success: true, message: 'Login demo berhasil.' };
        if (path.endsWith('/api/auth/logout')) return { success: true };
        if (path.endsWith('/api/profile')) {
            return { success: true, data: { id: 'demo', username: 'admin.demo', namaLengkap: 'Administrator Demo', email: 'admin.demo@sigma.local', role: 'Admin' } };
        }
        if (path.endsWith('/api/home/summary')) return dashboardSummary();
        if (path.endsWith('/api/home/inventory')) return filterInventory(url);
        if (path.endsWith('/api/home/user-info')) return { success: true, data: { role: 'Admin', username: 'admin.demo' } };
        if (path.endsWith('/api/opname/reminder-status')) return { success: true, data: { shouldRemind: false, completedDevices: 77, totalDevices: 77, pendingDevices: 0, periodKey: '2026-10', periodLabel: 'Oktober 2026' } };
        if (path.endsWith('/api/activitylog/unread-count')) return { success: true, data: 0 };
        if (path.endsWith('/api/activitylog') || path.includes('/api/activitylog/')) return { success: true, data: [] };
        if (path.endsWith('/api/perangkat/arsip')) return { success: true, data: [] };
        if (path.endsWith('/api/perangkat/validasi')) return { success: true, data: [] };
        if (path.endsWith('/api/perangkat/cetak-laporan')) {
            return {
                success: true,
                data: inventory.map(item => ({
                    ...item,
                    dokumentasi: item.dokumentasi
                        ? new URL(item.dokumentasi, window.location.href).href
                        : ''
                }))
            };
        }
        if (path.endsWith('/api/kelolabarang')) return { success: true, data: { masuk: [], keluar: [] } };
        if (path.includes('/api/peminjaman/create-options')) return { success: true, data: { perangkat: inventory, kategori: categories, lokasi: [] } };
        if (path.endsWith('/api/peminjaman')) return { success: true, data: [], pagination: { totalItems: 0, totalPages: 1, currentPage: 1 } };
        if (path.endsWith('/api/opname')) return { success: true, data: [], pagination: { totalItems: 0, totalPages: 1, currentPage: 1 } };
        if (path.endsWith('/api/user')) return { success: true, data: [], pagination: { totalItems: 0, totalPages: 1, currentPage: 1 } };
        if (path.endsWith('/api/vendor')) return { success: true, data: [], pagination: { totalItems: 0, totalPages: 1, currentPage: 1 } };
        if (path.includes('/api/files/')) return null;

        return method === 'GET' ? { success: true, data: [] } : { success: true, message: 'Aksi demo berhasil disimulasikan.' };
    }

    const nativeFetch = window.fetch.bind(window);
    window.fetch = function (input, init) {
        const value = typeof input === 'string' ? input : input.url;
        const url = new URL(value, window.location.href);
        if (url.pathname.toLowerCase().includes('/api/')) {
            const payload = route(url, init);
            if (payload === null) return Promise.resolve(new Response('', { status: 404 }));
            return Promise.resolve(ok(payload));
        }
        return nativeFetch(input, init);
    };

    window.signalR = {
        HubConnectionBuilder: class {
            withUrl() { return this; }
            withAutomaticReconnect() { return this; }
            build() {
                return {
                    on() {},
                    onreconnecting() {},
                    onreconnected() {},
                    onclose() {},
                    start: () => Promise.resolve(),
                    stop: () => Promise.resolve()
                };
            }
        }
    };

    document.addEventListener('click', event => {
        const link = event.target.closest('a[data-static-disabled="true"], a[href^="/"]');
        if (!link) return;
        event.preventDefault();
        window.alert('Halaman tindakan ini memerlukan backend SIGMA. Tampilan GitHub Pages berjalan dalam mode demo.');
    });
})();
