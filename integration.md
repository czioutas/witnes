🏛️ Integrating Witnes
Welcome to Witnes. To start capturing "receipts" of user behavior for your Customer Success team, you need to add our lightweight tracker to your frontend.

1. Quick Start (Standard Setup)
Copy and paste this snippet into the <head> of your website. This is ideal for traditional web applications where the user identity is known at the time of page render.

HTML
<script>
  window.witnesConfig = { 
    projectKey: 'YOUR_PROJECT_KEY',
    userId: 'user_123' // Replace with your internal User ID
  };
</script>
<script src="https://cdn.witnes.io/track.js" async></script>
2. Dynamic Setup (Single Page Apps)
If you are using a framework like React, Vue, or Next.js, you might not know the User ID until after the page has loaded and the user has logged in.

Step A: Initialize with the Project Key
Add this to your main HTML file:

HTML
<script>
  window.witnesConfig = { projectKey: 'YOUR_PROJECT_KEY' };
</script>
<script src="https://cdn.witnes.io/track.js" async></script>
Step B: Identify the User
Call the identify method as soon as your authentication is complete. Witnes will automatically begin tracking events once an ID is provided.

JavaScript
// Call this after your login logic
if (window.Witnes) {
    window.Witnes.identify("customer_id_99");
} else {
    // If the script is still loading, store the ID for later
    window.witnesConfig.userId = "customer_id_99";
}
3. How Billing Works
Witnes calculates usage based on Units.

1 Unit = 50 Events

Tracked Events: * LOAD: Triggered when a page is first opened.

SPA_NAV: Triggered when the URL changes (even without a page reload).

4. Technical Details
Performance
Zero Blocking: Our script uses the async attribute and navigator.sendBeacon. It will never slow down your page load or delay your users.

Lightweight: The library is < 1KB gzipped.

Security & Privacy
Project Key: Used to route data to your specific dashboard.

User ID: We require a User ID so your CS agents can search for specific sessions when a ticket is filed. We recommend using a non-sensitive internal ID or a hashed email.

Troubleshooting
Events not appearing?

Check that the projectKey matches your dashboard exactly.

Ensure Witnes.identify() is being called with a non-null string.

Open your browser console and verify that requests to api.witnes.io/ingest are returning 202 Accepted.