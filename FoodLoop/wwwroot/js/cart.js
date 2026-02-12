document.addEventListener("DOMContentLoaded", () => {
    const btn = document.getElementById("showMoreBtn");
    const moreBox = document.getElementById("moreHistory");

    if (btn && moreBox) {
        btn.addEventListener("click", () => {
            if (moreBox.style.display === "none") {
                moreBox.style.display = "block";
                btn.textContent = "Hide";
            } else {
                moreBox.style.display = "none";
                btn.textContent = "Show more";
            }
        });
    }
});

// Show more / Show less for Client Cart