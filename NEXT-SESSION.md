# NEXT-SESSION — ล้างคำต้องห้าม "ab***t" ออกจาก GitHub

status: active

> **ห้ามพิมพ์คำเต็มลงไฟล์ใด ๆ ใน repo** (รวมไฟล์นี้) — ใช้ "ab***t" แทนเสมอ

① 2026-10-14 · commit ล่าสุดตอนเริ่ม: `effeb57` (ก่อน rewrite) — **ไฟล์นี้อาจเก่า เช็ค `git log --oneline -5` ก่อน** · หลัง rewrite SHA เปลี่ยนทั้งหมดตั้งแต่ `662cb88`

② ขั้นตอน (คุณสมกกเลือก B = แก้ไฟล์ + exe + เขียนประวัติ git ใหม่)

| Step | งาน | ใคร | สถานะ |
|------|-----|-----|-------|
| 0 | สำรวจ + grilling + NEXT-SESSION/WORK-LOG | main | ✅ |
| 1 | backup repo ทั้งก้อน (`git clone --mirror` → `bin\repo-backup-pre-rewrite.git`) | main | ✅ ครบทุก tag |
| 2 | เปลี่ยนชื่อ session ตัวอย่างใน `src/PopupShot.cs` → `design-archive` + วาดชื่อใหม่ทับใน 8 ภาพ (pixel-match err=0, สคริปต์ใน scratchpad replace.ps1) | main | ✅ |
| 3 | `git filter-repo` เขียนประวัติใหม่ (แทนข้อความ + แทน blob ภาพ) แล้วตรวจ `git log -S` ว่าไม่เหลือ | main | ⬜ |
| 4 | force push main + ย้าย tag v1.9.1 / v1.10.0 | main | ⬜ |
| 5 | build zip ใหม่จาก tag v1.9.1 และ v1.10.0 → `gh release upload --clobber` | main | ⬜ |
| 6 | ตรวจจากภายนอก (raw/README/release) + จด memory | main | ⬜ |

หยุดตรงนี้ได้ = หลังขั้น 2 (ยังไม่ได้แตะ GitHub) หรือหลังขั้น 5 (ทุกอย่างบน GitHub สะอาด เหลือแค่ตรวจ)

③ สถานะจริง: ยังไม่เริ่มแก้ · โควตา 5h 33% · 7d 3%
งานเก่าที่ค้าง: สปรินต์ Working blink จบแล้ว (v1.10.0 release แล้ว) เหลือ step 3–7 Goal progress ยังไม่สั่ง — ดู [WORK-LOG-2026-10-14-working-blink-goal-progress.md](WORK-LOG-2026-10-14-working-blink-goal-progress.md)

④ ถัดไป: ขั้น 3 git filter-repo (blob-callback แทนภาพเก่าทุกเวอร์ชัน + replace-text)

⑤ กับดัก
- ห้ามพิมพ์คำเต็มลงไฟล์ใน repo / commit message
- hero GIF `docs/demo-v1.10.0.gif` มีชื่อโปรเจกต์จริงอีกตัว (Dental Freelance...) — คุณสมกกยังไม่ได้ตอบว่าให้เบลอไหม (ไม่ได้เลือก C)
- commit เก่าอาจยังเปิดด้วย SHA ตรงได้บน GitHub จนกว่า GitHub Support จะล้าง cache
- ห้ามใส่ backtick ใน bash heredoc — เขียน md ด้วย Write/Edit เท่านั้น

⑥ log เต็ม: [WORK-LOG-2026-10-14-scrub-word.md](WORK-LOG-2026-10-14-scrub-word.md)
