# NEXT-SESSION — Self-update (กดปุ่มแล้วอัปเดตตัวเอง) → v1.13.0

status: done

> **ห้ามพิมพ์คำต้องห้าม "ab***t" เต็ม ๆ ลงไฟล์ใด ๆ ใน repo** · ภาพ/GIF ที่ขึ้น GitHub ต้องเป็น mockup ภาษาอังกฤษ ไม่มีชื่อโปรเจกต์จริง/ข้อความไทย

① 2026-10-19 · เริ่มจาก `6666441`, release ที่ tag `v1.13.0` — ไฟล์นี้อาจเก่า เช็ค `git log --oneline -5` ก่อน

② ขั้นตอน (ตัดสินใจครบใน grilling — ดู WORK-LOG)

| Step | งาน | ใคร | สถานะ |
|------|-----|-----|-------|
| 1 | `Updater.cs` โหลด/sha256/เวอร์ชัน/rename วางทับ + `Program.cs` `--updated` รอ mutex + ลบ `.old.exe` | main (เขียนเอง — โค้ดเสี่ยงสูง) | ✅ |
| 2 | เมนู `Check for updates now`, `⬆ Update to vX` + balloon → TaskDialog Update now / What's new / Later | main | ✅ |
| 3 | reviewer + main re-review | reviewer/main | ✅ แก้ข้อ 1-4,6-11; ไม่แก้ 5,12,13 (ช่วง swap เสี้ยววิ / same-user malware) |
| 4 | ทดสอบ 1.11.9 → v1.12.0 จริง + sha ผิด + รูปหน้าต่าง | main | ✅ `docs/design/update-prompt-v1.13.0.png` |
| 5 | bump 1.13.0 + CHANGELOG/README/release note | main | ✅ |
| 6 | release v1.13.0 + rebuild portable | main | ✅ https://github.com/SKGoC-CLI/claude-meter-for-windows/releases/tag/v1.13.0 — digest บน GitHub = sha256 ไฟล์ (de78337f…), หน้า release + zip ตอบ 200 แบบไม่ล็อกอิน |

③ สถานะจริง: release แล้ว, push ครบ, portable 1.13.0 รันอยู่. **ยังไม่เคยเห็นการอัปเดตจาก v1.13.0 → รุ่นถัดไปของจริง** (ทดสอบได้แค่ 1.11.9 → 1.12.0) จะได้เห็นตอนออก v1.13.1/v1.14.0

**2026-10-21 hotfix → RELEASED v1.13.1** (https://github.com/SKGoC-CLI/claude-meter-for-windows/releases/tag/v1.13.1 · digest = sha256 zip 9dbdc4e8… · หน้า/zip 200 ไม่ล็อกอิน · **อัปเดตเองจาก 1.13.0 → 1.13.1 ทดสอบผ่านของจริง** ด้วย `--update-selftest` ในโฟลเดอร์ scratch · portable = 1.13.1 รันอยู่) · สรุปส่งต่อโปรเจกต์อื่น: [docs/DESKTOP-LOGIN-MOVED-2026-10-20.md](docs/DESKTOP-LOGIN-MOVED-2026-10-20.md) · รายละเอียดเดิม: meter ค้าง "stale" ตั้งแต่ 23:56 คืน 20 ต.ค. — Claude Desktop อัปเดตแล้วย้ายโฟลเดอร์ login ไป `%LOCALAPPDATA%\Claude-Data\claude_<pkg>\` (ที่เดิมเหลือแค่ไฟล์ `.user-data-left.*`) → เพิ่มที่ค้นใน `DesktopCredentialStore.FindDataDir` + portable สร้างใหม่แล้วรันอยู่ (log 08:02 `usage ok`). exe เก่าอยู่ `bin\portable-v1.13.0-backup\`. ผู้ใช้คนอื่นที่ใช้แต่ Desktop ก็ค้างเหมือนกัน → ควรออก v1.13.1

④ ถัดไป: ไม่มีงานค้างของสปรินต์นี้. ตอนออกรุ่นถัดไป: กด App → Check for updates now บน portable เพื่อยืนยันว่าอัปเดตเองได้จริง

⑤ กับดัก
- ทดสอบอัปเดตในโฟลเดอร์ทดสอบแยก **ห้ามใช้ `portable\`** (จะถูกวางทับ) — `--update-selftest [badsha]` (ไม่มีหน้าต่าง ผลใน log), `--check-updates-now` (เปิดหน้าต่างถาม)
- ทดสอบต้องหยุด meter ตัวที่รันอยู่ก่อน (single-instance mutex) แล้วเปิดกลับ
- asset ต้องชื่อ `ClaudeMeter-portable.zip`, exe อยู่ราก zip, FileVersion = tag — ไม่งั้นอัปเดตจะเปิดหน้าเว็บแทน
- ห้าม backtick ใน Bash heredoc → ใช้ Write + `git commit -F` (พลาดซ้ำ 2 ครั้งในสปรินต์นี้)
- ถ่ายรูปหน้าต่างด้วย PowerShell ต้อง `SetProcessDPIAware` ไม่งั้นรูปโดนครอป

⑥ log เต็ม: [WORK-LOG-2026-10-19-self-update.md](WORK-LOG-2026-10-19-self-update.md)
