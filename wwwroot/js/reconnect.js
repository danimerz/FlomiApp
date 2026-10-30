// Nach einer längeren Pause im Hintergrund (Handy gesperrt, Browser weggewischt) kann die
// Verbindung zum Server tot sein, ohne dass der Browser es merkt: Knöpfe reagieren dann nicht
// mehr. Beim Zurückkommen kurz beim Server nachfragen (Helpers/ConnectionCheck.cs) und neu
// laden, wenn keine Antwort kommt.
(function () {
    var MIN_HIDDEN_MS   = 30 * 1000; // kürzere Pausen übersteht die Verbindung von selbst
    var PING_TIMEOUT_MS = 4000;
    var hiddenSince = null;
    var checking    = false;

    // Blazor verbindet gerade selbst neu und zeigt seinen Hinweis – nicht dazwischenfunken
    function blazorIsReconnecting() {
        var modal = document.getElementById('components-reconnect-modal');
        return modal != null && modal.style.display !== 'none';
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
