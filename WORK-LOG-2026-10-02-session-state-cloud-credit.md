# WORK-LOG 2026-10-02 — Session state + Cloud credit (grilling)

โจทย์จากคุณสมกก: (1) SESSION CONTEXT บอกได้ว่า session ไหน active / ไม่ active
(2) แสดง cloud session credit ($98 of $100 left, หมดอายุ 5 พ.ย.)

## ข้อเท็จจริงที่ตรวจแล้ว

- `GET /api/oauth/usage` (endpoint เดิมของมิเตอร์) **ส่ง cloud credit มาแล้ว** ในฟิลด์ชื่อรหัส
  `iguana_necktie`:
  `{"utilization":1.49,"resets_at":"2026-11-05T07:59:00+00:00","limit_dollars":100,"used_dollars":1.49,"remaining_dollars":98.51}`
  ตรงกับหน้าเว็บทุกตัวเลข (07:59 UTC = 14:59 +7). ไม่อยู่ใน array `limits` → parser ปัจจุบัน
  (`UsageClient.cs` modern path) ข้ามไป. ตรวจด้วย console ชั่วคราวใน scratchpad ที่เรียก
  `DesktopCredentialStore.TryRead()` — ไม่ได้ refresh token.
- SESSION CONTEXT ปัจจุบัน = transcript ที่เขียนภายใน 10 นาที (`TrayAppContext.cs:547`
  `ContextMonitor.GetActive(TimeSpan.FromMinutes(10), …)`). "1h old" = อายุตั้งแต่เริ่ม ไม่ใช่สถานะ.
- โควตาตอน grilling (จาก meter `history.json`): **five_hour 62%**, **seven_day 32%**.

## ข้อตัดสิน (grilling)

| # | คำถาม | คำตอบ |
|---|-------|-------|
| 1 | active หมายถึงอะไร | A. 3 สถานะ Working / Waiting / Idle |
| 2 | แสดงยังไง | A. จุดสี ● เขียว/ส้ม/เทา หน้าชื่อ, แถว Idle จาง |
| 3 | เกณฑ์เวลา | A. Waiting ≤10 นาที → Idle 10–30 นาที → หายหลัง 30 นาที; Working ไม่หมดเวลา; AskUserQuestion ค้าง = Waiting |
| 4 | cloud credit แสดงยังไง | A. แถวใหม่ `Cloud credit: $98.51 left` · `expires in 34d (5 Nov)` แถบ = ส่วนที่ใช้ไป; อยู่ใน Show limits; ไม่ขึ้น tray, ไม่มี ETA, ไม่มีกราฟ; ไม่มีฟิลด์ = ไม่โชว์ |
| 5 | หาเครดิตใน API ยังไง | A. จับชื่อ `iguana_necktie` ตัวเดียว (เปลี่ยนชื่อ = แถวหายเงียบ แก้ 1 บรรทัด) |

ไม่ทำ (YAGNI): sort แบบ "Waiting ขึ้นบน" — ใช้ `ContextSort` เดิม ค่า default เรียงตามเขียนล่าสุดอยู่แล้ว.

## Sprint

ประมาณการ % ต่อ step เป็น **การเดา** (five_hour เหลือ ~38%).

| Step | งาน | ใครทำ | ~% 5h | หยุดตรงนี้ได้ = |
|------|-----|-------|-------|-----------------|
| 1 | `ContextMonitor`: อ่านบรรทัดท้าย transcript → state + ขยายหน้าต่างเป็น 30 นาที | coder | 6 | ได้ state ในข้อมูล ยังไม่โชว์ |
| 2 | `PopupForm`: จุดสี + Idle จาง (dark+light) | main (งานภาพ) | 8 | session state ใช้ได้ครบ |
| 3 | `UsageClient`: parse `iguana_necktie` → แถว Cloud credit | coder | 4 | ได้ข้อมูล ยังไม่โชว์ |
| 4 | `PopupForm`: วาดแถว Cloud credit + toggle ใน Show limits | main | 6 | ฟีเจอร์ครบ |
| 5 | `--popup-shot` ภาพ dark+light ให้คุณสมกกดู | main | 3 | — |
| 6 | reviewer รีวิว diff + rebuild portable | reviewer | 4 | พร้อมใช้ |

ตัดได้ถ้าโควตาตึง: step 5 (เปลี่ยนเป็นเปิดแอปจริงดูเอง) — step อื่นตัดไม่ได้.

## สถานะ

- grilling เสร็จ, CONTEXT.md อัปเดตแล้ว (Session state, Cloud credit, แก้ข้อ Balance)
- โค้ดเสร็จ: `872a5be` (ฟีเจอร์) + `a862368` (แก้ตาม review). build 0 error 0 warning.
- reviewer เจอ 9 ข้อ แก้ 5:
  (1) ฟิลด์ codename เปลี่ยนชนิดข้อมูล → เดิมจะทำ poll ทั้งก้อนพัง ตอนนี้หายแค่แถว cloud
  (2) บรรทัดท้ายยาวเกิน 128 KB → อ่านซ้ำ 2 MB
  (3) ไฟล์เล็กไม่ข้ามบรรทัดแรก
  (4) cloud ที่ไม่มีวันหมดอายุไม่โชว์ "$used / $limit" ซ้ำ
  (7) content รูปร่างแปลกไม่ทำ timestamp หาย
  ไม่แก้: tooltip/log โชว์ "Cloud credit 1%" (= ใช้ไป), ประวัติเก็บ series cloud_credit ที่ไม่มีใครใช้,
  จุดสีค้างได้จนถึง poll ถัดไป (≤5 นาที).
- เพิ่มเอง: **ตัด transcript ของ subagent ออกจากลิสต์** — มันจบด้วย tool call เสมอ จะเขียว Working ค้าง 30 นาที.
- ทดสอบจริง: เรียก ContextMonitor.GetActive ผ่าน reflection → session นี้ = Working, session อื่นเงียบ >30 นาทีหายถูกต้อง.
  portable ใหม่รันแล้ว log ขึ้น Cloud credit 1%.
- ถอยกลับ: copy `bin\portable-v1.9.1-backup\ClaudeMeter.exe` ทับ `portable\ClaudeMeter.exe` (ปิดแอปก่อน)
  หรือ revert สอง commit `a862368` แล้ว `872a5be`.
- อุบัติเหตุ: backtick ใน bash heredoc ทำให้ shell รัน `git revert` จริง (ไม่ได้ push) — กู้ด้วย `git reset --keep a862368`.
