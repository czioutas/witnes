/**
 * Witnes.js - Lightweight "Black Box" Session Recorder
 * Focusing on: Session IDs, Environment, and Full Waterfall Latency.
 */
(function (window) {
    if (window.WitnesInitialized) return;
    window.WitnesInitialized = true;

    // --- 1. Setup & Session Identity ---
    const config = window.witnesConfig || { debug: true };
    let identifiedUserId = config.userId || null;
    let lastUrl = window.location.href;

    const KEYS = { 
        REF: 'wit_ref', // original referrer
        SID: 'wit_sid', // session id
        GID: 'wit_gid'  // guest id
    };

    // Set original referrer if not present
    if (!sessionStorage.getItem(KEYS.REF)) {
        sessionStorage.setItem(KEYS.REF, document.referrer || 'direct');
    }

    // Tab-specific Session ID
    let sessionId = sessionStorage.getItem(KEYS.SID);
    if (!sessionId) {
        sessionId = 'sid_' + Math.random().toString(36).substr(2, 9) + '_' + Date.now();
        sessionStorage.setItem(KEYS.SID, sessionId);
    }

    // STICKY Guest ID: The anchor
    let guestId = localStorage.getItem(KEYS.GID);
    if (!guestId) {
        guestId = 'gid_' + Math.random().toString(36).substr(2, 9);
        localStorage.setItem(KEYS.GID, guestId);
    }

    const sessionState = { lcp: 0, cls: 0, longTasks: [] };
    let loadPending = null;

    // --- 2. Data Collectors ---

    const getContext = () => ({
        userAgent: navigator.userAgent,
        platform: navigator.platform,
        language: navigator.language,
        screen: { w: window.screen.width, h: window.screen.height, dpr: window.devicePixelRatio },
        viewport: { w: window.innerWidth, h: window.innerHeight }
    });

    const getConnectivity = () => {
        const conn = navigator.connection || navigator.mozConnection || navigator.webkitConnection;
        return conn ? {
            effectiveType: conn.effectiveType,
            rtt: conn.rtt + 'ms',
            downlink: conn.downlink + 'Mb/s'
        } : null;
    };

    const getWaterfall = () => {
        return performance.getEntriesByType('resource').map(r => {
            const ttfb = r.responseStart > 0 ? Math.round(r.responseStart - r.requestStart) : 0;
            const stalled = Math.round(r.requestStart - r.startTime);
            
            return {
                name: r.name.split('/').pop() || r.name,
                fullUrl: r.name,
                initiator: r.initiatorType,
                protocol: r.nextHopProtocol,
                start: Math.round(r.startTime),
                latency: {
                    stalled: stalled, // Browser queueing
                    ttfb: ttfb,       // Server processing + Network trip
                    total: Math.round(r.duration)
                },
                data: {
                    transfer: (r.transferSize / 1024).toFixed(2) + 'KB',
                    isCompressed: r.encodedBodySize < r.decodedBodySize && r.encodedBodySize > 0
                }
            };
        }).sort((a, b) => a.start - b.start);
    };

    const getManualLCP = () => {
        const entries = performance.getEntriesByType('resource')
            .filter(r => r.initiatorType === 'img')
            .sort((a, b) => b.transferSize - a.transferSize);
        
        if (entries.length > 0) {
            return entries[0].responseEnd; // Largest image load time
        }
        
        // Fallback to FCP if no images
        const paint = performance.getEntriesByType('paint');
        return paint.find(p => p.name === 'first-contentful-paint')?.startTime || 0;
    };

    const getVitals = (isInitial) => {
        const nav = performance.getEntriesByType('navigation')[0];
        const paint = performance.getEntriesByType('paint');
        if (!nav) return null;
        
        return {
            pageLoad: {
                interactive: Math.round(nav.domInteractive) + 'ms',
                complete: Math.round(nav.domComplete) + 'ms'
            },
            webVitals: isInitial ? {
                fcp: Math.round(paint.find(p => p.name === 'first-contentful-paint')?.startTime || 0) + 'ms',
                lcp: Math.round(sessionState.lcp || getManualLCP()) + 'ms',
                cls: parseFloat(sessionState.cls.toFixed(4))
            } : null
        };
    };

    // --- 3. Passive Observers ---
    new PerformanceObserver(l => { sessionState.lcp = l.getEntries().pop().startTime; }).observe({ type: 'largest-contentful-paint', buffered: true });

    new PerformanceObserver(l => { for (const e of l.getEntries()) if (!e.hadRecentInput) sessionState.cls += e.value; }).observe({ type: 'layout-shift', buffered: true });
    try {
        new PerformanceObserver(l => { 
            for (const e of l.getEntries()) sessionState.longTasks.push({ d: Math.round(e.duration) + 'ms' }); 
        }).observe({ type: 'longtask', buffered: true });
    } catch(e) {}

    // --- 4. The Dispatcher ---

    const Witnes = {
        identify: (id) => {
            identifiedUserId = id;
        },
        emit: (eventType, isPartial = false) => {
            const pk = config.projectKey || config.project_key;
            if (!pk || (!identifiedUserId && !guestId)) return;

            const isInitial = eventType === 'LOAD';
            
            // Structured for easy .NET class mapping
            const payload = {
                metadata: {
                    event: eventType,
                    pk: pk,
                    ts: new Date().toISOString(),
                    incomplete: isPartial 
                },
                session: {
                    userId: identifiedUserId,
                    sessionId: sessionId,
                    guestId: guestId,
                    url: window.location.href,
                    ref: sessionStorage.getItem(KEYS.REF)
                },
                network: getConnectivity(),
                device: getContext(),
                performance: {
                    vitals: getVitals(isInitial),
                    waterfall: getWaterfall(),
                    jank: sessionState.longTasks.splice(0)
                }
            };

            if (config.debug) {
                const style = "background: #6e41e2; color: #fff; padding: 2px 5px; border-radius: 3px; font-weight: bold;";
                console.groupCollapsed(`%cWITNES%c ${eventType} %c${sessionId}`, style, "color: #6e41e2; font-weight: bold;", "color: #888;");
                console.log("Environment:", { network: payload.network, device: payload.device });
                console.log("Waterfall Timeline:");
                console.table(payload.performance.waterfall);
                console.groupEnd();
            }

            const blob = new Blob([JSON.stringify(payload)], { type: 'application/json' });
            const endpoint = `https://api-witnes.ziou.xyz/api/v1/ingestion?pk=${pk}`;
            
            if (navigator.sendBeacon) {
                navigator.sendBeacon(endpoint, blob);
            } else {
                fetch(endpoint, { method: 'POST', body: blob, keepalive: true });
            }
        }
    };

    // --- 5. Event Listeners ---
    let spaNavTimeout = null;
    let dataEmitted = false;
    
    window.addEventListener('load', () => {
        loadPending = setTimeout(() => {
            dataEmitted = true;
            Witnes.emit('LOAD', false);
        }, 5000);
    });

    window.addEventListener('pagehide', () => {
        if (!dataEmitted) {
            clearTimeout(loadPending);
            dataEmitted = true;
            Witnes.emit('LOAD', true);
        }
        if (spaNavTimeout) clearTimeout(spaNavTimeout); // Clear pending SPA nav
    });

    const onNav = () => {
        if (window.location.href !== lastUrl) {
            lastUrl = window.location.href;
            spaNavTimeout = setTimeout(() => Witnes.emit('SPA_NAV'), 800);
        }
    };

    window.addEventListener('popstate', onNav);
    ['pushState', 'replaceState'].forEach(m => {
        const o = history[m];
        history[m] = function () { o.apply(this, arguments); onNav(); };
    });

    window.Witnes = Witnes;
    if (identifiedUserId) Witnes.identify(identifiedUserId);
})(window);