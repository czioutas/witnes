Witnes — Signal Architecture Recap

1. What We Measure (Tracker / Bronze)
   The tracker fires one payload per page load (SPA navigation tracking is a TODO). It captures:
   Performance Vitals

FCP, LCP, CLS
DOM interactive, DOM complete
Long tasks (start time + duration)
Snapshots at load event and at identify() call

Initial Document Load

Protocol
Stalled time, TTFB, total duration
Transfer size, compression flag

Resource Waterfall

Every resource: URL, initiator type, protocol, same-origin flag
Per resource: stalled, TTFB, download time, total duration
Per resource: transfer size, compression flag, download throughput (bytes/sec)

Network Info (when available)

Effective connection type (4G, 3G, etc.)
RTT
Downlink bandwidth

Device / Context

User agent, platform, language
Screen size, DPR, viewport
Referrer

Session

User ID (via identify())
URL, referrer
Navigation ID, timestamps

Lifecycle

Finalize reason (idle / timeout / pagehide)
Duration, identify delay, load event timing

2. What We Compute (Silver)
   Silver reads bronze events and produces signals per page load:
   Page Sentiment Signal

Actual page load time (LCP-based, not just document complete)
Verdict vs industry threshold (e.g., >3s = slow)
Verdict vs user's own historical baseline (from user_page_stats — see section 4)

vs last 10 / 50 / 200 loads
vs monthly / yearly averages

Classification: fast / normal / slow / critical

Network Signal

If Network Info API available: flag connection type (3G = bad, 4G = probably fine)
If unavailable: "unknown"
CDN control measurement: load time of the Witnes tracker script itself

vs expected baseline (~30ms)
vs this user's historical CDN load time

Verdict: network likely a factor / not a factor / unknown

Frontend Signal

Render-blocking resources detected (large sync JS/CSS before FCP)
Long task density: count and total duration of long tasks overlapping with or following LCP
JS execution time as percentage of total load time
Number of concurrent requests
CLS score
Page size (total decoded body size)

Data Volume Signal

Total transfer size for this page load
vs industry average for this type of page
vs this user's historical transfer size for this page
Missing compression flags (uncompressed resources that should be compressed)

Backend Signal

Document TTFB (from initial navigation)

vs historical baseline
High = server was slow to respond to the page request

API call TTFB (from waterfall, filtered to fetch/xmlhttprequest initiator types)

Flag any individual API call with high TTFB
vs historical baseline for that endpoint
High = specific endpoint(s) slow

3. What We Decide (Gold)
   Gold reads silver signals and produces verdicts per page load:
   Overall Sentiment

Was this a good, neutral, or bad experience?
Is it worse/better/same compared to this user's history?

Root Cause Attribution

Using the signals from silver, determine which layer(s) contributed:

Network: connection was bad (3G, CDN was slow)
Frontend: render blocking, excessive JS, high CLS, too many concurrent calls
Backend: document TTFB high, or specific API call(s) slow
Data: excessive transfer size, missing compression
Unknown: can't determine

Multiple can be true simultaneously

Critical Path (future refinement)

Which of the flagged issues was actually on the critical path of the user's perceived load
E.g., LCP was blocked by a slow API call → backend is the primary cause, even if frontend also had long tasks

4. Aggregate Storage
   Table: user_page_stats
   ColumnDescriptionuser_idIdentified userpage_pathURL pathwindow_typelast_10, last_50, last_200, month_YYYY_MM, year_YYYYvisit_countNumber of loads in this windowavg_load_timeAverage actual page load timeavg_doc_ttfbAverage document TTFBavg_api_ttfb_worstAverage of the slowest API call per loadavg_transfer_sizeAverage total transfer sizeavg_cdn_load_timeAverage tracker CDN load timeupdated_atLast recomputed
   Metrics stored: averages (not medians), because we want to reflect accumulated user pain, not hide outliers.
   How count-based windows work (Prometheus bucket model):

last_10: average of last 10 loads. If only 3 loads exist, average of 3.
last_50: average of last 50. If only 3 exist, same value as last_10.
last_200: same logic. Buckets converge when data is sparse, diverge as data grows.
Gold can detect "all three buckets identical" = low data confidence for this user.

How they're computed:

last*10 / last_50 / last_200: recalculated from bronze on each new event. Bronze IS the ring buffer.
month*\_: sum + count, stored permanently. Immutable after month ends.
year\_\_: sum + count, stored permanently. Immutable after year ends.

Retention trade-off by tier:

7-day tier: accurate last_10, likely accurate last_50. last_200 may equal last_50 for most users. Monthly/yearly always available.
14-day tier: accurate last_50 for regular users.
30-day tier: accurate last_200 for users visiting a few times per week. Full accuracy = upgrade incentive.

Storage footprint: ~80 rows per user per 5 pages per year. Negligible.

INFO

code/tracker/w.js the tracking code

we use medallion architecture

we dont really differentiate between SPA and MPA

ideally if you make an enum place it in code/libs/Libs/Domain

dont worry about database or loss of data

Ingestion happens here code/api/Api/Product/Ingestion
we take the code/api/Api/Product/Ingestion/Models/IngestMetricRequest.cs and we more or less store it as is in code/api/Api/Product/MetricsProcessing/Bronze/MetricBronzeEntity.cs

We then move it to Silver and to Gold using Events/subscribers

We write the logic that transforms from stage to stage in the specific service of each.

We have a LOT of extra logic that we probably do not need. check what we do have and if it doesnt match remove it.

Your task is. Read all of this. Ask questions before you make the plan. come up with a plan. disect it into chunks - we could do it per pillar aka Frontend etc but maybe some data are shared so let me know.

Then make a Task.md file in which you will write all the things that need to be done and as you execute them you mark them as done.
If you need to spawn other agents and you being the main one go ahead. dont create long sessions and build your tasks so that another agent can pick up from where you left off. Add info what you did.
