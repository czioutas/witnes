Notes:

navigator.sendBeacon()

SPAs (Single Page Applications) like React, Vue, or Angular present a unique challenge: the "Initial Load" only happens once. After that, when a user clicks a link, the page doesn't actually "refresh"—the JavaScript just swaps out the content.If your script only listens for the load event, you’ll be totally blind to 90% of the user's experience. To a support agent, the user might say "The dashboard is slow," but your tool would show a "Fast" 1.2s initial login.Here is how you handle SPAs without making your script too heavy.1. The "Soft Navigation" ProblemIn an SPA, when a user moves from /login to /dashboard, it’s called a Soft Navigation.The issue: The browser's Navigation Timing API (which gives us those nice DNS/TCP/SSL metrics) does not fire again. * The solution: Your script needs to "hook" into the browser's History API to know when the URL has changed.2. How to "See" SPA ChangesYour script should listen for the popstate event or "monkey-patch" the history.pushState method. This allows you to detect exactly when a user "navigates."The logic inside your script:Detect URL Change: "Oh, the user clicked '/billing'."Start a Timer: const startTime = performance.now();Track the "Work": Instead of full network stats (which are hard to group for SPAs), you track the XHR/Fetch calls that happen immediately after that URL change.End the Timer: Once the new "view" has settled (usually detected by the last API call finishing or a short period of inactivity).3. Measuring the "Total SPA Experience"For SPAs, you want to show the support agent a "Route Change" event rather than a "Page Load" event.MetricTraditional Site (Hard Load)SPA (Soft Load)TriggerBrowser load eventURL change (pushState)Main DataHTML, CSS, JS DownloadAPI / JSON RequestsKey BottleneckServer Response (TTFB)Data Fetching + Rendering4. The "Double requestAnimationFrame" TrickTo know when the user actually sees the new page in a React/Vue app, professional tools use a trick:JavaScript// After the route change is detected
requestAnimationFrame(() => {
  requestAnimationFrame(() => {
    const totalTime = performance.now() - startTime;
    // This is the most accurate "Perceived Load Time" for an SPA
  });
});
The first requestAnimationFrame happens before the browser paints; the second happens after the paint is finished. This gives you the moment the user's eyes actually see the new content.5. API Monitoring: The Core of SPA SpeedIn an SPA, "Slowness" is almost always a slow API call. Your script should automatically wrap window.fetch and XMLHttpRequest.What the support agent sees in the sidebar:Route: /settingsTransition Time: 3.4s (🔴 Slow)Primary Cause: GET /api/user-profile took 2.8s.Should you support SPAs in the MVP?Yes, but keep it simple. Don't try to build deep React-specific hooks. Just:Watch for URL changes.Measure the time between "URL Change" and "Last API call finished."Send that as a "Route Change" packet.This covers 99% of modern SaaS apps (which are mostly SPAs) without requiring your customers to install complex framework-specific libraries.


Exactly. The moment the page is "finished" (or the user moves on), you bundle everything and ship it.

However, "finishing" is a bit of a trick in 2026. If you send data too early, you miss late-loading scripts. If you send it too late, the user might close the tab and you lose the data.

Here is the "Golden Logic" for your data gatherer to ensure you get 100% of the payload without slowing down the site.

1. The Payload Bundle
Your script should build a JSON object as the page loads. It’s like a suitcase you’re packing.

The "Tag" Info: User ID, Organization ID, Project Key, and current URL.

The "Milestones": TTFB, First Paint, and Total Load Time.

The "Waterfall": An array of every network request captured by performance.getEntriesByType('resource').

2. When to "Ship It" (The Two Triggers)
To be bulletproof, your script needs two different "shipping" modes:

A. The "Load" Trigger (The Happy Path)
You wait for the window.onload event. But since some sites are "lazy" (they load things 2 seconds after the spinner stops), we add a tiny buffer.

Logic: window.addEventListener('load', () => setTimeout(shipData, 1000)); This captures the initial load plus any immediate "late" requests.

B. The "Visibility" Trigger (The Safety Net)
This is the most important part for modern browsers and mobile. If a user closes the tab before the page officially finishes loading, you still want that "Slow" data.

Logic: Listen for visibilitychange. If the state becomes hidden, ship whatever you have immediately.

3. The "Silent" Shipping Method
You should not use a standard fetch() or Axios call to send this data. If the user is closing the tab, those requests will be cancelled.

Instead, use navigator.sendBeacon().

Why? It’s a "fire and forget" API. The browser moves the data to a special background queue. Even if the user kills the browser process, the operating system will finish sending your data to the server.

Cost: It doesn't block the next page from loading, so it has zero impact on the user's experience.

🖼️ The Data Journey (Visualized)
📦 Example Payload Structure
Your backend should expect a JSON that looks something like this:

JSON
{
  "project_key": "SL-99231",
  "metadata": {
    "url": "https://client.com/dashboard",
    "user_id": "u_882",
    "duration": 3400,
    "connection": "4g"
  },
  "waterfall": [
    { "name": "/api/user", "duration": 250, "type": "fetch", "ttfb": 180 },
    { "name": "hero.jpg", "duration": 1200, "type": "img", "size": "2.4mb" }
  ]
}
The "Witnes" Decision Flow:
Ingestion Worker receives this.

Logic: if (payload.metadata.duration > 2000) { saveFullWaterfall(payload); }

Else: incrementProjectBaseline(payload.project_key, payload.metadata.duration);

One final "Gotcha": CORS
When your script on client-site.com tries to send data to api.witnes.io, the browser will check for CORS (Cross-Origin Resource Sharing) permissions.

Your Ingress Worker must respond with the header: Access-Control-Allow-Origin: * (or specifically the client's domain). Without this, sendBeacon will fail.