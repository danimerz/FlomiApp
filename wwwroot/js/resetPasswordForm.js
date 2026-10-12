// Passwort-Reset-Seite: Stärke-Anzeige, Generator, Sichtbarkeits-Toggle.
// Ausgelagert aus ResetPassword.cshtml, damit keine Inline-Event-Handler
// (onclick=/oninput=) nötig sind und die Content-Security-Policy ohne
// 'unsafe-inline' für script-src auskommt.
document.addEventListener('DOMContentLoaded', function () {
    var emailDisplay = document.getElementById('emailDisplay');
    var emailHidden  = document.getElementById('emailHidden');
    if (emailDisplay && emailHidden) {
        emailDisplay.textContent = emailHidden.value;
    }

    function togglePw(inputId, btn) {
        var inp = document.getElementById(inputId);
        var show = inp.type === 'password';
        inp.type = show ? 'text' : 'password';
        btn.textContent = show ? '🙈' : '👁️';
    }

    function generatePw() {
        // Jede Gruppe mind. 1× — entspricht den ASP.NET Identity-Regeln
        var upper   = 'ABCDEFGHJKLMNPQRSTUVWXYZ';
        var lower   = 'abcdefghjkmnpqrstuvwxyz';
        var digits  = '23456789';
        var special = '!#$%+-=?';
        var all     = upper + lower + digits + special;

        var pick = function (s) { return s[Math.floor(Math.random() * s.length)]; };

        // Garantierte Zeichen
        var pw = [pick(upper), pick(upper), pick(lower), pick(lower),
                  pick(digits), pick(digits), pick(special)];

        // Rest auffüllen auf 14 Zeichen
        while (pw.length < 14) pw.push(pick(all));

        // Mischen (Fisher-Yates)
        for (var i = pw.length - 1; i > 0; i--) {
            var j = Math.floor(Math.random() * (i + 1));
            var t = pw[i]; pw[i] = pw[j]; pw[j] = t;
        }

        var result = pw.join('');
        var inp  = document.getElementById('pwInput');
        var conf = document.getElementById('pwConfirm');
        inp.value  = result;
        conf.value = result;
        inp.type   = 'text';
        updateStrength(result);
        document.getElementById('generatedPwText').textContent = result;
        document.getElementById('generatedHint').style.display = 'flex';
    }

    function copyPw() {
        navigator.clipboard.writeText(document.getElementById('pwInput').value);
        var btn = document.getElementById('copyPwBtn');
        btn.textContent = '✅ Kopiert';
        setTimeout(function () { btn.textContent = '📋 Kopieren'; }, 2000);
    }

    function updateStrength(pw) {
        var fill  = document.getElementById('strengthFill');
        var label = document.getElementById('strengthLabel');
        var len   = pw.length;
        var score = 0;
        if (len >= 6)  score++;
        if (len >= 10) score++;
        if (/[A-Z]/.test(pw)) score++;
        if (/[0-9]/.test(pw)) score++;
        if (/[^a-zA-Z0-9]/.test(pw)) score++;

        var configs = [
            { pct: '0%',   color: '#e5e7eb', text: '' },
            { pct: '25%',  color: '#ef4444', text: '⚠️ Schwach' },
            { pct: '50%',  color: '#f97316', text: '👍 Ausreichend' },
            { pct: '75%',  color: '#eab308', text: '✅ Gut' },
            { pct: '90%',  color: '#22c55e', text: '🔒 Sehr sicher' },
            { pct: '100%', color: '#16a34a', text: '🔒 Ausgezeichnet' }
        ];
        var c = configs[score] || configs[0];
        fill.style.width = c.pct;
        fill.style.background = c.color;
        label.textContent = len > 0 ? c.text : '';
        label.style.color = c.color;
    }

    var pwInput = document.getElementById('pwInput');
    if (pwInput) pwInput.addEventListener('input', function () { updateStrength(this.value); });

    var toggleInput = document.getElementById('togglePwInput');
    if (toggleInput) toggleInput.addEventListener('click', function () { togglePw('pwInput', this); });

    var toggleConfirm = document.getElementById('togglePwConfirm');
    if (toggleConfirm) toggleConfirm.addEventListener('click', function () { togglePw('pwConfirm', this); });

    var generateBtn = document.getElementById('generatePwBtn');
    if (generateBtn) generateBtn.addEventListener('click', generatePw);

    var copyBtn = document.getElementById('copyPwBtn');
    if (copyBtn) copyBtn.addEventListener('click', copyPw);
});
