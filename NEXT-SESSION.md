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
| 3–7 | Goal progress (ดู WORK-LOG) | — | ⬜ ยังไม่สั่ง |

③ สถานะจริง: step 1–2 build Release 0 error, commit แล้ว. portable สลับเป็น build ใหม่และรันอยู่
(exe เก่าอยู่ `bin\portable-pre-blink-backup\`). ยังไม่ release, **ยังไม่ผ่าน reviewer subagent**
(ข้ามเพราะโควตา 7 วัน 95% — main ตรวจเองแล้ว). ภาพไอคอนเรนเดอร์ด้วย reflection ดูแล้วโอเค
แต่ **คุณสมกกยังไม่ได้ดูของจริงบน taskbar**.
`docs/PROMO_TODO.md` (modified) + ไฟล์ docs/ untracked เป็นของเก่า **ไม่เกี่ยว อย่า commit รวม**.

การทำงาน: `TrayAppContext._blinkTimer` 1 วิ — ทุก 5 วิ `RefreshSessionContext()` (อ่าน tail transcript),
สลับ `_workingEdgeShown` ทุกวินาทีถ้ามี session Working → `RenderTrayIcon()` วาดเส้นเขียวซ้าย
(`IconRenderer.DrawWorkingEdge`, สี `#6bcb77`). ปิด SESSION CONTEXT = ไม่มี session = ไม่กระพริบ.

④ ถัดไป: รอคุณสมกกดู taskbar รอบ 5 (popup ผ่านแล้ว) → ปรับ/ผ่าน. ค่าอยู่ `TrayAppContext.cs` บรรทัด ~442: `WorkingPulse(2000)` = คาบ, `* 0.55` = ความลึกการจาง. แล้วค่อย step 3 (หลังโควตา 7 วันรีเซ็ต) + ส่ง reviewer ตรวจ dc30df2..HEAD

⑤ กับดัก
- ห้ามใส่ backtick ใน bash heredoc — เขียน md ด้วย Write/Edit เท่านั้น
- tray วาดใหม่ทุก 1 วิ → ต้อง Dispose/DestroyIcon ของเก่า (กัน GDI leak)

⑥ log เต็ม: [WORK-LOG-2026-10-14-working-blink-goal-progress.md](WORK-LOG-2026-10-14-working-blink-goal-progress.md)
