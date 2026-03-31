// Herblore calculator — client-side filtering and sorting for three tabs.

function safeParseInt(value, defaultVal) {
    var parsed = parseInt(value, 10);
    return isNaN(parsed) || parsed < 0 ? defaultVal : parsed;
}

function saveFilters(pageKey, filters) {
    try { localStorage.setItem('osrs_filters_' + pageKey, JSON.stringify(filters)); } catch (e) { }
}

function loadFilters(pageKey) {
    try {
        var saved = localStorage.getItem('osrs_filters_' + pageKey);
        return saved ? JSON.parse(saved) : null;
    } catch (e) { return null; }
}

var sortState = {
    cleaning:     { field: 'profitPerUnit', ascending: false },
    fullProcess:  { field: 'profitPerUnit', ascending: false },
    potionMaking: { field: 'profitPerUnit', ascending: false }
};

document.addEventListener('DOMContentLoaded', function () {
    var saved = loadFilters('herblore');
    if (saved) {
        if (saved.minProfit) document.getElementById('filterMinProfit').value = saved.minProfit;
        if (saved.minVolume) document.getElementById('filterMinVolume').value = saved.minVolume;
        if (saved.profitability) document.getElementById('filterProfitable').value = saved.profitability;
    }
    applyFilters('cleaning');
    applyFilters('fullProcess');
    applyFilters('potionMaking');

    document.querySelectorAll('#herbloreTabs [data-bs-toggle="tab"]').forEach(function (btn) {
        btn.addEventListener('shown.bs.tab', function () {
            applyFilters(btn.getAttribute('data-tab'));
        });
    });
});

function getActiveTab() {
    var active = document.querySelector('#herbloreTabs .nav-link.active');
    return active ? active.getAttribute('data-tab') : 'cleaning';
}

function getTabData(tab) {
    if (tab === 'cleaning')     return cleaningItems;
    if (tab === 'fullProcess')  return fullProcessItems;
    return potionMakingItems;
}

function getBodyId(tab) {
    if (tab === 'cleaning')     return 'cleaningBody';
    if (tab === 'fullProcess')  return 'fullProcessBody';
    return 'potionMakingBody';
}

function getColspan(tab) {
    if (tab === 'cleaning')     return 6;
    if (tab === 'fullProcess')  return 7;
    return 8;
}

function applyFilters(tabOverride) {
    var tab  = tabOverride || getActiveTab();
    var data = getTabData(tab);

    var minProfit  = safeParseInt(document.getElementById('filterMinProfit').value, 0);
    var minVolume  = safeParseInt(document.getElementById('filterMinVolume').value, 0);
    var profitable = document.getElementById('filterProfitable').value;

    saveFilters('herblore', {
        minProfit: document.getElementById('filterMinProfit').value,
        minVolume: document.getElementById('filterMinVolume').value,
        profitability: document.getElementById('filterProfitable').value
    });

    var filtered = data.filter(function (item) {
        if (item.profitPerUnit < minProfit) return false;
        if (item.volume24Hr < minVolume) return false;
        if (profitable === 'profitable'   && !item.isProfitable) return false;
        if (profitable === 'unprofitable' &&  item.isProfitable) return false;
        return true;
    });

    var sort = sortState[tab];
    filtered.sort(function (a, b) {
        var valA = a[sort.field];
        var valB = b[sort.field];
        if (typeof valA === 'string') { valA = valA.toLowerCase(); valB = valB.toLowerCase(); }
        if (valA < valB) return sort.ascending ? -1 : 1;
        if (valA > valB) return sort.ascending ?  1 : -1;
        return 0;
    });

    renderTable(tab, filtered);

    if (tab === getActiveTab()) {
        document.getElementById('showingCount') &&
            (document.getElementById('showingCount').textContent = filtered.length);
    }
}

function sortTable(field) {
    var tab = getActiveTab();
    if (sortState[tab].field === field) {
        sortState[tab].ascending = !sortState[tab].ascending;
    } else {
        sortState[tab].field     = field;
        sortState[tab].ascending = (field === 'herbName' || field === 'name');
    }
    applyFilters(tab);
}

function resetFilters() {
    document.getElementById('filterMinProfit').value  = '';
    document.getElementById('filterMinVolume').value  = '';
    document.getElementById('filterProfitable').value = 'all';
    localStorage.removeItem('osrs_filters_herblore');
    var active = getActiveTab();
    ['cleaning', 'fullProcess', 'potionMaking'].forEach(function (tab) {
        if (tab !== active) applyFilters(tab);
    });
    applyFilters(active);
}

function exportCsv() {
    var tab = getActiveTab();
    var data = getTabData(tab);
    var headers, rows;
    if (tab === 'cleaning') {
        headers = ['Herb','Grimy Price','Clean Price','Profit','ROI%','Volume 24h'];
        rows = data.map(function(item) { return [item.herbName, item.herbPrice, item.outputPrice, item.profitPerUnit, item.roiPercent, item.volume24Hr]; });
    } else if (tab === 'fullProcess') {
        headers = ['Herb','Grimy Price','Vial Price','Unf. Potion Price','Profit','ROI%','Volume 24h'];
        rows = data.map(function(item) { return [item.herbName, item.herbPrice, item.secondaryPrice, item.outputPrice, item.profitPerUnit, item.roiPercent, item.volume24Hr]; });
    } else {
        headers = ['Herb','Potion','Clean Herb Price','Secondary Price','Potion Price','Profit','ROI%','Volume 24h'];
        rows = data.map(function(item) { return [item.herbName, item.name, item.herbPrice, item.secondaryPrice, item.outputPrice, item.profitPerUnit, item.roiPercent, item.volume24Hr]; });
    }
    var csv = [headers.join(',')]
        .concat(rows.map(function(r) { return r.map(function(v) {
            return typeof v === 'string' ? '"' + v.replace(/"/g, '""') + '"' : v;
        }).join(','); }))
        .join('\n');
    var blob = new Blob([csv], { type: 'text/csv' });
    var url = URL.createObjectURL(blob);
    var a = document.createElement('a');
    a.href = url;
    a.download = 'osrs-herblore-' + tab + '-' + new Date().toISOString().slice(0,10) + '.csv';
    a.click();
    URL.revokeObjectURL(url);
}

function renderTable(tab, data) {
    var bodyId  = getBodyId(tab);
    var colspan = getColspan(tab);
    var tbody   = document.getElementById(bodyId);
    var html    = '';

    if (data.length === 0) {
        html = '<tr><td colspan="' + colspan + '" class="text-center text-muted py-3">No items match the current filters.</td></tr>';
        tbody.innerHTML = html;
        return;
    }

    for (var i = 0; i < data.length; i++) {
        var item        = data[i];
        var profitClass = item.profitPerUnit > 0 ? 'profit-positive'
                        : item.profitPerUnit < 0 ? 'profit-negative'
                        : 'profit-neutral';

        html += '<tr>';
        var iconHtml = item.iconUrl ? '<img src="' + escapeHtml(item.iconUrl) + '" class="item-icon" alt="" loading="lazy"> ' : '';
        html += '<td>' + iconHtml + escapeHtml(item.herbName) + '</td>';

        if (tab === 'potionMaking') {
            html += '<td>' + escapeHtml(item.name) + '</td>';
        }

        html += '<td>' + formatGp(item.herbPrice) + '</td>';

        if (tab !== 'cleaning') {
            html += '<td>' + formatGp(item.secondaryPrice) + '</td>';
        }

        html += '<td>' + formatGp(item.outputPrice) + '</td>';
        html += '<td class="' + profitClass + '">' + formatGp(item.profitPerUnit) + '</td>';
        html += '<td class="' + profitClass + '">' + item.roiPercent.toFixed(2) + '%</td>';
        var volWarning = item.volume24Hr < 1000 ? ' <span title="Low output volume" style="color:var(--profit-negative)">&#9888;</span>' : '';
        html += '<td>' + formatNumber(item.volume24Hr) + volWarning + '</td>';
        html += '</tr>';
    }

    tbody.innerHTML = html;
}

function formatGp(value) {
    if (value === null || value === undefined) return '--';
    return value.toLocaleString() + ' gp';
}

function formatNumber(value) {
    if (value === null || value === undefined) return '--';
    return value.toLocaleString();
}

function escapeHtml(text) {
    var div = document.createElement('div');
    div.appendChild(document.createTextNode(text));
    return div.innerHTML;
}

// Auto-refresh
var refreshCountdownTimer = null;
var refreshSecondsLeft = 0;

function startAutoRefresh(intervalSeconds) {
    stopAutoRefresh();
    if (intervalSeconds <= 0) return;
    refreshSecondsLeft = intervalSeconds;
    refreshCountdownTimer = setInterval(function() {
        refreshSecondsLeft--;
        var el = document.getElementById('refreshCountdown');
        if (el) el.textContent = refreshSecondsLeft + 's';
        if (refreshSecondsLeft <= 0) refreshData();
    }, 1000);
}

function stopAutoRefresh() {
    clearInterval(refreshCountdownTimer);
    var el = document.getElementById('refreshCountdown');
    if (el) el.textContent = '';
}

function refreshData() {
    fetch('/Herblore/Data')
        .then(function(r) { return r.json(); })
        .then(function(data) {
            cleaningItems = data.cleaningItems;
            fullProcessItems = data.fullProcessItems;
            potionMakingItems = data.potionMakingItems;
            ['cleaning', 'fullProcess', 'potionMaking'].forEach(function(tab) { applyFilters(tab); });
            var interval = parseInt(document.getElementById('refreshInterval').value);
            refreshSecondsLeft = interval;
            if (interval <= 0) stopAutoRefresh();
        })
        .catch(function(err) { console.error('Refresh failed:', err); });
}

(function() {
    var intervalEl = document.getElementById('refreshInterval');
    var refreshBtn = document.getElementById('refreshNow');
    if (intervalEl) {
        intervalEl.addEventListener('change', function() { startAutoRefresh(parseInt(this.value)); });
        startAutoRefresh(parseInt(intervalEl.value));
    }
    if (refreshBtn) refreshBtn.addEventListener('click', refreshData);
})();
