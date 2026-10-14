# NEXT-SESSION — Working blink + Goal progress sprint

status: active

① 2026-10-14 · commit ล่าสุดตอนเริ่ม: `ebb8a25` — **ไฟล์นี้อาจเก่า เช็ค `git log --oneline -5` ก่อน**

② ขั้นตอน (คุณสมกกสั่งทำแค่ step 1–2 รอบนี้ เพราะโควตา 7 วัน 95%)

| Step | งาน | ใคร | สถานะ |
|------|-----|-----|-------|
| 0 | grilling + CONTEXT.md + WORK-LOG | main | ✅ |
| 1 | popup: จุด Working กระพริบ | main | ✅ |
| 2 | tray: เส้นเขียวขอบกระพริบเมื่อมี session Working | main | ✅ |
| 2a | รอบ 2: popup breathing glow 1.2 วิ + halo | main | ✅ |
| 2b | รอบ 2: tray กรอบเขียว fade 10 fps (แทนเส้นซ้าย) | main | ✅ (commit รวมกับ 2a — 2a ใช้ `IconRenderer.WorkingPulse`) |
| 2c | รอบ 3: taskbar ตัวเลข cross-fade ↔ เขียว (เอากรอบออก) + popup glow แผ่ออก 1.6 วิ | main | ✅ |
| 2d | รอบ 4: popup fade 1.4 วิ ไม่มี glow + taskbar ตัวเลข fade หาย/กลับ 1.2 วิ | main | ✅ |
| 2e | รอบ 5: popup ผ่าน · taskbar จางเหลือ ~45% (ไม่หายหมด) รอบละ 2 วิ | main | ✅ portable สลับแล้ว — รอคุณสมกกดูจริง |
| 6a | Asking state (คำถามค้าง) แยกจาก Waiting + popup จุดส้ม fade | coder | ✅ |
| 6b | tray พื้นหลังส้ม fade ตอน Asking (ชนะ Working) | coder | ✅ |
| 6c | เมนู Session alerts: ความเร็ว ช้า/ปกติ/เร็ว + toggle tray Working + toggle tray Asking | coder | ✅ |
| 6d | build + สลับ portable + commit | main | ✅ คุณสมกกดูจริงผ่าน — tray ส้ม fade 45% (ลอง 60/35/ทึบมาแล้ว) |
| 6f | SESSION CONTEXT แสดง effort ข้างชื่อ model ("Opus · High") — อ่าน `"effort":"..."` จาก tail transcript (`ContextMonitor.Parse`) | main | ✅ portable สลับแล้ว (exe เก่า `bin\portable-pre-effort-backup\`) — รอคุณสมกกดูจริง |
| 6e | reviewer ตรวจ dc30df2..HEAD (รวม 6f) | reviewer | ✅ ไม่มีบั๊กร้ายแรง — ดู [CODE-REVIEW-2026-10-14.md](CODE-REVIEW-2026-10-14.md) |
| 6g | แก้ข้อ review 1,2,4,5,6,10 (scan session ย้ายไป `Task.Run`, popup ไม่ Reposition ถ้าขนาดเท่าเดิม, `_anim` หยุดเองเมื่อไม่มีจุด, try/catch ใน blink tick) | coder + main ตรวจ diff | ✅ build 0 error, portable สลับแล้ว — รอคุณสมกกดูว่า popup/tray ยังกระพริบปกติ |
| 6h | ข้อ review 3: Asking ค้างเกิน 10 นาทีให้หยุดกระพริบส้มไหม | — | ⬜ คุณสมกกเลือกไม่แก้ (14 ต.ค.) |
| 6i | **RELEASE v1.10.0** (tag + zip 63.2 MB + hero GIF `docs/demo-v1.10.0.gif` จาก Recording 11:04 ช่วง 15–45 วิ) | main | ✅ https://github.com/SKGoC-CLI/claude-meter-for-windows/releases/tag/v1.10.0 — คนไม่ login เห็น (200), releases/latest = v1.10.0 |
| 3–7 | Goal progress (ดู WORK-LOG) | — | ⬜ ยังไม่สั่ง |

③ สถานะจริง: step 1–2 build Release 0 error, commit แล้ว. portable สลับเป็น build ใหม่และรันอยู่
(exe เก่าอยู่ `bin\portable-pre-blink-backup\`). ยังไม่ release, **ยังไม่ผ่าน reviewer subagent**
(ข้ามเพราะโควตา 7 วัน 95% — main ตรวจเองแล้ว). ภาพไอคอนเรนเดอร์ด้วย reflection ดูแล้วโอเค
แต่ **คุณสมกกยังไม่ได้ดูของจริงบน taskbar**.
`docs/PROMO_TODO.md` (modified) + ไฟล์ docs/ untracked เป็นของเก่า **ไม่เกี่ยว อย่า commit รวม**.

การทำงาน: `TrayAppContext._blinkTimer` 1 วิ — ทุก 5 วิ `RefreshSessionContext()` (อ่าน tail transcript),
สลับ `_workingEdgeShown` ทุกวินาทีถ้ามี session Working → `RenderTrayIcon()` วาดเส้นเขียวซ้าย
(`IconRenderer.DrawWorkingEdge`, สี `#6bcb77`). ปิด SESSION CONTEXT = ไม่มี session = ไม่กระพริบ.

④ ถัดไป: รอบ 6 ผ่านแล้ว. เหลือ 6e reviewer ตรวจ dc30df2..HEAD (หลัง 7d รีเซ็ต) แล้วค่อยตัดสินว่าจะ release หรือไป step 3 Goal progress.
รอบ 5 (taskbar จาง 45%) คุณสมกกยังไม่ได้ยืนยันว่าผ่าน. ค่าอยู่ `TrayAppContext.cs` บรรทัด ~442: `WorkingPulse(2000)` = คาบ, `* 0.55` = ความลึกการจาง. แล้วค่อย step 3 (หลังโควตา 7 วันรีเซ็ต) + ส่ง reviewer ตรวจ dc30df2..HEAD

⑤ กับดัก
- ห้ามใส่ backtick ใน bash heredoc — เขียน md ด้วย Write/Edit เท่านั้น
- tray วาดใหม่ทุก 1 วิ → ต้อง Dispose/DestroyIcon ของเก่า (กัน GDI leak)

⑥ log เต็ม: [WORK-LOG-2026-10-14-working-blink-goal-progress.md](WORK-LOG-2026-10-14-working-blink-goal-progress.md)
