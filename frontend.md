our dashboard should have the following

sidenav
- home
- users
- organizations
- analytics

in the bottom nav items
- Team (this is what Users is now, rename it)
- Usage
- ORganization settings

Users page
- table view
- top left search for user id
- top right time filter (select a time range)

User detail page
- top user info (user id, browser(s), os(s))
- bellow table with pagination show the info from the gold table for this user

Page load detail page
- Waterfal view of network
- Jank Reports

Home page
- first load if there is no project key, component telling them to add a project key by going to the project key page
- second load if there is a project key, saying, time to add it and send data, check our integration page (link) and come back
- third load if there is a project key and integration is done we show a larger component for them to search for a user id
- bellow we show recent users top 4 by date
- bellow we show a "data ingestion working" as long as we have data for the past 1 day

Usage (project keys)
- They can see their project key (only one we allow atm)
- We show how many total page loads they have consumed
- We show them their package info

Remove pages:
- Everything under reporting
- Everything under Ledger
- Everything under Catalog
- Remove accounting

Order:
- Project keys/ usage
    - add controllers for this
    - actual view

- Users view
    - add controller for users, this will query the gold table to get users
    - add the search component and the filters
    - use pagination code/libs/Libs/Pagination/PaginationRequest.cs
    - actual view

- User Detail view
    - add controller, this will query gold to get the page loads for a user
    - add time filter (we should make this component filter reusable not just for tables)
    - actual view

- Page load view
    - add controller, this will query silver
    - actual view

- Home page
    - as described

do not use ANY, use types from api.ts
you can regenerate api.ts by calling sh scripts/generate-openapi.sh 

create a TODO.md so we can follow it