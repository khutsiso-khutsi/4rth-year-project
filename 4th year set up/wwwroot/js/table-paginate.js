/* table-paginate.js - adds paging to every data table on the page.
   - Pages of 10 rows (the user can switch to 25 or 50). Tables with 10 rows or fewer get no pager.
   - Works with the pages' own search / filter boxes: rows a filter hides (style.display = 'none') are not counted.
   - Opt out for one table with  data-no-paginate  on the <table>, or put it inside an element with class "no-paginate".
   - Printing shows every row. */
(function () {
    'use strict';
    var SIZES = [10, 25, 50];

    function injectCss() {
        if (document.getElementById('pg-style')) return;
        var s = document.createElement('style');
        s.id = 'pg-style';
        s.textContent =
            '.pg-hide{display:none !important}' +
            '.pg-bar{display:flex;align-items:center;justify-content:space-between;gap:12px;flex-wrap:wrap;padding:12px 4px 4px;font-family:inherit;font-size:.82rem;color:var(--muted,#64748b)}' +
            '.pg-bar[hidden]{display:none !important}' +
            '.pg-info{white-space:nowrap}' +
            '.pg-nav{display:flex;align-items:center;gap:4px;flex-wrap:wrap}' +
            '.pg-btn{min-width:34px;height:34px;padding:0 10px;border:1px solid var(--border,#e2e8f0);background:#fff;color:var(--navy,#0f1f3d);border-radius:8px;font:inherit;font-weight:500;cursor:pointer;transition:background .15s,border-color .15s}' +
            '.pg-btn:hover:not([disabled]):not(.pg-current){background:var(--teal-light,#f0fdfa);border-color:var(--teal,#0d9488)}' +
            '.pg-btn[disabled]{opacity:.45;cursor:default}' +
            '.pg-btn:focus-visible{outline:2px solid var(--teal,#0d9488);outline-offset:2px}' +
            '.pg-current{background:var(--teal,#0d9488);border-color:var(--teal,#0d9488);color:#fff;cursor:default}' +
            '.pg-gap{padding:0 4px;color:var(--muted,#64748b)}' +
            '.pg-size{display:flex;align-items:center;gap:6px}' +
            '.pg-size select{height:34px;padding:0 8px;border:1px solid var(--border,#e2e8f0);border-radius:8px;background:#fff;color:var(--navy,#0f1f3d);font:inherit}' +
            '@media print{.pg-hide{display:table-row !important}.pg-bar{display:none !important}}';
        document.head.appendChild(s);
    }

    function eligibleTable(t) {
        if (t.hasAttribute('data-no-paginate')) return false;
        if (t.parentNode && t.parentNode.closest && t.parentNode.closest('table')) return false;   // nested table
        if (t.closest && t.closest('.no-paginate')) return false;
        return true;
    }

    function pageList(page, pages) {
        var out = [], i;
        if (pages <= 7) { for (i = 1; i <= pages; i++) out.push(i); return out; }
        out.push(1);
        if (page > 3) out.push('...');
        for (i = Math.max(2, page - 1); i <= Math.min(pages - 1, page + 1); i++) out.push(i);
        if (page < pages - 2) out.push('...');
        out.push(pages);
        return out;
    }

    function setup(table) {
        var state = { page: 1, size: parseInt(table.getAttribute('data-page-size'), 10) || SIZES[0], busy: false };
        var bar = document.createElement('div');
        bar.className = 'pg-bar';
        bar.setAttribute('role', 'navigation');
        bar.setAttribute('aria-label', 'Table pages');
        bar.hidden = true;
        var host = table.parentNode;
        var anchor = (host && /wrap|scroll|table-container|responsive/i.test(host.className || '')) ? host : table;
        anchor.parentNode.insertBefore(bar, anchor.nextSibling);

        function dataRows() {
            var body = table.tBodies[0];
            if (!body) return [];
            return Array.prototype.filter.call(body.rows, function (r) { return r.style.display !== 'none'; });
        }
        function allRows() {
            var body = table.tBodies[0];
            return body ? Array.prototype.slice.call(body.rows) : [];
        }

        function render() {
            state.busy = true;
            var rows = dataRows(), total = rows.length;
            var pages = Math.max(1, Math.ceil(total / state.size));
            if (state.page > pages) state.page = pages;
            if (state.page < 1) state.page = 1;

            allRows().forEach(function (r) { r.classList.remove('pg-hide'); });
            if (total <= SIZES[0]) { bar.hidden = true; state.busy = false; return; }

            var from = (state.page - 1) * state.size, to = Math.min(total, from + state.size);
            rows.forEach(function (r, i) { if (i < from || i >= to) r.classList.add('pg-hide'); });

            bar.hidden = false;
            bar.innerHTML = '';
            var info = document.createElement('div');
            info.className = 'pg-info';
            info.textContent = 'Showing ' + (from + 1) + '\u2013' + to + ' of ' + total;
            bar.appendChild(info);

            var nav = document.createElement('div');
            nav.className = 'pg-nav';
            function btn(label, page, opts) {
                var b = document.createElement('button');
                b.type = 'button';
                b.className = 'pg-btn' + (opts && opts.current ? ' pg-current' : '');
                b.textContent = label;
                if (opts && opts.aria) b.setAttribute('aria-label', opts.aria);
                if (opts && opts.current) b.setAttribute('aria-current', 'page');
                if (opts && opts.disabled) b.disabled = true;
                else if (!(opts && opts.current)) b.addEventListener('click', function () { go(page); });
                nav.appendChild(b);
            }
            btn('\u2039 Prev', state.page - 1, { disabled: state.page === 1, aria: 'Previous page' });
            pageList(state.page, pages).forEach(function (p) {
                if (p === '...') {
                    var g = document.createElement('span'); g.className = 'pg-gap'; g.textContent = '\u2026'; nav.appendChild(g);
                } else btn(String(p), p, { current: p === state.page, aria: 'Page ' + p });
            });
            btn('Next \u203A', state.page + 1, { disabled: state.page === pages, aria: 'Next page' });
            bar.appendChild(nav);

            var size = document.createElement('label');
            size.className = 'pg-size';
            size.appendChild(document.createTextNode('Rows per page'));
            var sel = document.createElement('select');
            SIZES.forEach(function (n) {
                var o = document.createElement('option'); o.value = n; o.textContent = n; if (n === state.size) o.selected = true; sel.appendChild(o);
            });
            sel.addEventListener('change', function () { state.size = parseInt(sel.value, 10); state.page = 1; render(); });
            size.appendChild(sel);
            bar.appendChild(size);
            state.busy = false;
        }

        function go(p) {
            state.page = p;
            render();
            var top = table.getBoundingClientRect().top;
            if (top < 0) { try { table.scrollIntoView({ block: 'start' }); } catch (e) { } }
        }

        var timer = null;
        var obs = new MutationObserver(function () {
            if (state.busy) return;
            clearTimeout(timer);
            timer = setTimeout(render, 0);
        });
        obs.observe(table, { childList: true, subtree: true, attributes: true, attributeFilter: ['style'] });
        render();
    }

    function start() {
        injectCss();
        Array.prototype.forEach.call(document.querySelectorAll('table'), function (t) {
            if (eligibleTable(t) && !t.getAttribute('data-pg-ready')) { t.setAttribute('data-pg-ready', '1'); setup(t); }
        });
    }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', start); else start();
}());