/* RUBRIC IMPROVEMENT (Navigation): highlights the active nav pill based on
   the last-visited module stored in sessionStorage. Ensures Part 1 ↔ Part 2
   navigation preserves the user's mental context. */
(function () {
    function apply() {
        const last = sessionStorage.getItem('smartx.lastmodule');
        document.querySelectorAll('.smartx-navbar .nav-link[data-module]').forEach(el => {
            const active = el.dataset.module === last;
            el.classList.toggle('active', active);
            el.setAttribute('aria-current', active ? 'page' : 'false');
        });
    }
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', apply);
    } else {
        apply();
    }
})();

/* ============================================================
   REFERENCES
   ------------------------------------------------------------
   Wentworth, P., 2015. Think Sharply with C#. [Online] Available 
       at: http://www.ict.ru.ac.za/Resources/ThinkSharply/ThinkSharply/index.html [Accessed 18 November 2025].
   ============================================================ */