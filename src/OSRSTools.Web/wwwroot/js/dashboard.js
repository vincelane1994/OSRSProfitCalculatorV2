// Dashboard carousel for High Alchemy top items

(function () {
    var currentIndex = 0;
    var autoTimer = null;
    var AUTO_INTERVAL = 5000; // 5 seconds

    document.addEventListener('DOMContentLoaded', function () {
        if (typeof topAlchItems === 'undefined' || topAlchItems.length === 0) return;

        buildDots();
        showItem(0);
        startAutoTimer();

        document.getElementById('alchPrev').addEventListener('click', function () {
            prevItem();
            resetAutoTimer();
        });

        document.getElementById('alchNext').addEventListener('click', function () {
            nextItem();
            resetAutoTimer();
        });
    });

    function showItem(index) {
        var item = topAlchItems[index];
        currentIndex = index;

        document.getElementById('alchItemName').textContent = item.name;
        document.getElementById('alchItemProfit').textContent =
            'Profit: ' + item.profit.toLocaleString() + ' gp';
        document.getElementById('alchItemRoi').textContent =
            'ROI: ' + item.roiPercent.toFixed(2) + '%';

        updateDots();
    }

    function nextItem() {
        var next = (currentIndex + 1) % topAlchItems.length;
        showItem(next);
    }

    function prevItem() {
        var prev = (currentIndex - 1 + topAlchItems.length) % topAlchItems.length;
        showItem(prev);
    }

    function startAutoTimer() {
        autoTimer = setInterval(nextItem, AUTO_INTERVAL);
    }

    function resetAutoTimer() {
        clearInterval(autoTimer);
        startAutoTimer();
    }

    function buildDots() {
        var container = document.getElementById('alchDots');
        for (var i = 0; i < topAlchItems.length; i++) {
            var dot = document.createElement('span');
            dot.className = 'carousel-dot';
            dot.dataset.index = i;
            dot.addEventListener('click', function () {
                showItem(parseInt(this.dataset.index));
                resetAutoTimer();
            });
            container.appendChild(dot);
        }
    }

    function updateDots() {
        var dots = document.querySelectorAll('#alchDots .carousel-dot');
        for (var i = 0; i < dots.length; i++) {
            dots[i].classList.toggle('active', i === currentIndex);
        }
    }
})();
