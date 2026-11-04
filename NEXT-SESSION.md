# NEXT-SESSION — Popup ลากได้เมื่อปิด Always on top → v1.13.2

status: done

> **ห้ามพิมพ์คำต้องห้าม "ab***t" เต็ม ๆ ลงไฟล์ใด ๆ ใน repo** · ภาพ/GIF ที่ขึ้น GitHub ต้องเป็น mockup ภาษาอังกฤษ ไม่มีชื่อโปรเจกต์จริง/ข้อความไทย

① 2026-11-04 · commit ล่าสุดตอนเขียน `0cb36a3` (+ commit zzz นี้) · tag `v1.13.2` — ไฟล์นี้อาจเก่า เช็ก `git log --oneline -5` ก่อนเชื่อ

② งานใน session นี้

| ขั้น | งาน | สถานะ |
|---|---|---|
| 1 | ปิด Always on top → popup เป็นหน้าต่างปกติ: ลากได้, จำตำแหน่ง, ไม่ซ่อนเมื่อหลุดโฟกัส, ไม่ลอยทับ (`src/PopupForm.cs`, `src/TrayAppContext.cs`) | ✅ `3c7a9d0` |
| 2 | tray/Ctrl+U ตอนโดนบัง = ดึงขึ้นหน้า, กดซ้ำ = ซ่อน · ปิด Always on top ไม่ปิดหน้าต่าง | ✅ |
| 3 | reviewer: แก้คลิกเฉย ๆ ไม่บันทึกตำแหน่ง + clamp ตำแหน่งที่จำไว้ให้อยู่ในจอ | ✅ |
| 4 | bump 1.13.2 + CHANGELOG + `docs/RELEASE_NOTES_v1.13.2.md` + release | ✅ `4771aae` https://github.com/SKGoC-CLI/claude-meter-for-windows/releases/tag/v1.13.2 |
| 5 | อัปเดต portable จริงด้วย self-update 1.13.1 → 1.13.2 | ✅ log 23:17:58 |

③ สถานะจริง: ทดสอบในแอปจริงผ่าน (ลาก 300×200px, หลุดโฟกัสยังอยู่, ไม่มี TOPMOST, hotkey ดึงขึ้น/ซ่อน) · คุณสมกกดูภาพแล้วบอก "ดีละ" · digest GitHub = sha256 zip `9f8ee661…`, หน้า/zip 200 · portable (`C:\Users\User\Projects\App Claude Meter\portable`) = 1.13.2 รันอยู่ · exe 1.13.1 สำรองที่ `App Claude Meter\bin\portable-v1.13.1-backup\` · ทุก commit ผ่าน reviewer แล้ว · push ครบ

④ ถัดไป: ไม่มีงานค้าง · ไอเดียรอบหน้า (ถ้าอยากทำ): เมนู "Reset position" ให้หน้าต่างกลับมุมขวาล่าง · กดไอคอนถาดค้าง >0.4 วิ ตอนหน้าต่างอยู่หน้า → ครั้งแรกไม่ซ่อน (จุดเล็ก รู้แล้ว)

⑤ กับดัก
- `App Claude Meter` = clone ที่สองของ repo เดียวกัน เป็นที่อยู่ของ portable ที่รันจริง (ไม่ใช่ repo นี้)
- build portable ต้องมี `/p:EnableCompressionInSingleFile=true` — ลืม = exe 162MB แทน 72MB: `dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:EnableCompressionInSingleFile=true`
- ไอคอน meter อยู่ในถาดซ่อน (^) — `Shell_NotifyIconGetRect` คืนตำแหน่งปุ่ม ^ คลิกแล้วไม่โดนไอคอน → ทดสอบด้วย hotkey Ctrl+U แทน
- ทดสอบที่ต้องแก้ `settings.json` → สำรองก่อน แล้วคืนหลังทดสอบ (หยุด meter ก่อน เพราะมี single-instance mutex)
- `--update-selftest` บน portable จริง = อัปเดตจริงทันที ไม่มีหน้าต่างถาม (ใช้ได้เมื่อตั้งใจอัปเดต — สำรอง exe ก่อน)

⑥ log เต็มของงานก่อนหน้า: [WORK-LOG-2026-10-19-self-update.md](WORK-LOG-2026-10-19-self-update.md) · งานนี้บันทึกใน CHANGELOG `[1.13.2]` + ไฟล์นี้
