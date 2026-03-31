// Flipping calculator client-side filtering, sorting, and pagination

var currentSort = { field: 'flipScore', ascending: false };
var currentPage = 1;
var pageSize = 50;
var currentFiltered = [];
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
    var saved = loadFilters('flipping');
    if (saved) {
        if (saved.members) document.getElementById('filterMembers').value = saved.members;
        if (saved.minMargin) document.getElementById('filterMinMargin').value = saved.minMargin;
        if (saved.minVolume) document.getElementById('filterMinVolume').value = saved.minVolume;
        if (saved.minGpHr) document.getElementById('filterMinGpHr').value = saved.minGpHr;
        if (saved.minConfidence) document.getElementById('filterMinConfidence').value = saved.minConfidence;
        if (saved.maxFillHours) document.getElementById('filterMaxFillHours').value = saved.maxFillHours;
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

function safeParseFloat(value, defaultVal, min, max) {
    var parsed = parseFloat(value);
    if (isNaN(parsed)) return defaultVal;
    return Math.max(min, Math.min(max, parsed));
}

function applyFilters() {
    var membersFilter = document.getElementById('filterMembers').value;
    var minMargin = safeParseInt(document.getElementById('filterMinMargin').value, 0);
    var minVolume = safeParseInt(document.getElementById('filterMinVolume').value, 0);
    var minGpHr = safeParseInt(document.getElementById('filterMinGpHr').value, 0);
    var minConfidence = safeParseFloat(document.getElementById('filterMinConfidence').value, 0, 0, 1);
    var maxFillHoursVal = document.getElementById('filterMaxFillHours').value;
    var maxFillHours = maxFillHoursVal !== '' ? parseFloat(maxFillHoursVal) : Infinity;
    if (isNaN(maxFillHours) || maxFillHours < 0) maxFillHours = Infinity;

    saveFilters('flipping', {
        members: membersFilter, minMargin: document.getElementById('filterMinMargin').value,
        minVolume: document.getElementById('filterMinVolume').value, minGpHr: document.getElementById('filterMinGpHr').value,
        minConfidence: document.getElementById('filterMinConfidence').value, maxFillHours: document.getElementById('filterMaxFillHours').value
    });

    var searchEl = document.getElementById('itemSearch');
    var searchTerm = searchEl ? searchEl.value.toLowerCase() : '';

    var filtered = items.filter(function (item) {
        if (membersFilter === 'members' && !item.members) return false;
        if (membersFilter === 'f2p' && item.members) return false;
        if (item.margin < minMargin) return false;
        if (item.volume24Hr < minVolume) return false;
        if (item.gpPerHour < minGpHr) return false;
        if (item.confidenceRating < minConfidence) return false;
        if (item.estimatedFillHours > maxFillHours) return false;
        if (searchTerm && item.name.toLowerCase().indexOf(searchTerm) === -1) return false;
        return true;
    });

    if (showFavoritesOnly) {
        var favs = getFavorites('flipping');
        filtered = filtered.filter(function(item) { return favs.indexOf(item.itemId) >= 0; });
    }

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
    currentPage = 1;
    renderPage();
}

function renderPage() {
    var totalPages = Math.max(1, Math.ceil(currentFiltered.length / pageSize));
    if (currentPage > totalPages) currentPage = totalPages;

    var start = (currentPage - 1) * pageSize;
    var end = Math.min(start + pageSize, currentFiltered.length);
    var pageData = currentFiltered.slice(start, end);

    renderTable(pageData);
    renderPagination(totalPages);
    document.getElementById('showingCount').textContent =
        (start + 1) + '–' + end + ' of ' + currentFiltered.length;
}

function goToPage(page) {
    currentPage = page;
    renderPage();
    document.getElementById('flipTable').scrollIntoView({ behavior: 'smooth' });
}

function changePageSize(size) {
    pageSize = size;
    currentPage = 1;
    renderPage();
}

function renderPagination(totalPages) {
    var container = document.getElementById('paginationControls');
    if (!container) return;

    var html = '<div class="d-flex align-items-center justify-content-between flex-wrap gap-2">';

    // Page size selector
    html += '<div class="d-flex align-items-center gap-2">';
    html += '<span class="text-muted" style="font-size:0.85rem">Per page:</span>';
    var sizes = [25, 50, 100, 200];
    for (var s = 0; s < sizes.length; s++) {
        var active = sizes[s] === pageSize ? ' active' : '';
        html += '<button class="btn btn-sm btn-outline-secondary' + active + '" onclick="changePageSize(' + sizes[s] + ')">' + sizes[s] + '</button>';
    }
    html += '</div>';

    // Page buttons
    html += '<nav><ul class="pagination pagination-sm mb-0">';

    // Previous
    html += '<li class="page-item' + (currentPage === 1 ? ' disabled' : '') + '">';
    html += '<a class="page-link" href="javascript:void(0)" onclick="goToPage(' + (currentPage - 1) + ')">&laquo;</a></li>';

    // Page numbers — show up to 7 pages around current
    var startPage = Math.max(1, currentPage - 3);
    var endPage = Math.min(totalPages, startPage + 6);
    if (endPage - startPage < 6) startPage = Math.max(1, endPage - 6);

    if (startPage > 1) {
        html += '<li class="page-item"><a class="page-link" href="javascript:void(0)" onclick="goToPage(1)">1</a></li>';
        if (startPage > 2) html += '<li class="page-item disabled"><span class="page-link">...</span></li>';
    }

    for (var p = startPage; p <= endPage; p++) {
        var cls = p === currentPage ? ' active' : '';
        html += '<li class="page-item' + cls + '"><a class="page-link" href="javascript:void(0)" onclick="goToPage(' + p + ')">' + p + '</a></li>';
    }

    if (endPage < totalPages) {
        if (endPage < totalPages - 1) html += '<li class="page-item disabled"><span class="page-link">...</span></li>';
        html += '<li class="page-item"><a class="page-link" href="javascript:void(0)" onclick="goToPage(' + totalPages + ')">' + totalPages + '</a></li>';
    }

    // Next
    html += '<li class="page-item' + (currentPage === totalPages ? ' disabled' : '') + '">';
    html += '<a class="page-link" href="javascript:void(0)" onclick="goToPage(' + (currentPage + 1) + ')">&raquo;</a></li>';

    html += '</ul></nav>';
    html += '</div>';

    container.innerHTML = html;
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
    document.getElementById('filterMinMargin').value = '';
    document.getElementById('filterMinVolume').value = '';
    document.getElementById('filterMinGpHr').value = '';
    document.getElementById('filterMinConfidence').value = '';
    document.getElementById('filterMaxFillHours').value = '';
    localStorage.removeItem('osrs_filters_flipping');
    applyFilters();
}

function trendArrow(trend) {
    if (trend === 1) return ' <span class="profit-positive" title="Rising">&#9650;</span>';
    if (trend === -1) return ' <span class="profit-negative" title="Falling">&#9660;</span>';
    return '';
}

function formatAge(isoString) {
    if (!isoString) return '<span class="text-secondary">--</span>';
    var seconds = Math.floor((Date.now() - new Date(isoString).getTime()) / 1000);
    if (seconds < 0) seconds = 0;
    var ageClass = seconds > 900 ? 'profit-negative' : (seconds > 300 ? 'text-secondary' : 'profit-positive');
    var text;
    if (seconds < 60) text = seconds + 's ago';
    else if (seconds < 3600) text = Math.floor(seconds / 60) + 'm ago';
    else if (seconds < 86400) text = Math.floor(seconds / 3600) + 'h ago';
    else text = Math.floor(seconds / 86400) + 'd ago';
    return '<span class="' + ageClass + '">' + text + '</span>';
}

function renderTable(data) {
    var tbody = document.getElementById('flipTableBody');
    var html = '';

    var favs = getFavorites('flipping');

    for (var i = 0; i < data.length; i++) {
        var item = data[i];
        var profitClass = item.profitPerUnit > 0 ? 'profit-positive' : item.profitPerUnit < 0 ? 'profit-negative' : 'profit-neutral';
        var confidenceClass = item.confidenceRating >= 0.8 ? 'profit-positive' : item.confidenceRating >= 0.5 ? 'text-warning' : 'profit-negative';
        var isFav = favs.indexOf(item.itemId) >= 0;

        html += '<tr style="cursor:pointer" onclick="showItemDetail(' + item.itemId + ')">';
        html += '<td class="text-center" style="cursor:pointer;color:' + (isFav ? 'var(--accent)' : 'var(--text-secondary)') + '" onclick="event.stopPropagation();toggleFavorite(\'flipping\',' + item.itemId + ')">' + (isFav ? '&#9733;' : '&#9734;') + '</td>';
        var iconHtml = item.iconUrl ? '<img src="' + escapeHtml(item.iconUrl) + '" class="item-icon" alt="" loading="lazy"> ' : '';
        html += '<td>' + iconHtml + escapeHtml(item.name) + (item.members ? ' <i class="bi bi-star-fill text-warning" style="font-size:0.7rem" title="Members"></i>' : '') + '</td>';
        html += '<td>' + formatGp(item.recommendedBuyPrice) + trendArrow(item.buyTrend) + '</td>';
        html += '<td>' + formatGp(item.recommendedSellPrice) + trendArrow(item.sellTrend) + '</td>';
        html += '<td class="' + profitClass + '">' + formatGp(item.margin) + '</td>';
        html += '<td>' + formatGp(item.taxAmount) + '</td>';
        html += '<td class="' + profitClass + '">' + formatGp(item.profitPerUnit) + '</td>';
        html += '<td>' + formatNumber(item.quantity) + '</td>';
        html += '<td class="' + profitClass + '">' + formatNumber(item.totalProfit) + ' gp</td>';
        html += '<td class="' + profitClass + '">' + formatNumber(item.profitPerCycle) + ' gp</td>';
        html += '<td class="' + profitClass + '">' + item.roiPercent.toFixed(2) + '%</td>';
        html += '<td>' + formatGpHr(item.gpPerHour) + '</td>';
        html += '<td class="' + confidenceClass + '">' + item.confidenceRating.toFixed(2) + '</td>';
        html += '<td>' + item.flipScore.toFixed(1) + '</td>';
        var volHtml = formatNumber(item.volume24Hr);
        var w24 = (item.windowPrices || []).find(function(w) { return w.window === '24 hour'; });
        if (w24 && w24.buyVolume && w24.sellVolume) {
            var bPct = w24.buyVolume / (w24.buyVolume + w24.sellVolume) * 100;
            if (bPct < 25 || bPct > 75) volHtml += ' <span title="Volume imbalance" style="color:var(--profit-negative)">&#9888;</span>';
        }
        html += '<td>' + volHtml + '</td>';
        html += '<td>' + formatAge(item.lastTradeTime) + '</td>';
        html += '</tr>';
    }

    tbody.innerHTML = html;
}

function formatGp(value) {
    if (value === null || value === undefined) return '--';
    return value.toLocaleString() + ' gp';
}

function formatGpHr(value) {
    if (value === null || value === undefined) return '--';
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

function detailStat(label, value) {
    return '<div class="col-6 col-md-3 mb-2">'
        + '<div style="color:#adb5bd;font-size:0.75rem;text-transform:uppercase;letter-spacing:0.5px">' + label + '</div>'
        + '<div class="fw-bold" style="font-size:1rem">' + value + '</div>'
        + '</div>';
}

function exportCsv() {
    var headers = ['Name','Buy','Sell','Margin','Tax','Profit/Unit','Qty',
                   'Total Profit','Profit/Cycle','ROI%','GP/hr','Confidence',
                   'Score','Volume 24h','Last Trade'];
    var rows = currentFiltered.map(function(item) {
        return [
            item.name, item.recommendedBuyPrice, item.recommendedSellPrice,
            item.margin, item.taxAmount, item.profitPerUnit, item.quantity,
            item.totalProfit, item.profitPerCycle, item.roiPercent,
            Math.round(item.gpPerHour), item.confidenceRating,
            item.flipScore, item.volume24Hr, item.lastTradeTime || ''
        ];
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
    a.download = 'osrs-flipping-' + new Date().toISOString().slice(0,10) + '.csv';
    a.click();
    URL.revokeObjectURL(url);
}

function showItemDetail(itemId) {
    var item = items.find(function (i) { return i.itemId === itemId; });
    if (!item) return;

    var title = document.getElementById('itemDetailLabel');
    title.textContent = item.name + (item.members ? ' (Members)' : ' (F2P)');

    var body = document.getElementById('itemDetailBody');
    var html = '';

    // Pricing section
    html += '<h6 style="color:#e9ecef;border-bottom:1px solid #495057;padding-bottom:6px;margin-bottom:12px">Pricing</h6>';
    html += '<div class="row">';
    html += detailStat('Buy Price', formatGp(item.recommendedBuyPrice) + trendArrow(item.buyTrend));
    html += detailStat('Sell Price', formatGp(item.recommendedSellPrice) + trendArrow(item.sellTrend));
    html += detailStat('Margin', formatGp(item.margin));
    html += detailStat('Tax', formatGp(item.taxAmount));

    // Instant price gap (Improvement 4)
    if (item.latestBuyPrice) {
        var buyGap = Math.abs(item.latestBuyPrice - item.recommendedBuyPrice) / item.recommendedBuyPrice * 100;
        var buyGapClass = buyGap > 5 ? 'profit-negative' : '';
        var buyDir = item.latestBuyPrice > item.recommendedBuyPrice ? '+' : '';
        html += detailStat('Latest Buy', formatGp(item.latestBuyPrice) + ' <span class="' + buyGapClass + '" style="font-size:0.8rem">(' + buyDir + buyGap.toFixed(1) + '%)</span>');
    }
    if (item.latestSellPrice) {
        var sellGap = Math.abs(item.latestSellPrice - item.recommendedSellPrice) / item.recommendedSellPrice * 100;
        var sellGapClass = sellGap > 5 ? 'profit-negative' : '';
        var sellDir = item.latestSellPrice > item.recommendedSellPrice ? '+' : '';
        html += detailStat('Latest Sell', formatGp(item.latestSellPrice) + ' <span class="' + sellGapClass + '" style="font-size:0.8rem">(' + sellDir + sellGap.toFixed(1) + '%)</span>');
    }
    html += '</div>';

    // Profitability section
    html += '<h6 style="color:#e9ecef;border-bottom:1px solid #495057;padding-bottom:6px;margin-bottom:12px;margin-top:16px">Profitability</h6>';
    html += '<div class="row">';
    html += detailStat('Profit / Unit', formatGp(item.profitPerUnit));
    html += detailStat('Profit / Cycle (4h)', formatNumber(item.profitPerCycle) + ' gp');
    html += detailStat('Total Profit', formatNumber(item.totalProfit) + ' gp');
    html += detailStat('ROI', item.roiPercent.toFixed(2) + '%');
    html += detailStat('GP / Hour', formatGpHr(item.gpPerHour));
    html += detailStat('Est. Fill Time', item.estimatedFillHours.toFixed(1) + ' hrs');
    html += '</div>';

    // Item info section
    html += '<h6 style="color:#e9ecef;border-bottom:1px solid #495057;padding-bottom:6px;margin-bottom:12px;margin-top:16px">Item Info</h6>';
    html += '<div class="row">';
    html += detailStat('Buy Limit', formatNumber(item.buyLimit));
    html += detailStat('Quantity', formatNumber(item.quantity));
    html += detailStat('24h Volume', formatNumber(item.volume24Hr));
    html += detailStat('Volatility', item.priceVolatilityPercent.toFixed(1) + '%');
    html += detailStat('Score', item.flipScore.toFixed(1) + ' / 10');
    html += detailStat('Confidence', item.confidenceRating.toFixed(2));
    html += detailStat('Buy Windows', item.buyWindowsUsed + ' of 4');
    html += detailStat('Sell Windows', item.sellWindowsUsed + ' of 4');
    html += detailStat('Last Trade', formatAge(item.lastTradeTime));

    // Volume balance (Improvement 3)
    var w24h = (item.windowPrices || []).find(function(w) { return w.window === '24 hour'; });
    if (w24h && w24h.buyVolume && w24h.sellVolume) {
        var totalBuySell = w24h.buyVolume + w24h.sellVolume;
        var buyPct = Math.round(w24h.buyVolume / totalBuySell * 100);
        var sellPct = 100 - buyPct;
        var balanceClass = Math.abs(buyPct - 50) > 25 ? 'profit-negative' : 'profit-positive';
        html += detailStat('Volume Balance', '<span class="' + balanceClass + '">Buy ' + buyPct + '% / Sell ' + sellPct + '%</span>');
    }
    html += '</div>';

    // Window prices table
    html += '<h6 style="color:#e9ecef;border-bottom:1px solid #495057;padding-bottom:6px;margin-bottom:12px;margin-top:16px">Time Window Prices</h6>';

    // Check for volume inconsistencies
    var windows = item.windowPrices || [];
    var hasVolumeAnomaly = false;
    var windowVolumes = {};
    for (var w = 0; w < windows.length; w++) {
        var vol = ((windows[w].buyVolume || 0) + (windows[w].sellVolume || 0));
        windowVolumes[windows[w].window] = vol;
    }
    if (windowVolumes['24 hour'] !== undefined && windowVolumes['6 hour'] !== undefined
        && windowVolumes['24 hour'] < windowVolumes['6 hour']) {
        hasVolumeAnomaly = true;
    }
    if (windowVolumes['6 hour'] !== undefined && windowVolumes['1 hour'] !== undefined
        && windowVolumes['6 hour'] < windowVolumes['1 hour']) {
        hasVolumeAnomaly = true;
    }

    if (hasVolumeAnomaly) {
        html += '<div class="alert alert-warning py-2 px-3 mb-2" style="font-size:0.85rem">';
        html += '<i class="bi bi-exclamation-triangle me-1"></i>';
        html += 'Volume anomaly detected — a shorter window reports higher volume than a longer one. ';
        html += 'This is a known OSRS Wiki API data quality issue where some windows have stale or incomplete data.';
        html += '</div>';
    }

    html += '<div style="overflow-x:auto">';
    html += '<table class="table table-sm table-dark table-bordered mb-0">';
    html += '<thead><tr>';
    html += '<th>Window</th>';
    html += '<th>Avg Buy Price</th>';
    html += '<th>Avg Sell Price</th>';
    html += '<th>Spread</th>';
    html += '<th>Buy Volume</th>';
    html += '<th>Sell Volume</th>';
    html += '<th>Total Volume</th>';
    html += '</tr></thead><tbody>';

    for (var w = 0; w < windows.length; w++) {
        var wp = windows[w];
        var buyP = wp.avgBuyPrice;
        var sellP = wp.avgSellPrice;
        var spread = (buyP && sellP) ? (buyP - sellP) : null;
        var buyVol = wp.buyVolume;
        var sellVol = wp.sellVolume;
        var totalVol = (buyVol || 0) + (sellVol || 0);
        var showTotalVol = (buyVol !== null || sellVol !== null) ? totalVol : null;

        html += '<tr>';
        html += '<td class="fw-bold">' + escapeHtml(wp.window) + '</td>';
        html += '<td>' + (buyP ? formatGp(buyP) : '<span style="color:#6c757d">--</span>') + '</td>';
        html += '<td>' + (sellP ? formatGp(sellP) : '<span style="color:#6c757d">--</span>') + '</td>';
        html += '<td>' + (spread !== null ? formatGp(spread) : '<span style="color:#6c757d">--</span>') + '</td>';
        html += '<td>' + (buyVol !== null ? formatNumber(buyVol) : '<span style="color:#6c757d">--</span>') + '</td>';
        html += '<td>' + (sellVol !== null ? formatNumber(sellVol) : '<span style="color:#6c757d">--</span>') + '</td>';
        html += '<td>' + (showTotalVol !== null ? formatNumber(showTotalVol) : '<span style="color:#6c757d">--</span>') + '</td>';
        html += '</tr>';
    }

    if (windows.length === 0) {
        html += '<tr><td colspan="7" style="color:#6c757d" class="text-center">No window data available</td></tr>';
    }

    html += '</tbody></table>';
    html += '</div>';

    body.innerHTML = html;

    var modal = new bootstrap.Modal(document.getElementById('itemDetailModal'));
    modal.show();
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
    fetch('/Flipping/Data')
        .then(function(r) { return r.json(); })
        .then(function(data) {
            items = data;
            applyFilters();
            var interval = parseInt(document.getElementById('refreshInterval').value);
            refreshSecondsLeft = interval;
            if (interval <= 0) stopAutoRefresh();
            var syncEl = document.getElementById('syncStatus');
            if (syncEl) syncEl.querySelector('span').textContent = 'Last sync: ' + new Date().toLocaleTimeString() + ' (auto)';
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
