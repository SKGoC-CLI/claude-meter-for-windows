# NEXT-SESSION — Working blink + Goal progress sprint

status: active

① 2026-10-14 · commit ล่าสุดตอนเริ่ม: `ebb8a25` — **ไฟล์นี้อาจเก่า เช็ค `git log --oneline -5` ก่อน**

② ขั้นตอน (คุณสมกกสั่งทำแค่ step 1–2 รอบนี้ เพราะโควตา 7 วัน 95%)

| Step | งาน | ใคร | สถานะ |
|------|-----|-----|-------|
| 0 | grilling + CONTEXT.md + WORK-LOG | main | ✅ |
| 1 | popup: จุด Working กระพริบ | main | ✅ |
| 2 | tray: เส้นเขียวขอบกระพริบเมื่อมี session Working | main | ⬜ |
| 3–7 | Goal progress (ดู WORK-LOG) | — | ⬜ ยังไม่สั่ง |

③ สถานะจริง: ยังไม่เริ่มโค้ด. `docs/PROMO_TODO.md` (modified) + ไฟล์ docs/ untracked เป็นของเก่า **ไม่เกี่ยว อย่า commit รวม**.

④ ถัดไป: step 1

⑤ กับดัก
- ห้ามใส่ backtick ใน bash heredoc — เขียน md ด้วย Write/Edit เท่านั้น
- tray วาดใหม่ทุก 1 วิ → ต้อง Dispose/DestroyIcon ของเก่า (กัน GDI leak)

⑥ log เต็ม: [WORK-LOG-2026-10-14-working-blink-goal-progress.md](WORK-LOG-2026-10-14-working-blink-goal-progress.md)
