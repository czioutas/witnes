# TODOS

## [DONE] Task 1 - Emphasize on EU data

take a look at https://www.simpleanalytics.com/
we need to make it clear that the data are stored in EU and we do not use cookies to track the users or use services that do.
even when we collect data they are non identifieable and we do not share them with third parties. for more see privacy policy

## [DONE] Task 2 - Pricing model

see code/api/Api/Application/Pricing

we will have 2 tiers but we need to be able to add extra custom tiers for specific customers

- starter
  - 29 euros per month
  - 2 team members (admin + 1 user)
  - 50k page loads per month
  - 7 days data retention
- growth
  - 80 euros per month
  - 4 team members (admin + 3 users)
  - 100k page loads per month
  - 14 days data retention

dont worry about throttling, we will do it differently.

we need to see the db with this pricing tiers.

## [TODO] Task 3 - Domain sanitization

When someone adds a domain to the project key, we need to sanitize it and normalize it before storing it in the db. this is important for CORS and also for security reasons.

example.com
www.example.com
https://example.com

Normalize before storing:
lowercase
remove protocol
remove trailing slash
And compare only hostnames in code/api/Api/Application/ProjectKeys/ProjectKeyMiddleware.cs

## [TODO] Task 4 - Tenant pricing

code/api/Api/Application/Pricing/Entities/TenantPricingTierEntity.cs

here we assign the pricing to the tenant. the end date is only if they concluded one pricing tier and then they want to switch to another one. we need to be able to see the history of the pricing tiers for a tenant.

in the tenant pricing we should add 3 flags

- has_trial, which will be true on the first sign up of the user
- has_trial_expired, which will be true when the trial period of 7 days is over
- discount_percentage, which will be used to apply a discount to the pricing tier for specific customers

we dont do billing atm still

changing the tenant pricing will come into effect immediately from the start of the current day of change. We will bother with billing elsehow

## [TODO]Task 5 - Data cleanup

we need to be cleaning up the data of tenants based on their pricing model.

we will make a cronjob using coravel and add it to code/api/Api/Application/Extensions/SchedulerExtensions.cs
the cronjob will run every day at midnight and will check for tenants that have expired data retention and will delete the data of those tenants.

Under product add another domain called MetricCleanup
in there we will have a service which will be called by the cronjob
the idea is

Any info it needs it will use other services and will not directly access the db.

- get all tenants
- for each tenant get the pricing tier
- check the data retention of the pricing tier
- delete the data of the tenant that is older than the data retention

## [PENDING] Task 6 - Billing

We will create a module under Product called Billing. in there we will have a service which will be responsible for billing the tenants based on their pricing tier.

Any info it needs it will use other services and will not directly access the db. for example it will use the tenant pricing service to get the pricing tier of the tenant and then calculate the billing amount based on that.

Rules for billing:

- We process billing on the 1st day of each month for the previous month. so on the 1st of July we will process billing for June.
- Tiers can have a 7 day free trial (there is a flag called is_trial)
- Even if the invoice is 0, we generate it
- We use prorated billing
  - so if they sign up on the middle of the month they will be charged for half of the month.
  - when we do prorated billing we need to take into account the 7 day free trial. so if they sign up on X day of the month, they have 7 more days free, after that they will be billed for the remainder of the month. so if they sign up on the 20th of the month, they will have 7 days free until the 27th, and then they will be billed for the remaining 4 days of the month.
  - if however they sign up for the trial on the 25th of the month, they will have 7 days free until the 1st of the next month, and then they will be billed for the full month of the next month.
  - we will change the has_trial_expired when we do billing, but if the month didnt have fully 7 days we wont set it to expired until the 7 days are over. so if they sign up on the 20th of the month, we will set has_trial_expired to true on the 27th of the month, but if they sign up on the 25th of the month, we will set has_trial_expired to true on the 1st of the next month.

## [PENDING] Task 7 - Generate invoices and send them to customers

We have the Billing module.

We now need to generate the invoice and send it to each tenant and also store each invoice.
We will not store pdfs, we will just store the data we need to generate the invoice
we should have an endpoint in which they can see all invoces
we will have an entity to hold the invoice
the entity will have an enum saying Pending, Due, Paid, Overdue

## [PENDING] Task 8 - Dyanmic CORS

We currently set CORS from appsettings.json and in Startup.cs we do WithOrigins
Unfortunately as we need to dynamically be able to add origins based on the tenant, we need to change this and make it dynamic.

We need to make a module called CORS under Product and in there we will have a service which will be responsible for managing the CORS origins for each tenant.

public interface IAllowedOriginService
{
Task<HashSet<string>> GetAllowedOriginsAsync();
}

this will check the db Project Keys and get the domains for cors. This should be a singleton and we should be caching the results for 1 hour.

When any change to a project key happens (add or remove) we will clear the cache and the next time the GetAllowedOriginsAsync is called it will get the new origins.

we will need to add

public class DatabaseCorsPolicyProvider : ICorsPolicyProvider
{
private readonly IAllowedOriginService \_originService;

    public DatabaseCorsPolicyProvider(IAllowedOriginService originService)
    {
        _originService = originService;
    }

    public async Task<CorsPolicy> GetPolicyAsync(HttpContext context, string policyName)
    {
        var origins = await _originService.GetAllowedOriginsAsync();

        var policy = new CorsPolicyBuilder()
            .WithOrigins(origins.ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .Build();

        return policy;
    }

}

services.AddSingleton<ICorsPolicyProvider, DatabaseCorsPolicyProvider>();
services.AddCors();

Important: Normalize domains

Customers will input:

example.com
www.example.com
https://example.com

Normalize before storing:

lowercase

remove protocol

remove trailing slash

And compare only hostnames.

## [PENDING] Task 9 - Checking usage and applying limits

We will have a cronjob that will run every day at midnight and will check the usage of each tenant and will apply the limits based on their pricing tier.
