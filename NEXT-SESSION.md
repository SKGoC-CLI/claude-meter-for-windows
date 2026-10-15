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
| 6 | release — **รอคุณสมกกสั่งเท่านั้น** | main | ⬜ |

③ สถานะจริง: step 1-5 เสร็จ + commit แล้ว, build ผ่าน 0 error, portable รันเวอร์ชันใหม่อยู่. ยังไม่ bump เวอร์ชัน ยังไม่ release. แถว Usage credits **ยังไม่โผล่ในเครื่องจริง** เพราะ API ส่ง `spend.balance: null` (เห็นแค่ใน `--popup-shot` ที่ใส่ $7.50 ตัวอย่าง → `docs/design/popup-usage-credits-2026-10-15.png`)

④ ถัดไป: รอคุณสมกกตรวจเมนูคลิกขวา + ภาพป๊อปอัป แล้วตัดสินข้อด้านล่าง

## รอคุณสมกกตัดสิน
- แถว Usage credits ไม่มีหลอด เลยมีที่ว่างเปล่าใต้แถว (สูงเท่าแถวอื่น) — A. ปล่อยไว้ B. ทำแถวเตี้ยลง (ต้องแตะ layout engine) C. แสดงเป็นบรรทัดเล็กใต้ Cloud credit แทน
- Release v1.12.0 เลยไหม — แนะนำรอจน `spend.balance` มีค่าจริงสักครั้ง (ลองเปิด usage credits บนเว็บ) จะได้รู้ว่ารูปข้อมูลที่เดาไว้ถูก

⑤ กับดัก
- Claude อ่าน token เองไม่ได้ (auto mode กัน "Credential Exploration") → ใช้ไฟล์ dump จากแอปเท่านั้น
- ถ้า API ไม่มี Balance → **ข้าม step 3 ห้ามเดาตัวเลข** (ห้ามใช้ limit − used แทน)
- build portable: stop ClaudeMeter ทุกตัวก่อน (mutex ทำให้ตัวใหม่ออกเงียบ) แล้วรันแบบ Hidden
- ห้าม backtick ใน Bash heredoc → commit ด้วย `git commit -F` จากไฟล์

⑥ log เต็ม: [WORK-LOG-2026-10-15-usage-credits-menu.md](WORK-LOG-2026-10-15-usage-credits-menu.md)
