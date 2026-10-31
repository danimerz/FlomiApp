// Verbindung zum Server (Blazor Server) im Blick behalten.
//
// 1. Anzeige bei Unterbruch (#components-reconnect-modal in _Host.cshtml): Blazor setzt die Klassen
//    components-reconnect-show/-failed/-rejected und meldet jeden Wechsel als Event. Daraus wird:
//    erst eine dezente Leiste, nach 8 Sekunden eine Karte, danach kurz «Wieder verbunden».
//    «rejected» heisst: Der Server kennt die Sitzung nicht mehr – dann einfach neu laden.
//
// 2. Nach einer längeren Pause im Hintergrund (Handy gesperrt, Browser weggewischt) kann die
//    Verbindung tot sein, ohne dass der Browser es merkt: Knöpfe reagieren dann nicht mehr. Beim
//    Zurückkommen kurz beim Server nachfragen (Helpers/ConnectionCheck.cs) und neu laden, wenn
//    keine Antwort kommt.
(function () {
    var CARD_AFTER_MS   = 8000;
    var OK_VISIBLE_MS   = 2000;
    var MIN_HIDDEN_MS   = 30 * 1000; // kürzere Pausen übersteht die Verbindung von selbst
    var PING_TIMEOUT_MS = 4000;

    var modal = document.getElementById('components-reconnect-modal');

    // ── 1. Anzeige bei Unterbruch ────────────────────────────────────────────
    var cardTimer = null;
    var okTimer   = null;
    var wasDown   = false;

    function setState(cls) {
        modal.classList.remove('is-card', 'is-ok');
        if (cls) modal.classList.add(cls);
    }

    function updateOffline() {
        modal.classList.toggle('is-offline', navigator.onLine === false);
    }

    if (modal) {
        modal.addEventListener('components-reconnect-state-changed', function (e) {
            var state = e.detail && e.detail.state;
            clearTimeout(okTimer);
            updateOffline();

            if (state === 'show' || state === 'retrying') {
                if (!wasDown) {
                    wasDown = true;
                    setState(null);
                    cardTimer = setTimeout(function () { setState('is-card'); }, CARD_AFTER_MS);
                }
            } else if (state === 'hide') {
                clearTimeout(cardTimer);
                if (wasDown) {
                    setState('is-ok');
                    okTimer = setTimeout(function () { setState(null); }, OK_VISIBLE_MS);
                }
                wasDown = false;
            } else if (state === 'failed' || state === 'resume-failed') {
                clearTimeout(cardTimer);
                setState('is-card');
            } else if (state === 'rejected') {
                location.reload();
            }
        });

        modal.addEventListener('click', function (e) {
            if (e.target.closest('[data-reconnect-reload]')) {
                location.reload();
            } else if (e.target.closest('[data-reconnect-retry]')) {
                retry();
            }
        });

        window.addEventListener('online', function () {
            updateOffline();
            // Netz ist zurück: nicht auf den nächsten Versuch warten
            if (modal.classList.contains('components-reconnect-failed')) retry();
        });
        window.addEventListener('offline', updateOffline);
    }

    async function retry() {
        try {
            if (!(await Blazor.reconnect())) location.reload();
        } catch (e) {
            location.reload();
        }
    }

    // ── 2. Prüfung nach einer Pause im Hintergrund ───────────────────────────
    var hiddenSince = null;
    var checking    = false;

    function blazorIsReconnecting() {
        return modal != null && (modal.classList.contains('components-reconnect-show')
            || modal.classList.contains('components-reconnect-failed'));
    }

    async function checkConnection() {
        if (checking || typeof DotNet === 'undefined') return;
        checking = true;
        try {
            var timeout = new Promise(function (resolve) {
                setTimeout(function () { resolve('timeout'); }, PING_TIMEOUT_MS);
            });
            var result = await Promise.race([DotNet.invokeMethodAsync('FlomiApp', 'Ping'), timeout]);
            if (result === 'timeout' && !blazorIsReconnecting()) location.reload();
        } catch (e) {
            // Verbindung ist schon als getrennt erkannt – Blazor verbindet selbst neu bzw. lädt neu
        } finally {
            checking = false;
        }
    }

    document.addEventListener('visibilitychange', function () {
        if (document.visibilityState === 'hidden') {
            hiddenSince = Date.now();
            return;
        }
        if (hiddenSince !== null && Date.now() - hiddenSince >= MIN_HIDDEN_MS) checkConnection();
        hiddenSince = null;
    });

    // Seite kommt aus dem Zurück-Cache des Browsers: die Verbindung ist sicher weg
    window.addEventListener('pageshow', function (e) {
        if (e.persisted) location.reload();
    });
})();
