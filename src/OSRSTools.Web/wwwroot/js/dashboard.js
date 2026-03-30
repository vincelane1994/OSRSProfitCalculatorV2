// Dashboard carousels for all profit calculator panels

(function () {
    var AUTO_INTERVAL = 5000; // 5 seconds

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

    function createCarousel(items, ids, renderFn) {
        if (!items || items.length === 0) return;

        var currentIndex = 0;
        var autoTimer = null;

        var prevBtn = document.getElementById(ids.prev);
        var nextBtn = document.getElementById(ids.next);
        var dotsContainer = document.getElementById(ids.dots);

        if (!prevBtn || !nextBtn || !dotsContainer) return;

        function buildDots() {
            for (var i = 0; i < items.length; i++) {
                var dot = document.createElement('span');
                dot.className = 'carousel-dot';
                dot.dataset.index = i;
                dot.addEventListener('click', function () {
                    showItem(parseInt(this.dataset.index));
                    resetAutoTimer();
                });
                dotsContainer.appendChild(dot);
            }
        }

        function updateDots() {
            var dots = dotsContainer.querySelectorAll('.carousel-dot');
            for (var i = 0; i < dots.length; i++) {
                dots[i].classList.toggle('active', i === currentIndex);
            }
        }

        function showItem(index) {
            currentIndex = index;
            renderFn(items[index]);
            updateDots();
        }

        function nextItem() {
            showItem((currentIndex + 1) % items.length);
        }

        function prevItem() {
            showItem((currentIndex - 1 + items.length) % items.length);
        }

        function startAutoTimer() {
            autoTimer = setInterval(nextItem, AUTO_INTERVAL);
        }

        function resetAutoTimer() {
            clearInterval(autoTimer);
            startAutoTimer();
        }

        buildDots();
        showItem(0);
        startAutoTimer();

        prevBtn.addEventListener('click', function () { prevItem(); resetAutoTimer(); });
        nextBtn.addEventListener('click', function () { nextItem(); resetAutoTimer(); });
    }

    document.addEventListener('DOMContentLoaded', function () {

        // High Alching carousel
        createCarousel(
            typeof topAlchItems !== 'undefined' ? topAlchItems : [],
            { prev: 'alchPrev', next: 'alchNext', dots: 'alchDots' },
            function (item) {
                document.getElementById('alchItemName').textContent = item.name;
                document.getElementById('alchItemProfit').textContent = 'Profit: ' + formatGp(item.profit);
                document.getElementById('alchItemRoi').textContent = 'ROI: ' + item.roiPercent.toFixed(2) + '%';
            }
        );

        // Flipping carousel
        createCarousel(
            typeof topFlipItems !== 'undefined' ? topFlipItems : [],
            { prev: 'flipPrev', next: 'flipNext', dots: 'flipDots' },
            function (item) {
                document.getElementById('flipItemName').textContent = item.name;
                document.getElementById('flipItemGpHr').textContent = formatGpHr(item.gpPerHour);
                document.getElementById('flipItemRoi').textContent = 'ROI: ' + item.roiPercent.toFixed(2) + '%';
            }
        );

        // Smithing carousel
        createCarousel(
            typeof topSmithingItems !== 'undefined' ? topSmithingItems : [],
            { prev: 'smithPrev', next: 'smithNext', dots: 'smithDots' },
            function (item) {
                document.getElementById('smithItemName').textContent = item.name;
                document.getElementById('smithItemProfit').textContent = 'Profit: ' + formatGp(item.profitPerUnit) + '/bar';
                document.getElementById('smithItemRoi').textContent = 'ROI: ' + item.roiPercent.toFixed(2) + '%';
            }
        );

        // Herblore carousel
        createCarousel(
            typeof topHerbloreItems !== 'undefined' ? topHerbloreItems : [],
            { prev: 'herbPrev', next: 'herbNext', dots: 'herbDots' },
            function (item) {
                document.getElementById('herbItemName').textContent = item.name;
                document.getElementById('herbItemProfit').textContent = 'Profit: ' + formatGp(item.profitPerUnit) + '/op';
                document.getElementById('herbItemMethod').textContent = item.method;
            }
        );

    });
})();
