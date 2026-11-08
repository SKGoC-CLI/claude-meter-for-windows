# WORK-LOG 2026-11-08 — เลขเวอร์ชันที่หัว popup

> ที่มา: `/ggg` — คุณสมกกอยากรู้ว่าตอนนี้รันเวอร์ชันอะไร โดยไม่ต้องเปิด About

## โควตา
- 5h ~59% (get_usage ล่าสุด, reset 03:20) · 7d 46% · extra usage 89% ($25.49/$28.40) — งานนี้เล็ก ประมาณ 3–5% (เดา)

## ของเดิม
- หัว popup วาดใน `PopupForm` (โลโก้ + "Claude Usage Meter" ด้วย `_headerFont`) เฉพาะเมื่อ Show logo เปิด; แถบหัวสูง `HeaderHeight` เสมอ
- เวอร์ชันปัจจุบัน: `Updater.Current`; ผลเช็ก GitHub: `ReleaseInfo` (TrayAppContext ~758 เทียบ `release.Version <= Updater.Current`) → balloon + เมนู + หน้าต่าง Update now / What's new / Later

## ข้อตัดสิน
1. **A — ต่อท้ายชื่อ** ตัวเล็กสีจาง: `Claude Usage Meter  v1.14.0`; ถ้าปิดโลโก้ ให้เลขยังอยู่ในแถบหัว (ชิดซ้าย)
2. **A — บอกเมื่อมีอัปเดต + กดได้**: กลายเป็น `v1.14.0 → v1.14.1` สีเหลือง (Theme.Warning) คลิกแล้วเปิดหน้าต่างอัปเดตตัวเดิม (เคอร์เซอร์มือเมื่อชี้)

## Sprint table
| ขั้น | งาน | ใครทำ | หยุดตรงนี้ได้ = |
|---|---|---|---|
| 1 | PopupForm: วาดเลขเวอร์ชันต่อชื่อ (มี/ไม่มีโลโก้) + property รับ release ใหม่ + hit-test คลิก → event | coder | ได้ — build ผ่าน |
| 2 | TrayAppContext: ส่ง release ใหม่ให้ popup + ผูกคลิกกับหน้าต่างอัปเดตเดิม; PopupShot เพิ่มภาพแบบมีอัปเดต | coder | ได้ |
| 3 | reviewer + screenshot มืด/สว่างให้คุณสมกกดู | reviewer / main model | ได้ — รอรอบแก้ |
| 4 | commit (+ release 1.14.1 ถ้าสั่ง) | main model | จบ |

## ผล (2026-11-08)
- โค้ด commit 2619c86 (รีวิว diff โดย main model เพราะ 5h 81%), bump d21a6a7, tag v1.14.1
- https://github.com/SKGoC-CLI/claude-meter-for-windows/releases/tag/v1.14.1 - zip sha256 1e87018b...3efc ตรง digest, exe 1.14.1.0 selftest ผ่าน
- ยังไม่ได้ลองคลิกปุ่มเหลืองจริง (จะเห็นได้ตอนมี release ถัดไป) และยังไม่ได้ถ่ายแบบปิดโลโก้

