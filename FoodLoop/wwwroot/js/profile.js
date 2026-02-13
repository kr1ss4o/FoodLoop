document.addEventListener("DOMContentLoaded", () => {

    const modal = document.getElementById("editProfileModal");
    const openBtn = document.getElementById("epm-open");
    const closeBtn = document.getElementById("epm-close");
    const resetBtn = document.getElementById("epm-reset");

    const fullNameInput = document.getElementById("epm-fullname");
    const phoneInput = document.getElementById("epm-phone");
    const preview = document.getElementById("epm-preview");
    const fileInput = document.getElementById("epm-file");
    const text = document.getElementById("epm-text");

    // Store initial values so reset works correctly
    let initial = {
        fullName: "",
        phone: "",
        imgSrc: ""
    };

    // OPEN MODAL + PREFILL
    if (openBtn) {
        openBtn.addEventListener("click", () => {

            // New profile selectors
            const nameEl = document.querySelector(".profile-name");
            const phoneEl = document.querySelector(".profile-phone");
            const avatarEl = document.querySelector(".profile-avatar");

            initial.fullName = nameEl?.innerText.trim() ?? "";
            initial.phone = phoneEl?.innerText.trim() ?? "";
            initial.imgSrc = avatarEl?.src ?? "";

            fullNameInput.value = initial.fullName;
            phoneInput.value = initial.phone;

            // Image preview (only if custom image exists)
            if (initial.imgSrc && !initial.imgSrc.includes("default")) {
                preview.src = initial.imgSrc;
                preview.style.display = "block";
                text.style.display = "none";
            } else {
                preview.style.display = "none";
                text.style.display = "block";
            }

            modal.classList.add("show");
        });
    }

    // CLOSE MODAL
    if (closeBtn) {
        closeBtn.addEventListener("click", () => {
            modal.classList.remove("show");
        });
    }

    // RESET BUTTON (restore original values)
    if (resetBtn) {
        resetBtn.addEventListener("click", () => {
            fullNameInput.value = initial.fullName;
            phoneInput.value = initial.phone;

            fileInput.value = "";

            if (initial.imgSrc && !initial.imgSrc.includes("default")) {
                preview.src = initial.imgSrc;
                preview.style.display = "block";
                text.style.display = "none";
            } else {
                preview.style.display = "none";
                text.style.display = "block";
            }
        });
    }
});