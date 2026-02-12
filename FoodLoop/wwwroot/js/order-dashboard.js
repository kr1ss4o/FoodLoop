document.addEventListener("DOMContentLoaded", () => {

    function setupToggle(btnId, boxId) {
        const btn = document.getElementById(btnId);
        const box = document.getElementById(boxId);

        if (!btn || !box) return;

        let expanded = false;

        btn.addEventListener("click", () => {
            expanded = !expanded;

            box.style.display = expanded ? "block" : "none";
            btn.textContent = expanded ? "Show less" : "Show more";
        });
    }

    setupToggle("btn-confirmed-toggle", "confirmed-more");
    setupToggle("btn-history-toggle", "history-more");
});

// Show more / Show less for OrdersDashboard