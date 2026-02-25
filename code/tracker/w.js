/**
 * Witnes.js - Lightweight "Black Box" Session Recorder
 * Focus: Environment + Full Waterfall Latency
 * Supports: Full page loads (MPA) and SPA navigations
 */
(function (window) {
    if (window._wi) return;
    window._wi = true;

    // ==============================
    // 1. Module-Level State (Never Reset)
    // ==============================

    const config = window.witnesConfig || {};
    const debug = !!config.debug;
    const log = (...args) => { if (debug) console.log('[Witnes]', ...args); };
    let identifiedUserId = config.userId || null;

    const trackerScriptSrc = document.currentScript ? document.currentScript.src : null;
    const pageOrigin = window.location.origin;

    const IDLE_WINDOW = 2000;
    const MAX_TOTAL_WINDOW = 30000;

    const K_REF = '_wr';
    // Update referrer on every page load so LOAD→LOAD session stitching works
    sessionStorage.setItem(K_REF, document.referrer || 'direct');

    // Global accumulators — never reset, shared across all navigations
    let globalClsTotal = 0;
    const globalClsEvents = [];  // { t: ms (performance.now), v: shift value }
    const globalLongTasks = [];  // { s: startTime, d: duration }

    // The navigationId of the initial hard load — SPA navs reference this as parent
    let parentNavigationId = null;

    // LCP tracked globally — observer keeps running across SPA navs
    let currentLcp = 0;

    const isSameOrigin = (url) => {
        try {
            return new URL(url).origin === pageOrigin;
        } catch {
            return true;
        }
    };

    // ==============================
    // 2. Per-Navigation State Factory
    // ==============================

    const createNav = (resourceBaseline, isSpaNav) => {
        const navId = crypto.randomUUID();

        if (!parentNavigationId) {
            parentNavigationId = navId;
        }

        return {
            navigationId: navId,
            navigationStart: performance.now(),
            isSpaNav: !!isSpaNav,
            resourceBaseline: resourceBaseline || 0,
            clsBaseline: globalClsTotal,

            finalized: false,
            finalizeReason: null,

            identifyCalled: false,
            identifyCalledAt: null,
            loadEventFiredAt: null,

            lastActivityAt: performance.now(),
            lastResourceCount: 0,
            pendingRequests: 0,

            metricsAtLoad: { lcp: 0, cls: 0 },
            metricsAtIdentify: { lcp: 0, cls: 0 },

            wasBackgroundTab: !isSpaNav && document.visibilityState === 'hidden',
            tabVisibleAt: (!isSpaNav && document.visibilityState === 'hidden') ? null : 0
        };
    };

    // Active navigation — closures reference this variable, so reassigning
    // automatically redirects all subsequent callbacks to the new nav's state.
    let nav = createNav(0, false);

    // Background tab visibility listener for initial load
    if (nav.wasBackgroundTab) {
        const onVisible = () => {
            if (document.visibilityState === 'visible' && nav.tabVisibleAt === null) {
                nav.tabVisibleAt = Math.round(performance.now());
                document.removeEventListener('visibilitychange', onVisible);
            }
        };
        document.addEventListener('visibilitychange', onVisible);
    }

    // ==============================
    // 3. Data Collectors
    // ==============================

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
        return performance.getEntriesByType('resource')
            .slice(nav.resourceBaseline)
            .filter(r => r.startTime >= nav.navigationStart)
            .map(r => {
                const timingRestricted = r.responseStart === 0 && r.duration > 0;
                const ttfb = r.responseStart > 0 ? Math.round(r.responseStart - r.requestStart) : 0;
                const stalled = r.requestStart > 0 ? Math.max(0, Math.round(r.requestStart - r.startTime)) : 0;
                const downloadMs = r.responseStart > 0 && r.responseEnd > r.responseStart
                    ? Math.round(r.responseEnd - r.responseStart)
                    : 0;
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
                    latency: { stalled, ttfb, downloadMs, total: Math.round(r.duration) },
                    data: {
                        transferInBytes: r.transferSize,
                        isCompressed: r.encodedBodySize < r.decodedBodySize && r.encodedBodySize > 0,
                        downloadBytesPerSec
                    }
                };
            })
            .sort((a, b) => a.start - b.start);
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
        if (nav.isSpaNav) {
            return {
                pageLoad: { interactive: 0, complete: 0 },
                webVitals: {
                    fcp: 0,
                    lcp: Math.round(currentLcp || getManualLCP())
                },
                metricsAtLoad: { lcp: Math.round(nav.metricsAtLoad.lcp), cls: parseFloat(nav.metricsAtLoad.cls.toFixed(2)) },
                metricsAtIdentify: { lcp: Math.round(nav.metricsAtIdentify.lcp), cls: parseFloat(nav.metricsAtIdentify.cls.toFixed(2)) }
            };
        }

        const navEntry = performance.getEntriesByType('navigation')[0];
        const paint = performance.getEntriesByType('paint');
        if (!navEntry) return null;

        return {
            pageLoad: {
                interactive: Math.round(navEntry.domInteractive),
                complete: Math.round(navEntry.domComplete)
            },
            webVitals: {
                fcp: Math.round(paint.find(p => p.name === 'first-contentful-paint')?.startTime || 0),
                lcp: Math.round(currentLcp || getManualLCP())
            },
            metricsAtLoad: { lcp: Math.round(nav.metricsAtLoad.lcp), cls: parseFloat(nav.metricsAtLoad.cls.toFixed(2)) },
            metricsAtIdentify: { lcp: Math.round(nav.metricsAtIdentify.lcp), cls: parseFloat(nav.metricsAtIdentify.cls.toFixed(2)) }
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
        if (nav.isSpaNav) return null;

        const navEntry = performance.getEntriesByType('navigation')[0];
        if (!navEntry) return null;

        return {
            protocol: navEntry.nextHopProtocol,
            latency: {
                stalled: Math.max(0, Math.round(navEntry.requestStart - navEntry.startTime)),
                ttfb: Math.round(navEntry.responseStart - navEntry.requestStart),
                total: Math.round(navEntry.duration)
            },
            data: {
                transferInBytes: navEntry.transferSize,
                isCompressed: navEntry.encodedBodySize < navEntry.decodedBodySize && navEntry.encodedBodySize > 0
            }
        };
    };

    // ==============================
    // 4. XHR/Fetch Tracking
    // ==============================

    const origSend = XMLHttpRequest.prototype.send;
    XMLHttpRequest.prototype.send = function () {
        nav.pendingRequests++;
        nav.lastActivityAt = performance.now();
        this.addEventListener('loadend', () => {
            nav.pendingRequests--;
            nav.lastActivityAt = performance.now();
        });
        return origSend.apply(this, arguments);
    };

    const origFetch = window.fetch;
    window.fetch = function () {
        nav.pendingRequests++;
        nav.lastActivityAt = performance.now();
        return origFetch.apply(this, arguments).finally(() => {
            nav.pendingRequests--;
            nav.lastActivityAt = performance.now();
        });
    };

    // ==============================
    // 5. Passive Observers (Global)
    // ==============================

    new PerformanceObserver(l => {
        currentLcp = l.getEntries().pop().startTime;
        nav.lastActivityAt = performance.now();
    }).observe({ type: 'largest-contentful-paint', buffered: true });

    new PerformanceObserver(l => {
        for (const e of l.getEntries()) {
            if (!e.hadRecentInput) {
                globalClsTotal += e.value;
                globalClsEvents.push({
                    t: Math.round(e.startTime),
                    v: parseFloat(e.value.toFixed(4))
                });
                nav.lastActivityAt = performance.now();
            }
        }
    }).observe({ type: 'layout-shift', buffered: true });

    try {
        new PerformanceObserver(l => {
            for (const e of l.getEntries()) {
                globalLongTasks.push({
                    s: Math.round(e.startTime),
                    d: Math.round(e.duration)
                });
                nav.lastActivityAt = performance.now();
            }
        }).observe({ type: 'longtask', buffered: true });
    } catch (e) {}

    // ==============================
    // 6. Finalize Logic
    // ==============================

    const finalize = (reason) => {
        if (nav.finalized) return;

        nav.finalized = true;
        nav.finalizeReason = reason;

        const durationMs = Math.round(nav.lastActivityAt - nav.navigationStart);

        emitPayload({
            durationMs,
            finalizeReason: reason,
            pendingRequests: nav.pendingRequests,
            loadEventFiredAt: nav.loadEventFiredAt ? Math.round(nav.loadEventFiredAt) : null,
            identifyDelayMs: nav.identifyCalledAt ? Math.round(nav.identifyCalledAt - nav.navigationStart) : null
        });
    };

    // ==============================
    // 7. Idle Detection (After Identify)
    // ==============================

    const idleCheck = () => {
        if (nav.finalized) return;

        const now = performance.now();
        const resources = performance.getEntriesByType('resource').length;

        if (resources !== nav.lastResourceCount) {
            nav.lastResourceCount = resources;
            nav.lastActivityAt = now;
        }

        if (nav.pendingRequests > 0) {
            nav.lastActivityAt = now;
        }

        if (now - nav.lastActivityAt >= IDLE_WINDOW) {
            finalize('idle');
            return;
        }

        setTimeout(idleCheck, 500);
    };

    // Load event — only meaningful for the initial page load
    window.addEventListener('load', () => {
        nav.loadEventFiredAt = performance.now();
        nav.metricsAtLoad.lcp = currentLcp;
        nav.metricsAtLoad.cls = globalClsTotal;
        log('Load event fired, metrics snapshot taken.');
    });

    window.addEventListener('pagehide', () => {
        log('pagehide fired, finalized:', nav.finalized);
        if (!nav.finalized) finalize('pagehide');
    });

    window.addEventListener('beforeunload', () => {
        log('beforeunload fired, finalized:', nav.finalized);
        if (!nav.finalized) finalize('beforeunload');
    });

    document.addEventListener('visibilitychange', () => {
        if (document.visibilityState === 'hidden') {
            log('visibilitychange→hidden, finalized:', nav.finalized);
            if (!nav.finalized) finalize('visibilitychange');
        }
    });

    // ==============================
    // 8. SPA Navigation Detection
    // ==============================

    const handleSpaNav = () => {
        // 1. Finalize the current nav — emits its beacon
        if (!nav.finalized) {
            finalize('spa_nav');
        }

        // 2. Snapshot baselines for the new nav
        const resourceBaseline = performance.getEntriesByType('resource').length;

        // 3. Create fresh nav state
        nav = createNav(resourceBaseline, true);

        // 4. Reset LCP for new nav (observer will report new entries)
        currentLcp = 0;
    };

    let spaNavTimer = null;
    let lastTrackedUrl = window.location.href;

    const scheduleSpaNav = () => {
        clearTimeout(spaNavTimer);
        spaNavTimer = setTimeout(() => {
            const currentUrl = window.location.href;
            if (currentUrl !== lastTrackedUrl) {
                lastTrackedUrl = currentUrl;
                handleSpaNav();
            }
        }, 0);
    };

    const patchHistory = () => {
        const wrap = (orig) => function (...args) {
            orig.apply(this, args);
            scheduleSpaNav();
        };
        history.pushState = wrap(history.pushState.bind(history));
        history.replaceState = wrap(history.replaceState.bind(history));
        window.addEventListener('popstate', scheduleSpaNav);
    };

    patchHistory();

    // ==============================
    // 9. Dispatcher
    // ==============================

    const emitPayload = (lifecycleMeta) => {
        const pk = config.projectKey || config.project_key;
        if (!pk) return;

        const payload = {
            metadata: {
                event: nav.isSpaNav ? 'SPA_NAV' : 'LOAD',
                pk,
                navigationId: nav.navigationId,
                parentNavigationId: nav.isSpaNav ? parentNavigationId : null,
                loadEventFiredAt: lifecycleMeta.loadEventFiredAt,
                pageRequestedAtByVisitor: new Date(
                    performance.timeOrigin + nav.navigationStart
                ).toISOString(),
                emittedAt: new Date().toISOString(),
                wasBackgroundTab: nav.wasBackgroundTab,
                tabVisibleAtMs: nav.tabVisibleAt,
                ...lifecycleMeta
            },
            session: {
                userId: identifiedUserId,
                url: window.location.href,
                ref: sessionStorage.getItem(K_REF)
            },
            network: getConnectivity(),
            device: getContext(),
            performance: {
                vitals: getVitals(),
                initialLoad: getInitialLoad(),
                cdnBaseline: nav.isSpaNav ? null : getCdnBaseline(),
                waterfall: getWaterfall(),
                clsEvents: globalClsEvents.slice(0),
                jank: globalLongTasks.slice(0)
            }
        };

        const blob = new Blob([JSON.stringify(payload)], { type: 'application/json' });
        const endpoint = `https://api.witnes.io/v1/events?pk=${pk}`;

        log('Emitting payload, reason:', lifecycleMeta.finalizeReason, 'navId:', nav.navigationId, 'size:', blob.size);
        navigator.sendBeacon
            ? navigator.sendBeacon(endpoint, blob)
            : fetch(endpoint, { method: 'POST', body: blob, keepalive: true });
    };

    // ==============================
    // 10. Public API
    // ==============================

    const Witnes = {
        identify: (id, options) => {
            if (nav.identifyCalled) return;

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
            nav.identifyCalled = true;
            nav.identifyCalledAt = performance.now();

            // Snapshot metrics at identify time
            nav.metricsAtIdentify.lcp = currentLcp;
            nav.metricsAtIdentify.cls = globalClsTotal;

            // Start idle detection
            log('identify() called, starting idle detection.');
            nav.lastActivityAt = performance.now();
            nav.lastResourceCount = performance.getEntriesByType('resource').length;
            requestAnimationFrame(idleCheck);

            // Hard cutoff: 30s total from navigation start, guarded against stale nav
            const timeElapsed = performance.now() - nav.navigationStart;
            const timeRemaining = MAX_TOTAL_WINDOW - timeElapsed;
            const thisNavId = nav.navigationId;

            if (timeRemaining > 0) {
                setTimeout(() => {
                    if (!nav.finalized && nav.navigationId === thisNavId) {
                        log('Hard cutoff reached (30s total).');
                        finalize('timeout');
                    }
                }, timeRemaining);
            } else {
                finalize('timeout');
            }
        }
    };

    window.Witnes = Witnes;

    if (config.userId || config.guest) {
        Witnes.identify(config.userId, config.guest ? { guest: true } : undefined);
    }

})(window);
