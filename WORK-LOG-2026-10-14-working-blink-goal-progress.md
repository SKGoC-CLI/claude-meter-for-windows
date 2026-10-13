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
