# WORK-LOG 2026-10-19 — Self-update (กดปุ่มแล้วอัปเดตตัวเอง) (grilling)

โจทย์คุณสมกก: ใน app มีปุ่มกด check update แล้ว auto update ไปเลย

## ข้อเท็จจริงที่ค้นเจอ (ไม่ต้องถาม)
- มีการเช็กอัปเดตอยู่แล้ว: `CheckUpdatesAsync` ใน `src/TrayAppContext.cs` (~บรรทัด 731) เรียก
  `releases/latest` ของ `SKGoC-CLI/claude-meter-for-windows`, throttle 20 ชม., เจอใหม่ → เมนู
  `⬆ Update available: vX` + balloon; กดแล้วแค่ `OpenUpdatePage()` เปิดหน้าเว็บ
- setting `CheckUpdates` (ค่าเริ่ม true) = toggle "Check for updates" ในเมนู App
- release asset เดียว: `ClaudeMeter-portable.zip` (v1.12.0 = 66,325,591 bytes) ข้างในมี
  `ClaudeMeter.exe` ไฟล์เดียว self-contained (~72 MB)
- GitHub API ให้ `assets[].digest` = `sha256:...` มาฟรี (v1.12.0 มีแล้ว) → ไม่ต้องเปลี่ยนขั้นตอนออก release
- exe ไม่ได้เซ็นโค้ด (README บอกเรื่อง SmartScreen)
- Single instance ด้วย Mutex `ClaudeMeter_SingleInstance` (`src/Program.cs:30`) → ตัวใหม่ต้องรอ
  ตัวเก่าปล่อย mutex (ส่ง arg เช่น `--updated` ให้ตัวใหม่รอ/retry สักครู่)
- Autostart = Run key ชี้ path exe → วางทับที่เดิม path ไม่เปลี่ยน ไม่ต้องแก้
- Windows ยอม **rename exe ที่กำลังรัน** ได้ → ไม่ต้องมี helper/bat (และไม่มีหน้าต่าง console เด้ง)

## ผลการตัดสินใจ
| # | คำถาม | คำตอบ |
|---|---|---|
| 1 | อัตโนมัติแค่ไหน | **A.** กดแล้วถามก่อน → Yes = โหลด วางทับ เปิดใหม่เอง (ไม่อัปเดตเงียบ) |
| 2 | ตรวจไฟล์ | **A.** HTTPS จาก github.com เท่านั้น + เทียบ sha256 กับ `digest` + `FileVersionInfo` ของ exe ใน zip ต้องตรงกับ tag. ไม่ตรงข้อไหน = ยกเลิก + เปิดหน้า release แทน |
| 3 | กู้คืน | **B.** ลบ `ClaudeMeter.old.exe` ทันทีที่ตัวใหม่เปิดขึ้นได้ (ไม่เก็บสำรอง; มีปัญหา = โหลดตัวเก่าจาก GitHub เอง) |
| 4 | ตำแหน่งปุ่ม | **A.** เมนู App เพิ่ม `Check for updates now` (เช็กทันที ข้าม throttle; ล่าสุดแล้ว → `You're up to date (v1.x)`); บรรทัด `⬆ Update to vX` และการคลิก balloon → หน้าต่างถาม `Update now / What's new / Later` (What's new = เปิดหน้า release) |
| 5 | เวอร์ชัน | **A.** v1.13.0 รวม 2 ฟีเจอร์ที่ค้าง (Limit resets, ตัดชื่อ session ยาว) + self-update; release note บอก "ครั้งสุดท้ายที่ต้องโหลดเอง". **ต้องถามคุณสมกกก่อนกดปล่อยบน GitHub** |

ค่าเริ่มต้นที่ผมเลือกเอง (ไม่ได้ถาม):
- ลำดับวางทับ: โหลด zip ลง `%TEMP%\ClaudeMeter-update\` → ตรวจ sha256 → แตก exe → ตรวจเวอร์ชัน →
  rename `ClaudeMeter.exe` → `ClaudeMeter.old.exe` → ย้าย exe ใหม่เข้าที่ → start ตัวใหม่ `--updated` →
  ตัวเก่าปล่อย mutex + ปิด. ถ้าย้ายไม่สำเร็จ → rename กลับ + แจ้ง error
- ตัวใหม่ตอน start: ลบ `*.old.exe` ข้าง ๆ ตัวเอง (best-effort) + ลบโฟลเดอร์ temp
- โฟลเดอร์เขียนไม่ได้ (เช่น Program Files) → ข้อความ + เปิดหน้า release แทน
- ระหว่างโหลดโชว์สถานะในเมนู `Downloading v1.13.0… 45%` (ไม่ทำหน้าต่าง progress แยก)
- toggle เช็กรายวันเดิมคงไว้ ทำงานเหมือนเดิม

## วิธีทดสอบ (ก่อนปล่อยจริง)
build ตัวทดสอบตั้งเลข **1.11.9** ใส่โฟลเดอร์ทดสอบแยก (ไม่ใช่ `portable\`) → กด Check now →
มันต้องอัปเดตไปเป็น **v1.12.0 ตัวจริงบน GitHub** → ตรวจ: เปิดขึ้นใหม่, เวอร์ชัน 1.12.0, `.old.exe` หาย,
temp หาย. ทดสอบ sha ผิดด้วยการแก้ค่าคาดหวังในโค้ดทดสอบชั่วคราว → ต้องยกเลิกและเปิดหน้าเว็บ

## โควตาตอน grill (จาก history.json)
5h = 41%, 7d = 6% → พอทำได้ ตัวเลขต่อ step เป็นการประมาณ

## Sprint table
| # | step | ใคร | ~5h% | หยุดตรงนี้ได้ = |
|---|---|---|---|---|
| 1 | `Updater.cs`: ดึง release (tag, html_url, asset url, digest) → โหลด → sha256 → แตก zip → FileVersionInfo → rename/move → start `--updated`; `Program.cs` รอ mutex เมื่อมี `--updated` + ลบ `.old.exe` | coder | 6 | มีโค้ดอัปเดต ยังไม่มีปุ่ม |
| 2 | เมนู: `Check for updates now`, `⬆ Update to vX` + balloon click → dialog Update now / What's new / Later, สถานะ Downloading %, up-to-date message | coder | 4 | ฟีเจอร์ครบ |
| 3 | main model re-review (โค้ดโหลดไฟล์มารัน = high-stakes) + reviewer | reviewer + main | 4 | พร้อมทดสอบ |
| 4 | ทดสอบ 1.11.9 → 1.12.0 จริง + เคส sha ผิด; screenshot เมนู/dialog ให้คุณสมกกดู | main | 4 | ใช้ได้จริง |
| 5 | bump 1.13.0, CHANGELOG/README (วิธีอัปเดต + "ครั้งสุดท้ายที่ต้องโหลดเอง") | doc-writer | 1 | พร้อมปล่อย |
| 6 | **ถามคุณสมกกก่อน** → build zip, release v1.13.0, rebuild portable | main | 2 | จบ |

## สถานะ
- grilling เสร็จ 2026-10-19
- **สปรินต์เสร็จ + release v1.13.0 แล้ว (2026-10-19)**: commits 7ff1cb4 → eb70863, tag v1.13.0
  - step 1-2 main เขียนเอง (ไม่ใช้ coder: โค้ดโหลดไฟล์มารันแทนตัวเอง เสี่ยงสูง)
  - reviewer เจอ 13 ข้อ แก้ 10: แตก exe ลงโฟลเดอร์ app (rename ข้าม volume ไม่ได้ atomic), timeout 10 นาทีครอบการอ่าน body, install บน Task.Run, retry ลบ .old 10×300ms, log ตอน --updated รอ mutex ไม่ทัน, CanSelfUpdate (ไม่มี digest/โฟลเดอร์เขียนไม่ได้ → เปิดหน้าเว็บ), AllowCancel, สถานะ "installed — restart" ถ้าเปิดตัวใหม่ไม่ขึ้น
  - ทดสอบ: sha ผิด → ยกเลิก exe ไม่แตะ; 1.11.9 → v1.12.0 จริง ~4 วิ; cleanup ลบ .old + temp; รูปหน้าต่าง `docs/design/update-prompt-v1.13.0.png`
  - release: digest GitHub = `de78337f…` ตรงกับไฟล์, public 200
- ย้อนกลับ: ลบ release/tag v1.13.0 บน GitHub (ต้องถามคุณสมกก) + `git revert` 7ff1cb4..eb70863; portable ตัวเก่าโหลด v1.12.0 จาก GitHub
- บันทึก: ADR `docs/adr/0003-self-update-rename-in-place.md`, CONTEXT.md หมวด "อัปเดต"
