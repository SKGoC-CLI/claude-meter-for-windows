# NEXT-SESSION — Self-update (กดปุ่มแล้วอัปเดตตัวเอง) → v1.13.0

status: active

> **ห้ามพิมพ์คำต้องห้าม "ab***t" เต็ม ๆ ลงไฟล์ใด ๆ ใน repo** · ภาพ/GIF ที่ขึ้น GitHub ต้องเป็น mockup ภาษาอังกฤษ ไม่มีชื่อโปรเจกต์จริง/ข้อความไทย

① 2026-10-19 · เริ่มจาก `6666441` — ไฟล์นี้อาจเก่า เช็ค `git log --oneline -5` ก่อน

② ขั้นตอน (ตัดสินใจครบใน grilling — ดู WORK-LOG)

| Step | งาน | ใคร | สถานะ |
|------|-----|-----|-------|
| 1 | `Updater.cs` โหลด/sha256/เวอร์ชัน/rename วางทับ + `Program.cs` `--updated` รอ mutex + ลบ `.old.exe` | main (เขียนเอง — โค้ดเสี่ยงสูง) | ✅ `src/Updater.cs` |
| 2 | เมนู `Check for updates now`, `⬆ Update to vX` + balloon → dialog Update now / What's new / Later, Downloading % | main | ✅ TaskDialog ใน `TrayAppContext.cs` |
| 3 | reviewer + main re-review (โหลดไฟล์มารัน = high-stakes) | reviewer/main | ⬜ |
| 4 | ทดสอบตัว 1.11.9 อัปเดตไป v1.12.0 จริง + เคส sha ผิด + screenshot | main | ⬜ |
| 5 | bump 1.13.0 + CHANGELOG/README | main/doc-writer | ⬜ |
| 6 | **ถามคุณสมกกก่อน** → release v1.13.0 + rebuild portable | main | ⬜ |

③ สถานะจริง: step 1-2 เสร็จ build ผ่าน ยังไม่ได้ review/ทดสอบ. ของค้างไม่ release: Limit resets + ตัดชื่อ session ยาว (จะรวมใน v1.13.0)

④ ถัดไป: step 3 review

⑤ กับดัก
- ทดสอบอัปเดตในโฟลเดอร์ทดสอบแยก **ห้ามใช้ `portable\`** (จะถูกวางทับ)
- ห้ามเปิด console เด้ง (เกมเต็มจอ) — rename-in-place ไม่ต้องใช้ helper/bat
- asset ต้องชื่อ `ClaudeMeter-portable.zip` และ exe อยู่ราก zip, FileVersion = tag
- ห้าม backtick ใน Bash heredoc → ใช้ Write + `git commit -F`
- PopupShot ใช้ชื่อปลอมเท่านั้น

⑥ log เต็ม: [WORK-LOG-2026-10-19-self-update.md](WORK-LOG-2026-10-19-self-update.md)
