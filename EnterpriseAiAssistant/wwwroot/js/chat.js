window.chatUI ??= {};

window.chatUI.focusInput = (element) => {
    if (!element) {
        return;
    }

    requestAnimationFrame(() => {
        element.focus({ preventScroll: true });

        if (typeof element.value === "string" && element.setSelectionRange) {
            const end = element.value.length;
            element.setSelectionRange(end, end);
        }
    });
};

window.chatUI.scrollToBottom = (element) => {
    if (!element) {
        return;
    }

    requestAnimationFrame(() => {
        element.scrollTop = element.scrollHeight;
    });
};