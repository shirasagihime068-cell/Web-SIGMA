const inventory = [
    { code: 'ICT-LTP-001', name: 'Laptop Dell Latitude 5440', category: 'Laptop', location: 'Rak A-01', condition: 'Baik', status: 'Baik' },
    { code: 'ICT-LTP-014', name: 'Laptop Lenovo ThinkPad E14', category: 'Laptop', location: 'Ruang Operasional', condition: 'Baik', status: 'Dipinjam' },
    { code: 'ICT-MON-008', name: 'Monitor LG 24 Inch', category: 'Monitor', location: 'Rak B-02', condition: 'Baik', status: 'Baik' },
    { code: 'ICT-MON-011', name: 'Monitor Dell P2422H', category: 'Monitor', location: 'Ruang ICT', condition: 'Perlu servis', status: 'Perbaikan' },
    { code: 'ICT-PRN-004', name: 'Printer Epson L5290', category: 'Printer', location: 'Ruang Administrasi', condition: 'Baik', status: 'Baik' },
    { code: 'ICT-PRN-009', name: 'Printer HP LaserJet Pro', category: 'Printer', location: 'Rak C-01', condition: 'Perlu servis', status: 'Perbaikan' },
    { code: 'ICT-NET-003', name: 'Cisco Managed Switch 24 Port', category: 'Jaringan', location: 'Server Room', condition: 'Baik', status: 'Baik' },
    { code: 'ICT-NET-017', name: 'Access Point Ubiquiti U6', category: 'Jaringan', location: 'Gedung Utama', condition: 'Baik', status: 'Dipinjam' }
];

const rows = document.getElementById('inventoryRows');
const count = document.getElementById('resultCount');
const emptyState = document.getElementById('emptyState');
const searchInput = document.getElementById('searchInput');
const categoryFilter = document.getElementById('categoryFilter');
const statusFilter = document.getElementById('statusFilter');

function safeText(value) {
    return String(value).replace(/[&<>'"]/g, character => ({
        '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;'
    })[character]);
}

function statusClass(status) {
    return status.toLowerCase().replaceAll(' ', '-');
}

function renderInventory() {
    const query = searchInput.value.trim().toLowerCase();
    const category = categoryFilter.value;
    const status = statusFilter.value;

    const filtered = inventory.filter(item => {
        const matchesQuery = !query || [item.code, item.name, item.location]
            .some(value => value.toLowerCase().includes(query));
        return matchesQuery && (!category || item.category === category) && (!status || item.status === status);
    });

    rows.innerHTML = filtered.map((item, index) => `
        <tr>
            <td>${index + 1}</td>
            <td class="code">${safeText(item.code)}</td>
            <td class="device-name">${safeText(item.name)}</td>
            <td>${safeText(item.category)}</td>
            <td>${safeText(item.location)}</td>
            <td>${safeText(item.condition)}</td>
            <td><span class="status ${statusClass(item.status)}">${safeText(item.status)}</span></td>
        </tr>`).join('');

    count.textContent = filtered.length;
    emptyState.hidden = filtered.length !== 0;
}

[searchInput, categoryFilter, statusFilter].forEach(control => {
    control.addEventListener(control.tagName === 'INPUT' ? 'input' : 'change', renderInventory);
});

document.getElementById('inventoryFilter').addEventListener('reset', () => {
    window.setTimeout(renderInventory, 0);
});

const toast = document.getElementById('demoToast');
let toastTimer;
document.querySelectorAll('[data-demo-action]').forEach(button => {
    button.addEventListener('click', () => {
        toast.classList.add('show');
        window.clearTimeout(toastTimer);
        toastTimer = window.setTimeout(() => toast.classList.remove('show'), 2600);
    });
});

document.getElementById('todayLabel').textContent = new Intl.DateTimeFormat('id-ID', {
    weekday: 'long', day: 'numeric', month: 'long', year: 'numeric'
}).format(new Date());

renderInventory();
