/**
 * Witnes.js - Lightweight "Black Box" Session Recorder
 * Focus: Environment + Full Waterfall Latency
 */
(function (window) {
    if (window.WitnesInitialized) return;
    window.WitnesInitialized = true;

    // -----------------------------
    // 1. Setup & Navigation State
    // -----------------------------

    const config = window.witnesConfig || { debug: true };
    let identifiedUserId = config.userId || null;

    // Capture tracker script URL for CDN baseline measurement
    const trackerScriptSrc = document.currentScript ? document.currentScript.src : null;

    const navigationId = crypto.randomUUID();
    const navigationStart = performance.now();

    // Background tab detection: browser defers painting until tab is visible,
    // inflating LCP and settling times with idle wait time.
    const wasBackgroundTab = document.visibilityState === 'hidden';
    let tabVisibleAt = wasBackgroundTab ? null : 0;

    if (wasBackgroundTab) {
        const onVisible = () => {
            if (document.visibilityState === 'visible' && tabVisibleAt === null) {
                tabVisibleAt = Math.round(performance.now());
                document.removeEventListener('visibilitychange', onVisible);
            }
        };
        document.addEventListener('visibilitychange', onVisible);
    }

    let finalized = false;
    let finalizeReason = null;

    let identifyCalled = false;
    let identifyCalledAt = null;
    let loadEventFiredAt = null; // Added variable to track load event timing

    let lastActivityAt = performance.now();
    let lastResourceCount = 0;
    let pendingRequests = 0;

    const IDLE_WINDOW = 2000;
    const MAX_TOTAL_WINDOW = 30000;

    let metricsAtLoad = { lcp: 0, cls: 0 };
    let metricsAtIdentify = { lcp: 0, cls: 0 };

    const KEYS = { REF: 'wit_ref' };
    if (!sessionStorage.getItem(KEYS.REF)) {
        sessionStorage.setItem(KEYS.REF, document.referrer || 'direct');
    }

    const sessionState = { lcp: 0, cls: 0, longTasks: [] };

    const pageOrigin = window.location.origin;

    const isSameOrigin = (url) => {
        try {
            return new URL(url).origin === pageOrigin;
        } catch {
            return true; // relative URLs are same-origin
        }
    };

    // -----------------------------
    // 2. Data Collectors
    // -----------------------------

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
            downlinkInMbs: conn.downlink ?? 0
        } : null;
    };

    const getWaterfall = () => {
        return performance.getEntriesByType('resource').map(r => {
            // Cross-origin resources without Timing-Allow-Origin have responseStart=0
            const timingRestricted = r.responseStart === 0 && r.duration > 0;

            const ttfb = r.responseStart > 0 ? Math.round(r.responseStart - r.requestStart) : 0;
            const stalled = r.requestStart > 0 ? Math.max(0, Math.round(r.requestStart - r.startTime)) : 0;

            // Download phase: first byte → last byte
            const downloadMs = r.responseStart > 0 && r.responseEnd > r.responseStart
                ? Math.round(r.responseEnd - r.responseStart)
                : 0;

            // Effective throughput during download (bytes/sec)
            // Only meaningful if download took >10ms and had real data
            const downloadBytesPerSec = (downloadMs > 10 && r.transferSize > 0)
                ? Math.round(r.transferSize / (downloadMs / 1000))
                : null;

            return {
                name: r.name.split('/').pop() || r.name,
                fullUrl: r.name,
                initiator: r.initiatorType,
                protocol: r.nextHopProtocol,
                sameOrigin: isSameOrigin(r.name),
                timingRestricted,
                start: Math.round(r.startTime),
                latency: {
                    stalled,
                    ttfb,
                    downloadMs,
                    total: Math.round(r.duration)
                },
                data: {
                    transferInBytes: r.transferSize,
                    isCompressed: r.encodedBodySize < r.decodedBodySize && r.encodedBodySize > 0,
                    downloadBytesPerSec: downloadBytesPerSec
                }
            };
        }).sort((a, b) => a.start - b.start);
    };

    const getManualLCP = () => {
        const entries = performance.getEntriesByType('resource')
            .filter(r => r.initiatorType === 'img')
            .sort((a, b) => b.transferSize - a.transferSize);

        if (entries.length > 0) return entries[0].responseEnd;

        const paint = performance.getEntriesByType('paint');
        return paint.find(p => p.name === 'first-contentful-paint')?.startTime || 0;
    };

    const getVitals = () => {
        const nav = performance.getEntriesByType('navigation')[0];
        const paint = performance.getEntriesByType('paint');
        if (!nav) return null;

        return {
            pageLoad: {
                interactive: Math.round(nav.domInteractive),
                complete: Math.round(nav.domComplete)
            },
            webVitals: {
                fcp: Math.round(paint.find(p => p.name === 'first-contentful-paint')?.startTime || 0),
                lcp: Math.round(sessionState.lcp || getManualLCP()),
                cls: parseFloat(sessionState.cls.toFixed(2))
            },
            metricsAtLoad: {
                lcp: Math.round(metricsAtLoad.lcp),
                cls: parseFloat(metricsAtLoad.cls.toFixed(2))
            },
            metricsAtIdentify: {
                lcp: Math.round(metricsAtIdentify.lcp),
                cls: parseFloat(metricsAtIdentify.cls.toFixed(2))
            }
        };
    };

    const getCdnBaseline = () => {
        if (!trackerScriptSrc) return null;
        const entry = performance.getEntriesByType('resource')
            .find(r => r.name === trackerScriptSrc);
        if (!entry) return null;
        return {
            ttfbMs: entry.responseStart > 0 ? Math.round(entry.responseStart - entry.requestStart) : 0,
            totalMs: Math.round(entry.duration),
            transferBytes: entry.transferSize
        };
    };

    const getInitialLoad = () => {
    const nav = performance.getEntriesByType('navigation')[0];
    if (!nav) return null;

    return {
            protocol: nav.nextHopProtocol, // Important for Network Verdicts
            latency: {
                stalled: Math.max(0, Math.round(nav.requestStart - nav.startTime)),
                ttfb: Math.round(nav.responseStart - nav.requestStart),
                total: Math.round(nav.duration)
            },
            data: {
                transferInBytes: nav.transferSize,
                isCompressed: nav.encodedBodySize < nav.decodedBodySize && nav.encodedBodySize > 0
            }
        };
    };

    // -----------------------------
    // 3. XHR/Fetch Tracking
    // -----------------------------

    const origSend = XMLHttpRequest.prototype.send;
    XMLHttpRequest.prototype.send = function () {
        pendingRequests++;
        lastActivityAt = performance.now();
        this.addEventListener('loadend', () => {
            pendingRequests--;
            lastActivityAt = performance.now();
        });
        return origSend.apply(this, arguments);
    };

    const origFetch = window.fetch;
    window.fetch = function () {
        pendingRequests++;
        lastActivityAt = performance.now();
        return origFetch.apply(this, arguments).finally(() => {
            pendingRequests--;
            lastActivityAt = performance.now();
        });
    };

    // -----------------------------
    // 4. Passive Observers
    // -----------------------------

    new PerformanceObserver(l => {
        sessionState.lcp = l.getEntries().pop().startTime;
        lastActivityAt = performance.now();
    }).observe({ type: 'largest-contentful-paint', buffered: true });

    new PerformanceObserver(l => {
        for (const e of l.getEntries()) {
            if (!e.hadRecentInput) {
                sessionState.cls += e.value;
                lastActivityAt = performance.now();
            }
        }
    }).observe({ type: 'layout-shift', buffered: true });

    try {
        new PerformanceObserver(l => {
            for (const e of l.getEntries()) {
                sessionState.longTasks.push({ 
                    s: Math.round(e.startTime), // Start time relative to navigationStart
                    d: Math.round(e.duration)   // Duration in ms
                });
                lastActivityAt = performance.now();
            }
        }).observe({ type: 'longtask', buffered: true });
    } catch (e) {}

    // -----------------------------
    // 4. Finalize Logic
    // -----------------------------

    const finalize = (reason) => {
        if (finalized) return;

        finalized = true;
        finalizeReason = reason;

        const durationMs = Math.round(lastActivityAt - navigationStart);

        emitPayload({
            durationMs,
            finalizeReason,
            pendingRequests,
            loadEventFiredAt: loadEventFiredAt ? Math.round(loadEventFiredAt) : null,
            identifyDelayMs: identifyCalledAt ? Math.round(identifyCalledAt - navigationStart) : null
        });
    };

    // -----------------------------
    // Idle Detection (AFTER IDENTIFY)
    // -----------------------------

    const idleCheck = () => {
        if (finalized) return;

        const now = performance.now();
        const resources = performance.getEntriesByType('resource').length;

        if (resources !== lastResourceCount) {
            lastResourceCount = resources;
            lastActivityAt = now;
        }

        if (pendingRequests > 0) {
            lastActivityAt = now;
        }

        if (now - lastActivityAt >= IDLE_WINDOW) {
            finalize('idle');
            return;
        }

        setTimeout(idleCheck, 500);
    };

    // Snapshot metrics at load event
    window.addEventListener('load', () => {
        loadEventFiredAt = performance.now();
        metricsAtLoad.lcp = sessionState.lcp;
        metricsAtLoad.cls = sessionState.cls;
        console.log('[Witnes] Load event fired, metrics snapshot taken.');
    });

    window.addEventListener('pagehide', () => {
        if (!finalized) finalize('pagehide');
    });

    // -----------------------------
    // 5. Dispatcher
    // -----------------------------

    const emitPayload = (lifecycleMeta) => {
        const pk = config.projectKey || config.project_key;
        if (!pk) return;

        const payload = {
            metadata: {
                event: 'LOAD',
                pk,
                navigationId,
                loadEventFiredAt,
                pageRequestedAtByVisitor: new Date(performance.timeOrigin).toISOString(),
                emittedAt: new Date().toISOString(),
                wasBackgroundTab,
                tabVisibleAtMs: tabVisibleAt,
                ...lifecycleMeta
            },
            session: {
                userId: identifiedUserId,
                url: window.location.href,
                ref: sessionStorage.getItem(KEYS.REF)
            },
            network: getConnectivity(),
            device: getContext(),
            performance: {
                vitals: getVitals(),
                initialLoad: getInitialLoad(),
                cdnBaseline: getCdnBaseline(),
                waterfall: getWaterfall(),
                jank: sessionState.longTasks.splice(0)
            }
        };

        const blob = new Blob([JSON.stringify(payload)], { type: 'application/json' });
        const endpoint = `https://api.witnes.io/v1/events?pk=${pk}`;

        navigator.sendBeacon
            ? navigator.sendBeacon(endpoint, blob)
            : fetch(endpoint, { method: 'POST', body: blob, keepalive: true });
    };

    // -----------------------------
    // 6. Public API
    // -----------------------------

    const Witnes = {
        identify: (id, options) => {
            if (identifyCalled) return;

            if (id === null || id === undefined) {
                if (!options || options.guest !== true) {
                    throw new Error(
                        '[Witnes] identify() called with null/undefined userId. ' +
                        'If this is intentional, pass { guest: true } as the second argument: ' +
                        'Witnes.identify(null, { guest: true })'
                    );
                }
            }

            identifiedUserId = id;
            identifyCalled = true;
            identifyCalledAt = performance.now();

            // Snapshot metrics at identify time
            metricsAtIdentify.lcp = sessionState.lcp;
            metricsAtIdentify.cls = sessionState.cls;

            // Start idle detection
            console.log('[Witnes] identify() called, starting idle detection.');
            lastActivityAt = performance.now();
            lastResourceCount = performance.getEntriesByType('resource').length;
            requestAnimationFrame(idleCheck);

            // Hard cutoff: 30s total from navigation start
            const timeElapsed = performance.now() - navigationStart;
            const timeRemaining = MAX_TOTAL_WINDOW - timeElapsed;
            
            if (timeRemaining > 0) {
                setTimeout(() => {
                    if (!finalized) {
                        console.log('[Witnes] Hard cutoff reached (30s total).');
                        finalize('timeout');
                    }
                }, timeRemaining);
            } else {
                // Already past 30s, finalize immediately
                finalize('timeout');
            }
        }
    };

    window.Witnes = Witnes;

    if (config.userId || config.guest) {
        Witnes.identify(config.userId, config.guest ? { guest: true } : undefined);
    }

})(window);