const filters = [...document.querySelectorAll("[data-filter]")];
const cards = [...document.querySelectorAll(".shelf-card")];
const dialog = document.getElementById("product-dialog");

for (const button of filters) {
    button.addEventListener("click", () => {
        for (const filter of filters) filter.setAttribute("aria-pressed", String(filter === button));
        for (const card of cards) {
            card.hidden = button.dataset.filter !== "all" && button.dataset.filter !== card.dataset.category;
        }
    });
}

for (const button of document.querySelectorAll("[data-detail]")) {
    button.addEventListener("click", () => {
        document.getElementById("detail-title").textContent = button.closest(".shelf-card").querySelector("h3").textContent;
        document.getElementById("detail-description").textContent = button.dataset.detail;
        dialog.showModal();
    });
}
