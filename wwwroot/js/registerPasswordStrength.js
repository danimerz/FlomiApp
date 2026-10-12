// Registrierungs-Seite: Live-Anzeige der Passwort-Stärke.
// Ausgelagert aus Register.cshtml, damit kein Inline-Event-Handler
// (oninput=) nötig ist und die Content-Security-Policy ohne
// 'unsafe-inline' für script-src auskommt.
document.addEventListener('DOMContentLoaded', function () {
    var pwInput = document.getElementById('reg-pw');
    if (!pwInput) return;

    function updateRegPwStrength(pw) {
        var bar   = document.getElementById('reg-pw-bar');
        var label = document.getElementById('reg-pw-label');
        var setReq = function (id, ok) {
            var el = document.getElementById(id);
            if (!el) return;
            el.style.color = ok ? '#16a34a' : '#94a3b8';
            el.style.fontWeight = ok ? '700' : '400';
        };

        var hasLen     = pw.length >= 6;
        var hasUpper   = /[A-Z]/.test(pw);
        var hasDigit   = /[0-9]/.test(pw);
        var hasSpecial = /[^a-zA-Z0-9]/.test(pw);

        setReq('req-len',     hasLen);
        setReq('req-upper',   hasUpper);
        setReq('req-digit',   hasDigit);
        setReq('req-special', hasSpecial);

        var score = [hasLen, hasUpper, hasDigit, hasSpecial].filter(Boolean).length;
        var configs = [
            { w: '0%',   c: '#e2e8f0', t: '' },
            { w: '25%',  c: '#ef4444', t: '⚠️ Schwach' },
            { w: '50%',  c: '#f97316', t: '👍 Ausreichend' },
            { w: '75%',  c: '#eab308', t: '✅ Gut' },
            { w: '100%', c: '#16a34a', t: '🔒 Sicher' }
        ];
        var cfg = configs[pw.length > 0 ? score : 0];
        bar.style.width      = cfg.w;
        bar.style.background = cfg.c;
        label.textContent    = cfg.t;
        label.style.color    = cfg.c;
    }

    pwInput.addEventListener('input', function () { updateRegPwStrength(this.value); });
});
