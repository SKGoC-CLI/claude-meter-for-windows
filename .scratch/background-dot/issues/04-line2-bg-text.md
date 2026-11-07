# 04: "⟳" background text on line 2

**What to build:** A Background session's second line shows `⟳ <first task description>`, or `⟳ N bg: <first description>` when several tasks are live, truncated to fit the popup width. It sits alongside the step list when there is one, or alone when there isn't.

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] Session context exposes live task count + first description
- [ ] One task → `⟳ <desc>`; several → `⟳ N bg: <desc>`; long text ellipsised, never overlaps the token/age column
- [ ] Works with and without a step list; popup width calculation accounts for it
- [ ] Checked by running the app and screenshotting dark + light themes
