document.addEventListener("DOMContentLoaded", () => {

    const modal = document.getElementById("editProfileModal");
    const openBtn = document.getElementById("epm-open");
    const closeBtn = document.getElementById("epm-close");
    const resetBtn = document.getElementById("epm-reset");

    const fullName = document.getElementById("epm-fullname");
    const businessName = document.getElementById("epm-businessname");
    const businessEmail = document.getElementById("epm-email");
    const phone = document.getElementById("epm-phone");
    const address = document.getElementById("epm-address");

    const dropzone = document.getElementById("epm-dropzone");
    const fileInput = document.getElementById("epm-file");
    const preview = document.getElementById("epm-preview");
    const text = document.getElementById("epm-text");

    let initial = {};

    // -------------------------------------------------
    // OPEN MODAL + PREFILL
    // -------------------------------------------------
    if (openBtn) {
        openBtn.addEventListener("click", () => {

            const isBusiness = document.querySelector(".bp-wrapper") !== null;

            if (isBusiness) {

                const imgElement = document.querySelector("#rest-photo"); // <-- FIXED

                initial = {
                    fullName: document.getElementById("owner-name")?.innerText.trim() ?? "",
                    phone: document.getElementById("owner-phone")?.innerText.trim() ?? "",
                    businessName: document.getElementById("rest-name")?.innerText.trim() ?? "",
                    businessEmail: document.getElementById("rest-email")?.innerText.trim() ?? "",
                    address: document.getElementById("rest-address")?.innerText.trim() ?? "",
                    imgSrc: imgElement?.src ?? ""
                };

                fullName.value = initial.fullName;
                phone.value = initial.phone;
                businessName.value = initial.businessName;
                businessEmail.value = initial.businessEmail;
                address.value = initial.address;

            } else {

                const imgElement = document.getElementById("prof-photo");

                initial = {
                    fullName: document.getElementById("prof-name")?.innerText.trim() ?? "",
                    phone: document.getElementById("prof-phone")?.innerText.trim() ?? "",
                    imgSrc: imgElement?.src ?? ""
                };

                fullName.value = initial.fullName;
                phone.value = initial.phone;
            }

            // IMAGE preview
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

    // -------------------------------------------------
    // CLOSE MODAL
    // -------------------------------------------------
    closeBtn?.addEventListener("click", () => modal.classList.remove("show"));

    // -------------------------------------------------
    // RESET BUTTON
    // -------------------------------------------------
    resetBtn?.addEventListener("click", () => {

        if (fullName) fullName.value = initial.fullName;
        if (phone) phone.value = initial.phone;

        if (businessName) businessName.value = initial.businessName ?? "";
        if (businessEmail) businessEmail.value = initial.businessEmail ?? "";
        if (address) address.value = initial.address ?? "";

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

    // -------------------------------------------------
    // DRAG & DROP + FILE PREVIEW
    // -------------------------------------------------
    dropzone?.addEventListener("click", () => fileInput.click());

    dropzone?.addEventListener("dragover", e => {
        e.preventDefault();
        dropzone.classList.add("dragover");
    });

    dropzone?.addEventListener("dragleave", () => {
        dropzone.classList.remove("dragover");
    });

    dropzone?.addEventListener("drop", e => {
        e.preventDefault();
        dropzone.classList.remove("dragover");

        if (e.dataTransfer.files.length > 0) {
            const file = e.dataTransfer.files[0];
            fileInput.files = e.dataTransfer.files;
            showPreview(file);
        }
    });

    fileInput?.addEventListener("change", () => {
        if (fileInput.files.length > 0) {
            showPreview(fileInput.files[0]);
        }
    });

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