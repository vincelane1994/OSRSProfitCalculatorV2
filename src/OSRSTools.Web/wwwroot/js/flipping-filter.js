// Flipping calculator client-side filtering, sorting, and pagination

var currentSort = { field: 'flipScore', ascending: false };
var currentPage = 1;
var pageSize = 50;
var currentFiltered = [];

document.addEventListener('DOMContentLoaded', function () {
    applyFilters();
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

    var filtered = items.filter(function (item) {
        if (membersFilter === 'members' && !item.members) return false;
        if (membersFilter === 'f2p' && item.members) return false;
        if (item.margin < minMargin) return false;
        if (item.volume24Hr < minVolume) return false;
        if (item.gpPerHour < minGpHr) return false;
        if (item.confidenceRating < minConfidence) return false;
        if (item.estimatedFillHours > maxFillHours) return false;
        return true;
    });

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
    applyFilters();
}

function renderTable(data) {
    var tbody = document.getElementById('flipTableBody');
    var html = '';

    for (var i = 0; i < data.length; i++) {
        var item = data[i];
        var profitClass = item.profitPerUnit > 0 ? 'profit-positive' : item.profitPerUnit < 0 ? 'profit-negative' : 'profit-neutral';
        var confidenceClass = item.confidenceRating >= 0.8 ? 'profit-positive' : item.confidenceRating >= 0.5 ? 'text-warning' : 'profit-negative';

        html += '<tr style="cursor:pointer" onclick="showItemDetail(' + item.itemId + ')">';
        html += '<td>' + escapeHtml(item.name) + (item.members ? ' <i class="bi bi-star-fill text-warning" style="font-size:0.7rem" title="Members"></i>' : '') + '</td>';
        html += '<td>' + formatGp(item.recommendedBuyPrice) + '</td>';
        html += '<td>' + formatGp(item.recommendedSellPrice) + '</td>';
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
        html += '<td>' + formatNumber(item.volume24Hr) + '</td>';
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
    html += detailStat('Buy Price', formatGp(item.recommendedBuyPrice));
    html += detailStat('Sell Price', formatGp(item.recommendedSellPrice));
    html += detailStat('Margin', formatGp(item.margin));
    html += detailStat('Tax', formatGp(item.taxAmount));
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
