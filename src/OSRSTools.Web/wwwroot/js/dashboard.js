// Dashboard mini-tables for all profit calculator panels

(function () {

    function formatGp(value) {
        if (value >= 1000000) return (value / 1000000).toFixed(1) + 'M gp';
        if (value >= 1000) return (value / 1000).toFixed(1) + 'K gp';
        return value.toLocaleString() + ' gp';
    }

    function formatGpHr(value) {
        if (value >= 1000000) return (value / 1000000).toFixed(1) + 'M gp/hr';
        if (value >= 1000) return (value / 1000).toFixed(1) + 'K gp/hr';
        return Math.round(value).toLocaleString() + ' gp/hr';
    }

    function escapeHtml(text) {
        var div = document.createElement('div');
        div.appendChild(document.createTextNode(text));
        return div.innerHTML;
    }

    function renderMiniTable(tableId, items, columns) {
        var table = document.getElementById(tableId);
        if (!table) return;
        var tbody = table.querySelector('tbody');
        if (!items || items.length === 0) {
            tbody.innerHTML = '<tr><td colspan="' + columns.length + '" class="text-center" style="color:var(--text-secondary);padding:12px;">No data</td></tr>';
            return;
        }
        tbody.innerHTML = items.map(function(item) {
            return '<tr style="border-color:var(--card-border);">' + columns.map(function(col) {
                return '<td style="border-color:var(--card-border);padding:6px 8px;font-size:0.85rem;">' + col.format(item) + '</td>';
            }).join('') + '</tr>';
        }).join('');
    }

    document.addEventListener('DOMContentLoaded', function () {
        // High Alching
        renderMiniTable('topAlchTable',
            typeof topAlchItems !== 'undefined' ? topAlchItems : [],
            [
                { format: function(i) { return (i.iconUrl ? '<img src="' + escapeHtml(i.iconUrl) + '" class="item-icon" alt="" loading="lazy"> ' : '') + escapeHtml(i.name); } },
                { format: function(i) { return '<span class="profit-positive">' + formatGp(i.profit) + '</span>'; } },
                { format: function(i) { return i.roiPercent.toFixed(2) + '%'; } }
            ]
        );

        // Flipping
        renderMiniTable('topFlipsTable',
            typeof topFlipItems !== 'undefined' ? topFlipItems : [],
            [
                { format: function(i) { return (i.iconUrl ? '<img src="' + escapeHtml(i.iconUrl) + '" class="item-icon" alt="" loading="lazy"> ' : '') + escapeHtml(i.name); } },
                { format: function(i) { return '<span class="profit-positive">' + formatGpHr(i.gpPerHour) + '</span>'; } },
                { format: function(i) { return i.roiPercent.toFixed(2) + '%'; } }
            ]
        );

        // Smithing
        renderMiniTable('topSmithingTable',
            typeof topSmithingItems !== 'undefined' ? topSmithingItems : [],
            [
                { format: function(i) { return (i.iconUrl ? '<img src="' + escapeHtml(i.iconUrl) + '" class="item-icon" alt="" loading="lazy"> ' : '') + escapeHtml(i.name); } },
                { format: function(i) { return '<span class="profit-positive">' + formatGp(i.profitPerUnit) + '/bar</span>'; } },
                { format: function(i) { return i.roiPercent.toFixed(2) + '%'; } }
            ]
        );

        // Herblore
        renderMiniTable('topHerbloreTable',
            typeof topHerbloreItems !== 'undefined' ? topHerbloreItems : [],
            [
                { format: function(i) { return (i.iconUrl ? '<img src="' + escapeHtml(i.iconUrl) + '" class="item-icon" alt="" loading="lazy"> ' : '') + escapeHtml(i.name); } },
                { format: function(i) { return '<span class="profit-positive">' + formatGp(i.profitPerUnit) + '</span>'; } },
                { format: function(i) { return i.method || '--'; } }
            ]
        );
    });
})();
