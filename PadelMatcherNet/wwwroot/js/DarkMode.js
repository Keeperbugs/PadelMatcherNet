
        // === COOKIE FUNCTIONS ===
        window.setCookie = function(name, value, days) {
            const expires = new Date(Date.now() + days * 864e5).toUTCString();
        document.cookie = name + '=' + encodeURIComponent(value) + '; expires=' + expires + '; path=/; SameSite=Lax';
        };

        window.getCookie = function(name) {
            return document.cookie.split('; ').reduce((r, v) => {
                const parts = v.split('=');
        return parts[0] === name ? decodeURIComponent(parts[1]) : r;
            }, '');
        };

        // === SYSTEM THEME DETECTION ===
        window.detectSystemDarkMode = function() {
            return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
        };

        // === THEME APPLICATION ===
        window.applyThemeImmediately = function(isDarkMode) {
            const html = document.documentElement;
        const body = document.body;

        console.log('Applying theme immediately:', isDarkMode ? 'DARK' : 'LIGHT');

        // Rimuovi tutte le classi tema esistenti
        html.classList.remove('mud-theme-dark', 'mud-theme-light');
        body.classList.remove('mud-theme-dark', 'mud-theme-light');

        // Applica la classe corretta
        if (isDarkMode) {
            html.classList.add('mud-theme-dark');
        body.classList.add('mud-theme-dark');
        html.setAttribute('data-theme', 'dark');
            } else {
            html.classList.add('mud-theme-light');
        body.classList.add('mud-theme-light');
        html.setAttribute('data-theme', 'light');
            }

        // Dispatch evento per i componenti che ascoltano
        window.dispatchEvent(new CustomEvent('themeChanged', {
            detail: {isDarkMode: isDarkMode }
            }));

        return isDarkMode;
        };

        // === CALCULATE THEME FROM MODE ===
        window.calculateTheme = function(themeMode) {
            switch(themeMode) {
                case 'Dark': return true;
        case 'Light': return false;
        case 'Auto':
        default: return window.detectSystemDarkMode();
            }
        };

        // === APPLY THEME FROM COOKIE ===
        window.applyThemeFromCookie = function() {
            const themeMode = window.getCookie('padel_theme_mode') || 'Auto';
        const isDarkMode = window.calculateTheme(themeMode);

        console.log('Initial theme load:', {themeMode, isDarkMode, systemDark: window.detectSystemDarkMode() });

        return window.applyThemeImmediately(isDarkMode);
        };

        // === INITIALIZE THEME IMMEDIATELY ===
        (function() {
            console.log('=== THEME INITIALIZATION ===');

        // Applica il tema SUBITO per evitare flash
        const appliedDark = window.applyThemeFromCookie();

        console.log('Theme applied on load:', appliedDark);
        })();

        // === WATCH SYSTEM PREFERENCE CHANGES ===
        if (window.matchMedia) {
            window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', function (e) {
                console.log('System theme changed to:', e.matches ? 'DARK' : 'LIGHT');

                const themeMode = window.getCookie('padel_theme_mode') || 'Auto';
                console.log('Current theme mode:', themeMode);

                if (themeMode === 'Auto') {
                    console.log('Auto mode detected, applying system theme');
                    window.applyThemeImmediately(e.matches);
                } else {
                    console.log('Manual mode, ignoring system change');
                }
            });
        }

        // === DEBUG FUNCTIONS ===
        window.debugTheme = function() {
            const themeMode = window.getCookie('padel_theme_mode') || 'Auto';
        const systemDark = window.detectSystemDarkMode();
        const calculatedDark = window.calculateTheme(themeMode);

        console.log('=== THEME DEBUG ===');
        console.log('Theme Mode Cookie:', themeMode);
        console.log('System Dark Mode:', systemDark);
        console.log('Calculated Dark Mode:', calculatedDark);
        console.log('HTML Classes:', document.documentElement.className);
        console.log('Body Classes:', document.body.className);
        console.log('Data Theme:', document.documentElement.getAttribute('data-theme'));
        };

        // === FORCE THEME (for testing) ===
        window.forceTheme = function(mode) {
            console.log('Forcing theme to:', mode);
        window.setCookie('padel_theme_mode', mode, 365);
        const isDark = window.calculateTheme(mode);
        window.applyThemeImmediately(isDark);
        };