document.addEventListener("DOMContentLoaded", function () {

    const cartBadge = document.getElementById("cartBadge");
    const miniCart = document.getElementById("miniCartDropdown");
    const cartLink = document.getElementById("cartLink");

    /* ==============================================
       EVENT-BASED BADGE UPDATE
    ============================================== */

    document.addEventListener("cartUpdated", function (e) {

        const count = e.detail.count;

        if (!cartBadge) return;

        cartBadge.innerText = count;

        if (count > 0)
            cartBadge.classList.remove("d-none");
        else
            cartBadge.classList.add("d-none");
    });

    /* ==============================================
       MINI CART LOAD
    ============================================== */

    window.loadMiniCart = async function () {

        if (!miniCart) return;

        const content = document.getElementById("miniCartContent");

        const response = await fetch("/Cart/MiniCart");
        const html = await response.text();

        content.innerHTML = html;
    };

    /* ==============================================
       MINI CART HOVER
    ============================================== */

    if (cartLink && miniCart) {

        cartLink.addEventListener("mouseenter", async function () {
            miniCart.classList.remove("d-none");
            await loadMiniCart();
        });

        miniCart.addEventListener("mouseleave", function () {
            miniCart.classList.add("d-none");
        });
    }

});