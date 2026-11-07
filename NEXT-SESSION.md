# NEXT-SESSION — จุดเขียวฟ้ากระพริบ = session มีงานเบื้องหลัง

status: active

> **ห้ามพิมพ์คำต้องห้าม "ab***t" เต็ม ๆ ลงไฟล์ใด ๆ ใน repo** · ภาพ/GIF ที่ขึ้น GitHub ต้องเป็น mockup ภาษาอังกฤษ ไม่มีชื่อโปรเจกต์จริง/ข้อความไทย

① 2026-11-08 · commit ล่าสุดก่อนสปรินต์ `59ed5fc` — ไฟล์นี้อาจเก่า เช็ก `git log --oneline -5` ก่อนเชื่อ

② ขั้นของสปรินต์ (tickets ใน `.scratch/background-dot/issues/`, spec `.scratch/background-dot/spec.md`)

| ขั้น | งาน | สถานะ |
|---|---|---|
| 1 | Ticket 01 Monitor tracer — `SessionState.Background`, จุดเขียวฟ้ากระพริบ 2 วิ, สีธีม, selftest | ✅ (coder ทำ 01–04 รวดเดียว, commit เดียว) |
| 2 | Ticket 02 งานเบื้องหลังครบทุกแบบ (Bash/Agent) + failed/killed/TaskStop + หมดเวลา | ✅ |
| 3 | Ticket 03 แถวไม่หายที่ 30 นาทีถ้ายังมีงาน | ✅ |
| 4 | Ticket 04 ข้อความ ⟳ บรรทัด 2 | ✅ (ยังไม่เห็นภาพ — glyph ⟳ อาจ fallback font) |
| 5 | reviewer ตรวจ diff ทั้งหมด + แก้ | ⬜ |
| 6 | รันแอปจริง + screenshot มืด/สว่าง ให้คุณสมกกดู | ⬜ |

③ สถานะจริง: โค้ด tickets 01–04 commit แล้ว · `dotnet build` 0 error · `--bg-selftest` + `--goal-selftest` ผ่าน · ยังไม่ผ่าน reviewer · ยังไม่เห็นภาพจริง · ยังไม่ release (release = ถามคุณสมกกก่อน)
· resume tasks `resume-claude-meter-bg-1/2/3` ตั้งไว้ที่ 03:35 / 08:50 / 14:05 (8 พ.ย.)

④ ถัดไป: ขั้น 5 — reviewer ตรวจ `git diff 3a210a5..HEAD -- src` · ทดสอบซ้ำ: `dotnet build` แล้ว `bin\Debug\net8.0-windows\ClaudeMeter.exe --bg-selftest`

⑤ กับดัก
- `App Claude Meter` = clone ที่สองของ repo เดียวกัน เป็นที่อยู่ของ portable ที่รันจริง (ไม่ใช่ repo นี้)
- meter มี single-instance mutex — ต้องหยุดตัวที่รันอยู่ก่อนรัน build ทดสอบ แล้วเปิด portable คืนหลังทดสอบ
- ห้ามเปิดหน้าต่าง console ทับเกม: รันแอปด้วย `Start-Process -WindowStyle Hidden`
- task-notification บางอัน (Agent) ใช้ agentId เป็น task-id; Monitor/Bash ใช้ taskId / backgroundTaskId
- build portable ต้องมี `/p:EnableCompressionInSingleFile=true`

⑥ log เต็ม: [WORK-LOG-2026-11-08-background-dot.md](WORK-LOG-2026-11-08-background-dot.md)
