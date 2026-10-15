# NEXT-SESSION — v1.12.0 (credit line + step segments)

status: done

> **ห้ามพิมพ์คำต้องห้าม "ab***t" เต็ม ๆ ลงไฟล์ใด ๆ ใน repo** · ภาพ/GIF ที่ขึ้น GitHub ต้องเป็น mockup ภาษาอังกฤษ ไม่มีชื่อโปรเจกต์จริง/ข้อความไทย (กฎใหม่ 2026-10-15)

① 2026-10-15 · release commit หลัง `95bb06b` — ไฟล์นี้อาจเก่า เช็ค `git log --oneline -5` ก่อน

② ขั้นตอน (รายละเอียด: [WORK-LOG-2026-10-15-step-dots.md](WORK-LOG-2026-10-15-step-dots.md), งาน credit line: WORK-LOG-2026-10-15-usage-credits-menu.md)

| Step | งาน | ใคร | สถานะ |
|------|-----|-----|-------|
| 1 | หัวข้อ CURRENT SESSION(S) + เมนู Current session | main | ✅ |
| 2 | จุด step → แท่งสั้นไม่มีสี (grilling รอบ 2) | main | ✅ |
| 3 | TaskCreate ใช้ subject + ShortStep ตัดหัวข้อ (grilling รอบ 3) | main | ✅ |
| 4 | reviewer: ไม่มีบั๊กร้ายแรง แก้ข้อ 1 (ShortStep ว่างเมื่อขึ้นต้นด้วยวงเล็บ) | reviewer/main | ✅ |
| 5 | GIF จากคลิปคุณสมกก 16:17 ทับ "Oliver; Alinity negative query" → "api-server", ชื่อตัวอย่าง PopupShot เป็นชื่อปลอม | main | ✅ docs/demo-v1.12.0.gif 3.9 MB |
| 6 | release v1.12.0 (คุณสมกกสั่ง "ทำ release ได้เลย") | main | ✅ |

③ สถานะจริง: v1.12.0 commit + push + GitHub release (Latest) พร้อม zip 63.3 MB. portable 1.12.0.0 รันอยู่. ไฟล์กลาง docs/demo-v1.12.0-wm.mp4 ไม่ commit (เหมือนรุ่นก่อน)

④ ถัดไป: ไม่มีงานค้าง — sprint จบ

## รอคุณสมกกตัดสิน
- ไฟล์ค้างนอก git (มีมาก่อน session นี้): คลิป `docs/Recording *.mp4/.gif`, `docs/demo-*-wm.mp4`, `docs/demo 16 July 2026.mp4`, `docs/Screenshot 2026-10-15 022002.png`, `docs/GITHUB_APPEAL_4649709.md`, `docs/PROMO_DRAFTS_2026-09-03.md` + `docs/PROMO_TODO.md` ที่แก้ค้าง. /zzz **ไม่ได้ commit** เพราะคลิปดิบมีชื่อโปรเจกต์จริง/ข้อความไทย และ repo เป็น public (ขัดกฎ mockup)
  - A. ใส่ `docs/Recording*`, `docs/*-wm.mp4`, `docs/demo 16*` ลง `.gitignore`, commit แค่ PROMO_* (แนะนำ)
  - B. ย้ายคลิปดิบออกไปเก็บนอก repo
  - C. ปล่อยไว้แบบนี้

⑤ กับดัก
- Bash tool ในเครื่องนี้ล่มกลาง session ("claude-xxxx-cwd: No such file") → ใช้ PowerShell แทนได้
- ห้าม backtick ใน Bash heredoc (เกือบโดนอีกรอบวันนี้) → เขียนสคริปต์ด้วย Write แล้วรัน
- reviewer ข้อ 2 (ตัดที่ `: ` และ `, ` อาจตัดเกิน เช่น "Add A, B" → "Add A") = ตั้งใจตามที่คุณสมกกเลือก; ข้อ 3 (10 แท่งบน DPI สูงอาจชนข้อความอายุ) ยังไม่แก้

⑥ log เต็ม: [WORK-LOG-2026-10-15-step-dots.md](WORK-LOG-2026-10-15-step-dots.md)
