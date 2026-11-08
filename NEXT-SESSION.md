# NEXT-SESSION — เลขเวอร์ชันที่หัว popup (+ บอกเมื่อมีอัปเดต)

status: done

> **ห้ามพิมพ์คำต้องห้าม "ab***t" เต็ม ๆ ลงไฟล์ใด ๆ ใน repo** · ภาพ/GIF ที่ขึ้น GitHub ต้องเป็น mockup ภาษาอังกฤษ ไม่มีชื่อโปรเจกต์จริง/ข้อความไทย

① 2026-11-08 · commit ก่อนสปรินต์ `ab0da09` · release ล่าสุด v1.14.0 (จุดเขียวฟ้า) — ไฟล์นี้อาจเก่า เช็ก `git log --oneline -5` ก่อนเชื่อ

② ขั้นของสปรินต์

| ขั้น | งาน | สถานะ |
|---|---|---|
| 1 | PopupForm: `Claude Usage Meter  v1.14.0` ตัวเล็กสีจางต่อชื่อ (ปิดโลโก้ก็ยังเห็น) · มีเวอร์ชันใหม่ = `v1.14.0 → v1.14.1` สีเหลือง คลิกได้ · TrayAppContext ส่ง release + ผูกคลิกกับหน้าต่างอัปเดตเดิม · PopupShot | ✅ |
| 2 | รีวิว diff (main model อ่านเอง — diff 43 บรรทัด, โควตา 5h 81%) + screenshot มืด/สว่าง ปกติ/มีอัปเดต | ✅ ส่งให้คุณสมกกดูแล้ว |

③ สถานะจริง: commit แล้ว · build 0 error/0 warning · `--bg-selftest` ผ่าน · ภาพ `--popup-shot` 4 ไฟล์ถูกต้อง (เลขต่อชื่อ, มีอัปเดต = เหลือง `v1.14.0 → v1.14.1`) · ยังไม่ได้ลองคลิกจริงในแอป · ยังไม่ได้ถ่ายแบบปิดโลโก้ · resume tasks ลบแล้ว · ยังไม่ release 1.14.1 (ต้องถามก่อน)

④ ถัดไป: bump 1.14.1 + CHANGELOG + `docs/RELEASE_NOTES_v1.14.1.md` commit แล้ว · zip `%TEMP%\cm-release\ClaudeMeter-portable.zip` (sha256 `1E87018B…3EFC`, exe 1.14.1.0) · **รอคุณสมกกอนุมัติ publish** แล้ว `git tag v1.14.1` + `gh release create v1.14.1 <zip> --title v1.14.1 -F docs/RELEASE_NOTES_v1.14.1.md --verify-tag` · ทดสอบ: `dotnet build "/p:OutputPath=$env:TEMP\cm-bin\"` แล้ว `ClaudeMeter.exe --popup-shot <dir>`

⑤ กับดัก
- meter ที่รันจริงคือ `bin\Release\net8.0-windows\win-x64\ClaudeMeter.exe` ใน repo นี้ — build Release ต้องใส่ `"/p:OutputPath=$env:TEMP\cm-bin\"` ไม่งั้นไฟล์ล็อก (อย่าปิด meter เอง)
- commit message ที่มีเครื่องหมายคำพูด: เขียนลงไฟล์แล้ว `git commit -F` (PowerShell 5.1 ตัดอาร์กิวเมนต์)
- build portable ต้องมี `/p:EnableCompressionInSingleFile=true`

⑥ log เต็ม: [WORK-LOG-2026-11-08-header-version.md](WORK-LOG-2026-11-08-header-version.md) · งานก่อนหน้า [WORK-LOG-2026-11-08-background-dot.md](WORK-LOG-2026-11-08-background-dot.md)
