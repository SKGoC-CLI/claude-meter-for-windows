# Code review 2026-10-14 — dc30df2..HEAD (Working blink, Asking, Session alerts, effort)

ผู้ตรวจ: reviewer subagent (Sonnet) + main ตรวจซ้ำข้อ 1–2 · ไม่ได้แก้ไฟล์

**สรุป: ไม่มีบั๊กร้ายแรง** — ไม่มี GDI/HICON leak (Render clone + DestroyIcon ใน finally, SetIcon dispose ตัวเก่า),
settings เก่าได้ค่า default ครบ, `WorkingPulse` ไม่หารศูนย์, regex effort ไม่จับข้อความใน JSON ที่ escape แล้ว

| # | ระดับ | ที่ | ปัญหา | แก้ |
|---|-------|-----|-------|-----|
| 1 | กลาง | TrayAppContext.cs ~466 → `RefreshSessionContext` | ทุก 2 วิ สแกน `~/.claude/projects` ทั้งหมดบน UI thread (วัดจริง 1,045 ไฟล์ ~100 ms ผ่าน PowerShell) + parse tail 12 ไฟล์ | ย้ายไป `Task.Run` + กันซ้อน หรือ cache รายชื่อไฟล์ |
| 2 | กลาง | PopupForm.cs `Sessions` setter | set ทุก 2 วิแม้ข้อมูลเหมือนเดิม → RecomputeLayout + Reposition + Invalidate ทั้งหน้า ตอน popup เปิด (อาจกวนตอนลาก/hover) | ข้ามถ้าข้อมูลเท่าเดิม |
| 3 | กลาง-ต่ำ | ContextMonitor.cs:111 | Asking ไม่มีอายุ → คำถามที่ทิ้งไว้ tray ส้มกระพริบได้ถึง 30 นาที | ลดเป็น Waiting หลัง ~10 นาที (ต้องให้คุณสมกกตัดสิน) |
| 4 | ต่ำ | PopupForm.cs OnPaint | เข้า mini mode แล้ว `_workingDotRects` ค้าง → repaint mini bar 30 fps ได้ถึง 2 วิ | Clear ก่อน return mini mode |
| 5 | ต่ำ | PopupForm.cs `_anim` | 30 fps ตลอดตอน popup เปิดแม้ไม่มีจุดกระพริบ | หยุด timer เมื่อไม่มี rect |
| 6 | ต่ำ | TrayAppContext.cs blink tick | exception จาก GDI ใน tick ไม่มี try/catch | ห่อ try/catch |
| 7 | ต่ำ | ShowContext off | ปิด Session context = alert tray ปิดตามเงียบ ๆ | จดไว้ / แยก scan |
| 8 | ต่ำ | AlertSpeed แก้มือผิดค่า | ไม่มีเมนูไหนติ๊ก | normalise ตอนโหลด |
| 9 | ต่ำ | shutdown | ไม่ Stop `_blinkTimer` ก่อน Dispose (จริง ๆ ปลอดภัยเพราะ UI thread) | — |
| 10 | info | IconRenderer.cs:15 | comment บอก tray 1.2 วิ จริงคือ 2.0 วิ × AlertMul | แก้ comment |

ควรแก้ก่อน release: ข้อ 1, 2 (+ 4, 5, 6, 10 เล็กมาก ทำรวมได้)
