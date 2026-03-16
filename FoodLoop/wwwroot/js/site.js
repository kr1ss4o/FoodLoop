document.addEventListener("DOMContentLoaded", function () {

    const cartBadge = document.getElementById("cartBadge");
    const miniCart = document.getElementById("miniCartDropdown");
    const cartLink = document.getElementById("cartLink");

    
    //EVENT-BASED BADGE UPDATE

    document.addEventListener("cartUpdated", function (e) {

        const count = e.detail.count;

        if (!cartBadge) return;

        cartBadge.innerText = count;

        if (count > 0)
            cartBadge.classList.remove("d-none");
        else
            cartBadge.classList.add("d-none");
    });

    
    //MINI CART LOAD

    async function loadMiniCart() {

        if (!miniCart) return;

        const content = document.getElementById("miniCartContent");

        const response = await fetch("/Cart/MiniCart");
        const html = await response.text();

        content.innerHTML = html;
    }

    //MINI CART STABLE HOVER

    if (miniCart) {

        const wrapper = miniCart.closest(".nav-item.position-relative");

        let hideTimeout;

        wrapper.addEventListener("mouseenter", async function () {

            clearTimeout(hideTimeout);

            miniCart.classList.remove("d-none");

            setTimeout(() => {
                miniCart.classList.add("show");
            }, 10);

            await loadMiniCart();
        });

        wrapper.addEventListener("mouseleave", function () {

            miniCart.classList.remove("show");

            hideTimeout = setTimeout(() => {
                miniCart.classList.add("d-none");
            }, 200);
        });
    }

    // Categories carousel navigation with snapping so items are never partially visible
    function initCategoriesCarousel() {
        const categoriesCarousel = document.querySelector('.categories-carousel');
        const btnLeft = document.querySelector('.categories-left');
        const btnRight = document.querySelector('.categories-right');

        if (!categoriesCarousel || !btnLeft || !btnRight) return;

        const cards = Array.from(categoriesCarousel.querySelectorAll('.category-card'));
        if (!cards.length) return;

        function getGap() {
            const style = getComputedStyle(categoriesCarousel);
            // try columnGap or gap
            const gap = style.columnGap || style.gap || style.getPropertyValue('gap');
            return parseFloat(gap) || 0;
        }

        function getCardFullWidth() {
            // offsetWidth includes padding and border; gap is added between cards
            const cardW = cards[0].offsetWidth;
            return cardW + getGap();
        }

        function visibleCount() {
            const full = getCardFullWidth();
            const v = Math.floor(categoriesCarousel.clientWidth / full);
            return Math.max(1, v);
        }

        // Determine scroll step. By default we use visibleCount, but allow override via
        // `data-step` on the carousel element. We don't clamp step to visibleCount here
        // because pages calculation will ensure the last page never shows a single orphan item.
        const configuredStep = parseInt(categoriesCarousel.dataset.step);
        function getStep() {
            if (Number.isFinite(configuredStep) && configuredStep > 0) return configuredStep;
            return visibleCount();
        }

        // Build page start indices so each page starts at a whole card and the last page
        // shows up to `visibleCount()` cards (no single orphan left behind).
        function buildPages() {
            const step = getStep();
            const vis = visibleCount();
            const pages = [];
            const maxStart = Math.max(0, cards.length - vis);

            for (let i = 0; i < cards.length; i += step) {
                const start = Math.min(i, maxStart);
                pages.push(start);
                if (start === maxStart) break;
            }

            // ensure final page includes the last visible block (so last card is not orphaned)
            if (!pages.includes(maxStart)) pages.push(maxStart);

            // unique and sorted
            return Array.from(new Set(pages)).sort((a, b) => a - b);
        }

        function scrollToIndex(index) {
            const full = getCardFullWidth();
            const maxIndex = Math.max(0, cards.length - visibleCount());
            const clamped = Math.max(0, Math.min(index, maxIndex));
            const target = Math.round(clamped * full);
            categoriesCarousel.scrollTo({ left: target, behavior: 'smooth' });
        }

        function currentIndex() {
            const full = getCardFullWidth();
            return Math.round(categoriesCarousel.scrollLeft / full);
        }

        btnLeft.addEventListener('click', function (e) {
            e.preventDefault();
            const pages = buildPages();
            const idx = currentIndex();
            // find current page
            let pageIdx = pages.findIndex((p, i) => {
                const next = pages[i + 1] !== undefined ? pages[i + 1] : Infinity;
                return idx >= p && idx < next;
            });
            if (pageIdx === -1) pageIdx = 0;
            const targetPage = Math.max(0, pageIdx - 1);
            scrollToIndex(pages[targetPage]);
        });

        btnRight.addEventListener('click', function (e) {
            e.preventDefault();
            const pages = buildPages();
            const idx = currentIndex();
            let pageIdx = pages.findIndex((p, i) => {
                const next = pages[i + 1] !== undefined ? pages[i + 1] : Infinity;
                return idx >= p && idx < next;
            });
            if (pageIdx === -1) pageIdx = 0;
            const targetPage = Math.min(pages.length - 1, pageIdx + 1);
            scrollToIndex(pages[targetPage]);
        });

        // Optional: snap to nearest on resize/end of scroll to avoid partial items left behind
        let resizeTimer;
        window.addEventListener('resize', function () {
            clearTimeout(resizeTimer);
            resizeTimer = setTimeout(function () {
                // snap to nearest index after resize
                scrollToIndex(currentIndex());
            }, 120);
        });
    }

    // init (we're already inside DOMContentLoaded handler)
    initCategoriesCarousel();
});