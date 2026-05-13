window.themeManager = window.themeManager || {
    getTheme: function (key) {
        return localStorage.getItem(key);
    },
    setTheme: function (key, value) {
        localStorage.setItem(key, value);
    }
};
