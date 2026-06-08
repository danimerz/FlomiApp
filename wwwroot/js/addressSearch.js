window.swisstopoSearch = async (query) => {
    if (!query || query.length < 3) return [];
    const url = 'https://api3.geo.admin.ch/rest/services/api/SearchServer'
        + '?searchText=' + encodeURIComponent(query)
        + '&type=locations&origins=address&sr=4326&lang=de&limit=8';
    try {
        const resp = await fetch(url);
        if (!resp.ok) return [];
        const data = await resp.json();
        return (data.results || []).map(r => {
            const a = r.attrs || {};
            const cleanLabel = (a.label || '').replace(/<[^>]+>/g, '');
            const postalCode = String(a.postalcode || '');
            const city = a.city || '';
            const idx = cleanLabel.indexOf(' ' + postalCode + ' ');
            const street = idx > 0 ? cleanLabel.substring(0, idx).trim() : cleanLabel;
            return { label: cleanLabel, street, postalCode, city };
        });
    } catch {
        return [];
    }
};
