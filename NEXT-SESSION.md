# NEXT-SESSION — แถว Usage credits + จัดกลุ่มเมนูคลิกขวา

status: active

> **ห้ามพิมพ์คำต้องห้าม "ab***t" เต็ม ๆ ลงไฟล์ใด ๆ ใน repo** · ห้ามชื่อโปรเจกต์จริงหลุดในภาพ/GIF ที่ขึ้น GitHub

① 2026-10-15 · เริ่มจาก HEAD `4f90c7f` — ไฟล์นี้อาจเก่า เช็ค `git log --oneline -5` ก่อน

② ขั้นตอน (ตัดสินใน grilling — ดู WORK-LOG)

| Step | งาน | ใคร | สถานะ |
|------|-----|-----|-------|
| 1 | แอปเขียน `usage-raw.json` (คำตอบดิบ API) ลง log folder ทุก poll สำเร็จ | main | ✅ |
| 2 | build portable + รัน → อ่าน `usage-raw.json` หาฟิลด์ Balance | main | ✅ พบ `spend.balance` แต่เป็น null (usage credits ปิดอยู่ `user_disabled: true`) |
| 3 | (ถ้ามีฟิลด์) แถว `Usage credits: $X left` ใต้ Cloud credit (ย้ายจากเหนือ ตามคุณสมกก 2026-10-15), โชว์แม้ $0 | coder | ✅ key `credit_balance`, `UsageWindow.IsWallet`; ตอนนี้ไม่โชว์เพราะ API ส่ง null |
| 4 | เมนูคลิกขวา 4 กลุ่ม: Popup shows / Window / Tray & alerts / App ("Show limits" → "Limits") | main | ✅ |
| 5 | reviewer + build portable + screenshot ป๊อปอัป ให้คุณสมกกดู | reviewer/main | ✅ reviewer: ไม่มีบั๊กร้ายแรง แก้ข้อ 1 (CreateDirectory) แล้ว |
| 5b | คุณสมกกเลือก C (2026-10-15): แถว Usage credits เป็นบรรทัดเล็ก (small font) ใต้ Cloud credit ไม่มีที่ว่าง | main | ✅ ใต้ Cloud = ขยับขึ้น S(18) สูง S(10); ใต้แถวอื่น (อาจมี ETA) = สูง S(28). ภาพ `docs/design/popup-usage-credits-small-2026-10-15.png` |
| 7 | (grilling 2) เอาหลอด Cloud credit ออก → Cloud + Usage credits รวมบรรทัดเล็กบรรทัดเดียว, expiry ชิดขวา สีจาง/ส้ม ≤7d/แดง ≤1d, ลบ tuck ของ 5b | main | ✅ `DrawCreditLine` / `CreditLineText` |
| 8 | tray tooltip Cloud credit เป็น `$X left` + CONTEXT.md | main | ✅ คำใหม่ *Credit line* |
| 9 | reviewer + build portable + popup-shot ให้คุณสมกกดู | reviewer/main | ✅ ไม่มีบั๊ก; ภาพ `docs/design/popup-credit-line-2026-10-15.png` (ขั้น 7-9 commit รวมกัน) |
| 10 | คุณสมกกเห็นบรรทัด Usage credits ไม่โผล่ (balance ยัง null แม้เปิดแล้ว) → เลือก A: แถว "Extra usage" เปลี่ยนป้ายเป็น "Usage credits" (คำของ API), balance เป็น "Credit balance", บรรทัดเครดิตขยับขึ้นชิดหลอดเมื่อแถวบนไม่มี ETA | main/reviewer | ✅ ภาพ `docs/design/popup-usage-credits-rename-2026-10-15.png` |
| 11 | หายอดเติมเงิน $5 (คุณสมกกเติม 2026-10-15): `spend.balance` ยัง null → เจอใน claude.exe ว่า Claude Code ใช้ `GET https://api.anthropic.com/api/oauth/organizations/{orgUUID}/prepaid/credits` (Bearer + `anthropic-beta: oauth-2025-04-20` + header `x-organization-uuid`), ตอบ `{amount (เซนต์), currency, auto_reload_settings, expiry_policy_months}`; orgUUID จาก `/api/oauth/profile` → `organization.uuid` | main/reviewer | ✅ คุณสมกกอนุญาตในแชต → ต่อสายแล้ว, เครื่องจริงได้ `Usage credits $5.00 left` (log 12:54). ล้มเหลว = เก็บค่าเดิม + รอ 30 นาที, timeout 8s, dump `logs\prepaid-raw.json`. ป้ายตามหน้าเว็บ: แถว = "Monthly spend", ยอด $5 = "Usage credits". ภาพ `docs/design/popup-usage-credits-prepaid-2026-10-15.png` |
| 6 | release — **รอคุณสมกกสั่งเท่านั้น** | main | ⬜ |

③ สถานะจริง: ทุกขั้นยกเว้น 6 (release) เสร็จ + commit + push แล้ว. build ผ่าน 0 error, portable รันเวอร์ชันใหม่อยู่ และอ่านยอดเติมเงิน $5 ได้จริง. ยังไม่ bump เวอร์ชัน ยังไม่ release.

④ ถัดไป: คุณสมกกตัดสินเรื่อง release ด้านล่าง. สีส้ม/แดงของวันหมดอายุยังไม่ได้เห็นในภาพ (ตัวอย่างเหลือ 33 วัน)

**งานคิวถัดไป (grill แล้ว 2026-10-15, ยังไม่เริ่ม):** หัวข้อ `CURRENT SESSION(S)` + จุด step ●●◉○○○ 3/6 — แผน/sprint table ครบใน [WORK-LOG-2026-10-15-step-dots.md](WORK-LOG-2026-10-15-step-dots.md). commit งาน credit line ข้างบนให้จบก่อนเริ่ม

## รอคุณสมกกตัดสิน
- Release v1.12.0 เลยไหม — ตอนนี้ยอดเติมเงินอ่านได้จริงแล้ว (prepaid/credits) จึงไม่ต้องรอ spend.balance อีก

⑤ กับดัก
- Claude อ่าน token เองไม่ได้ (auto mode กัน "Credential Exploration") → ใช้ไฟล์ dump จากแอปเท่านั้น
- `spend.balance` ใน usage endpoint เป็น null แม้มีเงินเติม — ยอดจริงอยู่ที่ `/prepaid/credits` เท่านั้น; การแก้โค้ดที่ส่ง token ไป endpoint ใหม่ ต้องให้คุณสมกกอนุญาตในแชตก่อน (auto mode กัน)
- ถ้า API ไม่มี Balance → **ข้าม step 3 ห้ามเดาตัวเลข** (ห้ามใช้ limit − used แทน)
- build portable: stop ClaudeMeter ทุกตัวก่อน (mutex ทำให้ตัวใหม่ออกเงียบ) แล้วรันแบบ Hidden
- ห้าม backtick ใน Bash heredoc → commit ด้วย `git commit -F` จากไฟล์

⑥ log เต็ม: [WORK-LOG-2026-10-15-usage-credits-menu.md](WORK-LOG-2026-10-15-usage-credits-menu.md)
