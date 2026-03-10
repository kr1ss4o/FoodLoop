document.addEventListener("DOMContentLoaded", () => {

    // Table sorting in admin

    document.querySelectorAll(".admin-table thead th").forEach((header, index) => {

        if (header.classList.contains("admin-actions-header"))
            return;

        header.addEventListener("click", () => {

            const table = header.closest("table");
            const tbody = table.querySelector("tbody");
            const rows = Array.from(tbody.querySelectorAll("tr"));

            const asc = header.classList.toggle("asc");

            rows.sort((a, b) => {

                const A = a.children[index].innerText.trim();
                const B = b.children[index].innerText.trim();

                const numA = parseFloat(A.replace(",", "."));
                const numB = parseFloat(B.replace(",", "."));

                if (!isNaN(numA) && !isNaN(numB))
                    return asc ? numA - numB : numB - numA;

                return asc
                    ? A.localeCompare(B, 'bg')
                    : B.localeCompare(A, 'bg');

            });

            tbody.innerHTML = "";
            rows.forEach(r => tbody.appendChild(r));
        });

    });

});