document.addEventListener("DOMContentLoaded", () => {

    // ===== MODAL ELEMENTS =====
    const modal = document.getElementById("editProfileModal");
    const openBtn = document.getElementById("epm-open");
    const closeBtn = document.getElementById("epm-close");
    const resetBtn = document.getElementById("epm-reset");

    // ===== INPUTS =====
    const fullName = document.getElementById("epm-fullname");
    const phone = document.getElementById("epm-phone");

    // ===== IMAGE / DROPZONE =====
    const dropzone = document.getElementById("epm-dropzone");
    const fileInput = document.getElementById("epm-file");
    const preview = document.getElementById("epm-preview");
    const text = document.getElementById("epm-text");

    // ===== ORIGINAL VALUES =====
    let originalFullName = "";
    let originalPhone = "";

    // Extract values from profile page
    function loadOriginalValues() {
        const nameEl = document.querySelector(".profile-info p:nth-child(1)");
        const phoneEl = document.querySelector(".profile-info p:nth-child(3)");

        originalFullName = nameEl?.innerText.split("\n")[1] || "";
        originalPhone = phoneEl?.innerText.split("\n")[1] || "";
    }

    // ===== OPEN MODAL =====
    if (openBtn) {
        openBtn.addEventListener("click", () => {

            loadOriginalValues();

            fullName.value = originalFullName;
            phone.value = originalPhone;

            // Always show text mode (no preview)
            preview.style.display = "none";
            text.style.display = "block";
            fileInput.value = "";

            modal.classList.add("show");
        });
    }

    // ===== CLOSE MODAL =====
    if (closeBtn) {
        closeBtn.addEventListener("click", () => {
            modal.classList.remove("show");
        });
    }

    // ===== RESET =====
    if (resetBtn) {
        resetBtn.addEventListener("click", () => {

            // Restore original text fields
            fullName.value = originalFullName;
            phone.value = originalPhone;

            // Clear file input + show default text
            fileInput.value = "";
            preview.style.display = "none";
            text.style.display = "block";
        });
    }

    // ===== DRAG & DROP LOGIC =====
    dropzone.addEventListener("click", () => fileInput.click());

    dropzone.addEventListener("dragover", e => {
        e.preventDefault();
        dropzone.classList.add("dragover");
    });

    dropzone.addEventListener("dragleave", e => {
        e.preventDefault();
        dropzone.classList.remove("dragover");
    });

    dropzone.addEventListener("drop", e => {
        e.preventDefault();
        dropzone.classList.remove("dragover");

        if (e.dataTransfer.files.length > 0) {
            const file = e.dataTransfer.files[0];
            fileInput.files = e.dataTransfer.files;
            showPreview(file);
        }
    });

    fileInput.addEventListener("change", () => {
        if (fileInput.files.length > 0) {
            showPreview(fileInput.files[0]);
        }
    });

    // ===== PREVIEW FUNCTION =====
    function showPreview(file) {
        const reader = new FileReader();
        reader.onload = e => {
            preview.src = e.target.result;
            preview.style.display = "block";
            text.style.display = "none";
        };
        reader.readAsDataURL(file);
    }
});