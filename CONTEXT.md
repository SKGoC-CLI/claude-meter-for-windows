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

- **Wallet row / "Extra usage" row** — แถวเดียวในป๊อปอัปที่รวม `extra_usage` กับ `spend`
  จาก API (มันคือกระเป๋าเดียวกัน) โชว์เป็นเงิน `$used / $limit`. key = `extra_usage`.
- **used / limit** — เงินที่ใช้ไปเดือนนี้ (server-side cumulative, รีเซ็ต $0 วันที่ 1) เทียบ
  เพดานรายเดือน. อ่านสด monthly_limit จาก API เสมอ — **ห้าม hardcode** (เคยเป็น $50 ตอนนี้ $100).
- **Balance / promotional credit** — ยอดเครดิตคงเหลือ ($99+) **ไม่มีใน OAuth endpoint ใดๆ**
  ที่แอปเข้าถึงได้ จึงไม่แสดง.
