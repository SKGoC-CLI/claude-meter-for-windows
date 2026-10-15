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

## grilling รอบ 2 (2026-10-15 15:50) — "จุดดูซ้ำกับอันบน"
โควตา (จาก `history.json`): five_hour **48%**, seven_day **29%** — เหลือพอ. % ต่อ step เป็นการประมาณ

ปัญหา: จุด step กลม + เขียว/ส้ม เหมือนจุดสถานะ session หน้าชื่อ → อ่านเป็นสัญญาณเดียวกัน (ภาพจริงจากคุณสมกก 15:46)

| ข้อ | คำถาม | เลือก |
|-----|-------|-------|
| 1 | ทรง | **A** — แท่งสั้นต่อกันเป็นแถว ▬▬▬▭▭▭ (แบบแถบ story) ไม่ใช่วงกลม |
| 2 | สี | **A** — ไม่มีสี: เสร็จ = เทาอ่อน (`MutedColor`), กำลังทำ = ขาวสว่าง (`LabelColor`, กระพริบเฉพาะ Working), ยังไม่ทำ = เทาเข้ม (`TrackColor`). เขียว/ส้มเหลือไว้ให้จุดสถานะเท่านั้น |

ที่เหลือเหมือนเดิม: `5/6` จาง + ชื่อขั้น, >10 ขั้น = `Step x/y · ...`, ธีมสว่างใช้สีชุดเดียวกัน (Label = เข้ม)

| Step | งาน | ใคร | โควตาประมาณ | หยุดตรงนี้ได้ = ... |
|------|-----|-----|-------------|----------------------|
| 6 | วาดแท่งแทนจุด + สีขาว/เทา + CONTEXT.md | main | ~1-2% | แท่งใช้ได้ |
| 7 | build portable + popup-shot ให้คุณสมกกดู + commit | main | ~1% | รอดูภาพ |

## grilling รอบ 3 (2026-10-15 16:05) — "หลัง 6/6 เอาแค่หัวข้อสั้นๆ"
ต้นเหตุ: session ที่ wrap_up แล้วใช้ตาราง NEXT-SESSION.md แทน และเอาช่อง "งาน" มาทั้งช่อง
| ข้อ | คำถาม | เลือก |
|-----|-------|-------|
| 1 | ตัดยังไง | **A** — ตัดที่เครื่องหมายแรก `(` `—` `–` `:` `·` `,` ใช้กับทุกแหล่ง (update_plan / TaskCreate / NEXT-SESSION); ยังยาวเกิน = `…` เหมือนเดิม |
ทำแล้ว: `ContextMonitor.ShortStep` + เคสใน `--goal-selftest`

## release v1.12.0 (2026-10-15 ~16:30)
คุณสมกก: "ดูดีมาก ทำ release ได้เลย ถ้ารูปไหนมีข้อมูลฉันให้ mockup เป็นภาษาอังกฤษเสมอ" (จำไว้ใน memory แล้ว)
- grilling: รูป release → คุณสมกกอัดคลิปเอง `docs/Recording 2026-10-15 161731.mp4` (1010x938, 37 วิ)
- ทับชื่อจริง "Oliver; Alinity negative query" → "api-server" ทั้งคลิป: overlay PNG ทำด้วย GDI+ (Segoe UI Bold 16.25px, สี #8c8c8c พื้น #1e1e1e, กล่อง 176,476 460x28) ปรับขนาดตัวอักษรให้กว้างเท่าแถว "web · Opus · High"; สคริปต์อยู่ scratchpad (ไม่ commit)
- scale 1280 + ลายน้ำ v1.12.0 y=h-th-70 → `docs/demo-v1.12.0.gif` 3.9 MB; ตรวจเฟรม 1/18/35 วิ ไม่มีชื่อจริง
- `PopupShot.cs` ชื่อตัวอย่าง: CORSAIR XENEON EDGE Claude App → home-dashboard, design-archive → api-server; `docs/popup-v1.12.0.png`
- csproj 1.12.0, CHANGELOG, docs/RELEASE_NOTES_v1.12.0.md, README (hero GIF/ภาพ + ชื่อเมนู Current session)
- reviewer: ไม่มี blocker; แก้ ShortStep ให้คืนข้อความเดิมถ้าตัดแล้วว่าง

## ทำแล้ว 2026-10-15 (บ่าย)
- step 1-4 เสร็จใน commit เดียว: หัวข้อ CURRENT SESSION(S), เมนู Current session, จุด step ใน PopupForm.DrawContextBlock (>10 ขั้น = Step x/y แบบเดิม), TaskCreate ใช้ subject ก่อน, CONTEXT.md
- ภาพ docs/design/popup-step-dots-2026-10-15.png
