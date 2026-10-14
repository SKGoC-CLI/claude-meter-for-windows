# NEXT-SESSION — ล้างคำต้องห้าม "ab***t" ออกจาก GitHub

status: done

> **ห้ามพิมพ์คำเต็มลงไฟล์ใด ๆ ใน repo** (รวมไฟล์นี้) — ใช้ "ab***t" แทนเสมอ

① 2026-10-14 · **ประวัติถูก rewrite แล้ว — SHA ตั้งแต่ `662cb88` เดิมเปลี่ยนหมด** · main ใหม่เริ่ม `a97d099` — ไฟล์นี้อาจเก่า เช็ค `git log --oneline -5` ก่อน

② ขั้นตอน (คุณสมกกเลือก B = แก้ไฟล์ + exe + เขียนประวัติ git ใหม่)

| Step | งาน | ใคร | สถานะ |
|------|-----|-----|-------|
| 0 | สำรวจ + grilling + NEXT-SESSION/WORK-LOG | main | ✅ |
| 1 | backup repo (`bin\repo-backup-pre-rewrite.git`, mirror ครบทุก tag) | main | ✅ |
| 2 | ชื่อตัวอย่างใน `src/PopupShot.cs` → `design-archive` + วาดชื่อใหม่ทับใน 7 ภาพ (pixel-match err=0) | main | ✅ |
| 3 | `git filter-repo` — 0 hit ใน `git log -S` ทุก ref, ภาพเหลือเวอร์ชันเดียว | main | ✅ |
| 4 | force push main (lease = SHA เก่าเต็ม) + tag v1.9.1 / v1.10.0 | main | ✅ |
| 5 | build zip ใหม่จาก tag ทั้งสอง (dll ตรวจแล้วไม่มีคำ) → `gh release upload --clobber` | main | ✅ |
| 6 | ตรวจภายนอก + portable สลับ + memory | main | ✅ |

③ สถานะจริง: GitHub main / tag / release / raw / README สะอาด
**ยังเหลือ**: commit เก่าเปิดด้วย SHA ตรงได้ (เช่น `.../commit/662cb88.patch` ยังเห็นคำ) — ต้องให้ GitHub Support ล้าง
ข้อความพร้อมส่งอยู่ `docs/GITHUB_SUPPORT_PURGE_2026-10-14.md` (untracked ตั้งใจ ไม่ commit)
`docs/PROMO_TODO.md` (modified) + ไฟล์ docs/ untracked อื่นเป็นของเก่า ไม่เกี่ยว

④ ถัดไป: คุณสมกกส่งคำขอ GitHub Support → หลังเขาตอบ ตรวจ `https://github.com/SKGoC-CLI/claude-meter-for-windows/commit/662cb88` ต้อง 404
งานเก่ารอสั่ง: step 3–7 Goal progress — ดู [WORK-LOG-2026-10-14-working-blink-goal-progress.md](WORK-LOG-2026-10-14-working-blink-goal-progress.md)

⑤ กับดัก
- ห้ามพิมพ์คำเต็มลงไฟล์ใน repo / commit message
- clone อื่นบนเครื่อง/เครื่องอื่นที่ยังเป็นประวัติเก่า: ห้าม push — ให้ clone ใหม่ (ไม่งั้นคำกลับมา)
- `git filter-repo` ลบ remote origin + ทำ reset --hard → ต้อง backup ไฟล์ที่แก้ค้างก่อน และ add remote คืน
- `--force-with-lease=main:<sha ย่อ>` ล้มหลัง rewrite (object หาย) → ใช้ SHA เต็มจาก backup
- hero GIF `docs/demo-v1.10.0.gif` มีชื่อโปรเจกต์จริงอีกตัว (Dental Freelance...) — คุณสมกกยังไม่ได้ตัดสิน

⑥ log เต็ม: [WORK-LOG-2026-10-14-scrub-word.md](WORK-LOG-2026-10-14-scrub-word.md)
