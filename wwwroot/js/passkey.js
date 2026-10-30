// Passkeys (WebAuthn): Anmelden auf der Login-Seite, Hinzufügen in den Einstellungen (Blazor-Interop).
// Gegenstück auf dem Server: Endpoints/PasskeyEndpoints.cs und Login-Seite (Handler «Passkey»).
(() => {
    const b64urlToBuffer = (value) => {
        const base64 = value.replace(/-/g, '+').replace(/_/g, '/').padEnd(Math.ceil(value.length / 4) * 4, '=');
        const binary = atob(base64);
        const bytes = new Uint8Array(binary.length);
        for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);
        return bytes.buffer;
    };

    const bufferToB64url = (buffer) => {
        let binary = '';
        for (const b of new Uint8Array(buffer)) binary += String.fromCharCode(b);
        return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
    };

    // Ältere Browser kennen parse…FromJSON/toJSON noch nicht – dann von Hand umwandeln
    const parseCreationOptions = (json) => {
        if (PublicKeyCredential.parseCreationOptionsFromJSON) return PublicKeyCredential.parseCreationOptionsFromJSON(json);
        return {
            ...json,
            challenge: b64urlToBuffer(json.challenge),
            user: { ...json.user, id: b64urlToBuffer(json.user.id) },
            excludeCredentials: (json.excludeCredentials || []).map(c => ({ ...c, id: b64urlToBuffer(c.id) })),
        };
    };

    const parseRequestOptions = (json) => {
        if (PublicKeyCredential.parseRequestOptionsFromJSON) return PublicKeyCredential.parseRequestOptionsFromJSON(json);
        return {
            ...json,
            challenge: b64urlToBuffer(json.challenge),
            allowCredentials: (json.allowCredentials || []).map(c => ({ ...c, id: b64urlToBuffer(c.id) })),
        };
    };

    const credentialToJson = (credential) => {
        try {
            if (typeof credential.toJSON === 'function') return JSON.stringify(credential.toJSON());
        } catch { /* manche Passwortmanager liefern kein toJSON – Fallback unten */ }

        const r = credential.response;
        const response = { clientDataJSON: bufferToB64url(r.clientDataJSON) };
        if (r.attestationObject) {
            response.attestationObject = bufferToB64url(r.attestationObject);
            response.transports = typeof r.getTransports === 'function' ? r.getTransports() : [];
        } else {
            response.authenticatorData = bufferToB64url(r.authenticatorData);
            response.signature = bufferToB64url(r.signature);
            if (r.userHandle) response.userHandle = bufferToB64url(r.userHandle);
        }
        return JSON.stringify({
            id: credential.id,
            rawId: bufferToB64url(credential.rawId),
            type: credential.type,
            authenticatorAttachment: credential.authenticatorAttachment ?? undefined,
            clientExtensionResults: credential.getClientExtensionResults(),
            response,
        });
    };

    const postJson = async (url, body) => {
        const response = await fetch(url, {
            method: 'POST',
            credentials: 'same-origin',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body ?? {}),
        });
        const text = await response.text();
        let data = null;
        try { data = text ? JSON.parse(text) : null; } catch { /* keine JSON-Antwort */ }
        if (!response.ok) throw new Error(data?.error || 'Der Server hat die Anfrage abgelehnt.');
        return data;
    };

    // null = still ignorieren (bewusst abgebrochen)
    const friendlyError = (error) => {
        switch (error?.name) {
            case 'AbortError':        return null;
            case 'NotAllowedError':   return 'Abgebrochen oder Zeit abgelaufen. Bitte nochmals versuchen.';
            case 'InvalidStateError': return 'Auf diesem Gerät ist bereits ein Passkey für dein Konto gespeichert.';
            case 'SecurityError':     return 'Passkeys funktionieren nur auf pfadiflomi.ch (mit https).';
            default:                  return error?.message || 'Unbekannter Fehler.';
        }
    };

    const isSupported = () => !!(window.PublicKeyCredential && navigator.credentials);

    // Vorschlag für den Namen, den man in den Einstellungen sieht
    const defaultName = () => {
        const ua = navigator.userAgent;
        if (/iPhone/.test(ua))    return 'iPhone';
        if (/iPad/.test(ua))      return 'iPad';
        if (/Android/.test(ua))   return 'Android';
        if (/Macintosh/.test(ua)) return 'Mac';
        if (/Windows/.test(ua))   return 'Windows';
        if (/Linux/.test(ua))     return 'Linux';
        return 'Passkey';
    };

    // ── Einstellungen (Blazor): Passkey hinzufügen ────────────────────────────
    window.flomiPasskeys = {
        isSupported,
        defaultName,

        // Liefert { ok: true, name } oder { ok: false, error }
        register: async (name) => {
            try {
                const options = await postJson('/passkeys/creation-options');
                const credential = await navigator.credentials.create({ publicKey: parseCreationOptions(options) });
                const result = await postJson('/passkeys/register', { credential: credentialToJson(credential), name });
                return { ok: true, name: result?.name ?? name };
            } catch (error) {
                return { ok: false, error: friendlyError(error) ?? 'Abgebrochen.' };
            }
        },
    };

    // ── Login-Seite: Button «Mit Passkey anmelden» und Vorschlag im E-Mail-Feld ─
    const initLogin = () => {
        const box = document.querySelector('[data-passkey-login]');
        if (!box || !isSupported()) return;
        box.hidden = false;

        const form            = box.querySelector('form');
        const button          = box.querySelector('[data-passkey-start]');
        const credentialField = box.querySelector('[data-passkey-credential]');
        const rememberField   = box.querySelector('[data-passkey-remember]');
        const errorBox        = box.querySelector('[data-passkey-error]');
        const rememberSource  = document.getElementById('Input_RememberMe');
        let conditional       = null;

        const showError = (message) => {
            errorBox.textContent = message;
            errorBox.hidden = !message;
        };

        const submit = (credential) => {
            credentialField.value = credentialToJson(credential);
            rememberField.value = rememberSource?.checked ? 'true' : 'false';
            button.disabled = true;
            form.submit();
        };

        const request = async (mediation, signal) => {
            const options = await postJson('/passkeys/request-options');
            return navigator.credentials.get({ publicKey: parseRequestOptions(options), mediation, signal });
        };

        button.addEventListener('click', async () => {
            showError('');
            conditional?.abort();
            conditional = null;
            button.disabled = true;
            try {
                const credential = await request('optional');
                if (credential) submit(credential);
            } catch (error) {
                showError(friendlyError(error) ?? '');
            } finally {
                if (!credentialField.value) button.disabled = false;
            }
        });

        // Passkey direkt im E-Mail-Feld vorschlagen (autocomplete="username webauthn")
        PublicKeyCredential.isConditionalMediationAvailable?.().then(async (available) => {
            if (!available) return;
            conditional = new AbortController();
            try {
                const credential = await request('conditional', conditional.signal);
                if (credential) submit(credential);
            } catch (error) {
                const message = friendlyError(error);
                if (message && error?.name !== 'NotAllowedError') showError(message);
            }
        }).catch(() => { /* kein Vorschlag möglich – Button reicht */ });
    };

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', initLogin);
    else initLogin();
})();
