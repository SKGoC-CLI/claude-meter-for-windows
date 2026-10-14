# NEXT-SESSION — Goal progress (แถบความคืบหน้าใน SESSION CONTEXT)

status: done

> **ห้ามพิมพ์คำต้องห้าม "ab***t" เต็ม ๆ ลงไฟล์ใด ๆ ใน repo** (จากสปรินต์ก่อน)

① 2026-10-15 · เริ่มจาก HEAD `0e09cc2` — ไฟล์นี้อาจเก่า เช็ค `git log --oneline -5` ก่อน

② ขั้นตอน (ต่อจาก WORK-LOG 2026-10-14 ขั้น 3–7; ขั้น 1–2 ไฟกระพริบ ออกไปกับ v1.10.0 แล้ว)

| Step | งาน | ใคร | สถานะ |
|------|-----|-----|-------|
| 3 | ContextMonitor: อ่านแผนจาก transcript — `mcp__ccd_turn__update_plan` (แอป desktop) + TaskCreate/TaskUpdate (CLI) → done/total + ชื่อขั้นที่กำลังทำ, อ่านแบบต่อท้าย (cache offset ต่อไฟล์ เพราะ transcript ใหญ่ถึง 37 MB) | main | ✅ |
| 4 | fallback: NEXT-SESSION.md ใน cwd (`status: active`, นับ ✅/⬜) | main | ✅ |
| 5 | PopupForm: บรรทัดที่ 3 แถบฟ้า + `Step 3/6 · 50% · ชื่อขั้น` + fixture `--popup-shot` | main | ✅ |
| 6 | reviewer + rebuild portable + ภาพให้คุณสมกกดู | reviewer/main | ✅ |
| 7 | ~~กฎ "สร้าง Task ทุก sprint" ใน CLAUDE.md~~ → **ไม่ต้องทำ**: แอป desktop บังคับ update_plan ทุกงานหลายขั้นอยู่แล้ว | — | ✅ |

③ สถานะจริง: ครบทุกขั้น, reviewer ตรวจแล้ว + แก้ 3 ข้อ (`ccef0bd`), portable ใหม่รันอยู่ — **ยังไม่ release** (ยังเป็น v1.10.0) · `ClaudeMeter.exe --goal-selftest` ผ่าน + อ่าน session จริงได้ (ยังไม่แสดงใน popup) · ไฟล์ docs/ untracked + `docs/PROMO_TODO.md` เป็นของเก่า ไม่เกี่ยว

④ ถัดไป: คุณสมกกดูของจริงใน popup → ปรับหน้าตา หรือออก release v1.11.0 (ต้องถามก่อน release)

⑤ กับดัก
- transcript มี `prompt_snapshot` ที่มีคำว่า update_plan/TaskCreate อยู่ในข้อความ system prompt → ต้องจับเฉพาะ `"type":"tool_use"` ที่ `name` ตรง ไม่ใช่ค้นคำเฉย ๆ
- TaskCreate id = ลำดับ 1,2,3… ต่อ session; TaskUpdate `status: deleted` ไม่นับ
- update_plan: `items` = แทนทั้งชุด, `advance` = in_progress→completed แล้วตัว pending ถัดไป→in_progress, `done:[n]`/`start:n` เลข 1-based; `wrap_up` ติ๊กทุกขั้นเสร็จ
- tray วาดใหม่ทุก 1 วิ → Dispose HICON เก่า (มีอยู่แล้ว ห้ามทำพัง)

⑥ log เต็ม: [WORK-LOG-2026-10-14-working-blink-goal-progress.md](WORK-LOG-2026-10-14-working-blink-goal-progress.md)
