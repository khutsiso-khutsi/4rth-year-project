(function () {
    var overlay = document.getElementById('testModal');
    if (!overlay) return;
    var lastTrigger = null;

    function set(id, text) { document.getElementById(id).textContent = text || '—'; }

    function badge(text, css) {
        var s = document.createElement('span');
        s.className = 'badge ' + css;
        s.textContent = text;
        return s;
    }

    function open(link) {
        var d = link.dataset;
        lastTrigger = link;

        set('tmTitle', d.req + ' — ' + d.test);
        var badges = document.getElementById('tmBadges');
        badges.innerHTML = '';
        badges.appendChild(badge(d.urgent, d.urgent === 'Urgent' ? 'badge-urgent' : 'badge-routine'));
        badges.appendChild(badge(d.stage, d.stageCss || 'badge-awaiting'));

        set('tmPatient', d.patient);
        set('tmTest', d.test);
        set('tmCondition', d.condition);
        set('tmNotes', d.notes);

        var list = document.getElementById('tmMeds');
        list.innerHTML = '';
        var meds = (d.meds || '').split('||').filter(Boolean);
        if (meds.length === 0) meds = ['No medication recorded'];
        meds.forEach(function (m) {
            var li = document.createElement('li');
            li.textContent = m;
            list.appendChild(li);
        });

        document.getElementById('tmMore').href = link.href;   // full details page
        overlay.classList.add('open');
        overlay.setAttribute('aria-hidden', 'false');
        document.getElementById('tmClose').focus();
    }

    function close() {
        overlay.classList.remove('open');
        overlay.setAttribute('aria-hidden', 'true');
        if (lastTrigger) lastTrigger.focus();
    }

    // Delegated so it keeps working on rows that are filtered/re-rendered
    document.addEventListener('click', function (e) {
        var link = e.target.closest('.js-test-modal');
        if (link) {
            if (e.ctrlKey || e.metaKey || e.shiftKey) return;   // let "open in new tab" work
            e.preventDefault();
            open(link);
        }
    });

    document.getElementById('tmClose').addEventListener('click', close);
    document.getElementById('tmCancel').addEventListener('click', close);
    overlay.addEventListener('click', function (e) { if (e.target === overlay) close(); });
    document.addEventListener('keydown', function (e) {
        if (!overlay.classList.contains('open')) return;
        if (e.key === 'Escape') close();
        if (e.key === 'Tab') {                                  // simple focus trap
            var f = overlay.querySelectorAll('button, a[href]');
            var first = f[0], last = f[f.length - 1];
            if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
            else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
        }
    });
})();
