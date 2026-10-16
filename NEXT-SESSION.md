# NEXT-SESSION — Limit resets (ตั๋วฟรีรีเซ็ต) ใต้ Cloud credit

status: done

> **ห้ามพิมพ์คำต้องห้าม "ab***t" เต็ม ๆ ลงไฟล์ใด ๆ ใน repo** · ภาพ/GIF ที่ขึ้น GitHub ต้องเป็น mockup ภาษาอังกฤษ ไม่มีชื่อโปรเจกต์จริง/ข้อความไทย

① 2026-10-16 · เริ่มจาก `f70c28b` — ไฟล์นี้อาจเก่า เช็ค `git log --oneline -5` ก่อน

② ขั้นตอน (ตัดสินใจครบใน grilling — ดู WORK-LOG)

| Step | งาน | ใคร | สถานะ |
|------|-----|-----|-------|
| 1 | `UsageClient`: URL `?cedar_ember=1` + parse grants → แถว `limit_resets` | main (เล็ก ทำเอง) | ✅ |
| 2 | ยืนยันกับบัญชีจริงผ่าน portable (`usage-raw.json` มี cedar_ember + spend ยังอยู่) | main | ✅ API ตอบได้, spend ยังอยู่; บัญชีตอนนี้ `grants: []`, `eligible:false` เหตุผล `surface` → บรรทัดซ่อน |
| 3 | `PopupForm`: บรรทัด `Limit resets: 2 of 3 left` ใต้ Credit line + toggle Show limits + PopupShot | main | ✅ |
| 4 | reviewer + screenshot ให้คุณสมกก | reviewer/main | ✅ ไม่มีบั๊กร้ายแรง แก้ข้อ 1 (อ่านตัวเลขแบบ double) |
| 5 | CHANGELOG (README ไม่แตะ — ยังไม่ release) | main | ✅ |

③ สถานะจริง: commit + push แล้ว; portable รุ่นใหม่ (ยังเลขเวอร์ชัน 1.12.0) รันอยู่; exe เก่าสำรองที่ `bin\portable-v1.12.0-backup\`. **ยังไม่ release**, ยังไม่เคยเห็นบรรทัดนี้กับตั๋วจริง (เห็นแค่ใน PopupShot)

④ ถัดไป: รอบัญชีได้ตั๋วจริงแล้วดูป๊อปอัพ / หรือ release v1.13.0 เมื่อคุณสมกกสั่ง

⑤ กับดัก
- token ใน `~/.claude/.credentials.json` อาจหมดอายุ (401) — **อย่า refresh เอง** (token family) ให้มิเตอร์ทำ
- อย่าใส่ `skip_spend=1` ใน call หลัก — spend/Usage credits จะหาย
- ห้าม backtick ใน Bash heredoc → ใช้ Write
- PopupShot ใช้ชื่อปลอมเท่านั้น

⑥ log เต็ม: [WORK-LOG-2026-10-16-limit-resets.md](WORK-LOG-2026-10-16-limit-resets.md)
