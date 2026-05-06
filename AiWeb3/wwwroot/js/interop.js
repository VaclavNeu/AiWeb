window.bootstrapInterop = {
    showModal: function (modalId) {
        const el = document.getElementById(modalId);
        if (!el) { console.error("Modal element not found:", modalId); return; }

        if (typeof bootstrap === "undefined" || !bootstrap.Modal) {
            console.error("Bootstrap JS not loaded (bootstrap.Modal missing).");
            return;
        }

        let instance = bootstrap.Modal.getInstance(el);
        if (!instance) instance = new bootstrap.Modal(el, { keyboard: true, focus: true });
        instance.show();
    }
};

window.saveFileFromContent = function (fileName, content) {
    const blob = new Blob([content], { type: "text/html;charset=utf-8" });
    const a = document.createElement("a");
    a.href = URL.createObjectURL(blob);
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    setTimeout(() => { URL.revokeObjectURL(a.href); a.remove(); }, 0);
};