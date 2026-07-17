# สรุปงาน 2026-07-17 (เย็น) — Hotfix v1.6.2: Claude Desktop ย้ายโฟลเดอร์ (MSIX)

Record สำหรับกลับมาดูย้อนหลัง — เหตุการณ์ "Usage temporarily unavailable" หลังรันตัว portable

## อาการ

- ปิดตัว debug แล้วเปิด portable v1.6.1 → popup ขึ้น **"Usage temporarily unavailable."** สีส้มค้าง
- log (`%APPDATA%\ClaudeMeter\logs\`) ฟ้องทุกรอบ poll:
  `desktop token read failed: Could not find a part of the path 'C:\Users\SKGoC\AppData\Roaming\Claude\config.json'`

## สาเหตุ (root cause)

**Claude Desktop อัปเดตตัวเองตอน ~17:25 วันนี้ เป็นแพ็กเกจแบบ MSIX** แล้วย้ายที่เก็บข้อมูล:

- ที่เดิม: `%APPDATA%\Claude\` → **ถูกลบทิ้ง**
- ที่ใหม่: `%LOCALAPPDATA%\Packages\Claude_pzs8sxrjxfjjc\LocalCache\Roaming\Claude\`

meter ยังอ่าน path เดิม → ไม่เจอ Desktop token และ CLI token (`~/.claude/.credentials.json`)
ก็หมดอายุตั้งแต่ 15 ก.ค. (ไม่ได้ใช้ CLI) → ไม่มี token เลย → เข้าโหมด transient (ส้ม) ตามดีไซน์
v1.5.0 ซึ่งถูกต้อง แต่รอบนี้ไม่มีวันหายเองเพราะ path เปลี่ยนถาวร

จุดที่ทำให้งงตอนไล่ปัญหา:
- ตัว debug ที่เปิดก่อน 17:25 ยังโชว์ปกติ เพราะ token ค้างใน memory และถูกปิดก่อนถึงรอบ poll ที่จะพัง
- เชลล์ที่ Claude ใช้ตรวจ สืบทอด package identity จากแอป Desktop ทำให้ Windows (MSIX
  filesystem virtualization) redirect ให้เห็นไฟล์เสมือนยังอยู่ที่เดิม — ต้องมองผ่าน UNC
  (`\\localhost\C$\...`) ถึงเห็นว่าโฟลเดอร์จริงหายแล้ว

## แก้ยังไง

แก้ไฟล์เดียว: `src/DesktopCredentialStore.cs`

- เพิ่ม `FindDataDir()` — มองหาโฟลเดอร์ Desktop จากทั้งสองที่ (path เดิม + ทุกโฟลเดอร์
  `Claude_*` ใต้ `%LOCALAPPDATA%\Packages\...\LocalCache\Roaming\Claude`)
  เอาเฉพาะที่มี `config.json` + `Local State` ครบ แล้วให้อันที่ `config.json` ใหม่สุดชนะ
- **resolve ใหม่ทุกรอบ poll** (ไม่ cache ตอน start) — กันเคสแอปย้ายโฟลเดอร์กลางคันแบบวันนี้
- ไม่เจอโฟลเดอร์เลย = return null เงียบๆ (ไม่ log warn รัวๆ) — ไม่กระทบกติกา v1.5.0
  ที่ว่าการอ่านไฟล์ห้ามทำให้ขึ้น "login expired" ปลอม
- cache ที่ decrypt แล้ว ผูกกับ (path, mtime) — path สลับที่เมื่อไหร่ก็ decrypt ใหม่

## ผลทดสอบ (ของจริง)

```
17:36:23 [WARN ] desktop token read failed: Could not find a part of the path '...config.json'   ← v1.6.1 ตัวเก่า
17:38:39 [INFO ] === Claude Meter v1.6.2 starting ===
17:38:40 [INFO ] usage ok: Session (5h) 21%, Weekly 19%, Fable Weekly 27%                        ← หายทันที
```

## Deliverables อยู่ไหน

| ของ | ที่อยู่ |
|-----|--------|
| โค้ดที่แก้ | `src/DesktopCredentialStore.cs` |
| เวอร์ชัน + changelog | `ClaudeMeter.csproj` (1.6.2), `CHANGELOG.md` |
| Release notes | `docs/RELEASE_NOTES_v1.6.2.md` |
| exe ตัวใหม่ (รันอยู่ + autostart ชี้ที่นี่) | `portable\ClaudeMeter.exe` |
| zip สำหรับแนบ release | `ClaudeMeter-portable.zip` (root ของ repo) |

## Folder map — เก็บ / ลบได้

- **เก็บ:** ทุกอย่างใน git (`src/`, `docs/`, `CHANGELOG.md`) + `portable\` (autostart ชี้ที่นี่ **ห้ามลบ**)
- **ลบได้:** `bin/`, `obj/` (build output), `ClaudeMeter-portable.zip` หลังอัปขึ้น release แล้ว

## วิธี undo / recover

- ย้อนโค้ด: `git revert <hash ของ commit v1.6.2>` แล้ว build portable ใหม่ (คำสั่งอยู่ใน
  [SESSION_SUMMARY_2026-07-16.md](SESSION_SUMMARY_2026-07-16.md))
- ถ้า Desktop ย้ายโฟลเดอร์อีกในอนาคต: ดู log ว่า `desktop token read failed` ชี้ path ไหน
  แล้วเพิ่ม path ใหม่ใน `FindDataDir()` — โครงรองรับหลาย path อยู่แล้ว
