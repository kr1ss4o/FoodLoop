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

});