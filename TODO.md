# TODOS

## Task 1 - Emphasize on EU data

take a look at https://www.simpleanalytics.com/
we need to make it clear that the data are stored in EU and we do not use cookies to track the users or use services that do.
even when we collect data they are non identifieable and we do not share them with third parties. for more see privacy policy

## Task 2 - Pricing model

see code/api/Api/Application/Pricing

we will have 2 tiers but we need to be able to add extra custom tiers for specific customers

- starter
  - 29 euros per month
  - 2 team members (admin + 1 user)
  - 50k credits per month
  - 7 days data retention
- growth
  - 80 euros per month
  - 4 team members (admin + 3 users)
  - 100k credits per month
  - 14 days data retention

dont worry about throttling, we will do it differently.

we need to see the db with this pricing tiers.

## Task 3 - Tenant pricing

code/api/Api/Application/Pricing/Entities/TenantPricingTierEntity.cs

here we assign the pricing to the tenant. the end date is only if they concluded one pricing tier and then they want to switch to another one. we need to be able to see the history of the pricing tiers for a tenant.

in the tenant pricing we should add 3 flags

- has_trial, which will be true on the first sign up of the user
- has_trial_expired, which will be true when the trial period of 7 days is over
- discount_percentage, which will be used to apply a discount to the pricing tier for specific customers

we dont do billing atm still

changing the tenant pricing will come into effect immediately from the start of the current day of change. We will bother with billing elsehow

## Task 4 - Data cleanup

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

## Task 5 - Billing

We will create a module under Product called Billing. in there we will have a service which will be responsible for billing the tenants based on their pricing tier.

Any info it needs it will use other services and will not directly access the db. for example it will use the tenant pricing service to get the pricing tier of the tenant and then calculate the billing amount based on that.

Rules for billing:

- We process billing on the 1st day of each month for the previous month. so on the 1st of July we will process billing for June.
- Tiers can have a 7 day free trial (there is a flag called is_trial)
- Even if the invoice is 0, we generate it
- We use prorated billing
  - so if they sign up on the middle of the month they will be charged for half of the month.
- We use prorated billing i.e someone signs up, they have 7 free days, after that they will be charged for the tier they have selected but only for the remainder of days. so if they sign up on X day of the month, then they have 7 more days free, they will be billed
