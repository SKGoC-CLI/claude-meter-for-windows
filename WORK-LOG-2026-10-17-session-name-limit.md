# WORK-LOG 2026-10-17 — จำกัดความยาวชื่อ session

## ปัญหา
ชื่อ session ยาว (เช่น "Moano USB DM40 mic input fail not response · Opus · Medium") ดัน `10%` ไปทับ `103k / 1.0M`.
ต้นเหตุ: `DrawContextBlock` (src/PopupForm.cs) วาดชื่อเต็มไม่ตัด, ส่วน `PreferredWidth` ขยาย popup ตามชื่อได้แค่เพดาน 560px —
ชื่อที่ยาวกว่านั้นจึงล้นไปทับ.

## Grilling (2026-10-17)
โควตาจาก meter ตอนเริ่ม: five_hour 4%, seven_day 44%.

1. ตัดยังไง? A. ตามที่ว่างจริง (pixel) ตัดเฉพาะชื่อโปรเจกต์ + "…" / B. จำกัดจำนวนตัวอักษร / C. ตัดทั้งบรรทัด
   → **คุณสมกกเลือก A** — `· model · effort`, %, token อยู่ครบเสมอ ทุก DPI
2. ชื่อยาวควรทำให้ popup กว้างขึ้นไหม? A. ไม่ขยาย / B. ขยายถึง 560 แล้วตัด
   → **คุณสมกกเลือก A** — ความกว้างที่คิดจากชื่อ session มีเพดานราว 30 ตัวอักษร

## Sprint
| ขั้น | ใครทำ | หยุดตรงนี้ได้ = |
|---|---|---|
| 1. บันทึกการตัดสิน (ไฟล์นี้ + CONTEXT.md) | main model | การตัดสินไม่หาย |
| 2. แก้ PreferredWidth (เพดานชื่อ) + DrawContextBlock (ตัดชื่อ + …) | main model (เล็กมาก ไม่คุ้ม delegate) | โค้ดแก้แล้ว build ผ่าน |
| 3. build + rebuild portable + ภาพ --popup-shot ชื่อยาว | main model | คุณสมกกดูรูปได้ |
| 4. review + commit | reviewer → main | ปิดงาน (ยังไม่ release) |

## ผล
- `DrawContextBlock`: วัด token ชิดขวาก่อน แล้วตัดชื่อโปรเจกต์ทีละตัวอักษร + `…` จนชื่อ+% พอดีที่ว่าง (เว้น S(16) จาก token)
- `PreferredWidth`: ใช้ `Truncate(project, 30)` ตอนคิดความกว้าง → ชื่อยาวไม่ขยาย popup
- `PopupShot.cs`: ตัวอย่าง session ที่ 2 เปลี่ยนเป็นชื่อยาวปลอม `printer-driver-crash-after-wake-from-sleep-investigation` ไว้เช็คซ้ำ
- `--popup-shot` ผ่าน: แสดง `printer-driver-crash-after-wak… · Sonnet · Medium  24%   48k / 200k` ไม่ซ้อน, popup กว้างปกติ
- rebuild `portable\ClaudeMeter.exe` แล้วรันอยู่ (ยังไม่ release)
- ไม่ได้ส่ง reviewer — diff ~20 บรรทัด, ตรวจเอง (กรณีที่ว่างติดลบ: loop หยุดที่ 1 ตัวอักษร ไม่ค้าง)
- ย้อนกลับ: `git revert` commit นี้
