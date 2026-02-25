document.addEventListener("DOMContentLoaded", function () {

    const modal = document.getElementById("editProfileModal");
    const openBtn = document.getElementById("epm-open");
    const closeBtn = document.getElementById("epm-close");
    const resetBtn = document.getElementById("epm-reset");
    const form = document.getElementById("epm-form");

    const fileInput = document.getElementById("epm-file");
    const dropzone = document.getElementById("epm-dropzone");
    const preview = document.getElementById("epm-preview");
    const urlInput = document.getElementById("epm-url");
    const text = document.getElementById("epm-text");

    const currentPassword = document.getElementById("epm-currentpass");

    if (!modal) return;

    const PLACEHOLDER = "/images/placeholders/avatar.png";

    // Ако preview вече е с реална снимка от server-side model, пазим я,
    // за да може Reset да връща към нея (а не винаги към placeholder).
    const initialPreviewSrc = (preview && preview.getAttribute("src")) ? preview.getAttribute("src") : PLACEHOLDER;

    let currentObjectUrl = null;
    let urlDebounceTimer = null;

    function setPreviewSrc(src) {
        if (!preview) return;

        preview.src = src || PLACEHOLDER;

        if (text) {
            text.style.display = (preview.src.includes(PLACEHOLDER)) ? "block" : "none";
        }
    }

    function clearObjectUrlIfAny() {
        if (currentObjectUrl) {
            URL.revokeObjectURL(currentObjectUrl);
            currentObjectUrl = null;
        }
    }

    function isValidHttpUrl(str) {
        try {
            const url = new URL(str);
            return url.protocol === "http:" || url.protocol === "https:";
        } catch {
            return false;
        }
    }

    function isBlockedHost(urlString) {
        // блокира localhost / 127.0.0.1 / ::1, за да не “виси” заявката
        try {
            const u = new URL(urlString);
            const h = (u.hostname || "").toLowerCase();
            return h === "localhost" || h === "127.0.0.1" || h === "::1";
        } catch {
            return false;
        }
    }

    // =========================
    // OPEN MODAL
    // =========================
    if (openBtn) {
        openBtn.addEventListener("click", function () {
            modal.style.display = "flex";
        });
    }

    // =========================
    // CLOSE MODAL
    // =========================
    if (closeBtn) {
        closeBtn.addEventListener("click", function () {
            modal.style.display = "none";
        });
    }

    window.addEventListener("click", function (e) {
        if (e.target === modal) {
            modal.style.display = "none";
        }
    });

    // =========================
    // RESET BUTTON
    // =========================
    if (resetBtn) {
        resetBtn.addEventListener("click", function () {
            if (form) form.reset();

            clearObjectUrlIfAny();
            setPreviewSrc(initialPreviewSrc || PLACEHOLDER);

            if (text) text.style.display = "block"; // ако initial е placeholder, ще си е block; ако не — setPreviewSrc ще го скрие
        });
    }

    // =========================
    // PASSWORD REQUIRED
    // =========================
    if (form) {
        form.addEventListener("submit", function (e) {
            if (currentPassword && !currentPassword.value.trim()) {
                e.preventDefault();
                alert("You must enter your current password to save changes.");
                currentPassword.focus();
            }
        });
    }

    // =========================
    // FILE PREVIEW (change)
    // =========================
    if (fileInput) {
        fileInput.addEventListener("change", function () {
            const file = this.files && this.files[0];
            if (!file) return;

            // ако юзърът избере файл -> чистим URL input
            if (urlInput) urlInput.value = "";

            clearObjectUrlIfAny();
            currentObjectUrl = URL.createObjectURL(file);
            setPreviewSrc(currentObjectUrl);
        });
    }

    // =========================
    // DRAG & DROP
    // =========================
    if (dropzone && fileInput) {

        dropzone.addEventListener("click", () => fileInput.click());

        dropzone.addEventListener("dragover", (e) => {
            e.preventDefault();
            dropzone.classList.add("dragging");
        });

        dropzone.addEventListener("dragleave", () => {
            dropzone.classList.remove("dragging");
        });

        dropzone.addEventListener("drop", (e) => {
            e.preventDefault();
            dropzone.classList.remove("dragging");

            const dt = e.dataTransfer;
            const file = dt && dt.files && dt.files[0];
            if (!file) return;

            // задава файловете на input-а (за да се submit-нат)
            fileInput.files = dt.files;

            // чистим URL input
            if (urlInput) urlInput.value = "";

            clearObjectUrlIfAny();
            currentObjectUrl = URL.createObjectURL(file);
            setPreviewSrc(currentObjectUrl);
        });
    }

    // =========================
    // URL LIVE PREVIEW (works while typing/paste)
    // =========================
    if (urlInput && preview) {
        urlInput.addEventListener("input", function () {
            clearTimeout(urlDebounceTimer);

            urlDebounceTimer = setTimeout(() => {
                const url = (urlInput.value || "").trim();

                // ако е празно -> връща initial/placeholder
                if (!url) {
                    clearObjectUrlIfAny();
                    setPreviewSrc(initialPreviewSrc || PLACEHOLDER);
                    return;
                }

                // блокира localhost, за да не чака
                const lower = url.toLowerCase();
                if (lower.includes("localhost") || lower.includes("127.0.0.1")) {
                    return;
                }

                // ако не започва с http, пробва да добави https (много хора paste-ват без протокол)
                let finalUrl = url;
                if (!/^https?:\/\//i.test(finalUrl)) {
                    finalUrl = "https://" + finalUrl;
                }

                // чисти file input ако юзърът е тръгнал по URL вариант
                if (fileInput) fileInput.value = "";
                clearObjectUrlIfAny();

                // сетва preview
                setPreviewSrc(finalUrl);
            }, 150);
        });
    }

    // ако URL е счупен -> placeholder (или initial ако искаш)
    if (preview) {
        preview.addEventListener("error", function () {
            clearObjectUrlIfAny();
            setPreviewSrc(PLACEHOLDER);
        });
    }

});

console.log("EPM script active");

const debugUrl = document.getElementById("epm-url");
const debugImg = document.getElementById("epm-preview");

console.log("Found elements:", debugUrl, debugImg);

if (debugUrl && debugImg) {
    debugUrl.addEventListener("input", function () {
        console.log("Typing:", debugUrl.value);
        debugImg.src = debugUrl.value.trim();
        console.log("New src:", debugImg.src);
    });
}