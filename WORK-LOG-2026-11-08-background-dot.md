# WORK-LOG 2026-11-08 — จุดเขียวฟ้ากระพริบ = session มีงานเบื้องหลัง

> ที่มา: `/ggg` — คุณสมกกเห็น session GPU Lab มี background task (Monitor เฝ้า `queue.log`)
> แต่จุดหน้าชื่อใน CURRENT SESSIONS ขึ้นเหลือง/เทา ดูไม่ออกว่ายังมีงานวิ่งอยู่

## โควตาตอนคุย (จาก ClaudeMeter history.json)
- 5h: **49%** (00:05) · 7d: **44%** — เหลือพอสำหรับงานนี้ (ประมาณ 10–15% ของ 5h, เป็นการเดา)

## ของเดิม (ตรวจในโค้ดแล้ว)
- `src/ContextMonitor.cs:20` `enum SessionState { Working, Asking, Waiting, Idle }`
- `src/ContextMonitor.cs:115` ตัดสินสถานะจากบรรทัดท้าย transcript; `DropAfter` 30 นาทีลบแถว
- `src/PopupForm.cs:1010-1034` วาดจุด: Working = `Theme.Success` กระพริบ 1.4 s, Asking = Warning กระพริบ 1 s,
  Waiting = Warning นิ่ง, Idle = เทา; `_workingDotRects` + `_anim` repaint เฉพาะจุด

## หลักฐานใน transcript (GPU Lab `6a260162-…jsonl`)
- เริ่มงาน: `tool_use` ชื่อ `Monitor` (input มี `description`, `timeout_ms`) → `toolUseResult`
  `{"taskId":"b5utz2aet","timeoutMs":600000,"persistent":false}`
- Bash `run_in_background:true` / Agent background ก็คืน taskId ทำนองเดียวกัน (ยังต้องยืนยันชื่อฟิลด์ของ Bash ตอนทำ)
- จบงาน: user line มี `<task-notification>` … `<task-id>X</task-id>` … `<status>completed|failed|killed</status>`
  หรือ `TaskStop` ที่ระบุ task id, หรือ Monitor ครบ `timeoutMs` นับจาก timestamp ที่เริ่ม

## ข้อตัดสิน (คุณสมกกเลือกครบ 3 ข้อ)
1. **A — เขียวฟ้าเฉพาะตอนพักเทิร์น**: ลำดับ Working (เขียวกระพริบ) > Asking (เหลืองกระพริบ)
   > **Background (เขียวฟ้ากระพริบ)** > Waiting (เหลือง) > Idle (เทา)
2. **A — ต่ออายุมีเพดาน**: session ที่มีงานเบื้องหลังไม่โดน DropAfter 30 นาที จนถึง timeout ของงาน
   (Monitor) หรือสูงสุด **3 ชม.** สำหรับงานที่ไม่มี timeout แล้วถือว่างานจบ (กันค้างถ้า app ถูกปิด)
3. **A — จุด + ข้อความสั้น**: บรรทัดที่ 2 เติม `⟳ <description ของงาน>` (หลายงาน = `⟳ 2 bg: <ตัวแรก>`)
   ไม่แตะไอคอน tray

## ออกแบบ (ร่าง)
- `SessionState.Background` ใหม่ + `SessionContext` มี `BackgroundTasks` (จำนวน + ชื่องานแรก)
- ไล่ทั้งไฟล์แบบ incremental เหมือน `ReadPlan` (ตำแหน่ง `Pos` ต่อไฟล์) เก็บ dict taskId → (ชื่อ, เริ่ม, deadline)
  เพิ่มเมื่อเจอ result ที่มี taskId, ลบเมื่อเจอ task-notification สถานะจบ / TaskStop / เลย deadline
- สีเขียวฟ้า: เพิ่ม `Theme.Info` (teal ~ #2DD4BF โทนมืด / #0D9488 โทนสว่าง) ต้องเช็ก contrast กับทุกธีม
- กระพริบจังหวะช้ากว่า Working (เช่น 2 s) จะได้แยกด้วยตาจากเขียว
- เพิ่ม selftest ใน `--goal-selftest` (หรือ `--bg-selftest`) จำลอง start → notification → หาย, และ timeout

## Sprint table
| ขั้น | งาน | ใครทำ | หยุดตรงนี้ได้ = |
|---|---|---|---|
| 1 | ยืนยันรูปแบบ taskId ของ Bash/Agent background จาก transcript จริง | main model | ได้ — ยังไม่แตะโค้ด |
| 2 | `ContextMonitor`: ติดตามงานเบื้องหลัง + `SessionState.Background` + ต่ออายุ DropAfter + selftest | coder | ได้ — selftest ผ่าน แต่ UI ยังไม่เปลี่ยน |
| 3 | `PopupForm` + `Theme`: จุดเขียวฟ้ากระพริบ + ข้อความ `⟳` บรรทัด 2 | coder | ได้ — build ผ่าน |
| 4 | รีวิว diff | reviewer | ได้ — ยังไม่ commit |
| 5 | รันแอปจริง + screenshot ให้คุณสมกกดู (ทุกธีม) | main model | ได้ — รอรอบแก้สี |
| 6 | commit (+ release ถ้าคุณสมกกสั่ง) | main model | จบ |

ขั้นที่ตัดได้ถ้าโควตาตึง: selftest ส่วน timeout (ขั้น 2) และ screenshot ทุกธีม (ขั้น 5 เหลือธีมเดียว)

## /sss — spec + tickets (2026-11-08)
- ยืนยันรูปแบบ taskId แล้ว (ขั้น 1 ของ Sprint table เสร็จ): Monitor `taskId`+`timeoutMs`, Bash `backgroundTaskId`,
  Agent `isAsync`/`agentId`; จบด้วย `<status>completed|failed|killed</status>` หรือ `TaskStop {task_id}`
- Tracker = Markdown ในเครื่อง (repo GitHub เป็นสาธารณะ) — กติกาอยู่ใน `docs/agents/issue-tracker.md`
- Spec: `.scratch/background-dot/spec.md`
- Tickets (`.scratch/background-dot/issues/`):
  | # | ticket | บล็อกโดย |
  |---|---|---|
  | 01 | Monitor tracer — จุดเขียวฟ้ากระพริบ + สีธีม + selftest | — |
  | 02 | งานเบื้องหลังครบทุกแบบ + failed/killed/TaskStop + หมดเวลา (timeoutMs / 3 ชม.) | 01 |
  | 03 | แถวไม่หายที่ 30 นาทีถ้ายังมีงาน | 01 |
  | 04 | ข้อความ `⟳` บรรทัด 2 | 01 |
- ปิดท้าย: reviewer → รันแอป screenshot มืด/สว่าง → commit
