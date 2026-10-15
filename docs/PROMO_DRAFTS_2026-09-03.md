# ร่างโปรโมต Claude Meter v1.9.1 — 2026-09-03

_เขียนหลังยืนยันว่า GitHub ปลดแฟลกแล้ว (ตรวจแบบไม่ล็อกอิน 200 ทุกจุด ดู PROMO_TODO.md ท้ายไฟล์)_
_ทุกโพสต์ในไฟล์นี้ **ยังไม่ถูกโพสต์** — เตรียมไว้ให้คุณอ่านก่อน แล้วสั่งได้ว่าจะเอาอันไหน_

---

## สรุปสถานะที่ตรวจสดวันนี้

| สิ่งที่ตรวจ | ผล |
|---|---|
| repo / โปรไฟล์ / release v1.9.1 แบบไม่ล็อกอิน | **200 ทุกอัน — ปลดแฟลกแล้ว** ✅ |
| `api.github.com/users/SKGoC-CLI` แบบไม่ล็อกอิน | 200 ✅ (เดือนที่แล้ว 404) |
| `releases/latest` | คืน `v1.9.1` ✅ ตัวเช็คอัปเดตในแอปมองเห็นแล้ว |
| ดาวบน repo | 5 |
| ยอดโหลด `ClaudeMeter-portable.zip` v1.9.1 | **0** — เพราะปล่อยตอนบัญชีถูกซ่อนพอดี |
| awesome-claude-code **#2215** | ยัง **open** ไม่มีใครแตะตั้งแต่ 12 ก.ค. (เป็น *issue* ไม่ใช่ PR) |
| โพสต์เก่า r/ClaudeCode `1uux0l8` | ยัง `is_robot_indexable: false`, `approved_at_utc: null` = **ตายสนิท** |
| karma ของ u/Overall_Rate_8351 | **1** (comment karma 1, post karma 0) — ยังเท่าเดิม |

**ข้อสรุปสำคัญ:** ปัญหา GitHub จบแล้ว แต่ **ปัญหา karma ยังอยู่ 100%** โพสต์ Reddit
ซับใหญ่ตอนนี้มีโอกาสสูงมากที่จะโดน automod กรองเงียบเหมือนเดิม — ผมเลยจัดลำดับใหม่
เอาช่องที่ **ไม่มีกำแพง karma** ขึ้นก่อน

---

## ลำดับที่ผมแนะนำ

| ลำดับ | ช่อง | ทำไม | กำแพง karma |
|---|---|---|---|
| 1 | **X (Twitter)** | โพสต์ได้ทันที ไม่มี automod ใช้ GIF ได้ เก็บไว้เป็นลิงก์อ้างอิงถาวร | ไม่มี |
| 2 | **Show HN (Hacker News)** | บัญชีใหม่โพสต์ Show HN ได้เลย ไม่ต้องมี karma และคนที่นั่นชอบ tray app เล็กๆ ที่ทำเอง | ไม่มี |
| 3 | **awesome-claude-code #2215** | คอมเมนต์สั้นๆ ครั้งเดียว มีเหตุผลจริง (repo ถูกซ่อนไปเดือนนึง กรรมการอาจเปิดแล้วเจอ 404) | ไม่มี |
| 4 | **r/SideProject** | ซับที่เป็นมิตรกับ karma ต่ำที่สุดในบรรดาที่เล็งไว้ | เสี่ยง |
| 5 | **r/csharp / r/dotnet / r/opensource** | รอหลังมี karma แล้วค่อยลุย | สูง |

---

# 1. X (Twitter) — โพสต์นี้ก่อน

**แนบ:** `docs/demo-credit-graph.gif` (5.41 MB — ใต้ลิมิต 15 MB ของ X)
**ทางเลือกภาพนิ่ง:** `docs/light-dark-v1.9.1.png`

```
I kept blowing through my Claude limits and never knew until it was too late.

Every usage meter I found was Mac-only or a Chrome extension. I'm on Windows.
So I built one for the system tray.

Session (5h) + weekly + per-model, live. Portable exe, no .NET install. MIT.

https://github.com/SKGoC-CLI/claude-meter-for-windows
```

**เธรดต่อ (ตอบตัวเอง) — ไม่บังคับ แต่ช่วยให้คนเห็นฟีเจอร์:**

```
It also answers the thing a percentage can't: how much longer can I keep working?

- burn-rate ETA under each bar ("full in ~2h 41m")
- usage credits in real dollars vs your monthly cap
- every active Claude Code session's context window
- mini mode: one bar, always on screen
```

**แฮชแท็ก (ต่อท้ายทวีตแรก ถ้าอยากได้):** `#ClaudeCode #buildinpublic #dotnet`

---

# 2. Show HN

**Title (ต้องขึ้นต้น "Show HN:" เป๊ะๆ):**

```
Show HN: A Windows tray app that shows your Claude usage limits live
```

**URL:**

```
https://github.com/SKGoC-CLI/claude-meter-for-windows
```

**คอมเมนต์แรก (โพสต์เสร็จแล้วตอบตัวเองทันที — ธรรมเนียม HN):**

```
I build small tools for myself on Windows, and I kept hitting Claude's usage
limits mid-task with no warning. Claude Code has /usage, but that means stopping
what I'm doing to go ask. Every meter I could find was a Mac menu-bar app or a
Chrome extension.

So this is a ~16px tray icon that renders the number itself — it can stack
Session (5h) over Weekly on two rows, drawn with a hand-rolled bitmap font
because anything downscaled turned to mush at that size. Click it and you get
the full popup: a bar and reset countdown per limit, a usage graph that marks
downtime as "no data" instead of drawing a fake line through it, usage credits
in real dollars against the monthly cap, and every active Claude Code session's
context window.

It reads the OAuth token your existing Claude login already stores locally,
read-only — it never sees a password and never writes to those files. Single
self-contained exe, no .NET install, MIT.

Two things I'd do differently: the whole thing is WinForms with GDI+ drawing,
which is unfashionable but starts in under a second and idles at a few MB; and
the burn-rate ETA measures pace over a window matched to each limit's own length
(1h for the 5-hour session, 24h for weekly) because an hour of heavy use
extrapolated across a week predicts a wall that a night's sleep erases.

Happy to answer anything.
```

**เวลาที่ควรโพสต์:** วันธรรมดา ~20:00–22:00 น. เวลาไทย (= เช้าอเมริกา)

---

# 3. awesome-claude-code #2215 — คอมเมนต์เดียวจบ

https://github.com/hesreallyhim/awesome-claude-code/issues/2215

> ⚠️ **นี่คือครั้งเดียวที่จะคอมเมนต์** maintainer เขียนไว้เองว่าไม่ชอบการมาจี้
> แต่รอบนี้มีเหตุผลจริง ไม่ใช่การทวง: repo ถูก GitHub ซ่อนจากคนไม่ล็อกอิน
> ตั้งแต่ 5 ส.ค. ถึงต้น ก.ย. ถ้ากรรมการเปิดดูช่วงนั้นจะเจอ 404 ล้วนๆ

```
Quick factual note, not a nudge on the queue: between Aug 5 and early Sept my
GitHub account was hidden from logged-out visitors by an anti-spam flag, so
anyone opening this repo without being signed in got a 404 and the README
images didn't load. GitHub has since lifted it. If anyone did look during that
window, that's what they would have seen — the link works normally now:

https://github.com/SKGoC-CLI/claude-meter-for-windows

Happy to wait however long the queue takes.
```

---

# 4. r/SideProject — ถ้าจะลอง Reddit ให้เริ่มที่นี่

**Title:**

```
I kept hitting Claude's usage limits without warning, so I built a Windows tray meter for it
```

**Body:**

```
I'm a solo dev on Windows. I was building a bot for my condo and kept slamming
into Claude's 5-hour and weekly limits mid-task — always as a surprise, always
at the worst moment.

Claude Code has a /usage command, but checking it means stopping what you're
doing. Every ambient meter I found was a Mac menu-bar app or a Chrome extension.
Nothing for the Windows tray. So I made one.

The tray icon draws the percentage itself and shifts colour as you approach the
limit — it can show Session (5h) and Weekly stacked on two rows, so a draining
week can't hide behind a fresh session. Click it and you get bars, reset
countdowns, a usage graph, your usage credits in actual dollars against the
monthly cap, and every active session's context window.

The bit I use most is the burn-rate line: "full in ~2h 41m" at your current
pace. That's the question the percentage never answers — how much longer can I
keep working before I have to stop?

It reads the OAuth token your existing Claude login already stores on the
machine, read-only. Never sees your password, never writes to those files.

Free, MIT, portable single exe — no .NET install needed.

https://github.com/SKGoC-CLI/claude-meter-for-windows

Built it for myself, figured someone else on Windows has the same problem.
```

**แนบรูป:** `docs/demo-credit-graph.gif` หรือ `docs/light-dark-v1.9.1.png`

---

# 5. r/csharp / r/dotnet — เก็บไว้ทีหลัง (ต้องมี karma ก่อน)

**Title:**

```
Drawing a readable two-row tray icon at 16x16 in WinForms — what I learned building a Claude usage meter
```

**Body (มุมเทคนิค ไม่ใช่โฆษณา — ซับสาย dev ต้องแบบนี้ถึงจะรอด):**

```
I built a system-tray app in .NET 8 WinForms that shows my Claude AI usage
limits, and the hardest part turned out to be the 16x16 icon.

The goal: show two numbers at once — Session (5h) on top, Weekly below — each
coloured by its own severity. At 16 pixels tall, that's 7 pixels per row.

Things that did not work:
- GDI+ text rendering at 5-6pt: unreadable mush regardless of TextRenderingHint
- Drawing at 32x32 and downscaling: antialiasing eats thin strokes entirely
- Any font with subpixel positioning

What worked: a hand-rolled 3x5 bitmap font, drawn at exactly 16x16, no scaling,
no antialiasing anywhere. Each glyph is a hardcoded bit pattern. It's about
60 lines and it's the only thing that stays crisp.

Other notes from the project:
- The whole app is WinForms with manual GDI+ painting. Unfashionable, but it
  cold-starts in well under a second and idles at a few MB, which matters for
  something that sits in your tray all day.
- Getting the light theme right needed a real contrast audit. The status colours
  had been inherited from dark mode, where they sit on #1e1e1e; on a pale
  background the main percentage was running at 2.4:1 against a 4.5:1 WCAG
  requirement. Switched to the Windows 11 Fluent semantic colours.
- Tinting the DWM window border (DwmSetWindowAttribute) is the way to give a
  near-white popup a visible edge without losing the rounded corners Windows
  already draws for you.

Source is MIT if any of it is useful:
https://github.com/SKGoC-CLI/claude-meter-for-windows
```

> **ทำไมโพสต์นี้ต่างจากอันอื่น:** r/csharp กับ r/dotnet ลบโพสต์ที่เป็น "ดูโปรเจกต์ผมสิ"
> เฉยๆ แต่รับโพสต์ที่มีเนื้อหาเทคนิคให้คนอ่านได้ของ เลยเขียนให้อ่านจบได้โดยไม่ต้องคลิกลิงก์

---

# 6. แผนแก้ปัญหา karma (ต้นตอที่ทำให้ r/ClaudeCode ตาย)

karma = 1 คือเหตุผลเดียวที่โพสต์เดิมโดนกรอง ไม่ใช่เนื้อหา ถ้าไม่แก้ตรงนี้
โพสต์ Reddit ทุกอันข้างบนก็เสี่ยงจบแบบเดียวกัน

**วิธีที่เร็วที่สุดโดยไม่ต้องฝืน:** ตอบคอมเมนต์ในซับที่คุณอ่านอยู่แล้ว วันละ 2–3 อัน
สัก 1–2 สัปดาห์ ให้ถึง ~30–50 karma ตอบแบบมีประโยชน์จริงพอ ไม่ต้องเขียนยาว
ซับที่ให้ karma ง่ายและตรงกับคุณ: r/ClaudeAI, r/ClaudeCode, r/csharp, r/thailand

**ห้ามทำ:** ลบแล้วโพสต์ซ้ำที่ r/ClaudeCode ตอนนี้ — โดนกรองรอบสองจะเข้าข่ายสแปม

**ทางลัดที่ทำได้เลย:** ส่ง modmail ขอ approve โพสต์เก่า
https://www.reddit.com/message/compose?to=%2Fr%2FClaudeCode

```
Hi mods — my post from July 13 (id 1uux0l8) about a Windows tray app for
Claude usage limits appears to have been caught by the automod filter; it's
never been approved and isn't indexable. I think it's my low account karma
rather than the content. It's a free MIT-licensed open-source tool, no
monetisation, no affiliate links.

If it doesn't fit the sub that's completely fine — I'd just like to know
rather than leave it sitting invisible. Happy to repost with whatever flair
or format you prefer.

Thanks.
```

---

# ตารางงานที่ **คุณ** ต้องลงมือเอง

ทุกอย่างในนี้ต้องล็อกอินบัญชีคุณ ผมทำแทนไม่ได้ (และไม่ควรทำแทน)

| # | งาน | ที่ไหน | คุณต้องทำ | ผมเตรียมให้แล้ว | เวลา |
|---|---|---|---|---|---|
| 1 | โพสต์ X + แนบ GIF | x.com | ล็อกอิน → วางข้อความ → แนบ `docs/demo-credit-graph.gif` → โพสต์ | ข้อความ + เธรดต่อ (ข้อ 1) | 5 นาที |
| 2 | โพสต์ Show HN | news.ycombinator.com/submit | ล็อกอิน (สมัครใหม่ได้ ไม่ต้องมี karma) → วาง title + URL → โพสต์ → **ตอบคอมเมนต์ตัวเองทันที** | title + URL + คอมเมนต์แรก (ข้อ 2) | 10 นาที |
| 3 | คอมเมนต์ awesome-claude-code | issue #2215 | เปิดลิงก์ → วางคอมเมนต์ → ส่ง **ครั้งเดียว ห้ามทวงซ้ำ** | คอมเมนต์ (ข้อ 3) | 2 นาที |
| 4 | ส่ง modmail r/ClaudeCode | ลิงก์ compose ในข้อ 6 | ล็อกอิน u/Overall_Rate_8351 → วาง → ส่ง | ข้อความ modmail (ข้อ 6) | 3 นาที |
| 5 | สะสม karma | r/ClaudeAI, r/csharp ฯลฯ | ตอบคอมเมนต์วันละ 2–3 อัน จนถึง ~30–50 | ลิสต์ซับ (ข้อ 6) | 1–2 สัปดาห์ |
| 6 | โพสต์ r/SideProject | reddit | **รอหลังข้อ 5** แล้วค่อยโพสต์ | โพสต์เต็ม (ข้อ 4) | 5 นาที |
| 7 | โพสต์ r/csharp | reddit | **รอหลังข้อ 5** | โพสต์เต็ม (ข้อ 5) | 5 นาที |

**ทำวันนี้ได้เลย: ข้อ 1–4** (รวม ~20 นาที) ที่เหลือรอ karma

---

## สิ่งที่ผมจงใจ **ไม่** ทำ

- ไม่โพสต์อะไรแทนคุณสักช่อง — ทุกอันเป็นการเผยแพร่สู่สาธารณะ ต้องให้คุณกดเอง
- ไม่แตะ awesome-claude-code เกินหนึ่งคอมเมนต์ — maintainer เขียนไว้ชัด
- ไม่ลบโพสต์เก่าใน r/ClaudeCode — เสี่ยงโดนตีเป็นสแปม
- ไม่ไปเปิดช่องใหม่อีกสิบช่อง (Product Hunt, alternativeto, ฯลฯ) — เอา 4 ช่องนี้ให้ติดก่อน
  ค่อยว่ากัน ไม่งั้นเหนื่อยฟรี

---

## ถ้าโพสต์แล้ว — วัดผลยังไง

ตัวเลขที่บอกความจริงที่สุดคือ **ยอดโหลด zip** (ตอนนี้ 0)

ให้ผมรันเช็คให้ได้ทุกเมื่อ — ยอดโหลด, ดาว, สถานะโพสต์ Reddit (ผ่าน Arctic Shift API
เพราะ reddit.com บล็อก fetch ตรง — ดู Gotcha ท้าย PROMO_TODO.md)
