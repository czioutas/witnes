Plan: SPA Navigation Support for w.js
Context
The tracker (code/tracker/w.js) only fires once per full page load. SPAs do client-side navigations via History API (pushState/popstate), but the tracker never captures these. We need each SPA navigation to produce its own tracking event, similar to a full page load.

Approach: Refactor state into resettable nav object + hook History API
Key Constraint
Several Performance APIs don't work for SPA navigations:

navigation[0] (domInteractive, TTFB) - only reflects initial page load
Paint timing (FCP) - doesn't re-fire
LCP observer - stops after first user interaction
Resource buffer - cumulative (not scoped per navigation)
What works: CLS (delta), long tasks (delta), XHR/Fetch tracking, navigator.connection, device info, new resource entries (filtered by startTime).

Changes (all in code/tracker/w.js)

1. Extract per-navigation state into a resettable nav object

Replace the 13+ flat let/const declarations with let nav = createNav(0). A createNav(resourceBaseline) factory returns fresh state. Module-level things stay outside: config, trackerScriptSrc, pageOrigin, XHR/Fetch patches, PerformanceObservers, identifiedUserId.

2. Add global accumulators for CLS and long tasks

let globalClsTotal = 0 - CLS observer appends here
const globalLongTasks = [] - long task observer appends here
Per-nav CLS = globalClsTotal - nav.clsBaseline
Per-nav jank = globalLongTasks.filter(t => t.s >= nav.navigationStart) 3. Refactor observers/XHR/fetch to use nav.\*

Since closures capture the nav variable (not value), when nav is reassigned, subsequent callbacks write to the new navigation's state automatically.

4. Refactor getWaterfall() to scope resources per navigation

performance.getEntriesByType('resource').slice(nav.resourceBaseline) - for initial LOAD, baseline is 0 (all resources). For SPA navs, baseline is the count at nav start.

5. Refactor getVitals() to branch on nav.isSpaNav

SPA navs send zeroed pageLoad and fcp (unavailable), CLS as delta, LCP as-is (likely 0).

6. Add handleSpaNav() function

On SPA nav detected:

Finalize current nav with reason 'spa_nav'
Capture resource baseline + CLS baseline
Reset nav = createNav(...) with isSpaNav: true
Auto-restart idle detection if user was already identified 7. Hook History API (with debounce)

Patch history.pushState, history.replaceState, listen to popstate. Use setTimeout(0) debounce to handle frameworks that call pushState + replaceState in sequence. Skip if URL hasn't actually changed.

8. Refactor emitPayload()

Set event: nav.isSpaNav ? 'SPA_NAV' : 'LOAD'
Read all state from nav.\*
pageRequestedAtByVisitor = new Date(performance.timeOrigin + nav.navigationStart) for SPA navs
Send zeroed (not null) initialLoad and vitals for SPA navs (backend model InitialLoadModel is non-nullable)
Send cdnBaseline: null for SPA navs (already nullable in backend) 9. Guard timeout against stale nav

Capture nav.navigationId when setting timeout; check it matches before finalizing.

Backend Impact
Zero backend changes needed - Bronze already anticipates SPA_NAV event type, Silver uses null-safe ?. operators, Gold will produce benign results (fast/good) for zeroed metrics.
Files Modified
code/tracker/w.js - all changes here
code/tracker/dist/w.min.js - regenerate after (manual minification step)
Verification
Load a page with the tracker - confirm normal LOAD event fires as before
Navigate via SPA (click a link that uses pushState) - confirm a new SPA_NAV event fires
Check the beacon payload: metadata.event should be SPA_NAV, session.url should be the new URL, waterfall should only contain new resources
Browser back/forward should also trigger new events
Rapid SPA navigations should debounce correctly (one event per navigation)
