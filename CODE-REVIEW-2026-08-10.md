# Code review 2026-08-10 — 3 commits ที่ยังไม่ push (light theme rebuild)

**ขอบเขต:** `git diff origin/main...HEAD` = `662cb88`, `b6221c1`, `011e252`
(src/Theme.cs, src/PopupForm.cs, src/PopupShot.cs, src/Program.cs,
docs/design/render-palette-variants.py) + working tree (docs/PROMO_TODO.md เท่านั้น)

## ผ่าน — ตรวจแล้วไม่มีปัญหา

- `IconRenderer.*` → `Theme.*` ใน PopupForm.cs **แทนที่ครบทุกจุด** ไม่มีตกค้าง
  (grep `IconRenderer.(Accent|Warning|Danger|ColorFor)` เหลือแค่ใน Theme.cs เอง)
- Threshold ของ `Theme.ColorFor` (≥90 Danger / ≥70 Warning) **ตรงกับ**
  `IconRenderer.ColorFor` เป๊ะ → dark mode ไม่เปลี่ยนสีจริงตามที่ commit อ้าง
- ค่าสี dark ทั้งหมด map กลับไปที่ IconRenderer / `#6bcb77` เดิม → byte-for-byte เท่าเดิม
- Contrast บน `#fbfbfb` คำนวณแล้วผ่าน 4.5:1 ทุกตัว:
  `#005FB8` 6.0:1 · `#9D5D00` 5.0:1 · `#C42B1C` 5.5:1 · `#0F7B0F` 5.3:1
- `ApplyBorderColor()` เรียกตอน handle ยังไม่เกิด → return ก่อน แล้ว `OnHandleCreated`
  เรียกซ้ำให้เอง ไม่มีช่องที่ border หาย
- `BorderColorRef` = `unchecked((int)0xFFFFFFFF)` = DWMWA_COLOR_DEFAULT ถูกต้อง
- `--popup-shot` เรียก `new UsageHistory()` ซึ่ง **อ่านอย่างเดียว** (`Prune()` แก้แค่ในหน่วยความจำ)
  → รันพร้อมแอปตัวจริงได้ ไม่ทำ history.json พัง
- `bmp.Save(path)` ไม่ระบุ format → .NET fallback เป็น PNG สำหรับ MemoryBmp ถูกต้อง

## เจอ 4 ข้อ

| # | ที่ | เรื่อง |
|---|-----|--------|
| 1 | `render-palette-variants.py:21` | guard "src สะอาดไหม" ไม่เช็ค `returncode` ของ git — ถ้า git พัง (path ย้าย / ไม่มี .git) จะผ่าน แล้วทับ Theme.cs + PopupForm.cs โดย `git checkout` กู้คืนไม่ได้ |
| 2 | `PopupShot.cs:55` | `DrawToBitmap` ไม่จับกรอบ DWM → PNG light ทุกใบ **ไม่มีขอบ** ที่ commit นี้เพิ่งเพิ่ม = ตัดสินพาเลตต์จากภาพที่ไม่ตรงของจริง |
| 3 | `render-palette-variants.py:95` | `range(999, 1300)` ฮาร์ดโค้ดเลขบรรทัด — PopupForm.cs ขยับเมื่อไหร่ replace ผิดที่เงียบๆ |
| 4 | `render-palette-variants.py:115` | restore source แล้ว แต่ `bin/Debug/ClaudeMeter.exe` ยังเป็น variant สุดท้าย — binary ไม่ตรง source |

ข้อ 2 เกี่ยวตรงกับที่พาเลตต์ยังไม่ผ่าน 5 รอบ — ภาพที่ใช้ตัดสินขาดขอบไป
