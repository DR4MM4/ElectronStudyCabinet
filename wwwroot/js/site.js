document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll("form").forEach(form => {
        form.addEventListener("submit", () => {
            const button = form.querySelector('button[type="submit"]');
            if (button && !button.dataset.noLoading) {
                button.dataset.originalText = button.textContent;
                button.disabled = true;
                button.textContent = "Обработка…";
                setTimeout(() => {
                    button.disabled = false;
                    button.textContent = button.dataset.originalText || "Отправить";
                }, 5000);
            }
        });
    });
});
