# สรุปงาน 2026-07-16 — v1.6.0 Multi-session context

Record สำหรับกลับมาดูย้อนหลัง งานทั้งหมดใน session นี้ ทำเสร็จ + push + release แล้ว

## ทำอะไรไปบ้าง (ตามลำดับ)

### 1. แก้บั๊ก % context ผิดบน account 1M window
- **อาการ:** SESSION CONTEXT โชว์ 84% ทั้งที่ Claude โชว์ 17% (168.8k / 1.0M)
- **สาเหตุ:** `ContextMonitor.cs` เดา window จากจำนวน token (`>200k ⇒ 1M`) — ผิดทุกครั้งที่ยังไม่ถึง 200k บน account 1M
- **แก้:** เดาจากชื่อโมเดลแทน (Opus/Sonnet ⇒ 1M, Haiku ⇒ 200k)
- **commit:** `48f779e` (push แล้ว) · dev note: [DEV_NOTES_context-window.md](DEV_NOTES_context-window.md)

### 2. ฟีเจอร์ Multi-session context (ตัวหลัก v1.6.0)
- SESSION CONTEXT โชว์**ทุก session ที่ active** (แต่ละ jsonl ที่เขียนใน 10 นาที = 1 บล็อก)
- แต่ละบล็อก: **ชื่อ session ตัวหนา** → % (สี) → `current / capacity` tokens ชิดขวา (เช่น `169k / 1.0M`)
- header = `SESSION CONTEXT (n)`
- **2 option ใหม่** (คลิกขวาที่ tray): `Context: max shown` (1/2/3/5, default 3), `Context: sort by` (last active / name A–Z / context high→low)
- ข้อความ **"resets 5h after next use"** ตอน 5h window = 0% idle (แทนช่องว่าง)
- ไฟล์ที่แก้: `ContextMonitor.cs`, `PopupForm.cs`, `AppSettings.cs`, `TrayAppContext.cs`
- **commit:** `30108d3` (push แล้ว) · dev note: [DEV_NOTES_multi-session-context.md](DEV_NOTES_multi-session-context.md)
- ปรับหน้าตาตาม feedback หลายรอบ: ระยะ bar, เว้นบน 5px, ชื่อมาหน้า % + ตัวหนา

### 3. สอบสวนบั๊ก "Session 5h ค้าง 0% ข้ามคืน" → ไม่ใช่บั๊ก
- อ่าน log `%APPDATA%\ClaudeMeter\logs\` → poll ทำงานทุก 3 นาทีตลอดคืน ไม่มี error/gap
- สาเหตุจริง: 5h window รีเซ็ตจริงตอน 09:01 (0% + `resets_at: null`) + poll latency ≤3 นาที เผอิญมาดูจอพอดีจังหวะ
- ตัดออกแล้ว: timer ค้าง, `_fetching` ค้าง, sleep, rate-limit backoff — ไม่มีอะไรในนั้น
- ทำ UX เสริม: ข้อความ "resets 5h after next use" (อยู่ในข้อ 2)
- dev note: [DEV_NOTES_session-reset-lag.md](DEV_NOTES_session-reset-lag.md)

### 4. Release v1.6.0
- bump csproj 1.5.0 → 1.6.0, CHANGELOG `[1.6.0] - 2026-07-16`, [RELEASE_NOTES_v1.6.0.md](RELEASE_NOTES_v1.6.0.md)
- **commit:** `10e281b` (push แล้ว)
- build portable zip (self-contained single-file ~68MB exe → zip 63MB) + smoke-test ผ่าน
- **GitHub release + tag `v1.6.0`** = Latest, ไม่ใช่ draft
- 🔗 https://github.com/SKGoC-CLI/claude-meter-for-windows/releases/tag/v1.6.0

### 5. Demo GIF
- ต้นฉบับ `demo 16 July 2026.mp4` (1152×746, 59.6s) → ใส่ watermark "Claude Usage Meter v1.6.0" จางๆ (opacity 50%, ฟอนต์ 28) มุมขวาล่างเหนือ taskbar
- แปลงเป็น gif (640px, 12fps, palette 2-pass) = `demo-multi-session.gif` (3.8MB)
- **commit:** `f053c6e` (push แล้ว) — เพิ่มใน README ใต้ screenshot + แนบเป็น asset ใน release v1.6.0

## Commits ทั้งหมด (push ขึ้น main หมดแล้ว)
| hash | เรื่อง |
|------|-------|
| `48f779e` | Fix SESSION CONTEXT percent on 1M-window accounts |
| `30108d3` | Show all active sessions in SESSION CONTEXT + token readout |
| `10e281b` | Prepare v1.6.0 release |
| `f053c6e` | Add multi-session demo GIF to README |

## Folder map — เก็บ / ลบได้

**เก็บ (อยู่ใน git):**
- `src/*.cs`, `ClaudeMeter.csproj`, `CHANGELOG.md`, `README.md`
- `docs/DEV_NOTES_*.md`, `docs/RELEASE_NOTES_v1.6.0.md`, `docs/demo-multi-session.gif`, ไฟล์สรุปนี้

**ลบได้ทุกเมื่อ (build artifact / ไฟล์ทำงาน ไม่อยู่ใน git):**
- ~~`portable/`~~ **ห้ามลบแล้ว (แก้ไข 17 ก.ค.)** — autostart (HKCU Run key) ชี้มาที่ `portable\ClaudeMeter.exe` แล้ว ถ้าลบต้อง build ใหม่แล้วชี้ key ใหม่
- `ClaudeMeter-portable.zip` (63MB, อัปขึ้น release แล้ว)
- `bin/`, `obj/` (build output)

**เก็บกวาดแล้ว 17 ก.ค. 07:3x:** ทิ้ง working media เก่า 11 ไฟล์ (~20MB) ลง **Recycle Bin** (กู้ได้):
screenshot เก่า ×5, วิดีโอดิบยุค v1.2–1.4 ×4 (`demo.mp4`, `demo 12 July...` ×2, `Recording 2026-07-12...`),
`demo 16 July 2026 watermarked.mp4` + `demo 16 July 2026.gif` (ซ้ำกับ `demo-multi-session.gif` ใน git)

**ต้นฉบับ (อย่าลบ):** `docs/demo 16 July 2026.mp4` — วิดีโอดิบก่อนใส่ watermark (demo v1.6.0 ปัจจุบัน)
และ `docs/Recording 2026-07-17 022750*` (mp4 ดิบ + -wm.mp4 + .gif) — demo ชุดใหม่ โชว์ฟีเจอร์ "no data" ไว้ใช้กับ v1.6.1

## คำสั่ง build portable (ไม่มี publish profile ใน repo)
```
dotnet publish ClaudeMeter.csproj -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true -p:DebugType=none -o portable
```
แล้ว `Compress-Archive portable\ClaudeMeter.exe ClaudeMeter-portable.zip`
(⚠ `EnableCompressionInSingleFile` สำคัญ — ไม่ใส่ exe จะ 154MB แทน 68MB)

## วิธี undo / recover
- **ย้อน code:** `git revert <hash>` (เช่น `git revert f053c6e`) — ไม่ควร reset --hard เพราะ push แล้ว
- **ลบ release + tag:** `gh release delete v1.6.0 --cleanup-tag` (แล้ว `git push origin :refs/tags/v1.6.0` ถ้าค้าง)
- **แก้ watermark GIF:** rerun ffmpeg จาก `demo 16 July 2026.mp4` (คำสั่งเต็มดูใน transcript / dev note)
- **ผู้ใช้ v1.5.0** จะได้ update notification อัตโนมัติ (in-app check เจอ tag v1.6.0)
