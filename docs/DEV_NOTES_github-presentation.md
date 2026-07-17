# Dev Note — รีวิว + ปรับ presentation หน้า GitHub

**วันที่:** 2026-07-17 (หลัง release v1.6.1)
**ที่มา:** ผู้ใช้ขอรีวิวหน้า repo — เปิดดูหน้า README + release จริงผ่าน browser

## ผลรีวิว (ก่อนแก้)

| # | ปัญหา | ระดับ |
|---|-------|-------|
| 1 | README โหลด GIF 3 ตัว + screenshot รวม ~11.5MB — หนักจน browser render ช้า/ค้าง | 🔴 |
| 2 | ภาพเก่าโชว์ UI ที่ไม่มีแล้ว: `screenshot.png` = ยุค v1.4, `demo.gif` = เมนู flat เก่า, `demo2.gif` = session context เดี่ยวยุค v1.3 | 🔴 สำคัญสุด |
| 3 | Requirements บอกต้องมี CLI `/login` — ขัดกับ How it works ที่บอกใช้ Desktop app ได้ (ฟีเจอร์ v1.5.0) | 🔴 |
| 4 | GIF ในหน้า release v1.6.1 เป็นแค่ asset แนบ ไม่ได้ embed ในเนื้อ — คนไม่เห็น | 🔴 |
| 5 | ช่อง Right-click ในตาราง Usage ยาว 13 รายการ | 🟡 |
| - | Badges / topics / About / social preview / ชื่อ release — ดีอยู่แล้ว | ✅ |

## สิ่งที่แก้ (ทั้งหมด commit + push แล้ว)

1. **จัดรูป README ใหม่** — เหลือ 2 GIF ที่เป็น UI v1.6.1 จริง:
   - hero: `demo-multi-session.gif` (3.7MB)
   - ตัวที่สอง: `demo-no-data.gif` (3.5MB — copy จาก `Recording 2026-07-17 022750.gif`)
   - เอา `screenshot.png`, `demo.gif`, `demo2.gif` ออกจาก README → หน้าเบาลง ~11.5MB → ~7.2MB
   - **ไฟล์เก่ายังอยู่ใน repo** (ไม่ git rm) — กัน hotlink เก่าจากโพสต์ Reddit/ที่อื่นเสีย และลบไปก็ไม่ลดขนาด clone (อยู่ใน history แล้ว)
2. **Requirements** — เขียนใหม่: login "อย่างใดอย่างหนึ่ง" (Desktop app ที่ sign in อยู่ / CLI `/login` ครั้งเดียว)
3. **Release v1.6.1** — embed `demo-no-data-graph.gif` (URL asset) ลงเนื้อ notes ทั้งไฟล์ local และบน GitHub (`gh release edit`)
4. **ตาราง Usage** — ย่อช่อง Right-click เหลือสรุปหมวด

## ค้าง / เผื่ออนาคต

- `screenshot.png` hero ยุคเก่าถูกถอดจาก README แล้ว — ถ้าอยากได้ hero ภาพนิ่งกลับมา ให้ snip หน้า popup v1.6.1 แล้วอัปเดต (รูปนิ่งโหลดไวกว่า GIF เหมาะเป็นภาพแรก)
- GIF เก่า (`demo.gif`, `demo2.gif`) ถ้าจะลบออกจาก tree จริงๆ รอให้แน่ใจว่าไม่มีโพสต์ไหน hotlink อยู่
- social preview (assets/social-preview.png) ยังเป็นภาพเดิม — ยังโอเค แต่ถ้า UI เปลี่ยนมากกว่านี้ค่อยทำใหม่

## วิธี undo

- README/notes: `git revert <commit>` ("Refresh README visuals...")
- Release notes บน GitHub: `gh release edit v1.6.1 --notes-file docs/RELEASE_NOTES_v1.6.1.md` จาก version เก่าใน git history
