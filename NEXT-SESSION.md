# NEXT-SESSION — Limit resets (ตั๋วฟรีรีเซ็ต) ใต้ Cloud credit

status: active

> **ห้ามพิมพ์คำต้องห้าม "ab***t" เต็ม ๆ ลงไฟล์ใด ๆ ใน repo** · ภาพ/GIF ที่ขึ้น GitHub ต้องเป็น mockup ภาษาอังกฤษ ไม่มีชื่อโปรเจกต์จริง/ข้อความไทย

① 2026-10-16 · เริ่มจาก `f70c28b` — ไฟล์นี้อาจเก่า เช็ค `git log --oneline -5` ก่อน

② ขั้นตอน (ตัดสินใจครบใน grilling — ดู WORK-LOG)

| Step | งาน | ใคร | สถานะ |
|------|-----|-----|-------|
| 1 | `UsageClient`: URL `?cedar_ember=1` + parse grants → แถว `limit_resets` | coder | ⬜ |
| 2 | ยืนยันกับบัญชีจริงผ่าน portable (`usage-raw.json` มี cedar_ember + spend ยังอยู่) | main | ⬜ |
| 3 | `PopupForm`: บรรทัด `Limit resets: 2 of 3 left` ใต้ Credit line + toggle Show limits + PopupShot | main | ⬜ |
| 4 | reviewer + screenshot ให้คุณสมกก | reviewer/main | ⬜ |
| 5 | CHANGELOG / README (ทิ้งได้) | doc-writer | ⬜ |

③ สถานะจริง: grilling เสร็จ, CONTEXT.md + WORK-LOG commit แล้ว; ยังไม่มีโค้ด

④ ถัดไป: step 1

⑤ กับดัก
- token ใน `~/.claude/.credentials.json` อาจหมดอายุ (401) — **อย่า refresh เอง** (token family) ให้มิเตอร์ทำ
- อย่าใส่ `skip_spend=1` ใน call หลัก — spend/Usage credits จะหาย
- ห้าม backtick ใน Bash heredoc → ใช้ Write
- PopupShot ใช้ชื่อปลอมเท่านั้น

⑥ log เต็ม: [WORK-LOG-2026-10-16-limit-resets.md](WORK-LOG-2026-10-16-limit-resets.md)
