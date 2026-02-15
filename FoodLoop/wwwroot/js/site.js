// ======================================================
// GLOBAL SITE JS
// ======================================================

document.addEventListener("DOMContentLoaded", function () {

    initBadges();

});


// ======================================================
// BADGE SYSTEM (Cart + Pending Orders)
// ======================================================

function initBadges() {

    fetch("/Badge/GetCounts")
        .then(response => {
            if (!response.ok) {
                throw new Error("Failed to load badge data.");
            }
            return response.json();
        })
        .then(data => {

            updateBadge("cartBadge", data.cart);
            updateBadge("pendingBadge", data.pending);

        })
        .catch(error => {
            console.error("Badge system error:", error);
        });
}

function updateBadge(elementId, value) {

    const badge = document.getElementById(elementId);
    if (!badge) return;

    if (value && value > 0) {
        badge.innerText = value;
        badge.classList.remove("d-none");
    } else {
        badge.innerText = "";
        badge.classList.add("d-none");
    }
}


// ======================================================
// OPTIONAL: expose manual refresh if needed later
// ======================================================

window.refreshBadges = function () {
    initBadges();
};