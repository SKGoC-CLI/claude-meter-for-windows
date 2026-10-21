# Claude Desktop ย้ายโฟลเดอร์ login (2026-10-20) — สรุปสาเหตุ + วิธีแก้

> เขียนจาก Claude Meter (แก้แล้วใน v1.13.1, commit `dff587a`) เพื่อส่งต่อให้โปรเจกต์อื่นที่อ่าน token ของ Claude Desktop แบบเดียวกัน

## อาการ
- ตั้งแต่ประมาณ **23:56 วันที่ 20 ต.ค. 2026** แอปที่อ่าน token จาก Claude Desktop เพื่อเรียก usage API จะหา token ที่ยังไม่หมดอายุไม่เจอ
- ใน Claude Meter อาการคือ log ขึ้น `usage fetch failed: Usage temporarily unavailable.` ทุก 3 นาที และ popup ค้างเลขเก่า + คำว่า **stale**
- ไม่มี error 401 หรือ crash ให้เห็น เพราะแอปไม่ได้ยิง API เลย มันหา token ไม่เจอตั้งแต่ก่อนส่ง request

## สาเหตุ
Claude Desktop อัปเดตตัวเอง แล้ว**ย้ายโฟลเดอร์ข้อมูล** (ที่มี `config.json` + `Local State`) ไปที่ใหม่:

| ช่วงเวลา | ตำแหน่ง |
|---|---|
| ก่อน 2026-07-17 | `%APPDATA%\Claude\` |
| 2026-07-17 → 2026-10-20 (MSIX) | `%LOCALAPPDATA%\Packages\Claude_<pkgid>\LocalCache\Roaming\Claude\` |
| **ตั้งแต่ 2026-10-20** | **`%LOCALAPPDATA%\Claude-Data\claude_<pkgid>\`** (ตัวอย่างจริง: `claude_pzs8sxrjxfjjc`) |

- โฟลเดอร์ MSIX เดิมเหลือแค่ไฟล์ marker `.user-data-left.claude_<pkgid>` ไม่มี `config.json` แล้ว
- ไฟล์ข้างในเหมือนเดิมทุกอย่าง: `config.json` ยังมี key `oauth:tokenCacheV2` (เข้ารหัสแบบ Chromium os_crypt v10 = AES-256-GCM, key ห่อด้วย DPAPI อยู่ใน `Local State` → `os_crypt.encrypted_key`) **วิธีถอดรหัสไม่เปลี่ยน มีแค่ path ที่เปลี่ยน**
- ไฟล์ CLI `~/.claude/.credentials.json` ก็ใช้แทนไม่ได้ ถ้าใช้แต่ Desktop (เครื่องนี้ `expiresAt` = 0 / ไม่ได้อัปเดตตั้งแต่ 5 ต.ค.)

## วิธีแก้ (ที่ทำใน Claude Meter)
เพิ่มโฟลเดอร์ย่อยทุกอันใต้ `%LOCALAPPDATA%\Claude-Data\` เข้าไปในรายชื่อที่ค้นหา **โดยเก็บที่เดิมทั้งสองที่ไว้ด้วย** (บางเครื่องอาจยังไม่อัปเดต) แล้วเลือกโฟลเดอร์ที่มีทั้ง `config.json` และ `Local State` ซึ่ง `config.json` ถูกเขียนล่าสุด

```csharp
var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
var claudeData = Path.Combine(local, "Claude-Data");
if (Directory.Exists(claudeData))
    try { roots.AddRange(Directory.GetDirectories(claudeData)); }
    catch (Exception) { }
// แล้วเลือก root ที่มี config.json + "Local State" และ config.json ใหม่สุด
```

ไฟล์เต็มดูได้ที่ `src/DesktopCredentialStore.cs` → `FindDataDir()`

## ข้อควรทำในโปรเจกต์อื่น
1. ค้นในโค้ดหา `LocalCache\Roaming\Claude`, `Packages`, `APPDATA%\Claude`, `oauth:tokenCache` เพื่อหาจุดที่ hard-code path ไว้
2. เพิ่ม `%LOCALAPPDATA%\Claude-Data\*` เข้าไป **ห้ามลบที่เดิม** แล้วเลือกที่ที่ `config.json` ใหม่สุด
3. **หา path ใหม่ทุกครั้งที่ poll ห้ามเก็บค่าไว้ตั้งแต่ตอนเปิดแอป** เพราะ Desktop ย้ายโฟลเดอร์ระหว่างที่แอปเรารันอยู่ได้ (เกิดมาแล้ว 2 ครั้ง)
4. ทดสอบ: เปิดแอปแล้วต้องได้เลข usage สด ไม่ใช่ค่าที่ cache ไว้
5. (แนะนำ) ถ้าหา `config.json` ไม่เจอในทุกที่ ให้ log บอกตรง ๆ ว่า "Desktop data folder not found" ไม่ใช่ขึ้นแค่ว่า "temporarily unavailable" ครั้งหน้าจะได้รู้ทันที

## ถ้าเกิดอีก (Desktop ย้ายรอบที่ 3)
หา `config.json` ที่มีคำว่า `oauth:tokenCache` ใต้ `%APPDATA%` และ `%LOCALAPPDATA%` (ลึกประมาณ 6 ชั้น) เจอที่ไหน ที่นั่นคือที่ใหม่
