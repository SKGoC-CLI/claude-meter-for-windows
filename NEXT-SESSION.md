# NEXT-SESSION — เลขเวอร์ชันที่หัว popup (+ บอกเมื่อมีอัปเดต)

status: active

> **ห้ามพิมพ์คำต้องห้าม "ab***t" เต็ม ๆ ลงไฟล์ใด ๆ ใน repo** · ภาพ/GIF ที่ขึ้น GitHub ต้องเป็น mockup ภาษาอังกฤษ ไม่มีชื่อโปรเจกต์จริง/ข้อความไทย

① 2026-11-08 · commit ก่อนสปรินต์ `ab0da09` · release ล่าสุด v1.14.0 (จุดเขียวฟ้า) — ไฟล์นี้อาจเก่า เช็ก `git log --oneline -5` ก่อนเชื่อ

② ขั้นของสปรินต์

| ขั้น | งาน | สถานะ |
|---|---|---|
| 1 | PopupForm: `Claude Usage Meter  v1.14.0` ตัวเล็กสีจางต่อชื่อ (ปิดโลโก้ก็ยังเห็น) · มีเวอร์ชันใหม่ = `v1.14.0 → v1.14.1` สีเหลือง คลิกได้ · TrayAppContext ส่ง release + ผูกคลิกกับหน้าต่างอัปเดตเดิม · PopupShot | ⬜ |
| 2 | reviewer + screenshot มืด/สว่างให้คุณสมกกดู | ⬜ |

③ สถานะจริง: ยังไม่เริ่มโค้ด · resume tasks `resume-claude-meter-ver-1/2/3` ที่ 03:35 / 08:50 / 14:05 (8 พ.ย.) · ยังไม่ release 1.14.1 (ต้องถามก่อน)

④ ถัดไป: ขั้น 1 · ทดสอบ: `dotnet build "/p:OutputPath=$env:TEMP\cm-bin\"` แล้ว `ClaudeMeter.exe --popup-shot <dir>`

⑤ กับดัก
- meter ที่รันจริงคือ `bin\Release\net8.0-windows\win-x64\ClaudeMeter.exe` ใน repo นี้ — build Release ต้องใส่ `"/p:OutputPath=$env:TEMP\cm-bin\"` ไม่งั้นไฟล์ล็อก (อย่าปิด meter เอง)
- commit message ที่มีเครื่องหมายคำพูด: เขียนลงไฟล์แล้ว `git commit -F` (PowerShell 5.1 ตัดอาร์กิวเมนต์)
- build portable ต้องมี `/p:EnableCompressionInSingleFile=true`

⑥ log เต็ม: [WORK-LOG-2026-11-08-header-version.md](WORK-LOG-2026-11-08-header-version.md) · งานก่อนหน้า [WORK-LOG-2026-11-08-background-dot.md](WORK-LOG-2026-11-08-background-dot.md)
