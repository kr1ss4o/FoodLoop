document.addEventListener("DOMContentLoaded", () => {

    const dropzone = document.getElementById("epm-dropzone");
    const fileInput = document.getElementById("epm-file");
    const preview = document.getElementById("epm-preview");
    const text = document.getElementById("epm-text");

    // CLICK = open file dialog
    dropzone.addEventListener("click", () => fileInput.click());

    // DRAG OVER
    dropzone.addEventListener("dragover", (e) => {
        e.preventDefault();
        e.stopPropagation();
        dropzone.classList.add("dragover");
    });

    // DRAG LEAVE
    dropzone.addEventListener("dragleave", (e) => {
        e.preventDefault();
        e.stopPropagation();
        dropzone.classList.remove("dragover");
    });

    // DROP FILE
    dropzone.addEventListener("drop", (e) => {
        e.preventDefault();
        e.stopPropagation();
        dropzone.classList.remove("dragover");

        if (e.dataTransfer.files.length > 0) {
            const file = e.dataTransfer.files[0];
            fileInput.files = e.dataTransfer.files;
            showPreview(file);
        }
    });

    // FILE SELECTED VIA CLICK
    fileInput.addEventListener("change", () => {
        if (fileInput.files.length > 0) {
            showPreview(fileInput.files[0]);
        }
    });

    // SHOW PREVIEW FUNCTION
    function showPreview(file) {
        const reader = new FileReader();
        reader.onload = (e) => {
            preview.src = e.target.result;
            preview.style.display = "block";
            text.style.display = "none";
        };
        reader.readAsDataURL(file);
    }
});