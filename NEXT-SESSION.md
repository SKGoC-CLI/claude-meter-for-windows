# NEXT-SESSION — หัวข้อ CURRENT SESSION + จุด step ●●◉○○○

status: active

> **ห้ามพิมพ์คำต้องห้าม "ab***t" เต็ม ๆ ลงไฟล์ใด ๆ ใน repo** · ห้ามชื่อโปรเจกต์จริงหลุดในภาพ/GIF ที่ขึ้น GitHub

① 2026-10-15 · เริ่มจาก HEAD `19371c8` — ไฟล์นี้อาจเก่า เช็ค `git log --oneline -5` ก่อน

② ขั้นตอน (แผนเต็ม + สิ่งที่ตัดสินแล้ว: [WORK-LOG-2026-10-15-step-dots.md](WORK-LOG-2026-10-15-step-dots.md))

| Step | งาน | ใคร | สถานะ |
|------|-----|-----|-------|
| 1 | หัวข้อ `CURRENT SESSION` / `CURRENT SESSIONS (n)` + เมนู tray `Current session` + CONTEXT.md | main | ✅ |
| 2 | จุด step + `3/6` จาง + หัวข้อ, >10 step = แบบเดิม, ส้มกระพริบเฉพาะ Working | main | ✅ `DrawContextBlock` |
| 3 | TaskCreate ใช้ `subject` ก่อน `activeForm` | main | ✅ self-check ครอบแล้ว |
| 4 | review + build portable + `--popup-shot` ให้คุณสมกกดู | main | ✅ self-review (diff ~45 บรรทัด), `--goal-selftest` OK, portable ใหม่รันอยู่, ภาพ `docs/design/popup-step-dots-2026-10-15.png` |
| 5 | ปรับตามภาพ: คุณสมกกว่าจุดซ้ำกับจุดสถานะ → grilling รอบ 2 เลือกแท่งสั้นต่อกัน ▬▬▭ ไม่มีสี (เสร็จเทาอ่อน / กำลังทำขาว / ยังไม่ทำเทาเข้ม) | main | ✅ ภาพ `docs/design/popup-step-segments-2026-10-15.png`, รอคุณสมกกดูของจริง |
| 6 | release v1.12.0 (รวมงาน credit line ที่เสร็จแล้ว) — **รอคุณสมกกสั่งเท่านั้น** | main | ⬜ |

③ สถานะจริง: step 1-4 เสร็จ + commit + push. build 0 error, portable เวอร์ชันใหม่รันอยู่. ยังไม่ bump เวอร์ชัน

④ ถัดไป: คุณสมกกดูแท่งในป๊อปอัปจริง → ผ่านแล้วค่อย release (step 6)

## รอคุณสมกกตัดสิน
- (ไม่มีตอนนี้ — release รอสั่งหลังงานจุด step)

⑤ กับดัก
- ตัวอย่างใน `PopupShot.cs` ห้ามใช้ชื่อโปรเจกต์/ลูกค้าจริง
- ส้มกระพริบต้องไม่ทำให้ `_anim` วิ่งตลอดตอน popup ซ่อน
- build portable: stop ClaudeMeter ทุกตัวก่อน (mutex ทำให้ตัวใหม่ออกเงียบ) แล้วรันแบบ Hidden
- ห้าม backtick ใน Bash heredoc → commit ด้วย `git commit -F` จากไฟล์

⑥ log เต็ม: [WORK-LOG-2026-10-15-step-dots.md](WORK-LOG-2026-10-15-step-dots.md)
