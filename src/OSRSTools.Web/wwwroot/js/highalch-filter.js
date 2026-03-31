// High Alchemy client-side filtering and sorting

var currentSort = { field: 'roiPercent', ascending: false };
var showFavoritesOnly = false;

function getFavorites(pageKey) {
    try { return JSON.parse(localStorage.getItem('osrs_favorites_' + pageKey) || '[]'); }
    catch (e) { return []; }
}

function toggleFavorite(pageKey, itemId) {
    var favs = getFavorites(pageKey);
    var idx = favs.indexOf(itemId);
    if (idx >= 0) favs.splice(idx, 1);
    else favs.push(itemId);
    localStorage.setItem('osrs_favorites_' + pageKey, JSON.stringify(favs));
    applyFilters();
}

function toggleFavoritesFilter() {
    showFavoritesOnly = !showFavoritesOnly;
    var btn = document.getElementById('favoritesToggle');
    if (btn) btn.classList.toggle('active', showFavoritesOnly);
    applyFilters();
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

document.addEventListener('DOMContentLoaded', function () {
    var saved = loadFilters('highalch');
    if (saved) {
        if (saved.members) document.getElementById('filterMembers').value = saved.members;
        if (saved.minProfit) document.getElementById('filterMinProfit').value = saved.minProfit;
        if (saved.maxBuyPrice) document.getElementById('filterMaxBuyPrice').value = saved.maxBuyPrice;
        if (saved.minVolume) document.getElementById('filterMinVolume').value = saved.minVolume;
        if (saved.maxInvestment) document.getElementById('filterMaxInvestment').value = saved.maxInvestment;
    }
    applyFilters();

    var searchEl = document.getElementById('itemSearch');
    if (searchEl) {
        searchEl.addEventListener('input', function() {
            clearTimeout(this._debounce);
            this._debounce = setTimeout(applyFilters, 200);
        });
    }
});

function safeParseInt(value, defaultVal) {
    var parsed = parseInt(value, 10);
    return isNaN(parsed) || parsed < 0 ? defaultVal : parsed;
}

function applyFilters() {
    var membersFilter = document.getElementById('filterMembers').value;
    var minProfit = safeParseInt(document.getElementById('filterMinProfit').value, 0);
    var maxBuyPrice = safeParseInt(document.getElementById('filterMaxBuyPrice').value, Number.MAX_SAFE_INTEGER);
    var minVolume = safeParseInt(document.getElementById('filterMinVolume').value, 0);
    var maxInvestment = safeParseInt(document.getElementById('filterMaxInvestment').value, Number.MAX_SAFE_INTEGER);

    saveFilters('highalch', {
        members: membersFilter, minProfit: document.getElementById('filterMinProfit').value,
        maxBuyPrice: document.getElementById('filterMaxBuyPrice').value,
        minVolume: document.getElementById('filterMinVolume').value,
        maxInvestment: document.getElementById('filterMaxInvestment').value
    });

    var searchEl = document.getElementById('itemSearch');
    var searchTerm = searchEl ? searchEl.value.toLowerCase() : '';

    var filtered = items.filter(function (item) {
        if (membersFilter === 'members' && !item.members) return false;
        if (membersFilter === 'f2p' && item.members) return false;
        if (item.profit < minProfit) return false;
        if (item.buyPrice > maxBuyPrice) return false;
        if (item.volume24Hr < minVolume) return false;
        if (item.buyPrice * item.buyLimit > maxInvestment) return false;
        if (searchTerm && item.name.toLowerCase().indexOf(searchTerm) === -1) return false;
        return true;
    });

    if (showFavoritesOnly) {
        var favs = getFavorites('highalch');
        filtered = filtered.filter(function(item) { return favs.indexOf(item.itemId) >= 0; });
    }

    // Apply current sort
    filtered.sort(function (a, b) {
        var valA = a[currentSort.field];
        var valB = b[currentSort.field];

        if (typeof valA === 'string') {
            valA = valA.toLowerCase();
            valB = valB.toLowerCase();
        }

        if (valA < valB) return currentSort.ascending ? -1 : 1;
        if (valA > valB) return currentSort.ascending ? 1 : -1;
        return 0;
    });

    currentFiltered = filtered;
    renderTable(filtered);
    document.getElementById('showingCount').textContent = filtered.length;
}

function sortTable(field) {
    if (currentSort.field === field) {
        currentSort.ascending = !currentSort.ascending;
    } else {
        currentSort.field = field;
        currentSort.ascending = field === 'name';
    }
    applyFilters();
}

function resetFilters() {
    document.getElementById('filterMembers').value = 'all';
    document.getElementById('filterMinProfit').value = '100';
    document.getElementById('filterMaxBuyPrice').value = '';
    document.getElementById('filterMinVolume').value = '10000';
    document.getElementById('filterMaxInvestment').value = '1000000';
    localStorage.removeItem('osrs_filters_highalch');
    applyFilters();
}

var currentFiltered = [];

function exportCsv() {
    var headers = ['Name','Buy Price','Alch Value','Nature Rune','Profit','ROI%','Volume 24h','Buy Limit','GP/hr'];
    var rows = currentFiltered.map(function(item) {
        return [item.name, item.buyPrice, item.highAlchValue, item.natureRuneCost,
                item.profit, item.roiPercent, item.volume24Hr, item.buyLimit,
                Math.round(item.gpPerHour || 0)];
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
    a.download = 'osrs-highalch-' + new Date().toISOString().slice(0,10) + '.csv';
    a.click();
    URL.revokeObjectURL(url);
}

function renderTable(data) {
    var tbody = document.getElementById('alchTableBody');
    var html = '';
    var favs = getFavorites('highalch');

    for (var i = 0; i < data.length; i++) {
        var item = data[i];
        var profitClass = item.profit > 0 ? 'profit-positive' : item.profit < 0 ? 'profit-negative' : 'profit-neutral';
        var isFav = favs.indexOf(item.itemId) >= 0;

        html += '<tr>';
        html += '<td class="text-center" style="cursor:pointer;color:' + (isFav ? 'var(--accent)' : 'var(--text-secondary)') + '" onclick="toggleFavorite(\'highalch\',' + item.itemId + ')">' + (isFav ? '&#9733;' : '&#9734;') + '</td>';
        var iconHtml = item.iconUrl ? '<img src="' + escapeHtml(item.iconUrl) + '" class="item-icon" alt="" loading="lazy"> ' : '';
        html += '<td>' + iconHtml + escapeHtml(item.name) + (item.members ? ' <i class="bi bi-star-fill text-warning" style="font-size:0.7rem" title="Members"></i>' : '') + '</td>';
        html += '<td>' + formatGp(item.buyPrice) + '</td>';
        html += '<td>' + formatGp(item.highAlchValue) + '</td>';
        html += '<td>' + formatGp(item.natureRuneCost) + '</td>';
        html += '<td class="' + profitClass + '">' + formatGp(item.profit) + '</td>';
        html += '<td class="' + profitClass + '">' + item.roiPercent.toFixed(2) + '%</td>';
        html += '<td>' + formatNumber(item.volume24Hr) + '</td>';
        html += '<td>' + formatNumber(item.buyLimit) + '</td>';
        html += '<td>' + formatGpHr(item.gpPerHour) + '</td>';
        html += '</tr>';
    }

    tbody.innerHTML = html;
}

function formatGp(value) {
    if (value === null || value === undefined) return '--';
    return value.toLocaleString() + ' gp';
}

function formatGpHr(value) {
    if (value === null || value === undefined || value === 0) return '--';
    if (value >= 1000000) return (value / 1000000).toFixed(1) + 'M gp/hr';
    if (value >= 1000) return (value / 1000).toFixed(0) + 'K gp/hr';
    return Math.round(value) + ' gp/hr';
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
    fetch('/HighAlching/Data')
        .then(function(r) { return r.json(); })
        .then(function(data) {
            items = data;
            applyFilters();
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
