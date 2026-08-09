# Burn-rate ETA uses a lookback matched to the window's length

The ETA line under each row ("full in ~2h 41m") is a straight-line extrapolation: take
the utilisation gained per second over some recent slice of `UsageHistory`, divide the
remaining headroom by it. The only real decision is how long that slice is, and one
length does not fit every window.

The 5-hour session is measured over the **last 1 hour**; every weekly window (`seven_day*`)
over the **last 24 hours**. The reason is that the same burst of typing means different
things on the two clocks. An hour of heavy Claude Code use can burn 20% of a 5-hour
session — extrapolated over a week that reads "weekly full in 4 h", which is not a
forecast, it is a description of the last hour dressed up as one. Weeks are lived at a
weekly rhythm: work, sleep, a day off. A 24-hour slice contains at least one of those
troughs, so the number survives contact with the next one.

## Considered options

- **One lookback for everything (1 h)** — cheapest, and correct for the session row.
  On weekly windows it produces alarming numbers that are wrong within minutes of the
  user standing up from the desk. Rejected: a meter that cries wolf gets ignored.
- **One lookback for everything (24 h)** — steady on weekly, useless on the session:
  the window it is predicting is itself only 5 hours long, so a 24-hour average is
  mostly made of a session that already reset.
- **Average since the start of the window** — the steadiest option and the least useful.
  It cannot react to a change in pace, which is the only time anyone looks at an ETA.
- **A weighted / least-squares fit instead of first-vs-last endpoints** — more defensible
  statistically, more code, and it changes the answer by minutes on a number already
  prefixed with "~". Not worth it until someone can point at a case where the endpoints lie.

## Consequences

- Non-credit history is pruned to 24 hours (`UsageHistory.Window`), so 24 h is the
  practical ceiling for any lookback. A longer weekly slice would need a retention
  change, not just a constant.
- The rule keys off `Key.StartsWith("seven_day")`, so a model-scoped weekly such as
  `seven_day_opus` inherits the weekly treatment automatically. Any *new* window shape
  the API introduces falls into the 1-hour branch by default — check that when it happens.
- Endpoints-only slope means a window that reset mid-slice yields a negative rate, which
  is discarded. Effect: no ETA for a while after a reset, rather than a wrong one.
- The ETA is suppressed when the limit would reset before it fills. On a fresh week that
  hides the weekly ETA almost always — by design. It appears when it is actually news.
