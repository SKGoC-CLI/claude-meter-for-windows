# WORK-LOG 2026-10-15 — แถว Usage credits + จัดกลุ่มเมนูคลิกขวาใหม่

ที่มา: `/grill-with-docs` — คุณสมกกอยากเห็นยอด Usage credits คงเหลือ (แบบหน้าเว็บ claude.ai
Settings → "Usage credits $0") วางบนแถว Cloud credit และอยากให้เมนูคลิกขวาที่ tray ไม่รก

โควตาตอนเริ่ม (จาก ClaudeMeter history.json): five_hour **10%**, seven_day **21%** — เหลือเยอะ

## ผลการ grilling

| # | คำถาม | ตัดสิน |
|---|---|---|
| 1 | "Usage credit ที่เหลือ" คือตัวไหน | **A. Balance** — ยอดเงินคงเหลือแบบหน้าเว็บ ($0) ไม่ใช่ "เหลือจากเพดานรายเดือน" (แถว Extra usage โชว์เพดานอยู่แล้ว). ถ้า API ไม่มี Balance → **ไม่ทำแถวนี้ ไม่เดาตัวเลข** |
| 2 | พิสูจน์ยังไงว่า API มี Balance | **A. ให้แอป dump คำตอบดิบ** `usage-raw.json` ลงโฟลเดอร์ log ทุก poll ที่สำเร็จ (ไฟล์เดียว เขียนทับ) แล้ว Claude อ่านไฟล์นั้น — token ไม่ออกจากแอป (ระบบกันไม่ให้ Claude อ่าน token เอง) |
| 3 | หน้าตาแถว Balance | **A. แถวเดียว โชว์เสมอแม้ $0** — `Usage credits: $0.00 left` สไตล์เดียวกับ Cloud credit (ไม่มีหลอด) แทรกระหว่าง Extra usage กับ Cloud credit |
| 4 | จัดเมนูคลิกขวา | **A. 4 กลุ่ม** — ชั้นบนเหลือ 6 รายการ (ดูโครงข้างล่าง) |

### โครงเมนูใหม่ (ข้อ 4)

```
[⬆ Update available…]   (โผล่เฉพาะตอนมี)
[🔑 Fix Claude login…]   (โผล่เฉพาะตอนมี)
Refresh now
────────
Popup shows   ▸  Limits ▸ (แถว limit + Show ETA)
                 Session context ▸ (Show, Show step progress, Max shown, Sort by)
                 Graphs ▸  Session graph ▸ (Show/Range/Now position)
                           Credit graph  ▸ (Show/Range/Now position)
Window        ▸  Always on top / Mini mode / Pin (Click-through)
                 ──── Theme ▸ / Size ▸ / Opacity ▸ / Show logo / Hotkey ▸
Tray & alerts ▸  Tray icon shows ▸ / Notify at ▸ / Session alerts ▸ (ย้ายออกจาก Session context)
App           ▸  Start with Windows / Check for updates / Open log folder / About…
────────
Exit
```
การตั้งค่าเดิมไม่หาย แค่ย้ายที่ (settings key เดิมทั้งหมด)

## Sprint table

| Step | งาน | ใครทำ | % 5h (ประมาณ) | หยุดตรงนี้ได้ = |
|---|---|---|---|---|
| 1 | แอปเขียน `usage-raw.json` ใน log folder ทุก poll สำเร็จ | coder | 2 | มีเครื่องมือดีบั๊ก ยังไม่มีฟีเจอร์ใหม่ |
| 2 | build portable + รัน → main model อ่าน `usage-raw.json` หาฟิลด์ Balance | main | 2 | รู้แล้วว่ามี/ไม่มี Balance |
| 3 | (ถ้ามี) parse Balance → แถว `Usage credits: $X left` ใต้ Cloud credit (ย้ายจากเหนือ ตามคุณสมกก 2026-10-15) + PopupShot sample | coder | 4 | แถวใหม่ทำงาน (ทิ้งได้ถ้าไม่มีฟิลด์) |
| 4 | จัดเมนูคลิกขวาเป็น 4 กลุ่มตามโครงข้างบน | coder | 4 | เมนูใหม่ใช้ได้ |
| 5 | reviewer ตรวจ diff + build portable + screenshot เมนู/ป๊อปอัปให้คุณสมกกดู | reviewer + main | 3 | พร้อมให้คุณสมกกตรวจด้วยตา |
| 6 | (รอคุณสมกกอนุมัติ) bump เวอร์ชัน + release | main | 3 | — ต้องถามก่อนเสมอ |

% ต่อ step เป็นค่าประมาณ ไม่ใช่จากมิเตอร์. step 3 ทิ้งได้ถ้า API ไม่มี Balance; step 6 ไม่ทำจนกว่าคุณสมกกสั่ง

## สถานะ
- grilling เสร็จ → คุณสมกกสั่งเริ่ม sprint
- Step 1 (`09297b4`): `%APPDATA%\ClaudeMeter\logs\usage-raw.json` ทับทุก poll
- Step 2: เจอ `spend.balance` ใน API แต่ **ค่าเป็น null** (บัญชีนี้ปิด usage credits: `extra_usage.user_disabled: true`).
  ฟิลด์ข้างเคียงที่ยังไม่ใช้: `spend.cap`, `spend.auto_reload`, `seven_day_breakdown` (สัดส่วน Claude Code/Chats/Cowork)
- Step 3 (`415386f`): แถว `credit_balance` — เดารูป `{amount_minor, exponent}` เหมือน `spend.used` หรือเป็นตัวเลขตรงๆ; null/รูปอื่น = ไม่มีแถว. `UsageWindow.IsWallet` กันออกจาก alert/ไอคอน tray/history/ETA
- Step 4: เมนู 4 กลุ่ม + README อัปเดต
- Step 5: reviewer ความเสี่ยงต่ำ; แก้ CreateDirectory แล้ว. ข้อเล็กที่ปล่อยไว้: สีตัวเลขแถว balance ไม่เปลี่ยนตามยอด, log บรรทัด "usage ok" เขียน "Usage credits 0%", แถวโผล่ในเมนู Limits ให้ซ่อนได้ (ตั้งใจ)
- ย้อนกลับ: `git revert` ทีละ commit ได้ (ทุก step แยก commit)
