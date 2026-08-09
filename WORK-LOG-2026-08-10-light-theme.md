# Work log — 2026-08-10 — ยกเครื่อง light theme

สถานะ: **เสร็จ commit เข้า main แล้ว ยังไม่ push ยังไม่ release** (อยู่ใต้ `[Unreleased]` ใน CHANGELOG)

## โจทย์

Khun Somgok บอกว่า light theme "ดูไม่ค่อยเข้ากัน" ต้นเหตุจริง: สีทั้งชุดถูกเลือกมาสำหรับ
พื้นดำ `#1e1e1e` แล้วเอามาใช้ซ้ำบนพื้นสว่าง ไม่ได้ออกแบบสำหรับพื้นสว่างจริงๆ

ค่า contrast ที่วัดได้ก่อนแก้ (เทียบพื้นเดิม `#f4f4f4`):

| จุด | เดิม | ผล |
|---|---|---|
| Accent `#4da3ff` — ตัวเลข % | 2.4 : 1 | ตก (ต้องการ 4.5:1) |
| Warning `#ffa940` | 1.7 : 1 | ตกหนักสุด |
| Danger `#ff5c5c` | 2.8 : 1 | ตก |
| Track `#d8d8d8` | 1.3 : 1 | รางแทบมองไม่เห็น |
| Muted `#6d6d6d` | 4.7 : 1 | ผ่านฉิวเฉียด |

บวกอีก 3 เรื่อง: โลโก้เป็นการ์ดพื้นดำ, popup ไม่มีขอบเลย (พื้น `#f4f4f4` ทับหน้าต่างขาว =
ไม่มีขอบเขต), และเส้น grid เป็นดำโปร่งที่แข็งเกินไปบนพื้นสว่าง

## สิ่งที่ทำ

### 1. `src/Theme.cs` — palette light ใหม่ + สีสถานะที่รู้จักธีม

| | เดิม | ใหม่ (light) |
|---|---|---|
| Background | `#f4f4f4` | `#fbfbfb` |
| Track | `#d8d8d8` | `#e3e3e3` |
| Muted | `#6d6d6d` | `#5d5d5d` (6.0:1) |
| Grid / GridStrong | alpha 30 / 70 | alpha 24 / 60 |

เพิ่มใหม่: `Theme.Accent` `#005FB8` (5.9:1), `Theme.Warning` `#9D5D00` (4.8:1),
`Theme.Danger` `#C42B1C` (5.2:1), `Theme.Success` `#0F7B0F` (5.0:1) — ชุด Fluent
semantic ของ Windows 11 พร้อม `Theme.ColorFor()` และ `Theme.BorderColorRef`

**กฎสำคัญ:** ฝั่ง dark ของทุกตัวใหม่ delegate กลับไปที่ `IconRenderer.*` (และ `#6bcb77`
สำหรับ Success) → **dark theme ไม่เปลี่ยนแม้แต่ไบต์เดียว**

### 2. แยกสีของ tray icon ออกจาก popup — ห้ามรวมกลับ

`IconRenderer.Accent/Warning/Danger/ColorFor` **ยังเป็นสีสดเดิม** และตอนนี้ถูกใช้โดย
tray icon เท่านั้น (`TrayAppContext.cs:418, 937, 941`)

เหตุผล: tray icon อยู่บน taskbar ซึ่งตามธีมของ **Windows** ไม่ใช่ setting ของแอป —
เครื่อง Khun Somgok คือ `SystemUsesLightTheme=0` (taskbar ดำ) + `AppsUseLightTheme=1`
(แอปสว่าง) ถ้าหรี่สี IconRenderer ตามธีมแอป ตัวเลขบน taskbar ดำจะอ่านไม่ออก

> ถ้าจะให้ tray icon ตามธีม taskbar จริงๆ ในอนาคต ต้องอ่าน registry
> `HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize\SystemUsesLightTheme`
> แล้วดัก `WM_SETTINGCHANGE` เพื่อวาดไอคอนใหม่ตอนผู้ใช้สลับธีม — ยังไม่ได้ทำ

### 3. `src/PopupForm.cs` — ขอบหน้าต่าง + ใช้สีจาก Theme

- ไล่เปลี่ยน `IconRenderer.*` → `Theme.*` ทั้งไฟล์ (17 จุด) และ `#6bcb77` ที่ hardcode
  ไว้กลายเป็น `Theme.Success`
- เพิ่ม `ApplyBorderColor()` เรียก `DwmSetWindowAttribute(Handle, 34 /*DWMWA_BORDER_COLOR*/)`
  — ใช้ native ของ Windows ไม่ต้องวาดขอบเอง มุมโค้งที่ `DWMWCP_ROUND` ทำอยู่แล้วยังอยู่ครบ
  เรียกจากทั้ง `OnHandleCreated` และ `ApplyTheme` (ต้องเรียกซ้ำตอนสลับธีม)
- ถ้า DWM ปฏิเสธ (Windows ก่อน build 22000) จะ `Log.Warn` ไว้ ไม่เงียบหาย

### 4. `assets/logo.png` — ถอดการ์ดพื้นดำ

สคริปต์ flood-fill จากขอบภาพ แล้ว un-blend ขอบ anti-alias เทียบพื้น `#1e1e1e` ตามสูตร
`F = (C - (1-a)·BG) / a` เพื่อไม่ให้เหลือรัศมีดำรอบมาร์ก — flood fill จากขอบ (ไม่ใช่
เลือกตามสี) ทำให้ **ตาสีดำของไก่ไม่โดนลบ** เพราะถูกล้อมด้วยสีเหลือง

ไฟล์เดียวใช้ได้ทั้งสองธีม ไม่ต้องมี logic สลับ · `app.ico` และ `social-preview.png`
**ยังเป็นการ์ดพื้นดำเหมือนเดิม** (นั่นคือ identity ของแอปบน desktop/taskbar)

สคริปต์อยู่ที่ scratchpad ของ session — ถ้าต้องทำใหม่ ดูสูตรข้างบน

### 5. `src/PopupShot.cs` + `--popup-shot <dir>` — เครื่องมือดูงานภาพ

ตามแพตเทิร์นของ `--icon-selftest` ที่มีอยู่แล้ว: เรนเดอร์ popup เป็น PNG ทั้งสองธีม
ด้วยข้อมูลจำลองที่ครบทุกระดับ severity (42% / 76% / 93% / wallet) + `UsageHistory` จริง
จาก `%APPDATA%` เพื่อให้กราฟมีเส้นจริง

```
ClaudeMeter.exe --popup-shot C:\some\dir
```

ใช้ทุกครั้งที่แก้อะไรที่เห็นด้วยตา จะได้ไม่ต้องไปงมหา popup จริงบนจอ

**ข้อจำกัด:** `DrawToBitmap` ได้แค่ client area จับขอบ DWM ไม่ได้ — เคยลองเขียน
`ShootFramed()` ที่ `CopyFromScreen` ทับ backdrop ขาว แต่ได้ wallpaper กลับมาเพราะพิกัด
เพี้ยนจาก DPI/หลายจอ **ลบทิ้งไปแล้ว อย่ารื้อกลับมาโดยไม่แก้เรื่อง DPI ก่อน** ตอนนี้ยืนยัน
ขอบด้วยการเช็คว่า `DwmSetWindowAttribute` คืน S_OK (ไม่มี WARN ใน log) แทน

## 6. สลับ portable เป็นบิลด์ใหม่ (2026-08-10 03:07)

`publish-portable\` → ทับ `portable\ClaudeMeter.exe` (68.40 MB) ปิดตัวเก่า เปิดตัวใหม่
ตัวเก่าสำรองไว้ที่ `bin\portable-v1.9.0-backup\ClaudeMeter.exe`

**csproj `<Version>` ยังเป็น 1.9.0** — งานนี้ยังไม่ release เลยไม่ bump ผลคือ
`portable\` ตอนนี้คือ "v1.9.0 + light theme" ซึ่งหมายเลขเวอร์ชันไม่ได้บอกไว้
ตอนจะ release จริงค่อย bump เป็น 1.9.1 (หรือ 1.10.0)

`settings.json` สำรอง/เทียบแล้ว — เหมือนเดิมทุกค่า (`Theme=light`, `Scale=1.3`,
`TrayShows=both`, `AlwaysOnTop=true`)

> **กฎที่ Khun Somgok สั่งไว้ 2026-08-10:** ทุกครั้งที่ทำเวอร์ชัน/บิลด์ใหม่
> **ให้ build portable ทับ ปิดตัวเก่า เปิดตัวใหม่เสมอ ไม่ต้องถาม**
> (autostart ชี้ที่ `portable\` — ถ้าไม่ทับ ตัวที่ใช้จริงจะยังเป็นของเก่า)

## ตรวจแล้ว

- `dotnet build` — 0 warning 0 error
- เรนเดอร์ทั้งสองธีมดูด้วยตา: light ดีขึ้นชัด, dark เหมือนเดิมเป๊ะ
- ไม่มี `DWMWA_BORDER_COLOR failed` ใน log → DWM รับค่าขอบแล้ว
- **ถ่ายป๊อปอัปจริงบนจอจริงจากบิลด์ที่รันอยู่** แล้วสแกนพิกเซลข้ามขอบซ้ายที่กลางความสูง:
  `(88,84,84)` หน้าต่างหลัง → **`(224,224,224)` = `#e0e0e0` ขอบ 1 px** → `(251,251,251)`
  = `#fbfbfb` พื้น — ตรงกับค่าที่ตั้งไว้เป๊ะทั้งคู่ มุมโค้งยังอยู่ครบ
  วิธีถ่าย: `SetProcessDPIAware()` → `EnumWindows` หา hwnd ของ process →
  `GetWindowRect` → `CopyFromScreen` โดยเผื่อขอบ 20 px (ป๊อปอัป pinned อยู่แล้วเลย
  ไม่ต้องกดอะไรเรียก) — **ได้ผลกว่า `CopyFromScreen` ใน PopupShot ที่พิกัดเพี้ยน**
  เพราะรันจาก host ที่ DPI-aware และใช้พิกัดจริงจาก `GetWindowRect`
- reviewer (Sonnet) ตรวจ diff: ไม่เจอบั๊ก — ยืนยันว่า tray icon ไม่ได้ผูกกับธีมแอป,
  dark เหมือนเดิม, threshold ของ `ContextColor` (85/60) ไม่ได้ถูกกลืนเข้า `ColorFor` (90/70)

## ถ้าจะย้อนกลับ

`git revert` commit นี้ได้ทั้งก้อน — ไม่มีอะไรแตะ settings หรือไฟล์ของผู้ใช้
(`assets/logo.png` เวอร์ชันการ์ดพื้นดำอยู่ใน git history)

## 7. รอบสอง: สำรวจพาเลตต์ — ยังไม่จบ

หลัง commit `662cb88` Khun Somgok ดูของจริงแล้วบอกว่า **"สียังไม่ใช่"** — ซึ่งถูก
ค่า contrast ผ่านหมดแล้วก็จริง แต่ปัญหาที่เหลือเป็นคนละเรื่อง: **ทั้งหน้าต่างเป็น
น้ำเงิน Microsoft สีเดียว** ขณะที่โลโก้เป็นโคลอลส้ม/เหลือง — คนละภาษาสี

เรนเดอร์ทางเลือก **5 แบบจากตัวโปรแกรมจริง** (ไม่ใช่ mockup) เก็บไว้ที่
**`docs/design/light-theme-variants/`** พร้อม README ที่สรุปว่าแต่ละแบบล้มเหลวตรงไหน
เครื่องมือสร้างใหม่: **`docs/design/render-palette-variants.py`**
(แก้ dict `VARIANTS` แล้วรัน — มันเขียนทับ `Theme.cs`/`PopupForm.cs` แล้วคืนค่าด้วย
`git checkout` เลย **ไม่ยอมรันถ้า `src/` ยังมีของค้าง**)

ข้อสรุปสั้นๆ ที่ไม่ต้องทดลองซ้ำ:
1. ตัวปัญหาคือ **สี accent ไม่ใช่พื้นหลัง** — เปลี่ยนพื้นเป็นครีมอุ่นแต่คงน้ำเงินไว้ *แย่ลง*
2. ใช้โคลอลเป็นสี "ปกติ" ไม่ได้ — ชนกับสีแดง 90%+ และสถานะปกติไม่ควรเป็นสีร้อน
3. ให้กราฟใช้พาเลตต์เดียวกับ severity แล้วกราฟตายซีด
4. แยก "สีของกราฟ" ออกจาก "สีของความรุนแรง" คือทางที่ดีที่สุดในห้าแบบ

**แต่ทั้ง 5 แบบยังไม่ถูกใจ — โค้ดใน repo ยังเป็นแบบ A (`662cb88`) ไม่ได้เปลี่ยนตามอันไหน**
ทางที่ยังไม่ได้ลองอยู่ท้าย README ในโฟลเดอร์นั้น

## ยังไม่ได้ทำ

- **push / release** — รอ Khun Somgok ใช้จริงก่อน
- ให้แอปตามธีม Windows อัตโนมัติ (ตอนนี้ยังเป็นเมนูเลือกเอง)
- tray icon ตามธีม taskbar (ดูข้อ 2)
- **สรุปสีให้จบ** — ดูข้อ 7 ยังค้างอยู่
