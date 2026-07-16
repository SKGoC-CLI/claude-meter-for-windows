# Dev Note — Multi-session context + token/capacity readout

**วันที่:** 2026-07-15
**ไฟล์ที่แก้:** `ContextMonitor.cs`, `PopupForm.cs`, `AppSettings.cs`, `TrayAppContext.cs`

## สิ่งที่เพิ่ม
1. **โชว์ tokens current / capacity** ในแต่ละบล็อก context เช่น `169k / 1.0M`, `144k / 200k` — เห็น max ชัดๆ
2. **รองรับหลาย session พร้อมกัน** — ทุก transcript ที่ถูกเขียนภายใน 10 นาที = 1 session, แสดงบล็อกละอัน (header เป็น `SESSION CONTEXT (n)`)
3. **Option ใหม่**:
   - `Context: max shown` — จำนวนบล็อกสูงสุดที่แสดง (1/2/3/5, default 3)
   - `Context: sort by` — Last active (default) / Name A–Z / Context high→low

## รายละเอียดโค้ด
- `ContextMonitor.GetActive(maxIdle, max, sort)` → คืน `IReadOnlyList<SessionContext>`
  - แยก logic parse ต่อไฟล์เป็น `Parse(FileInfo)`
  - `SessionContext` เพิ่มฟิลด์ `LastActive` (= file LastWriteTime) ไว้ใช้ sort
  - cap ที่ 12 ไฟล์ก่อน parse กัน cost บานปลาย
- `PopupForm`: `SessionCtx` (เดี่ยว) → `Sessions` (list); `DrawContextRow` → `DrawContextSection` + `DrawContextBlock`; helper `FmtTokens` (169k / 1.0M)
- `AppSettings`: `MaxContextSessions=3`, `ContextSort="active"`

## หมายเหตุ
- แต่ละ block สูง ~46px (2 บรรทัดข้อความ + bar); header ครั้งเดียวด้านบน
- ยังไม่ dedup ตาม project — resume session เก่าจะหยุดเขียน เลยหลุดจากกรอบ 10 นาทีเอง
- build ผ่าน 0 error; ยังไม่ commit (รอ approve หน้าตา)
