# WORK-LOG 2026-10-16 — Limit resets (ตั๋วฟรีรีเซ็ต) ใต้ Cloud credit (grilling)

โจทย์คุณสมกก: บัญชีที่มีตั๋วฟรีรีเซ็ต ให้โชว์ในป๊อปอัพใต้ Cloud credit ว่ามีกี่ใบ หมดอายุเมื่อไหร่

## ข้อเท็จจริงที่ค้นเจอ (ไม่ต้องถาม)
- Claude Code 2.1.299 เรียกตั๋วนี้ว่า **"limit reset"** (codename `cedar_ember`)
- ข้อมูลมาจาก `GET /api/oauth/usage?cedar_ember=1&skip_spend=1` → `cedar_ember`:
  `{eligible, grants:[{id, label, resets_total, resets_left, starts_at, ends_at, clears, paused, usable_now, use_requires_limit, ...}], next_grant_id, weekly_resets_at, cooldown_until}`
- endpoint เดิมของมิเตอร์ (ไม่มี param) ส่ง `"cedar_ember": null` เสมอ — ดูใน `%APPDATA%\ClaudeMeter\logs\usage-raw.json`
- **ยังไม่ได้เห็นข้อมูลจริงของบัญชีคุณสมกก**: ลองเรียกเองได้ 401 (token ใน `~/.claude/.credentials.json` หมดอายุ) — ไม่ refresh เองเพราะเสี่ยง token family (ดู memory oauth-refresh). ยืนยันตอนลงมือผ่านมิเตอร์ (dump `usage-raw.json`)
- `skip_spend=1` อาจตัดบล็อก spend ออก → ห้ามใส่ใน call หลักของมิเตอร์ ใส่แค่ `cedar_ember=1`

## ผลการตัดสินใจ
| # | คำถาม | คำตอบ |
|---|---|---|
| 1 | หน้าตา | **A.** บรรทัดเล็กใต้ Credit line ซ้าย `Limit resets: 2 of 3 left` ขวา `expires in 12d (28 Oct)` (สไตล์เดียวกับ Credit line ไม่มีหลอด) |
| 2 | ไม่มีตั๋ว / ใช้หมด / หมดอายุ | **A.** ซ่อนทั้งหมด — โชว์เฉพาะตอนมีตั๋วเหลือ >0 และยังไม่หมดอายุ |
| 3 | หลายชุด | **A.** รวมยอดบรรทัดเดียว + วันหมดอายุชุดใกล้สุด (`next expires ...` เฉพาะตอนมีหลายชุด) |
| 4 | ที่อื่น (tray/alert) | **A.** ป๊อปอัพอย่างเดียว + toggle `Limit resets` ใน Show limits; ไม่มี alert / tooltip / กราฟ |

ค่าเริ่มต้นที่ผมเลือกเอง (ไม่ได้ถาม): ชุดที่ `ends_at` เป็น null = ไม่มีฝั่งขวา; `usable_now`/`paused` ไม่มีผลกับการนับ (นับ `resets_left` ตรงๆ); สีวันหมดอายุใช้กติกาเดียวกับ Credit line (≤7 วันส้ม, ≤1 วันแดง)

## โควตาตอน grill (จาก history.json)
5h = 0%, 7d = 43% → ทำได้สบาย ตัวเลขต่อ step เป็นการประมาณ

## Sprint table
| # | step | ใคร | ~5h% | หยุดตรงนี้ได้ = |
|---|---|---|---|---|
| 1 | `UsageClient`: URL เติม `?cedar_ember=1`; parse `cedar_ember.grants` → แถว `limit_resets` (wallet-style, ซ่อนถ้าเหลือ 0) | coder | 4 | ได้ข้อมูล ยังไม่โชว์ |
| 2 | ยืนยันกับบัญชีจริง: รัน portable ใหม่ เปิด `usage-raw.json` ดูว่ามีบล็อก `cedar_ember` และ spend ยังอยู่ | main | 2 | รู้ว่า API ใช้ได้ (ถ้า spend หาย → แยก call) |
| 3 | `PopupForm`: วาดบรรทัด Limit resets ใต้ Credit line + toggle ใน Show limits + PopupShot sample (ชื่อปลอม) | main | 6 | ฟีเจอร์ครบ |
| 4 | reviewer ตรวจ diff + screenshot ให้คุณสมกกดู | reviewer + main | 3 | พร้อม commit |
| 5 | (ทิ้งได้) CHANGELOG / README | doc-writer | 1 | — |

## สถานะ
- grilling เสร็จ, CONTEXT.md เพิ่มคำว่า **Limit reset** แล้ว
- ยังไม่เขียนโค้ด — รอคุณสมกกยืนยันว่าเข้าใจตรงกัน
