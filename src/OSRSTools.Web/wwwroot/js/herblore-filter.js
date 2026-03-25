// Herblore calculator — client-side filtering and sorting for three tabs.

var sortState = {
    cleaning:     { field: 'profitPerUnit', ascending: false },
    fullProcess:  { field: 'profitPerUnit', ascending: false },
    potionMaking: { field: 'profitPerUnit', ascending: false }
};

document.addEventListener('DOMContentLoaded', function () {
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

    var minProfit  = parseInt(document.getElementById('filterMinProfit').value) || 0;
    var minVolume  = parseInt(document.getElementById('filterMinVolume').value) || 0;
    var profitable = document.getElementById('filterProfitable').value;

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
    var active = getActiveTab();
    ['cleaning', 'fullProcess', 'potionMaking'].forEach(function (tab) {
        if (tab !== active) applyFilters(tab);
    });
    applyFilters(active);
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
        html += '<td>' + escapeHtml(item.herbName) + '</td>';

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
