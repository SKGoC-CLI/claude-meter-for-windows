# WORK-LOG 2026-10-14 — ไฟกระพริบ Working + Goal progress bar (grilling)

สถานะ: **ออกแบบเสร็จ ยังไม่ได้เขียนโค้ด** — รอคุณสมกกยืนยันแล้วค่อยเริ่ม sprint

## โควตาตอน grilling (จาก `%APPDATA%\ClaudeMeter\history.json`)
- five_hour = **0%**, seven_day = **95%** ⚠️ (เกิน 80%) → แนะนำเริ่ม sprint หลัง 7 วันรีเซ็ต
- % ต่อขั้นด้านล่างเป็นการประมาณ

## ข้อเท็จจริงที่เช็คแล้ว
- จุดสถานะ ● Working/Waiting/Idle มีแล้ว (`src/ContextMonitor.cs` `SessionState`, วาดใน `src/PopupForm.cs` `DrawContextBlock`)
- popup มี timer 1 วิอยู่แล้ว (`_tick`)
- Task tools (TaskCreate/TaskUpdate) เจอใน transcript แค่ 1 session (BAR Tweak Yum Yum) — input: `subject`, `activeForm`, `taskId`, `status` (pending/in_progress/completed). TodoWrite = 0 ไฟล์
- NEXT-SESSION.md ทุก sprint มีตาราง ✅/⬜ + บรรทัด `status:`

## คำตัดสิน
| ข้อ | เรื่อง | เลือก |
|----|------|------|
| 1 | กระพริบที่ไหน | จุดเขียวใน popup **+** ไอคอน tray (A+B) |
| 2 | tray กระพริบแบบไหน | เส้นเขียวบาง 1–2px ที่ขอบ ติด-ดับทุก 1 วิ ตัวเลข % อ่านได้ตลอด (เฉพาะตอนมี session Working) |
| 3 | แหล่ง % | Task ใน transcript ก่อน → ไม่มีใช้ NEXT-SESSION.md (`status: active`) → ไม่มีทั้งคู่ไม่แสดง + เพิ่มกฎใน `~/.claude/CLAUDE.md` ให้ Claude สร้าง Task ทุก sprint |
| 4 | หน้าตา | บรรทัดที่ 3 แถบบางสีฟ้า + `Step 3/6 · 50% · <ชื่อขั้น>` (ตัด … ถ้ายาว) เฉพาะ session ที่มี goal |

## Sprint (ยังไม่เริ่ม)
| Step | งาน | ใคร | % โควตาประมาณ | หยุดตรงนี้ได้ = |
|------|-----|-----|------|------|
| 1 | popup: จุด Working กระพริบ (ใช้ `_tick`) | coder | ~2% | ได้ฟีเจอร์กระพริบใน popup แล้ว |
| 2 | tray: เส้นเขียวขอบกระพริบเมื่อมี session Working | coder | ~3% | กระพริบครบทั้งสองที่ |
| 3 | ContextMonitor: parse TaskCreate/TaskUpdate → done/total + ชื่อขั้น in_progress (`activeForm`) | coder | ~4% | มีข้อมูล goal จาก Task (ยังไม่แสดง) |
| 4 | ContextMonitor: fallback อ่าน NEXT-SESSION.md ใน cwd ของ session (`status: active`, นับ ✅/⬜, ชื่อขั้น ⬜ แรก) | coder | ~3% | — (ตัดได้ถ้าโควตาไม่พอ) |
| 5 | PopupForm: บรรทัดที่ 3 แถบฟ้า dark+light + `--popup-shot` fixture | main | ~5% | เห็น progress บนจอ |
| 6 | reviewer + rebuild portable + ภาพให้คุณสมกกดู | reviewer/main | ~4% | พร้อมใช้ (ยังไม่ release) |
| 7 | เพิ่มกฎ "สร้าง Task ทุก sprint" ใน `~/.claude/CLAUDE.md` | main | ~1% | — |

## กับดักที่คิดไว้ล่วงหน้า
- Task id เป็นเลขเรียงต่อ session; TaskUpdate อาจมี `status: deleted` → ไม่นับ
- session ที่ไม่ใช่ sprint แต่โฟลเดอร์มี NEXT-SESSION.md `status: active` ค้าง → จะโชว์ progress ของ sprint เก่า (ยอมรับได้ เพราะ `status: active` หมายถึง sprint ค้างจริง)
- tray วาดใหม่ทุก 1 วิ → ต้อง Dispose HICON เก่าทุกครั้ง (กัน GDI leak)

## รอบ 2 (grilling) — ปรับไฟกระพริบให้หรูขึ้น

หลัง step 1-2 (dc30df2, 866e55c) คุณสมกกขอให้ถี่ขึ้น + fade in/out และเปลี่ยนไอคอน tray เป็นกรอบรอบ.
โควตาตอนนี้: five_hour 12%, seven_day 96% (เกิน 80%).

| ข้อ | เรื่อง | เลือก |
|----|------|------|
| 1 | จุดใน popup | A. Breathing glow: fade sine รอบละ 1.2 วิ + halo เขียวอ่อนตอนสว่างสุด; timer ~30 fps เฉพาะตอน popup เปิดและมี Working |
| 2 | ไอคอน tray | A. กรอบเต็ม 4 ด้าน 1px วาดใต้ตัวเลข (ตัวเลขชนะตรงที่ทับ) แทนเส้นซ้าย |
| 3 | ความถี่ tray | A. 10 เฟรม/วิ จังหวะ 1.2 วิเท่า popup (fade = alpha ของกรอบ) |

### Sprint รอบ 2
| Step | งาน | ใคร | หยุดตรงนี้ได้ = |
|------|-----|-----|------|
| 2a | popup: breathing glow + anim timer | main | จุด popup หรูแล้ว |
| 2b | tray: กรอบ fade 10 fps (alpha แทน bool) | main | ครบรอบ 2 |

รอบ 2 เสร็จ commit `f169ec3` (CPU 0.23 วิ/20 วิ, GDI คงที่ 31).

## รอบ 3 (grilling) — taskbar เป็นตัวเลข cross fade, popup เป็น glow แผ่ออก ช้าลง

| ข้อ | เรื่อง | เลือก |
|----|------|------|
| 1 | taskbar | A. เอากรอบออก ตัวเลข cross-fade สีเดิม ↔ เขียว Working (โหมดตัวเลขเดียว: ขาว ↔ เขียว) |
| 2 | จุด popup | A. จุดนิ่ง + แสงแผ่ออกจากจุดแล้วจาง (ease-out), รอบ 1.2 → **1.6 วิ** ใช้ร่วมกับ taskbar |

ทำเลยในรอบเดียว: `IconRenderer.WorkingPhase()` (ramp) + `WorkingPulse()` (cosine) รอบ 1.6 วิ, `Mix()` ผสมสีไปทางเขียว,
tray ปัดค่า mix เป็น 20 ขั้นเพื่อข้ามเฟรมที่เหมือนเดิม. วัดได้ CPU 0.22 วิ/20 วิ, GDI 31 คงที่. commit `9efac69`.

## รอบ 4 — ยังไม่ถูกใจ: เอา glow ออก, ตัวเลขจางหายแล้วกลับมา

| ข้อ | เรื่อง | เลือก |
|----|------|------|
| popup | เอา glow ออก จุด fade 100% ↔ 25% รอบละ **1.4 วิ** | A |
| taskbar | เลิกเปลี่ยนเป็นเขียว ตัวเลข (สีเดิม) fade หาย **0%** แล้วกลับ รอบละ **1.2 วิ** | A |

`IconRenderer.WorkingPulse(periodMs)` รับคาบเอง, `Fade()` แทน `Mix()`, ลบ `WorkingPhase` + สี `Working`.
หมายเหตุ: โหมด 2 แถวตอนจางสุดไอคอนว่างเปล่าทั้งช่อง (ตั้งใจ); โหมดตัวเลขเดียวแถบ % ยังอยู่.
CPU 0.23 วิ/20 วิ, GDI 31 คงที่.

## รอบ 5 (grilling) — popup ผ่านแล้ว, taskbar ตอนจางหาย 0% "ดูตลกมาก"

โควตาจากมิเตอร์ตอนเริ่ม: 5h 0% · 7d **98%** (⚠️ เกิน 80% — ทำเฉพาะงานเบา)

| ข้อ | เรื่อง | เลือก |
|----|------|------|
| popup | จุด fade 1.4 วิ (รอบ 4) — คุณสมกกบอก "ดูดีละ" | คงไว้ |
| 1 | taskbar | **B.** ตัวเลขจางเหลือ ~45% (ไม่หายหมด) รอบละ **2 วิ** (ผมแนะนำ A = เอาออก แต่คุณสมกกเลือก B) |

Sprint รอบ 5:

| ขั้น | งาน | ใคร | หยุดตรงนี้ได้ = |
|-----|-----|-----|----------------|
| 5a | TrayAppContext: floor 45% + คาบ 2000 ms, build, สลับ portable | main (แก้ 1–2 บรรทัด ไม่คุ้มส่ง coder) | ได้ — portable ใหม่รันอยู่ รอดูของจริง |
| 5b | คุณสมกกดูของจริง → ปรับ/ผ่าน | คุณสมกก | ได้ |
| 5c | reviewer ตรวจ dc30df2..HEAD | reviewer | ทิ้งได้ถ้าโควตาไม่พอ — ทำหลัง 7d รีเซ็ต |

ประเมินโควตา (เดา ไม่ใช่วัด): 5a ~1–2% ของ 5h.
5a เสร็จ commit `0589b33` (floor 45%, คาบ 2 วิ, portable สลับแล้ว).

## รอบ 6 (grilling) — สถานะ Asking (ส้มกระพริบ) + เมนู Session alerts

โควตาจากมิเตอร์: 5h 4% · 7d **98%** ⚠️

ข้อเท็จจริงที่หาเจอก่อนถาม: `ContextMonitor.IsTurnEnd` นับ AskUserQuestion/ExitPlanMode เป็น turn จบ
→ ตอนนี้ = **Waiting ส้มนิ่ง** ปนกับ "ตอบจบแล้ว". permission prompt ไม่อยู่ใน transcript → ตรวจไม่ได้ (ยังเขียว).
ตัวเลข tray ใช้ส้ม `#ffa940` สำหรับโควตา 70–90% อยู่แล้ว.

| ข้อ | เรื่อง | เลือก |
|----|------|------|
| 1 | ส้มกระพริบใช้กับอะไร | **A.** state ใหม่ **Asking** = คำถามค้าง (AskUserQuestion/ExitPlanMode) เท่านั้น; ตอบจบยังเป็น Waiting ส้มนิ่ง |
| 2 | taskbar ตอน Asking | **A.** พื้นหลังสี่เหลี่ยมส้ม fade 0 ↔ ~60% รอบ ~1 วิ, ตัวเลขไม่จาง; Asking ชนะ Working |
| 3 | เมนู "Session alerts" (submenu กลุ่ม SESSION CONTEXT, เก็บใน settings.json) | **A+B+C**: ความเร็ว ช้า(×1.5)/ปกติ/เร็ว(×0.6) ใช้ทั้ง tray+popup · toggle tray Working · toggle tray Asking. **ไม่เอา** balloon แจ้งเตือน |

Sprint รอบ 6 (ยังไม่ลงมือ — รอคุณสมกกยืนยัน):

| ขั้น | งาน | ใคร | หยุดตรงนี้ได้ = |
|-----|-----|-----|----------------|
| 6a | `SessionState.Asking` ใน ContextMonitor (IsTurnEnd คืนค่าแยก) + popup จุดส้ม fade | coder | ได้ — popup แยก Asking แล้ว |
| 6b | tray: พื้นหลังส้ม fade ตอน Asking (Asking > Working) | coder | ได้ |
| 6c | settings 3 ค่า + submenu Session alerts | coder | ได้ |
| 6d | build + สลับ portable + CONTEXT.md + commit | main | ได้ — รอคุณสมกกดูจริง |
| 6e | reviewer ตรวจ dc30df2..HEAD | reviewer | ทิ้งได้ — ทำหลัง 7d รีเซ็ต |

ประเมินโควตา (เดา): 6a–6d รวม ~8–15% ของ 5h. 7d 98% อาจชนเพดานกลางทาง — 6c ตัดทิ้งได้ก่อน.

6a-6d เสร็จ commit `4d0d554`. ทดสอบจริง: คุณสมกกบอกส้มทึบไป ตัวเลขจม → ลด alpha พื้นหลัง 0.6 → **0.35**.


ส้ม 35% บน taskbar มืดกลายเป็นน้ำตาลขุ่น มองไม่เห็น → คุณสมกกเลือก **ส้มทึบ 100% + ตัวเลขดำ สลับกับไอคอนปกติ รอบละ 1 วิ** (ไม่ fade). อ่านสถานะ session ทุก 5 → **2 วิ** (คำถามที่ตอบเร็วจะได้ทันเห็น). ภาพ: `docs/design/asking-solid-preview.png`.


ส้มทึบสลับทันที: คุณสมกกไม่เอา ชอบแบบเข้มๆจางๆ → กลับเป็น fade พื้นหลังส้ม **0 ↔ 45%** รอบ 1 วิ ตัวเลขสีเดิม (อ่านสถานะทุก 2 วิ คงไว้). รอคุณสมกกดูจริง.

## 2026-10-15 — Goal progress ขั้น 3–7 (ทำแล้ว ยังไม่ release)

โควตาจากมิเตอร์ตอนเริ่ม: 5h 6% · 7d 7%

- **เปลี่ยนจากแบบเดิม**: แหล่งหลักคือ `mcp__ccd_turn__update_plan` (แอป desktop บังคับใช้ทุกงานหลายขั้น) — TaskCreate จริง ๆ เจอแค่ 1 session ใน 20 วัน. รองรับทั้งคู่. ขั้น 7 (กฎใน CLAUDE.md) จึง **ไม่ต้องทำ**
- `80e41b9` ขั้น 3 อ่านแผนจาก transcript แบบต่อท้าย (cache offset ต่อไฟล์) + `--goal-selftest`
- `5573de3` ขั้น 4 fallback NEXT-SESSION.md
- `7713b91` ขั้น 5 popup บรรทัด 3 (แถบ Accent + ตัด … ถ้ายาว) + fixture popup-shot
- `ccef0bd` แก้ตาม reviewer: รายการที่เสร็จครบซ่อน (เดิมค้าง "Done N/N" ทั้ง session และบัง NEXT-SESSION), อ่านทีละ 1 MB (ไฟล์ 37 MB ไม่โหลดทั้งก้อน), อ่านพลาด → แค่ไม่มี goal ไม่ทำแถว session หาย
- reviewer ข้อที่ไม่แก้ (ความเสี่ยงต่ำ): ตาราง ✅/⬜ อื่นใน NEXT-SESSION.md (เช่นตารางกับดัก) จะถูกนับรวม · Task id สมมติเรียง 1,2,3 · cache ไม่ล้างตลอดอายุ process (โตช้ามาก)
- portable สลับแล้ว (backup `bin\portable-v1.10.0.0-backup\`) · วัดจริง: private mem 70 MB, CPU 0.53 วิ/20 วิ (เดิม 0.23 — เพิ่มจากการอ่าน transcript ทุก 2 วิ; ยังเบา)

