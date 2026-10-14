# NEXT-SESSION — เตรียม release v1.11.0 (Step progress)

status: done

> **ห้ามพิมพ์คำต้องห้าม "ab***t" เต็ม ๆ ลงไฟล์ใด ๆ ใน repo** · ห้ามชื่อโปรเจกต์จริงหลุดในภาพ/GIF ที่ขึ้น GitHub

① 2026-10-15 · เริ่มจาก HEAD `65f0b96` — ไฟล์นี้อาจเก่า เช็ค `git log --oneline -5` ก่อน

② ขั้นตอน (คุณสมกกเลือก: GIF จากคลิป 02:24 + ปิดชื่อ, ทำครบ + Draft release)

| Step | งาน | ใคร | สถานะ |
|------|-----|-----|-------|
| 1 | version 1.11.0 + CHANGELOG + docs/RELEASE_NOTES_v1.11.0.md | main | ✅ |
| 2 | docs/popup-v1.11.0.png จาก `--popup-shot` | main | ✅ |
| 3 | docs/demo-v1.11.0.gif จาก `docs/Recording 2026-10-15 022419.mp4` ช่วงที่ลำดับแถวนิ่ง + วาดทับชื่อ (Oracle Exam Website → exam-site, KKS Card Management App → card-manager) + ลายน้ำ (skill demo-gif) | main | ✅ |
| 4 | README ชี้ภาพ/GIF ใหม่ + อธิบาย Step progress | main | ✅ |
| 5 | reviewer ตรวจ `7713b91..HEAD` + build zip portable | reviewer/main | ✅ |
| 6 | commit + push + `gh release create v1.11.0 --draft` แนบ zip | main | ✅ |

③ สถานะจริง: v1.11.0 ครบ — main `c97fd94` push แล้ว, release v1.11.0 (published) มี ClaudeMeter-portable.zip (exe เดียว 63 MB), portable รัน v1.11.0.0 · docs/Recording*.mp4 + demo-v1.11.0-wm.mp4 untracked ตั้งใจไม่ commit

④ ถัดไป: v1.11.0 PUBLISHED แล้ว (Latest, tag `1b003f4`) — ไม่มีงานค้างในสปรินต์นี้

⑤ กับดัก
- คลิป 02:24: ลำดับ session สลับกลางคลิป → ใช้เฉพาะช่วงต้นที่ลำดับนิ่ง ไม่งั้นต้องไล่ปิดชื่อตามตำแหน่ง
- ห้าม `gh release create` แบบไม่ใส่ `--draft` — คุณสมกกกด Publish เอง
- ห้าม backtick ในคำสั่ง Bash heredoc (พังจริง 2026-10-15) → เขียน commit message ลงไฟล์ด้วย Write แล้ว `git commit -F`

⑥ log เต็ม: [WORK-LOG-2026-10-14-working-blink-goal-progress.md](WORK-LOG-2026-10-14-working-blink-goal-progress.md) หัวข้อ "รอบ 9"
