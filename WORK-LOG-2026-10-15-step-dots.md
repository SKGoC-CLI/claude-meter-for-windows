# WORK-LOG 2026-10-15 — หัวข้อ CURRENT SESSION + จุด step

> grilling 2026-10-15 · ยังไม่เริ่มเขียนโค้ด — รอทำต่อ
> โควตาตอน grill (จาก `history.json`): five_hour **18%**, seven_day **25%** — เหลือเยอะ. % ต่อ step ด้านล่างเป็นการประมาณ

## ของเดิม (โค้ดตอนนี้)
- หัวข้อ section: `"SESSION CONTEXT"` / `"SESSION CONTEXT (n)"` — `src/PopupForm.cs:925` (`DrawContextSection`)
- บรรทัดที่ 2 ของแต่ละ session: `"Step 3/6 · <step>"` ซ้าย, อายุ session ขวา — `src/PopupForm.cs:990-1006` (`DrawContextBlock`)
- ข้อมูล: `record Goal(int Done, int Total, string? Current)` — `src/ContextMonitor.cs:23`; มีแค่ Done/Total/Current ไม่มีสถานะรายข้อ
- เมนู tray `"Session context"` — `src/TrayAppContext.cs:362`

## สิ่งที่ตัดสินแล้ว (คุณสมกกเลือก)
| ข้อ | คำถาม | เลือก |
|-----|-------|-------|
| 1 | ชื่อหัวข้อ | **A** — 1 session = `CURRENT SESSION`, หลายอัน = `CURRENT SESSIONS (3)`; เมนู tray เปลี่ยนตาม (`Current session`) |
| 2 | ตัวเลข 3/6 | **B** — จุด + `3/6` สีจางตัวเล็ก แล้วตามด้วยหัวข้อสั้น: `●●◉○○○ 3/6  Write tests` |
| 3 | step เยอะ | **A** — ≤10 step วาดจุด, >10 กลับไปใช้ `Step 3/14 · หัวข้อ` แบบเดิม |
| 4 | สีส้มกระพริบ | **A** — จุด step ปัจจุบันสีส้ม (Theme.Warning) **กระพริบเฉพาะตอน session = Working**; Waiting/Asking = ส้มนิ่ง |

สีจุด: เสร็จ = เขียวนิ่ง (Theme.Success), กำลังทำ = ส้ม, ยังไม่ทำ = เทา (MutedColor/TrackColor)

## ค่าที่ผมตั้งเอง (ไม่ได้ถาม — แก้ได้)
- **หัวข้อสั้น**: TaskCreate (CLI) ตอนนี้ใช้ `activeForm` ก่อน `subject` (`ContextMonitor.cs:287`) — `activeForm` มักยาวกว่า → สลับเป็น `subject` ก่อน. update_plan ใช้ `content` (≤7 คำอยู่แล้ว) เหมือนเดิม. ยาวเกินยังโดน `…` ตัดท้ายเหมือนเดิม
- จุดคิดจาก Done/Total: index < Done = เขียว, index == Done = ส้ม, ที่เหลือเทา. ถ้า update_plan ทำข้ามลำดับ (`done:[4]` ก่อน 3) จุดจะไม่ตรงตำแหน่งเป๊ะ — ยอมรับ (ponytail: เพิ่ม list สถานะรายข้อใน Goal เมื่อเจอจริง)
- ทุก step เสร็จ (Done == Total) = เขียวหมด ไม่มีส้ม
- กระพริบใช้กลไกเดิม `_workingDotRects` + `IconRenderer.WorkingPulse(1400 * AlertMul)` เหมือนจุดสถานะ Working
- ไม่มี Goal → บรรทัด `~Nk to auto-compact` เหมือนเดิม

## Sprint table
| Step | งาน | ใคร | โควตาประมาณ | หยุดตรงนี้ได้ = ... |
|------|-----|-----|-------------|----------------------|
| 1 | เปลี่ยนหัวข้อ `CURRENT SESSION(S)` + เมนู tray + CONTEXT.md | coder | ~1% | หัวข้อใหม่ใช้งานได้ ยังเป็น `Step 3/6` |
| 2 | วาดจุด step + `3/6` จาง + หัวข้อ, fallback >10, กระพริบเฉพาะ Working | main (UI) | ~3-4% | จุดใช้ได้ ยังใช้ activeForm |
| 3 | TaskCreate ใช้ `subject` ก่อน `activeForm` (+ แก้ self-check บรรทัด ~389) | coder | ~1% (ตัดทิ้งได้) | เสร็จฝั่งข้อมูล |
| 4 | reviewer + build portable + `--popup-shot` ส่งภาพให้คุณสมกก (ตัวอย่างต้องมี session ที่มี Goal 3/6 Working) | reviewer/main | ~2% | รอคุณสมกกดูภาพ |
| 5 | ปรับตามภาพ (คาดว่าหลายรอบ) | main | ~1-2%/รอบ | — |

## กับดัก
- ตัวอย่างใน `PopupShot.cs` ห้ามใช้ชื่อโปรเจกต์/ลูกค้าจริง
- งาน credit line (WORK-LOG-2026-10-15-usage-credits-menu.md ขั้น 7-9) ยังไม่ commit ตอน grill — commit ให้จบก่อนเริ่มงานนี้ จะได้แยก diff
- ส้มกระพริบต้องไม่ทำให้ `_anim` วิ่งตลอดตอน popup ซ่อน (ใช้เงื่อนไข `Visible` เดิม)

## ทำแล้ว 2026-10-15 (บ่าย)
- step 1-4 เสร็จใน commit เดียว: หัวข้อ CURRENT SESSION(S), เมนู Current session, จุด step ใน PopupForm.DrawContextBlock (>10 ขั้น = Step x/y แบบเดิม), TaskCreate ใช้ subject ก่อน, CONTEXT.md
- ภาพ docs/design/popup-step-dots-2026-10-15.png
