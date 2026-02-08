(function (window) {
    if (window.WitnesInitialized) return;
    window.WitnesInitialized = true;

    // 1. Internal State
    const CONFIG = window.witnesConfig || {};
    let userId = window.witnesUserId || null; 
    let lastUrl = window.location.href;

    const Witnes = {
        // Allow updating the ID at any time
        identify: (id) => {
            userId = id;
            // Retroactively fire a LOAD event if we finally got an ID after page load
            if (id && document.readyState === 'complete') {
                Witnes.emit('LOAD');
            }
        },

        emit: (eventType) => {
            const projectKey = CONFIG.projectKey || (window.witnesConfig && window.witnesConfig.projectKey);
            
            // Validation: No Key or No User = No Tracking
            if (!projectKey || !userId) return;

            const payload = JSON.stringify({
                ev: eventType,
                uid: userId,
                ts: Date.now(),
                url: window.location.href,
                ref: document.referrer
            });

            const target = `https://api.witnes.io/ingest?pk=${encodeURIComponent(projectKey)}`;

            // Robustness: fallback to fetch keepalive if sendBeacon is missing (very old browsers)
            if (navigator.sendBeacon) {
                const blob = new Blob([payload], { type: 'application/json' });
                navigator.sendBeacon(target, blob);
            } else {
                fetch(target, { 
                    method: 'POST', 
                    body: payload, 
                    keepalive: true,
                    headers: {
                        'Content-Type': 'application/json'
                    }
                });
            }
        }
    };

    // --- Navigation Tracking Logic ---

    // Initial Load
    window.addEventListener('load', () => Witnes.emit('LOAD'));

    // SPA Tracking: Watch for URL changes
    const handleSinking = () => {
        if (window.location.href !== lastUrl) {
            lastUrl = window.location.href;
            Witnes.emit('SPA_NAV');
        }
    };

    window.addEventListener('popstate', handleSinking);

    // Patch pushState AND replaceState (some frameworks use replaceState for redirects)
    const patch = (type) => {
        const orig = history[type];
        history[type] = function () {
            const result = orig.apply(this, arguments);
            handleSinking();
            return result;
        };
    };
    patch('pushState');
    patch('replaceState');

    // Export to global scope
    window.Witnes = Witnes;

    // Process any "pre-loading" identify calls
    if (window.witnesConfig && window.witnesConfig.userId) {
        Witnes.identify(window.witnesConfig.userId);
    }

})(window);