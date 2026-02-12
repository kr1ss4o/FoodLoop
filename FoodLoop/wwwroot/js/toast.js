function showToast(message, type = "success") {

    const container = document.getElementById("toast-container");

    if (!container) {
        console.log("ERROR: container NOT FOUND");
        return;
    }

    const toast = document.createElement("div");
    toast.classList.add("toast");
    toast.classList.add(type === "error" ? "toast-error" : "toast-success");

    toast.textContent = message;
    container.appendChild(toast);

    // Auto remove
    setTimeout(() => {
        toast.style.animation = "toast-slide-out 0.4s ease forwards";
        setTimeout(() => toast.remove(), 400);
    }, 3000);
}

window.showToast = showToast;