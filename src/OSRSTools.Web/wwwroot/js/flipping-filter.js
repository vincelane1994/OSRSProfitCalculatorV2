// Flipping calculator client-side filtering and sorting

var currentSort = { field: 'gpPerHour', ascending: false };

document.addEventListener('DOMContentLoaded', function () {
    applyFilters();
});

function applyFilters() {
    var membersFilter = document.getElementById('filterMembers').value;
    var minMargin = parseInt(document.getElementById('filterMinMargin').value) || 0;
    var minVolume = parseInt(document.getElementById('filterMinVolume').value) || 0;
    var minGpHr = parseInt(document.getElementById('filterMinGpHr').value) || 0;
    var minConfidence = parseFloat(document.getElementById('filterMinConfidence').value) || 0;

    var filtered = items.filter(function (item) {
        if (membersFilter === 'members' && !item.members) return false;
        if (membersFilter === 'f2p' && item.members) return false;
        if (item.margin < minMargin) return false;
        if (item.volume24Hr < minVolume) return false;
        if (item.gpPerHour < minGpHr) return false;
        if (item.confidenceRating < minConfidence) return false;
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
    document.getElementById('filterMinMargin').value = '';
    document.getElementById('filterMinVolume').value = '';
    document.getElementById('filterMinGpHr').value = '';
    document.getElementById('filterMinConfidence').value = '';
    applyFilters();
}

function renderTable(data) {
    var tbody = document.getElementById('flipTableBody');
    var html = '';

    for (var i = 0; i < data.length; i++) {
        var item = data[i];
        var profitClass = item.profitPerUnit > 0 ? 'profit-positive' : item.profitPerUnit < 0 ? 'profit-negative' : 'profit-neutral';
        var confidenceClass = item.confidenceRating >= 0.8 ? 'profit-positive' : item.confidenceRating >= 0.5 ? 'text-warning' : 'profit-negative';

        html += '<tr>';
        html += '<td>' + escapeHtml(item.name) + (item.members ? ' <i class="bi bi-star-fill text-warning" style="font-size:0.7rem" title="Members"></i>' : '') + '</td>';
        html += '<td>' + formatGp(item.recommendedBuyPrice) + '</td>';
        html += '<td>' + formatGp(item.recommendedSellPrice) + '</td>';
        html += '<td class="' + profitClass + '">' + formatGp(item.margin) + '</td>';
        html += '<td>' + formatGp(item.taxAmount) + '</td>';
        html += '<td class="' + profitClass + '">' + formatGp(item.profitPerUnit) + '</td>';
        html += '<td>' + formatNumber(item.quantity) + '</td>';
        html += '<td class="' + profitClass + '">' + formatNumber(item.totalProfit) + ' gp</td>';
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
