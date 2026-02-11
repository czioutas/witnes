1. Privacy-Preserving Visitor Tracking with Daily Salt Rotation
   Source: VisitorHashService.cs, DailySaltService.cs
   The Pattern: To comply with GDPR without sacrificing daily unique visitor metrics, the system hashes a combination of IP and User-Agent with a "Daily Salt".
   Why it's blog-worthy:

- It explains how to track unique users within a 24-hour window while ensuring they cannot be tracked across days (since the salt rotates at midnight).
- It showcases a clean implementation using FusionCache with absolute expiry at midnight to keep the salt rotation efficient and consistent across a distributed system.

2. Dynamic CORS with Database-Backed Origins
   Source: DatabaseCorsPolicyProvider.cs, AllowedOriginService.cs
   The Pattern: Instead of hardcoding CORS origins in appsettings.json, the system implements a custom ICorsPolicyProvider that fetches allowed domains from the database (linked to "Project Keys").
   Why it's blog-worthy:

- This is a common requirement for SaaS platforms where customers use their own domains.
- It demonstrates how to "hijack" the standard ASP.NET Core CORS middleware to inject dynamic, tenant-specific logic.

3. Medallion Architecture via MassTransit & Message Bus
   Source: code/api/Api/Product/MetricsProcessing/ (Bronze, Silver, Gold folders)
   The Pattern: The system uses a textbook Medallion Architecture (Bronze -> Silver -> Gold) to process event data asynchronously.
   Why it's blog-worthy:

- Bronze: Raw ingestion (fast, high throughput).
- Silver: Cleaned, validated, and normalized data.
- Gold: Aggregated data ready for dashboards.
- It shows how to use MassTransit to decouple these stages, ensuring that a failure in aggregation (Gold) doesn't lose the raw data (Bronze).

4. Database-Driven Feature Management for Multi-Tenancy
   Source: DatabaseFeatureDefinitionProvider.cs
   The Pattern: An implementation of IFeatureDefinitionProvider from Microsoft.FeatureManagement that reads feature flags from a database and filters them based on the current RequestTenant.
   Why it's blog-worthy:

- It allows toggling features for specific customers (e.g., "Beta" features or "Premium" features) without redeploying.
- It solves the challenge of accessing scoped services (like DbContext) from a singleton service using IHttpContextAccessor.

5. Robust Pro-rated Billing in a Multi-Tenant SaaS
   Source: BillingService.cs
   The Pattern: A sophisticated monthly billing job that handles complex scenarios like mid-month upgrades, 7-day trials, and multiple pricing tiers.
   Why it's blog-worthy:

- It covers the "hard parts" of billing: idempotency, date range overlaps, proration math, and taking data snapshots (to ensure an invoice remains accurate even if the customer's address changes later).

6. Small "Quality of Life" API Improvements
   The Pattern: A collection of small utilities that make the API feel professional and modern.
   Topics:

- Slug-Case Routing: Using IOutboundParameterTransformer to turn MyController into /my-controller.
- Snake-Case JSON: Using a custom JsonNamingPolicy.
- Respecting [EnumMember]: A custom JsonConverterFactory to bridge the gap between .NET enums and specific JSON string values.

7. Hybrid Multi-Tenant Caching with "Fail-Safe" FusionCache
   Source: StartupServiceExtensions.cs, AllowedOriginService.cs
   The Architecture: The project uses FusionCache (by ZiggyCreatures) with a L1 (Memory) + L2 (Redis) approach.
   Why it's blog-worthy:
   - It explains the architecture of "Fail-Safe" caching: what happens when the database is down? The system serves stale data from the cache rather than crashing.
   - It demonstrates how to handle the Cache Stampede problem in a multi-tenant environment—ensuring that when a tenant's configuration expires, only one request recomputes it while others wait or use stale data.
