# CONTEXT — ubiquitous language

คำที่ใช้ในโปรเจกต์นี้ให้ตรงกันทั้งใน UI, เมนู, โค้ด และเอกสาร (กัน section ในป๊อปอัป
กับ label ในเมนูเรียกคนละชื่อเหมือนที่เคยเกิด "SESSION GRAPH" vs "Usage graph").

## หน้าต่างป๊อปอัป

- **Pinned / Always on top** — เรื่องเดียวกัน: โหมดที่ป๊อปอัปลอยค้างบนสุด ลากย้ายได้ และ
  **ไม่หายตอนเสียโฟกัส**. เมนู = `Always on top`, setting = `AlwaysOnTop`, property = `Pinned`.
  (เมนู `Pin (Click-through)` เป็นคนละเรื่อง — นั่นคือให้คลิกทะลุ ใช้ได้เฉพาะตอน pin อยู่.)
- **Minimize button** — ปุ่มขีดเดียว `−` มุมขวาบนของป๊อปอัป กด = **ซ่อนป๊อปอัป** เท่านั้น
  (ผลเท่ากับ Esc) **ไม่แตะสถานะ pin** เปิดใหม่ก็กลับมาลอยที่เดิม. โชว์ทุกสถานะรวมทั้ง Mini mode.
  — **ห้ามเรียก "close button"** เพราะไม่ได้ปิดแอป และ **ห้ามเรียก "collapse"** เพราะนั่นคือ Mini mode.
- **Mini mode** — โหมดหุบป๊อปอัปเหลือ **แถบเดียว** `5h 29% · W 34%` (ตัวเลขสีตามระดับเหมือน
  tray icon, แถบกว้างพอดีข้อความ). เมนู = `Mini mode`, setting = `MiniMode`. เปิดแล้ว
  **บังคับ pin เสมอ** เพราะแถบที่หายตอนเสียโฟกัสไม่มีประโยชน์ ปิดแล้วคืน pin เดิม.
  ค่า `W` = *Highest weekly* (นิยามด้านล่าง) และ **ไม่สนใจ `HiddenLimits`** — ซ่อนแถวในป๊อปอัป
  เต็มไม่ทำให้แถบจิ๋วว่าง. เรียกว่า *mode*; **ห้ามเรียก "compact popup" / "widget"**.
- **ETA / burn rate** — บรรทัดเล็กใต้แถบ progress ของแต่ละแถว `full in ~2h 41m` = คาดว่าอีก
  นานเท่าไหร่จะ **เต็มลิมิต** (ไม่ใช่เวลารีเซ็ต — คนละอย่างกับ `resets in …` ที่อยู่ขวาแถวเดียวกัน).
  เมนู = `Show ETA` (อยู่ท้าย `Show limits`), setting = `ShowEta`. คิดจากความชันของประวัติ
  ย้อนหลังที่ **ยาวตามอายุ window** (session 1 ชม. / weekly 24 ชม. — ดู ADR 0002) และ
  **ถูกกลบ** เมื่อไม่ได้ใช้งาน, ประวัติสั้นกว่า 10 นาที, หรือคำนวณแล้วจะรีเซ็ตก่อนเต็ม.

## กราฟในป๊อปอัป

- **Session graph** — กราฟเส้น "usage remaining" ของ session 5 ชม. บนแกนเวลา 12/24 ชม.
  section header ในป๊อปอัป = `SESSION GRAPH (24H)`; เมนู = `Session graph` (Show/Range/Now position);
  setting = `ShowRemainingGraph`, key ประวัติ = `five_hour`. **ห้ามเรียก "Usage graph" อีก** —
  ชื่อเดิมกำกวมกับ "extra usage" / usage credits.
- **Credit graph** — กราฟเส้นสะสม "usage-credit spend" เทียบเพดานรายเดือน บน **rolling window**
  เลือกช่วง 7/15/30 วัน (`CreditRangeDays`) + Now position เลือกได้ (`CreditNowPositionPercent`,
  Center/3-4/Right เหมือน session แต่ตั้งแยกอิสระ). section header = `CREDIT (30D)` ตาม range;
  เมนู = `Credit graph` (Show/Range/Now position); setting = `ShowCreditGraph`, key ประวัติ =
  `credit_spend` (เก็บ dollars, retention 45 วัน). window ข้ามเดือนได้ → เห็นเดือนก่อน + เส้น
  ดิ่ง $0 ตรงวันที่ 1 (cumulative รีเซ็ต, ไม่มีมาร์ก). แต่ละกราฟเป็นคนละ section → มี submenu
  ของตัวเอง (หลักการเดิม: "each popup section owns its toggle + its options").

## ไอคอนใน tray

- **Tray icon mode** — "ไอคอนโชว์ตัวเลขอะไร" เลือกได้ค่าเดียวจากเมนู `Tray icon shows`
  (setting = `TrayShows`). ค่าที่ถูกต้อง: `auto` / `session` / `weekly` / `both` / `highest`
  — **เป็น mode เดียวจบ ไม่ใช่ toggle ซ้อนกัน** เพราะ "โชว์สองแถว" ตัดกันเองกับ "โชว์ session
  อย่างเดียว" อยู่แล้ว. เรียกว่า *mode* เท่านั้น — **ห้ามเรียก "tray style" / "icon layout"**.
- **Single-row icon** — ไอคอนเลขเดียว (mode `auto`/`session`/`weekly`/`highest`) วาดด้วย
  Segoe UI. mode `auto` มีกฎ **≥90% override** คือถ้ามี window ไหนแตะ 90% จะแย่งไปโชว์แทน
  limit ที่ server บอกว่า active — **กฎนี้ใช้กับ `auto` เท่านั้น**.
- **Two-row icon** — mode `both` วาดสองแถว: **แถวบน = Session (5h) เสมอ, แถวล่าง = Weekly**
  เมนู = `Session + Weekly`. ไม่มีกฎ ≥90% override (เห็นครบสองตัวอยู่แล้ว) และไม่มีแถบ fill.
- **Highest weekly** — ค่าที่แถวล่างของ two-row icon แสดง = ค่าสูงสุดของ **ทุก** window ที่ขึ้นต้น
  `seven_day` (รวม weekly แยกโมเดลอย่าง `seven_day_opus`) ไม่ใช่แค่ `seven_day` ตัวเดียว —
  บัญชีที่ Opus Weekly จะชนก่อนต้องเห็นตัวนั้น. **`extra_usage` ไม่เคยขึ้นไอคอน** เพราะเป็น
  กระเป๋าเงิน ไม่ใช่ลิมิตที่บล็อกงาน.

## Wallet / เครดิต

- **Wallet row / "Monthly spend" row** (ป้ายเดิม "Extra usage" — 2026-10-15 ตั้งตามหน้า Settings ที่เรียก
  `$0.00 of $1 this month` ว่า "Monthly spend limit") — แถวเดียวในป๊อปอัปที่รวม `extra_usage` กับ `spend`
  จาก API (มันคือกระเป๋าเดียวกัน) โชว์เป็นเงิน `$used / $limit`. key = `extra_usage`.
- **used / limit** — เงินที่ใช้ไปเดือนนี้ (server-side cumulative, รีเซ็ต $0 วันที่ 1) เทียบ
  เพดานรายเดือน. อ่านสด monthly_limit จาก API เสมอ — **ห้าม hardcode** (เคยเป็น $50 ตอนนี้ $100).
- **Balance / "Usage credits"** — ยอดเติมเงินคงเหลือ (Settings โชว์ "Usage credits $5"). `spend.balance` ใน usage
  endpoint เป็น null เสมอ (แม้เติมแล้ว) → ดึงจาก `GET /api/oauth/organizations/{org}/prepaid/credits` (`amount` = เซนต์)
  แบบเดียวกับ Claude Code; org จาก `/api/oauth/profile`. ในกระเป๋า *usage credits* (หน้าเว็บ Settings โชว์
  "Usage credits $0" มุมขวา; ซื้อหรือได้โปรโมชันมา ใช้ก่อนเกินโควตา). **ไม่ใช่** `limit − used`
  ของเพดานรายเดือน. 2026-10-02 ยังไม่พบใน OAuth endpoint; 2026-10-15 ตัดสินว่าจะตรวจซ้ำผ่าน
  `usage-raw.json` ที่แอป dump ไว้ — ถ้ามีจะแสดง `Usage credits: $0.00 left` ใน *Credit line*
  (โชว์แม้ $0), ถ้าไม่มี **ห้ามเดาตัวเลข**. (ระวังสับสนกับ *Cloud credit* ข้างล่าง)
- **Cloud credit** — เครดิตที่ได้มาพร้อมแพ็กเกจ (เช่น $100) ใช้กับ **cloud session** อย่างเดียว
  มี **วันหมดอายุ** ใช้หมดหรือหมดอายุแล้วจะกลับไปกินโควตาปกติ. ไม่ใช่ wallet / usage credits
  (คนละกระเป๋า ไม่รีเซ็ตรายเดือน ไม่มีเพดานรายเดือน). หน้าเว็บเรียก "Cloud session credits".
  แสดงใน *Credit line* (ไม่มีหลอด ตั้งแต่ 2026-10-15). **ห้ามเรียก "promo credit" / "balance"**.
- **Credit line** — บรรทัดเล็กบรรทัดเดียวท้ายแถวโควตา รวม Cloud credit + Usage credits:
  `Cloud credit: $98.51 left  ·  Usage credits: $7.50 left` ซ้าย (ขยับขึ้นชิดหลอดแถวบนเมื่อแถวนั้นไม่มีบรรทัด ETA), `expires in 33d (18 Nov)` ขวา
  (สีจาง, ≤7 วันส้ม, ≤1 วันแดง — เตือน "ใกล้หมดอายุแต่เงินยังเหลือ"). ไม่มีหลอด เพราะเครดิตคือ
  ของที่อยากใช้ให้หมด สีตาม % ใช้ไปจึงกลับความหมาย. ในโค้ด `UsageWindow.IsWallet` = สองตัวนี้
  (ชื่อ property เก่า — ไม่ใช่ *Wallet row* ข้างบน).
- **Limit reset** — ตั๋วฟรีที่ล้างโควตาที่ชนแล้ว (คุณสมกกเรียก "ฟรีรีเซ็ต"); Claude Code เสนอให้ใช้ตอนชน limit.
  มาเป็นชุด (*grant*) แต่ละชุดมีจำนวนเหลือ/ทั้งหมด + วันหมดอายุ. API: บล็อก codename `cedar_ember`
  ใน `GET /api/oauth/usage?cedar_ember=1` (ไม่เติม param = `null`). แสดงเป็นบรรทัดเล็กใต้ *Credit line*:
  `Limit resets: 2 of 3 left` ซ้าย, `expires in 12d (28 Oct)` ขวา (หลายชุด = รวมยอด + `next expires` ของชุดใกล้สุด).
  ไม่มีตั๋วใช้ได้ = ไม่โชว์. **ห้ามเรียก "ticket" / "free reset"** ในหน้าจอ — ใช้คำของ Claude Code.

## Current session

- **Session state** — สถานะของแต่ละ session ใน section `CURRENT SESSION` (หลายอัน = `CURRENT SESSIONS (n)`; เดิมชื่อ SESSION CONTEXT) มี 3 ค่า:
  **Working** = Claude กำลังตอบหรือรัน tool อยู่, **Waiting** = Claude ตอบจบ turn แล้วหรือ
  ถามค้างไว้ (รอคุณสมกก) ภายใน 10 นาที, **Idle** = รอเกิน 10 นาที (แถวจาง) และหายจากลิสต์
  หลัง 30 นาที. Working ไม่มีหมดเวลาตราบที่ Claude ยังทำงานไม่จบ turn. แยก "รอคุณ" กับ
  "ปิดหน้าต่างไปแล้ว" ไม่ได้ จึงใช้เวลาตัดสิน. แสดงเป็นจุดสี ● เขียว/ส้ม/เทา หน้าชื่อ.
  ต่างจาก "1h old" ซึ่งคือ *อายุ* นับจากตอนเริ่ม session ไม่ใช่สถานะ.
  ตอน Working จุดเขียว **fade** 100% ↔ 25% รอบละ 1.4 วิ (ไม่มี glow);
  **ตัวเลขบนไอคอน tray จางเหลือ ~45% แล้วกลับ** (สีเดิม) รอบละ 2 วิ ที่ 10 เฟรม/วิ
  ถ้ามี session ใดก็ได้เป็น Working (โหมดตัวเลขเดียว: แถบ % ยังอยู่ จางเฉพาะตัวเลข).
  **Asking** (ทำแล้ว 2026-10-14) = Claude ถามค้างด้วย AskUserQuestion/ExitPlanMode — แยกออกจาก
  Waiting: จุดส้ม fade 1 วิ ใน popup + พื้นหลัง tray ส้ม fade 0 ↔ 45% รอบ 1 วิ (ตัวเลขสีเดิม); Asking ชนะ Working บน tray.
  อ่านสถานะทุก 2 วิ. เมนู Current session → Session alerts: Speed ช้า/ปกติ/เร็ว (×1.5/1/0.6) + ปิด pulse tray แยกได้.
  Waiting จึงเหลือความหมายเดียว = ตอบจบ turn แล้ว (ส้มนิ่ง).
- **Goal progress** — ความคืบหน้าของงานใน session = ขั้นที่เสร็จ ÷ ขั้นทั้งหมด แสดงแทนบรรทัด 2
  ฝั่งซ้าย (แทน auto-compact) เป็นแท่งสั้นต่อกัน `▬▬▬▭▭▭ 3/6  <ชื่อขั้นสั้น>` ไม่มีสี เพื่อไม่ให้ดูเป็นจุดสถานะ (เสร็จ=เทาอ่อน, กำลังทำ=ขาว กระพริบเฉพาะตอน Working, ยังไม่ทำ=เทาเข้ม; เกิน 10 ขั้นกลับเป็น `Step 3/14 · <ชื่อขั้น>`) ไม่มีแถบของตัวเอง. CLI ใช้ `subject` ของ TaskCreate เป็นชื่อขั้น. ชื่อขั้นทุกแหล่งตัดเหลือแค่หัวข้อ (ก่อน `(` `—` `:` `·` `,` ตัวแรก).
  **อายุ session** (`34m old`) ชิดขวาบรรทัด 2 ทุก session; session **Idle** ย่อเหลือบรรทัดเดียว อายุต่อท้าย token. แหล่งข้อมูล: **รายการขั้นตอน** ใน transcript
  (แอป desktop = `update_plan`, CLI = TaskCreate/TaskUpdate) ก่อน ถ้าไม่มีหรือเสร็จครบแล้วใช้ตาราง ✅/⬜ ใน `NEXT-SESSION.md`
  ของโฟลเดอร์ session (เฉพาะ `status: active`) ไม่มีทั้งคู่ = ไม่แสดง. รายการที่เสร็จครบ (หลัง wrap_up) = ซ่อน. ปิดได้ที่เมนู Current session → Show step progress.
  **ไม่ใช่** % context (นั่นคือ context window เต็มแค่ไหน). (ทำแล้ว 2026-10-15, ยังไม่ release)
- **Session name** — บรรทัดแรกของ session = `<ชื่อโปรเจกต์> · <model · effort>` แล้วตามด้วย %. ชื่อยาว
  **ไม่ทำให้ popup กว้างขึ้น** (คิดความกว้างชื่อเพดานราว 30 ตัวอักษร) และตัด **เฉพาะชื่อโปรเจกต์** ตามที่ว่างจริง (pixel)
  ต่อ `…` — `· Opus · Medium`, % และ token ชิดขวาอยู่ครบเสมอ ไม่มีวันซ้อนกัน (ตัดสิน 2026-10-17,
  [WORK-LOG-2026-10-17-session-name-limit.md](WORK-LOG-2026-10-17-session-name-limit.md)).

## อัปเดต

- **Update check** — การเช็กว่ามี release ใหม่บน GitHub ไหม. อัตโนมัติวันละครั้ง (toggle `Check for updates`,
  setting `CheckUpdates`) หรือกดเอง `Check for updates now` (ข้าม throttle). **แค่เช็ก ไม่โหลดอะไร**.
- **Self-update** — กด `Update now` ในหน้าต่างถาม แล้ว app โหลด zip → ตรวจ sha256 + เวอร์ชัน → วางทับ exe เดิม
  → เปิดตัวใหม่เอง. **ต้องให้ผู้ใช้กดยืนยันเสมอ ไม่มีอัปเดตเงียบ**. ตัวเก่าระหว่างทางชื่อ `ClaudeMeter.old.exe`
  และถูกลบทันทีที่ตัวใหม่เปิดขึ้น (ADR 0003, [WORK-LOG-2026-10-19-self-update.md](WORK-LOG-2026-10-19-self-update.md)).
  **ห้ามเรียก "auto-update"** ในหน้าจอ เพราะจะเข้าใจว่าอัปเดตเองโดยไม่ถาม.
