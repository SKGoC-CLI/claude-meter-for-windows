# NEXT-SESSION — แถว Usage credits + จัดกลุ่มเมนูคลิกขวา

status: active

> **ห้ามพิมพ์คำต้องห้าม "ab***t" เต็ม ๆ ลงไฟล์ใด ๆ ใน repo** · ห้ามชื่อโปรเจกต์จริงหลุดในภาพ/GIF ที่ขึ้น GitHub

① 2026-10-15 · เริ่มจาก HEAD `4f90c7f` — ไฟล์นี้อาจเก่า เช็ค `git log --oneline -5` ก่อน

② ขั้นตอน (ตัดสินใน grilling — ดู WORK-LOG)

| Step | งาน | ใคร | สถานะ |
|------|-----|-----|-------|
| 1 | แอปเขียน `usage-raw.json` (คำตอบดิบ API) ลง log folder ทุก poll สำเร็จ | main | ✅ |
| 2 | build portable + รัน → อ่าน `usage-raw.json` หาฟิลด์ Balance | main | ✅ พบ `spend.balance` แต่เป็น null (usage credits ปิดอยู่ `user_disabled: true`) |
| 3 | (ถ้ามีฟิลด์) แถว `Usage credits: $X left` เหนือ Cloud credit, โชว์แม้ $0 | coder | ✅ key `credit_balance`, `UsageWindow.IsWallet`; ตอนนี้ไม่โชว์เพราะ API ส่ง null |
| 4 | เมนูคลิกขวา 4 กลุ่ม: Popup shows / Window / Tray & alerts / App | coder | ⬜ |
| 5 | reviewer + build portable + screenshot เมนู/ป๊อปอัป ให้คุณสมกกดู | reviewer/main | ⬜ |
| 6 | release — **รอคุณสมกกสั่งเท่านั้น** | main | ⬜ |

③ สถานะจริง: step 1-2 เสร็จ, portable รัน build ใหม่แล้ว (`%APPDATA%\ClaudeMeter\logs\usage-raw.json` มีจริง). ยังไม่ release

④ ถัดไป: Step 4 — จัดเมนูคลิกขวาใน `src/TrayAppContext.cs` (~บรรทัด 130-410) ตามโครงใน WORK-LOG

⑤ กับดัก
- Claude อ่าน token เองไม่ได้ (auto mode กัน "Credential Exploration") → ใช้ไฟล์ dump จากแอปเท่านั้น
- ถ้า API ไม่มี Balance → **ข้าม step 3 ห้ามเดาตัวเลข** (ห้ามใช้ limit − used แทน)
- build portable: stop ClaudeMeter ทุกตัวก่อน (mutex ทำให้ตัวใหม่ออกเงียบ) แล้วรันแบบ Hidden
- ห้าม backtick ใน Bash heredoc → commit ด้วย `git commit -F` จากไฟล์

⑥ log เต็ม: [WORK-LOG-2026-10-15-usage-credits-menu.md](WORK-LOG-2026-10-15-usage-credits-menu.md)
