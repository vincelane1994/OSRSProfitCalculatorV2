// High Alchemy client-side filtering and sorting

var currentSort = { field: 'roiPercent', ascending: false };

document.addEventListener('DOMContentLoaded', function () {
    applyFilters();
});

function applyFilters() {
    var membersFilter = document.getElementById('filterMembers').value;
    var minProfit = parseInt(document.getElementById('filterMinProfit').value) || 0;
    var maxBuyPrice = parseInt(document.getElementById('filterMaxBuyPrice').value) || Number.MAX_SAFE_INTEGER;
    var minVolume = parseInt(document.getElementById('filterMinVolume').value) || 0;
    var maxInvestment = parseInt(document.getElementById('filterMaxInvestment').value) || Number.MAX_SAFE_INTEGER;

    var filtered = items.filter(function (item) {
        if (membersFilter === 'members' && !item.members) return false;
        if (membersFilter === 'f2p' && item.members) return false;
        if (item.profit < minProfit) return false;
        if (item.buyPrice > maxBuyPrice) return false;
        if (item.volume24Hr < minVolume) return false;
        if (item.buyPrice * item.buyLimit > maxInvestment) return false;
        return true;
    });

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
    document.getElementById('filterMinVolume').value = '';
    document.getElementById('filterMaxInvestment').value = '1000000';
    applyFilters();
}

function renderTable(data) {
    var tbody = document.getElementById('alchTableBody');
    var html = '';

    for (var i = 0; i < data.length; i++) {
        var item = data[i];
        var profitClass = item.profit > 0 ? 'profit-positive' : item.profit < 0 ? 'profit-negative' : 'profit-neutral';

        html += '<tr>';
        html += '<td>' + escapeHtml(item.name) + (item.members ? ' <i class="bi bi-star-fill text-warning" style="font-size:0.7rem" title="Members"></i>' : '') + '</td>';
        html += '<td>' + formatGp(item.buyPrice) + '</td>';
        html += '<td>' + formatGp(item.highAlchValue) + '</td>';
        html += '<td>' + formatGp(item.natureRuneCost) + '</td>';
        html += '<td class="' + profitClass + '">' + formatGp(item.profit) + '</td>';
        html += '<td class="' + profitClass + '">' + item.roiPercent.toFixed(2) + '%</td>';
        html += '<td>' + formatNumber(item.volume24Hr) + '</td>';
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
