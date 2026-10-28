(function () {
    'use strict';

    function setText(id, value) {
        const element = document.getElementById(id);
        if (element) element.textContent = value;
    }

    function activateNavigation() {
        const body = document.body;
        const controller = (body.dataset.controller || '').toLowerCase();
        const action = (body.dataset.action || '').toLowerCase();
        let activeKey = '';

        if (controller === 'home') activeKey = 'dashboard';
        else if (controller === 'perangkat' && action === 'arsip') activeKey = 'archive';
        else if (controller === 'perangkat' && action === 'cetaklaporan') activeKey = 'reports';
        else if (controller === 'perangkat' || controller === 'kelolabarang') activeKey = 'inventory';
        else if (controller === 'peminjaman') activeKey = 'loans';
        else if (controller === 'opname') activeKey = 'stocktake';
        else if (controller === 'user') activeKey = 'users';
        else if (controller === 'vendor') activeKey = 'vendors';
        else if (controller === 'profile') activeKey = 'profile';

        const activeLink = activeKey
            ? document.querySelector(`#sidebarMenu .nav-link[data-nav-key="${activeKey}"]`)
            : null;

        if (activeLink) {
            activeLink.classList.add('active');
            activeLink.setAttribute('aria-current', 'page');
        }
    }

    async function loadProfile() {
        try {
            const response = await fetch('/api/profile', {
                method: 'GET', credentials: 'include', headers: { Accept: 'application/json' }
            });

            if (response.status === 401) {
                window.location.href = '/Auth/Login';
                return;
            }
            if (!response.ok) throw new Error('Profil tidak tersedia.');

            const result = await response.json();
            if (!result?.success || !result.data) throw new Error(result?.message || 'Profil tidak tersedia.');

            const user = result.data;
            setText('sidebarUsername', user.namaLengkap || user.username || 'Pengguna');
            setText('sidebarRole', user.role || 'Pengguna');

            if ((user.role || '').toLowerCase() !== 'admin') {
                document.querySelectorAll('.admin-only-menu').forEach(item => item.remove());
            }
        } catch (error) {
            setText('sidebarUsername', 'Pengguna SIGMA');
            setText('sidebarRole', 'Sesi tidak tersedia');
            console.warn('Profil gagal dimuat:', error.message);
        }
    }

    function setOpnameReminderBadge(shouldRemind) {
        const badge = document.getElementById('opnameReminderBadge');
        if (!badge) return;

        badge.classList.toggle('d-none', !shouldRemind);
        badge.setAttribute('aria-hidden', shouldRemind ? 'false' : 'true');
    }

    function getCurrentMonthReminderStorageKey(periodKey) {
        return `sigma.opname.reminder.${periodKey}`;
    }

    function wasMonthlyReminderShown(periodKey) {
        try {
            return window.sessionStorage.getItem(getCurrentMonthReminderStorageKey(periodKey)) === '1';
        } catch {
            return false;
        }
    }

    function markMonthlyReminderShown(periodKey) {
        try {
            window.sessionStorage.setItem(getCurrentMonthReminderStorageKey(periodKey), '1');
        } catch {
            // Storage can be unavailable in private/restricted browser contexts.
        }
    }

    function clearOpnameReminderSession() {
        const prefix = 'sigma.opname.reminder.';

        try {
            Object.keys(window.sessionStorage)
                .filter(key => key.startsWith(prefix))
                .forEach(key => window.sessionStorage.removeItem(key));

            // Remove markers created by the previous once-per-month behavior.
            Object.keys(window.localStorage)
                .filter(key => key.startsWith(prefix))
                .forEach(key => window.localStorage.removeItem(key));
        } catch {
            // Storage can be unavailable in private/restricted browser contexts.
        }
    }

    function updateOpnameReminderPanel(status) {
        const panel = document.getElementById('opnameReminderPanel');
        if (!panel || !status) return;

        const title = document.getElementById('opnameReminderTitle');
        const message = document.getElementById('opnameReminderMessage');
        const progress = document.getElementById('opnameReminderProgress');
        const button = document.getElementById('opnameReminderAction');

        panel.classList.toggle('d-none', !status.shouldRemind);
        if (!status.shouldRemind) return;

        const completed = Number(status.completedDevices || 0);
        const total = Number(status.totalDevices || 0);
        const pending = Number(status.pendingDevices || 0);
        const percentage = total > 0
            ? Math.min(100, Math.round((completed / total) * 100))
            : 0;

        if (title) title.textContent = `Reminder Stock Opname ${status.periodLabel || ''}`.trim();
        if (message) {
            message.textContent = `${pending} perangkat belum dilakukan opname bulan ini. Progress ${completed} dari ${total} perangkat.`;
        }
        if (progress) {
            progress.style.width = `${percentage}%`;
            progress.setAttribute('aria-valuenow', String(percentage));
        }
        if (button) {
            button.href = '/Opname/Index';
        }
    }

    async function loadOpnameReminderStatus() {
        try {
            const response = await fetch('/api/opname/reminder-status', {
                method: 'GET',
                credentials: 'include',
                headers: { Accept: 'application/json' }
            });

            if (response.status === 401) {
                setOpnameReminderBadge(false);
                return null;
            }

            if (!response.ok) {
                setOpnameReminderBadge(false);
                return null;
            }

            const result = await response.json();
            const status = result?.success ? result.data : null;
            if (!status) {
                setOpnameReminderBadge(false);
                return null;
            }

            setOpnameReminderBadge(Boolean(status.shouldRemind));
            window.SIGMA_OPNAME_REMINDER_STATUS = status;
            updateOpnameReminderPanel(status);

            const controller = (document.body.dataset.controller || '').toLowerCase();
            if (status.shouldRemind && controller !== 'opname' && !wasMonthlyReminderShown(status.periodKey)) {
                if (window.Swal?.fire) {
                    const popupResult = await window.Swal.fire({
                        icon: 'warning',
                        title: 'Reminder Stock Opname',
                        text: `${status.pendingDevices} perangkat belum dilakukan opname untuk ${status.periodLabel}.`,
                        confirmButtonText: 'Buka Opname',
                        cancelButtonText: 'Nanti',
                        showCancelButton: true,
                        reverseButtons: true
                    });

                    markMonthlyReminderShown(status.periodKey);

                    if (popupResult.isConfirmed) {
                        window.location.assign('/Opname/Index');
                    }
                }
            }

            return status;
        } catch (error) {
            setOpnameReminderBadge(false);
            console.warn('Reminder stock opname gagal dimuat:', error.message);
            return null;
        }
    }

    window.SIGMA_OPNAME_REMINDER = {
        refresh: loadOpnameReminderStatus,
        updatePanel: updateOpnameReminderPanel
    };

    function logout() {
        const button = document.getElementById('logoutButton');
        if (button) {
            button.disabled = true;
            button.setAttribute('aria-busy', 'true');
        }

        clearOpnameReminderSession();

        // Logout is handled locally by the MVC frontend. It clears the shared
        // SIGMA cookie and returns directly to the application's login page.
        window.location.assign('/Auth/Logout');
    }

    function wireSidebar() {
        const sidebar = document.getElementById('sidebarMenu');
        if (!sidebar) return;

        sidebar.querySelectorAll('.nav-link').forEach(link => {
            link.addEventListener('click', () => {
                if (window.innerWidth < 992) {
                    bootstrap.Offcanvas.getOrCreateInstance(sidebar).hide();
                }
            });
        });
    }

    let sigmaSelectSequence = 0;

    function getRackLetter(value) {
        const match = String(value || '').trim().match(/[A-Za-z]/);
        return match ? match[0].toUpperCase() : '#';
    }

    function renderSigmaRackDropdown(container, values, onSelect, options = {}) {
        if (!container || typeof onSelect !== 'function') return;

        const placeholder = options.placeholder || 'Pilih Rak';
        const normalizedValues = Array.from(new Set((values || [])
            .map(value => String(value ?? '').trim())
            .filter(Boolean)))
            .sort((left, right) => left.localeCompare(right, 'id', {
                numeric: true,
                sensitivity: 'base'
            }));
        const letters = Array.from(new Set(normalizedValues.map(getRackLetter)));

        container.innerHTML = '';
        container.classList.add('rack-filter-dropdown');
        container.setAttribute('aria-label', 'Pilih lokasi rak');

        const filters = document.createElement('div');
        filters.className = 'rack-letter-filters';
        filters.setAttribute('aria-label', 'Filter awalan lokasi rak');

        const optionList = document.createElement('div');
        optionList.className = 'rack-option-list';

        function applyLetterFilter(letter, activeButton) {
            filters.querySelectorAll('.rack-letter-button').forEach(button => {
                button.classList.toggle('active', button === activeButton);
            });
            optionList.querySelectorAll('.rack-option-item').forEach(item => {
                const isPlaceholder = item.dataset.rackPlaceholder === 'true';
                item.hidden = Boolean(letter) && !isPlaceholder && item.dataset.rackLetter !== letter;
            });
            optionList.scrollTop = 0;
        }

        function appendFilter(label, letter, isActive = false) {
            const button = document.createElement('button');
            button.type = 'button';
            button.className = `rack-letter-button${isActive ? ' active' : ''}`;
            button.textContent = label;
            button.dataset.rackFilter = letter;
            button.addEventListener('click', event => {
                event.stopPropagation();
                applyLetterFilter(letter, button);
            });
            filters.appendChild(button);
        }

        function appendOption(value, label, isPlaceholder = false) {
            const item = document.createElement('button');
            item.type = 'button';
            item.className = `dropdown-item-custom rack-option-item${isPlaceholder ? ' fw-semibold' : ''}`;
            item.textContent = label;
            item.dataset.rackLetter = isPlaceholder ? '' : getRackLetter(value);
            item.dataset.rackPlaceholder = String(isPlaceholder);
            item.addEventListener('click', event => {
                event.stopPropagation();
                onSelect(value, label);
            });
            optionList.appendChild(item);
        }

        appendFilter('Semua', '', true);
        letters.forEach(letter => appendFilter(letter, letter));
        appendOption('', placeholder, true);
        normalizedValues.forEach(value => appendOption(value, value));

        if (!normalizedValues.length) {
            const emptyState = document.createElement('div');
            emptyState.className = 'rack-filter-empty';
            emptyState.textContent = 'Belum ada lokasi rak.';
            optionList.appendChild(emptyState);
        }

        container.append(filters, optionList);
    }

    window.renderSigmaRackDropdown = renderSigmaRackDropdown;

    function closeSigmaSelects(exceptWrapper) {
        document.querySelectorAll('.sigma-select.is-open').forEach(wrapper => {
            if (wrapper === exceptWrapper) return;
            wrapper.classList.remove('is-open');
            wrapper.querySelector('.sigma-select-trigger')?.setAttribute('aria-expanded', 'false');
        });
    }

    function enhanceNativeSelect(select) {
        if (!select || select.dataset.sigmaSelect === 'true' || select.dataset.nativeSelect === 'true') return;
        select.dataset.sigmaSelect = 'true';

        const wrapper = document.createElement('div');
        wrapper.className = `sigma-select${select.classList.contains('panel-filter-select') ? ' sigma-select-compact' : ''}`;
        const menuId = `sigmaSelectMenu${++sigmaSelectSequence}`;
        const trigger = document.createElement('button');
        trigger.type = 'button';
        trigger.className = 'sigma-select-trigger';
        trigger.setAttribute('aria-haspopup', 'listbox');
        trigger.setAttribute('aria-expanded', 'false');
        trigger.setAttribute('aria-controls', menuId);
        const valueLabel = document.createElement('span');
        valueLabel.className = 'sigma-select-value';
        const chevron = document.createElement('i');
        chevron.className = 'bi bi-chevron-down sigma-select-chevron';
        trigger.append(valueLabel, chevron);

        const menu = document.createElement('div');
        menu.id = menuId;
        menu.className = 'sigma-select-menu';
        menu.setAttribute('role', 'listbox');

        select.parentNode.insertBefore(wrapper, select);
        wrapper.append(trigger, menu, select);
        select.classList.add('sigma-select-source');
        select.tabIndex = -1;

        function renderOptions() {
            const options = Array.from(select.options);
            const selected = options.find(option => option.selected) || options[0];
            valueLabel.textContent = selected?.textContent?.trim() || 'Pilih opsi';
            valueLabel.classList.toggle('is-placeholder', !selected?.value);
            trigger.disabled = select.disabled;
            menu.innerHTML = '';

            options.forEach(option => {
                const item = document.createElement('button');
                item.type = 'button';
                item.className = 'sigma-select-option';
                item.setAttribute('role', 'option');
                item.setAttribute('aria-selected', option.selected ? 'true' : 'false');
                item.disabled = option.disabled;
                item.dataset.value = option.value;
                const text = document.createElement('span');
                text.textContent = option.textContent.trim();
                const check = document.createElement('i');
                check.className = 'bi bi-check2 sigma-select-check';
                item.append(text, check);
                item.addEventListener('click', () => {
                    if (option.disabled) return;
                    select.value = option.value;
                    select.dispatchEvent(new Event('change', { bubbles: true }));
                    closeSigmaSelects();
                    trigger.focus();
                });
                menu.appendChild(item);
            });
        }

        function toggleMenu(forceOpen) {
            if (select.disabled) return;
            renderOptions();
            const shouldOpen = typeof forceOpen === 'boolean' ? forceOpen : !wrapper.classList.contains('is-open');
            closeSigmaSelects(shouldOpen ? wrapper : null);
            wrapper.classList.toggle('is-open', shouldOpen);
            trigger.setAttribute('aria-expanded', String(shouldOpen));
            if (shouldOpen) menu.querySelector('[aria-selected="true"]')?.scrollIntoView({ block: 'nearest' });
        }

        trigger.addEventListener('click', event => { event.stopPropagation(); toggleMenu(); });
        trigger.addEventListener('keydown', event => {
            if (event.key === 'ArrowDown' || event.key === 'Enter' || event.key === ' ') {
                event.preventDefault();
                toggleMenu(true);
                menu.querySelector('[aria-selected="true"]:not(:disabled), .sigma-select-option:not(:disabled)')?.focus();
            }
        });
        menu.addEventListener('keydown', event => {
            const enabled = Array.from(menu.querySelectorAll('.sigma-select-option:not(:disabled)'));
            const currentIndex = enabled.indexOf(document.activeElement);
            if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
                event.preventDefault();
                const direction = event.key === 'ArrowDown' ? 1 : -1;
                enabled[(currentIndex + direction + enabled.length) % enabled.length]?.focus();
            } else if (event.key === 'Escape') {
                event.preventDefault();
                closeSigmaSelects();
                trigger.focus();
            }
        });
        select.addEventListener('change', renderOptions);
        select.addEventListener('invalid', () => trigger.classList.add('is-invalid'));

        new MutationObserver(renderOptions).observe(select, { childList: true, subtree: true, attributes: true });
        renderOptions();
    }

    function enhanceAllNativeSelects(root = document) {
        root.querySelectorAll('select:not([data-native-select])').forEach(enhanceNativeSelect);
    }

    function labelMobileTableRows(table) {
        const headerRow = table.tHead?.rows?.[table.tHead.rows.length - 1];
        const headers = headerRow
            ? Array.from(headerRow.cells).map(cell => cell.textContent.replace(/\s+/g, ' ').trim())
            : [];

        table.querySelectorAll('tbody tr').forEach(row => {
            const cells = Array.from(row.cells || []);
            const isEmptyState = cells.length === 1 && cells[0].hasAttribute('colspan');
            row.classList.toggle('sigma-mobile-table-row', !isEmptyState);
            row.classList.toggle('sigma-mobile-table-empty', isEmptyState);
            if (isEmptyState) return;

            cells.forEach((cell, index) => {
                if (!cell.dataset.label) cell.dataset.label = headers[index] || `Kolom ${index + 1}`;
            });
        });
    }

    function enhanceMobileTable(table) {
        if (!table || table.dataset.mobileTable === 'scroll' || table.dataset.sigmaMobileTable === 'true') return;
        table.dataset.sigmaMobileTable = 'true';
        table.classList.add('sigma-mobile-table');
        labelMobileTableRows(table);

        const body = table.tBodies?.[0];
        if (!body) return;
        const observer = new MutationObserver(() => labelMobileTableRows(table));
        observer.observe(body, { childList: true, subtree: true });
        table.sigmaMobileTableObserver = observer;
    }

    function enhanceAllMobileTables(root = document) {
        root.querySelectorAll('.app-content .table-responsive table:not([data-mobile-table="scroll"])')
            .forEach(enhanceMobileTable);
    }

    let activeDateInput = null;
    let visibleCalendarMonth = null;

    function parseIsoDate(value) {
        const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value || '');
        if (!match) return null;
        return new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3]));
    }

    function toIsoDate(value) {
        const year = value.getFullYear();
        const month = String(value.getMonth() + 1).padStart(2, '0');
        const day = String(value.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }

    function isDateAllowed(input, isoDate) {
        return (!input.min || isoDate >= input.min) && (!input.max || isoDate <= input.max);
    }

    function getCalendarPopover() {
        let popover = document.getElementById('sigmaCalendarPopover');
        if (popover) return popover;

        popover = document.createElement('div');
        popover.id = 'sigmaCalendarPopover';
        popover.className = 'sigma-calendar-popover';
        popover.setAttribute('role', 'dialog');
        popover.setAttribute('aria-label', 'Pilih tanggal');
        popover.setAttribute('aria-hidden', 'true');
        document.body.appendChild(popover);
        return popover;
    }

    function renderCalendar() {
        if (!activeDateInput || !visibleCalendarMonth) return;
        const popover = getCalendarPopover();
        const year = visibleCalendarMonth.getFullYear();
        const month = visibleCalendarMonth.getMonth();
        const selectedIso = activeDateInput.value;
        const todayIso = toIsoDate(new Date());
        const monthName = new Intl.DateTimeFormat('id-ID', { month: 'long' }).format(visibleCalendarMonth);
        const monthLabel = `${monthName} ${year}`;
        const minDate = parseIsoDate(activeDateInput.min);
        const maxDate = parseIsoDate(activeDateInput.max);
        const currentYear = new Date().getFullYear();
        const firstYear = minDate
            ? minDate.getFullYear()
            : Math.min(currentYear - 50, year);
        const lastYear = maxDate
            ? maxDate.getFullYear()
            : Math.max(currentYear + 20, year);
        const yearOptions = [];
        for (let optionYear = firstYear; optionYear <= lastYear; optionYear += 1) {
            yearOptions.push(`<button type="button" class="sigma-calendar-year-option${optionYear === year ? ' is-selected' : ''}" data-calendar-year="${optionYear}" role="option" aria-selected="${optionYear === year}">${optionYear}</button>`);
        }
        const firstWeekday = (new Date(year, month, 1).getDay() + 6) % 7;
        const daysInMonth = new Date(year, month + 1, 0).getDate();
        const dayNames = ['Sen', 'Sel', 'Rab', 'Kam', 'Jum', 'Sab', 'Min'];
        const dayCells = [];

        for (let index = 0; index < firstWeekday; index += 1) {
            dayCells.push('<span class="sigma-calendar-empty" aria-hidden="true"></span>');
        }

        for (let day = 1; day <= daysInMonth; day += 1) {
            const value = toIsoDate(new Date(year, month, day));
            const isSelected = value === selectedIso;
            const isToday = value === todayIso;
            const disabled = !isDateAllowed(activeDateInput, value);
            dayCells.push(`<button type="button" class="sigma-calendar-day${isSelected ? ' is-selected' : ''}${isToday ? ' is-today' : ''}" data-date="${value}" ${disabled ? 'disabled' : ''} aria-label="${day} ${monthLabel}" aria-pressed="${isSelected}">${day}</button>`);
        }

        while (dayCells.length < 42) {
            dayCells.push('<span class="sigma-calendar-empty" aria-hidden="true"></span>');
        }

        popover.innerHTML = `
            <div class="sigma-calendar-header">
                <button type="button" class="sigma-calendar-nav" data-calendar-action="previous" aria-label="Bulan sebelumnya"><i class="bi bi-chevron-left"></i></button>
                <div class="sigma-calendar-heading">
                    <strong>${monthName}</strong>
                    <div class="sigma-calendar-year-picker">
                        <button type="button" class="sigma-calendar-year-trigger" aria-label="Pilih tahun" aria-haspopup="listbox" aria-expanded="false"><span>${year}</span><i class="bi bi-chevron-down"></i></button>
                        <div class="sigma-calendar-year-menu" role="listbox" aria-label="Daftar tahun">${yearOptions.join('')}</div>
                    </div>
                </div>
                <button type="button" class="sigma-calendar-nav" data-calendar-action="next" aria-label="Bulan berikutnya"><i class="bi bi-chevron-right"></i></button>
            </div>
            <div class="sigma-calendar-weekdays">${dayNames.map(day => `<span>${day}</span>`).join('')}</div>
            <div class="sigma-calendar-days">${dayCells.join('')}</div>
            <div class="sigma-calendar-footer">
                <button type="button" data-calendar-action="clear"><i class="bi bi-x-circle"></i> Hapus</button>
                <button type="button" data-calendar-action="today" ${isDateAllowed(activeDateInput, todayIso) ? '' : 'disabled'}><i class="bi bi-calendar-check"></i> Hari ini</button>
            </div>`;

        popover.querySelector('[data-calendar-action="previous"]')?.addEventListener('click', event => {
            event.preventDefault();
            event.stopPropagation();
            visibleCalendarMonth = new Date(year, month - 1, 1);
            renderCalendar();
            positionCalendar();
        });
        popover.querySelector('[data-calendar-action="next"]')?.addEventListener('click', event => {
            event.preventDefault();
            event.stopPropagation();
            visibleCalendarMonth = new Date(year, month + 1, 1);
            renderCalendar();
            positionCalendar();
        });
        const yearPicker = popover.querySelector('.sigma-calendar-year-picker');
        const yearTrigger = popover.querySelector('.sigma-calendar-year-trigger');
        yearTrigger?.addEventListener('click', event => {
            event.preventDefault();
            event.stopPropagation();
            const isOpen = yearPicker.classList.toggle('is-open');
            yearTrigger.setAttribute('aria-expanded', String(isOpen));
            if (isOpen) popover.querySelector('.sigma-calendar-year-option.is-selected')?.scrollIntoView({ block: 'center' });
        });
        popover.querySelectorAll('.sigma-calendar-year-option').forEach(option => option.addEventListener('click', event => {
            event.preventDefault();
            event.stopPropagation();
            const selectedYear = Number(option.dataset.calendarYear);
            if (!Number.isInteger(selectedYear)) return;
            visibleCalendarMonth = new Date(selectedYear, month, 1);
            renderCalendar();
            positionCalendar();
        }));
        popover.querySelector('[data-calendar-action="clear"]')?.addEventListener('click', () => {
            activeDateInput.value = '';
            activeDateInput.dispatchEvent(new Event('input', { bubbles: true }));
            activeDateInput.dispatchEvent(new Event('change', { bubbles: true }));
            closeCalendar();
        });
        popover.querySelector('[data-calendar-action="today"]')?.addEventListener('click', () => {
            if (!isDateAllowed(activeDateInput, todayIso)) return;
            setActiveDate(todayIso);
        });
        popover.querySelectorAll('.sigma-calendar-day').forEach(button => {
            button.addEventListener('click', () => setActiveDate(button.dataset.date));
        });
    }

    function positionCalendar() {
        if (!activeDateInput) return;
        const popover = getCalendarPopover();
        const anchor = activeDateInput.closest('.sigma-date') || activeDateInput;
        const bounds = anchor.getBoundingClientRect();
        const width = Math.min(296, window.innerWidth - 20);
        const height = popover.offsetHeight || 340;
        const left = Math.min(Math.max(10, bounds.left), window.innerWidth - width - 10);
        const belowTop = bounds.bottom + 8;
        const top = belowTop + height <= window.innerHeight - 10
            ? belowTop
            : Math.max(10, bounds.top - height - 8);
        popover.style.width = `${width}px`;
        popover.style.left = `${left}px`;
        popover.style.top = `${top}px`;
    }

    function openCalendar(input) {
        if (!input || input.disabled) return;
        closeSigmaSelects();
        activeDateInput = input;
        const selected = parseIsoDate(input.value) || new Date();
        visibleCalendarMonth = new Date(selected.getFullYear(), selected.getMonth(), 1);
        renderCalendar();
        const popover = getCalendarPopover();
        popover.classList.add('is-open');
        popover.setAttribute('aria-hidden', 'false');
        positionCalendar();
    }

    function closeCalendar() {
        const popover = document.getElementById('sigmaCalendarPopover');
        popover?.classList.remove('is-open');
        popover?.setAttribute('aria-hidden', 'true');
        activeDateInput = null;
    }

    function setActiveDate(isoDate) {
        if (!activeDateInput || !isoDate || !isDateAllowed(activeDateInput, isoDate)) return;
        const input = activeDateInput;
        input.value = isoDate;
        input.dispatchEvent(new Event('input', { bubbles: true }));
        input.dispatchEvent(new Event('change', { bubbles: true }));
        closeCalendar();
        input.focus();
    }

    function enhanceNativeDateInput(input) {
        if (!input || input.dataset.sigmaDate === 'true' || input.dataset.nativeDate === 'true') return;
        input.dataset.sigmaDate = 'true';
        input.classList.add('sigma-date-input');

        const wrapper = document.createElement('div');
        wrapper.className = 'sigma-date';
        input.parentNode.insertBefore(wrapper, input);
        wrapper.appendChild(input);

        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'sigma-date-button';
        button.setAttribute('aria-label', 'Buka kalender');
        button.innerHTML = '<i class="bi bi-calendar3"></i>';
        wrapper.appendChild(button);

        button.addEventListener('click', event => {
            event.stopPropagation();
            openCalendar(input);
        });
        input.addEventListener('pointerdown', event => {
            if (event.button !== 0) return;
            event.preventDefault();
            input.focus({ preventScroll: true });
            openCalendar(input);
        });
        input.addEventListener('keydown', event => {
            if (event.key === 'ArrowDown' || event.key === 'Enter' || event.key === ' ') {
                event.preventDefault();
                openCalendar(input);
            } else if (event.key === 'Escape') {
                closeCalendar();
            }
        });
    }

    function enhanceAllNativeDates(root = document) {
        root.querySelectorAll('input[type="date"]:not([data-native-date])').forEach(enhanceNativeDateInput);
    }

    document.addEventListener('DOMContentLoaded', function () {
        activateNavigation();
        wireSidebar();
        enhanceAllNativeSelects();
        enhanceAllMobileTables();
        enhanceAllNativeDates();
        loadProfile();
        loadOpnameReminderStatus();
        document.getElementById('logoutButton')?.addEventListener('click', logout);
        document.addEventListener('click', event => {
            closeSigmaSelects();
            if (!event.target.closest('.sigma-calendar-popover, .sigma-date')) closeCalendar();
        });
        document.addEventListener('keydown', event => {
            if (event.key !== 'Escape') return;
            closeSigmaSelects();
            closeCalendar();
        });
        window.addEventListener('resize', closeCalendar);
        window.addEventListener('scroll', event => {
            if (!event.target.closest?.('.sigma-calendar-popover')) closeCalendar();
        }, true);
    });
})();
window.escapeHtml = window.escapeHtml || function (value) {
    return String(value ?? '')
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll("'", '&#039;');
};

