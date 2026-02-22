// Smithing calculator — client-side filtering and sorting for two tabs.

var sortState = {
    cannonballs: { field: 'profitPerUnit', ascending: false },
    dartTips:    { field: 'profitPerUnit', ascending: false }
};

document.addEventListener('DOMContentLoaded', function () {
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

    var minProfit    = parseInt(document.getElementById('filterMinProfit').value) || 0;
    var minVolume    = parseInt(document.getElementById('filterMinVolume').value) || 0;
    var profitable   = document.getElementById('filterProfitable').value;

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
    applyFilters('cannonballs');
    applyFilters('dartTips');
    // Reset showing count to match whichever tab is active.
    applyFilters(getActiveTab());
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
        html += '<td>' + escapeHtml(item.name) + '</td>';
        html += '<td class="text-center">' + item.outputPerInput + '</td>';
        html += '<td>' + formatGp(item.barPrice) + '</td>';
        html += '<td>' + formatGp(item.outputPrice) + '</td>';
        html += '<td class="' + profitClass + '">' + formatGp(item.profitPerUnit) + '</td>';
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
