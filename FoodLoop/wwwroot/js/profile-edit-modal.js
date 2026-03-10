document.addEventListener("DOMContentLoaded", () => {

    /* =====================================================
       ELEMENTS
    ===================================================== */

    const modal = document.getElementById("editProfileModal");
    const openBtn = document.getElementById("epm-open");
    const closeBtn = document.getElementById("epm-close");
    const resetBtn = document.getElementById("epm-reset");
    const form = document.getElementById("epm-form");

    const dropzone = document.getElementById("epm-dropzone");
    const fileInput = document.getElementById("epm-file");
    const preview = document.getElementById("epm-preview");
    const urlInput = document.getElementById("epm-url");
    const text = document.getElementById("epm-text");

    const bannerInput = document.querySelector('input[name="BannerImage"]');
    const bannerUrlInput = document.querySelector('input[name="BannerImageUrl"]');
    const bannerPreview = document.getElementById("epm-banner-preview");

    const currentPassword = document.getElementById("epm-currentpass");

    if (!modal) return;

    /* =====================================================
       CONSTANTS
    ===================================================== */

    const AVATAR_PLACEHOLDER = "/images/placeholders/avatar.png";

    const initialAvatar = preview?.src || AVATAR_PLACEHOLDER;
    const initialBanner = bannerPreview?.src || null;

    let avatarObjectUrl = null;
    let bannerObjectUrl = null;

    /* =====================================================
       HELPERS
    ===================================================== */

    function revokeObject(url) {
        if (url) URL.revokeObjectURL(url);
    }

    function setAvatar(src) {

        if (!preview) return;

        preview.src = src || AVATAR_PLACEHOLDER;

        if (text) {
            text.style.display =
                preview.src.includes(AVATAR_PLACEHOLDER)
                    ? "block"
                    : "none";
        }
    }

    function setBanner(src) {

        if (!bannerPreview) return;

        bannerPreview.src = src;

        bannerPreview.style.display = src ? "block" : "none";
    }

    function normalizeUrl(url) {

        if (!url) return "";

        let final = url.trim();

        if (!/^https?:\/\//i.test(final))
            final = "https://" + final;

        return final;
    }

    /* =====================================================
       OPEN MODAL
    ===================================================== */

    if (openBtn) {
        openBtn.onclick = () => modal.classList.add("show");
    }

    /* =====================================================
       CLOSE MODAL
    ===================================================== */

    if (closeBtn) {
        closeBtn.onclick = () => modal.classList.remove("show");
    }

    window.addEventListener("click", (e) => {
        if (e.target === modal)
            modal.classList.remove("show");
    });

    /* =====================================================
       RESET FORM
    ===================================================== */

    if (resetBtn) {

        resetBtn.onclick = () => {

            form?.reset();

            revokeObject(avatarObjectUrl);
            revokeObject(bannerObjectUrl);

            setAvatar(initialAvatar);
            setBanner(initialBanner);
        };
    }

    /* =====================================================
       PASSWORD CHECK
    ===================================================== */

    if (form) {

        form.addEventListener("submit", (e) => {

            if (!currentPassword?.value.trim()) {

                e.preventDefault();

                alert("You must enter your current password to save changes.");

                currentPassword.focus();
            }
        });
    }

    /* =====================================================
       AVATAR FILE UPLOAD
    ===================================================== */

    if (fileInput) {

        fileInput.addEventListener("change", () => {

            const file = fileInput.files?.[0];
            if (!file) return;

            if (urlInput) urlInput.value = "";

            revokeObject(avatarObjectUrl);

            avatarObjectUrl = URL.createObjectURL(file);

            setAvatar(avatarObjectUrl);
        });
    }

    /* =====================================================
       AVATAR DROPZONE
    ===================================================== */

    if (dropzone && fileInput) {

        dropzone.onclick = () => fileInput.click();

        dropzone.addEventListener("dragover", (e) => {

            e.preventDefault();
            dropzone.classList.add("dragover");
        });

        dropzone.addEventListener("dragleave", () => {

            dropzone.classList.remove("dragover");
        });

        dropzone.addEventListener("drop", (e) => {

            e.preventDefault();

            dropzone.classList.remove("dragover");

            const file = e.dataTransfer?.files?.[0];
            if (!file) return;

            fileInput.files = e.dataTransfer.files;

            if (urlInput) urlInput.value = "";

            revokeObject(avatarObjectUrl);

            avatarObjectUrl = URL.createObjectURL(file);

            setAvatar(avatarObjectUrl);
        });
    }

    /* =====================================================
       AVATAR URL PREVIEW
    ===================================================== */

    if (urlInput) {

        urlInput.addEventListener("input", () => {

            let url = urlInput.value.trim();

            if (!url) {
                setAvatar(initialAvatar);
                return;
            }

            url = normalizeUrl(url);

            if (fileInput) fileInput.value = "";

            revokeObject(avatarObjectUrl);

            setAvatar(url);
        });
    }

    /* =====================================================
       AVATAR LOAD ERROR
    ===================================================== */

    if (preview) {

        preview.addEventListener("error", () => {

            revokeObject(avatarObjectUrl);

            setAvatar(AVATAR_PLACEHOLDER);
        });
    }

    /* =====================================================
       BANNER FILE PREVIEW
    ===================================================== */

    if (bannerInput && bannerPreview) {

        bannerInput.addEventListener("change", () => {

            const file = bannerInput.files?.[0];
            if (!file) return;

            if (bannerUrlInput) bannerUrlInput.value = "";

            revokeObject(bannerObjectUrl);

            bannerObjectUrl = URL.createObjectURL(file);

            setBanner(bannerObjectUrl);
        });
    }

    /* =====================================================
       BANNER URL PREVIEW
    ===================================================== */

    if (bannerUrlInput && bannerPreview) {

        bannerUrlInput.addEventListener("input", () => {

            let url = bannerUrlInput.value.trim();

            if (!url) {
                setBanner(initialBanner);
                return;
            }

            url = normalizeUrl(url);

            if (bannerInput) bannerInput.value = "";

            revokeObject(bannerObjectUrl);

            setBanner(url);
        });
    }

});