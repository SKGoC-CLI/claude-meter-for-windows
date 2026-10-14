# WORK-LOG 2026-10-14 — ล้างคำต้องห้าม "ab***t" ออกจาก GitHub

> ห้ามพิมพ์คำเต็มในไฟล์นี้ — ใช้ "ab***t"

## โจทย์
คุณสมกกสั่ง: ใน repo SKGoC-CLI/claude-meter-for-windows ห้ามมีคำนี้หลุด เอาออกให้หมด

## ผลสำรวจ (main)
- ต้นตอเดียว: ชื่อ session ตัวอย่าง "ab***t-archive" ใน `src/PopupShot.cs` (ใส่ตั้งแต่ commit `662cb88` 10 ส.ค.)
- แพร่ไป:
  1. ภาพ 7 ไฟล์ที่ render ด้วย `--popup-shot`: `docs/popup-v1.10.0.png`, `docs/light-dark-v1.9.1.png`, `docs/design/light-theme-variants/A–E*.png`
     (`compare-rows.png`, `compare-graph.png`, `asking-solid-preview.png` ตรวจแล้วไม่มี)
  2. exe ใน zip ของ release v1.9.1 และ v1.10.0 (string ถูก compile เข้าไป)
  3. ประวัติ git 27 commit ตั้งแต่ `662cb88`
- ไม่มีใน commit message / release body
- repo: 0 fork, 5 ดาว, author มีแต่ SKGoC

## Grilling
**Q1 ขอบเขต** — A. แก้ไฟล์ปัจจุบัน + exe · B. A + เขียนประวัติ git ใหม่ (force push) · C. A + เบลอ GIF
→ คุณสมกกเลือก **B**
→ ข้อ 4 (ชื่อโปรเจกต์ "Dental Freelance..." ใน hero GIF) ยังไม่ได้ตอบ = ไม่แตะ

## Sprint
| Step | งาน | ใคร | หยุดได้ไหม |
|------|-----|-----|-----------|
| 1 | mirror backup | main | ✅ |
| 2 | แก้ PopupShot.cs + ภาพ 7 ไฟล์ | main | ✅ หยุดตรงนี้ได้ = ยังไม่แตะ GitHub |
| 3 | filter-repo | main | ⚠️ ทำ 3–4 ต่อเนื่อง |
| 4 | force push + ย้าย tag | main | — |
| 5 | rebuild zip 2 release | main | ✅ หยุดตรงนี้ได้ = GitHub สะอาด |
| 6 | ตรวจภายนอก + memory | main | ✅ |

โควตาตอนเริ่ม (จากมิเตอร์): 5h 33% · 7d 3% — ประเมินทั้งงาน ~10–15% ของ 5h (เดา)

## บันทึกการทำ
(เติมระหว่างทำ)
