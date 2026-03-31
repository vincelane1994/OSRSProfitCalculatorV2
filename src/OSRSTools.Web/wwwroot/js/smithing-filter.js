// Smithing calculator — client-side filtering and sorting for two tabs.

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
    cannonballs: { field: 'profitPerUnit', ascending: false },
    dartTips:    { field: 'profitPerUnit', ascending: false }
};

document.addEventListener('DOMContentLoaded', function () {
    var saved = loadFilters('smithing');
    if (saved) {
        if (saved.minProfit) document.getElementById('filterMinProfit').value = saved.minProfit;
        if (saved.minVolume) document.getElementById('filterMinVolume').value = saved.minVolume;
        if (saved.profitability) document.getElementById('filterProfitable').value = saved.profitability;
    }
    applyFilters('cannonballs');
    applyFilters('dartTips');

    // Re-apply filters and update Showing count when a tab becomes active.
    document.querySelectorAll('#smithingTabs [data-bs-toggle="tab"]').forEach(function (btn) {
        btn.addEventListener('shown.bs.tab', function () {
            var tab = btn.getAttribute('data-tab');
            applyFilters(tab);
        });
    });
});

function getActiveTab() {
    var active = document.querySelector('#smithingTabs .nav-link.active');
    return active ? active.getAttribute('data-tab') : 'cannonballs';
}

function applyFilters(tabOverride) {
    var tab     = tabOverride || getActiveTab();
    var data    = tab === 'cannonballs' ? cannonballs : dartTips;
    var bodyId  = tab === 'cannonballs' ? 'cannonballsBody' : 'dartTipsBody';

    var minProfit    = safeParseInt(document.getElementById('filterMinProfit').value, 0);
    var minVolume    = safeParseInt(document.getElementById('filterMinVolume').value, 0);
    var profitable   = document.getElementById('filterProfitable').value;

    saveFilters('smithing', {
        minProfit: document.getElementById('filterMinProfit').value,
        minVolume: document.getElementById('filterMinVolume').value,
        profitability: document.getElementById('filterProfitable').value
    });

    var filtered = data.filter(function (item) {
        if (item.profitPerUnit < minProfit) return false;
        if (item.volume24Hr < minVolume) return false;
        if (profitable === 'profitable' && !item.isProfitable) return false;
        if (profitable === 'unprofitable' && item.isProfitable) return false;
        return true;
    });

    var sort = sortState[tab];
    filtered.sort(function (a, b) {
        var valA = a[sort.field];
        var valB = b[sort.field];
        if (typeof valA === 'string') {
            valA = valA.toLowerCase();
            valB = valB.toLowerCase();
        }
        if (valA < valB) return sort.ascending ? -1 : 1;
        if (valA > valB) return sort.ascending ? 1 : -1;
        return 0;
    });

    renderTable(bodyId, filtered);

    // Only update the Showing card when the active tab renders.
    if (tab === getActiveTab()) {
        document.getElementById('showingCount').textContent = filtered.length;
    }
}

function sortTable(field) {
    var tab = getActiveTab();
    if (sortState[tab].field === field) {
        sortState[tab].ascending = !sortState[tab].ascending;
    } else {
        sortState[tab].field     = field;
        sortState[tab].ascending = (field === 'name' || field === 'barName');
    }
    applyFilters(tab);
}

function resetFilters() {
    document.getElementById('filterMinProfit').value  = '';
    document.getElementById('filterMinVolume').value  = '';
    document.getElementById('filterProfitable').value = 'all';
    localStorage.removeItem('osrs_filters_smithing');
    // Render both tabs; applyFilters updates showingCount only for the active one.
    var active = getActiveTab();
    applyFilters(active === 'cannonballs' ? 'dartTips' : 'cannonballs');
    applyFilters(active);
}

function exportCsv() {
    var tab = getActiveTab();
    var data = tab === 'cannonballs' ? cannonballs : dartTips;
    var headers = ['Bar','Output','Per Bar','Bar Price','Output Price','Profit','ROI%','Volume 24h'];
    var rows = data.map(function(item) {
        return [item.barName, item.name, item.outputPerInput, item.barPrice,
                item.outputPrice, item.profitPerUnit, item.roiPercent, item.volume24Hr];
    });
    var csv = [headers.join(',')]
        .concat(rows.map(function(r) { return r.map(function(v) {
            return typeof v === 'string' ? '"' + v.replace(/"/g, '""') + '"' : v;
        }).join(','); }))
        .join('\n');
    var blob = new Blob([csv], { type: 'text/csv' });
    var url = URL.createObjectURL(blob);
    var a = document.createElement('a');
    a.href = url;
    a.download = 'osrs-smithing-' + tab + '-' + new Date().toISOString().slice(0,10) + '.csv';
    a.click();
    URL.revokeObjectURL(url);
}

function renderTable(bodyId, data) {
    var tbody = document.getElementById(bodyId);
    var html  = '';

    if (data.length === 0) {
        html = '<tr><td colspan="8" class="text-center text-muted py-3">No items match the current filters.</td></tr>';
        tbody.innerHTML = html;
        return;
    }

    for (var i = 0; i < data.length; i++) {
        var item        = data[i];
        var profitClass = item.profitPerUnit > 0 ? 'profit-positive'
                        : item.profitPerUnit < 0 ? 'profit-negative'
                        : 'profit-neutral';

        html += '<tr>';
        html += '<td>' + escapeHtml(item.barName) + '</td>';
        var iconHtml = item.iconUrl ? '<img src="' + escapeHtml(item.iconUrl) + '" class="item-icon" alt="" loading="lazy"> ' : '';
        html += '<td>' + iconHtml + escapeHtml(item.name) + '</td>';
        html += '<td class="text-center">' + item.outputPerInput + '</td>';
        html += '<td>' + formatGp(item.barPrice) + '</td>';
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
    fetch('/Smithing/Data')
        .then(function(r) { return r.json(); })
        .then(function(data) {
            cannonballs = data.cannonballs;
            dartTips = data.dartTips;
            applyFilters('cannonballs');
            applyFilters('dartTips');
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
