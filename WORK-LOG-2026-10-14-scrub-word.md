# WORK-LOG 2026-10-14 — ล้างคำต้องห้าม "ab***t" ออกจาก GitHub

> ห้ามพิมพ์คำเต็มในไฟล์นี้ — ใช้ "ab***t"

## โจทย์
คุณสมกกสั่ง: ใน repo SKGoC-CLI/claude-meter-for-windows ห้ามมีคำนี้หลุด เอาออกให้หมด

## ผลสำรวจ (main)
- ต้นตอเดียว: ชื่อ session ตัวอย่าง "ab***t-archive" ใน `src/PopupShot.cs` (ใส่ตั้งแต่ commit `662cb88` 10 ส.ค.)
- แพร่ไป:
  1. ภาพ 7 ไฟล์ที่ render ด้วย `--popup-shot`: `docs/popup-v1.10.0.png`, `docs/light-dark-v1.9.1.png`, `docs/design/light-theme-variants/A–E*.png`
     (`compare-rows.png`, `compare-graph.png`, `asking-solid-preview.png` ตรวจแล้วไม่มี)
  2. exe ใน zip ของ release v1.9.1 และ v1.10.0 (string ถูก compile เข้าไป)
  3. ประวัติ git 27 commit ตั้งแต่ `662cb88`
- ไม่มีใน commit message / release body
- repo: 0 fork, 5 ดาว, author มีแต่ SKGoC

## Grilling
**Q1 ขอบเขต** — A. แก้ไฟล์ปัจจุบัน + exe · B. A + เขียนประวัติ git ใหม่ (force push) · C. A + เบลอ GIF
→ คุณสมกกเลือก **B**
→ ข้อ 4 (ชื่อโปรเจกต์ "Dental Freelance..." ใน hero GIF) ยังไม่ได้ตอบ = ไม่แตะ

## Sprint
| Step | งาน | ใคร | หยุดได้ไหม |
|------|-----|-----|-----------|
| 1 | mirror backup | main | ✅ |
| 2 | แก้ PopupShot.cs + ภาพ 7 ไฟล์ | main | ✅ หยุดตรงนี้ได้ = ยังไม่แตะ GitHub |
| 3 | filter-repo | main | ⚠️ ทำ 3–4 ต่อเนื่อง |
| 4 | force push + ย้าย tag | main | — |
| 5 | rebuild zip 2 release | main | ✅ หยุดตรงนี้ได้ = GitHub สะอาด |
| 6 | ตรวจภายนอก + memory | main | ✅ |

โควตาตอนเริ่ม (จากมิเตอร์): 5h 33% · 7d 3% — ประเมินทั้งงาน ~10–15% ของ 5h (เดา)

## บันทึกการทำ
1. **backup**: `git clone --mirror` → `bin\repo-backup-pre-rewrite.git` (bin/ อยู่ใน .gitignore) — **ทางกู้คืน** ถ้าต้องย้อน: push จาก mirror นี้กลับ (แต่คำจะกลับมาด้วย)
2. **ภาพ**: วาดชื่อใหม่ "design-archive" ทับตำแหน่งเดิม (เลือกเพราะกว้างต่างจากของเดิมแค่ 1 px)
   - ปรับเทียบโดยวาดคำเดิมซ้ำแล้วเทียบ pixel: Segoe UI Bold 12.5 pt, ClearTypeGridFit, origin (-1,-2) → err = 0 ทุกภาพ
   - สคริปต์อยู่ scratchpad ของ session (`calib.ps1`, `replace.ps1`) — ไม่ได้เก็บใน repo
   - ภาพที่แก้ 7 ไฟล์: `docs/light-dark-v1.9.1.png` (2 จุด light/dark), `docs/popup-v1.10.0.png`, `docs/design/light-theme-variants/A–E`
3. **filter-repo** (สคริปต์ `scrub.py` ใช้ blob_callback): ทุก blob เก่าของ 7 path → แทนด้วยไฟล์ที่ล้างแล้ว, blob ข้อความ → แทนคำ
   - ผล: `git log -S` 0 hit, `git grep` ทุก rev 0 hit, ภาพแต่ละ path เหลือ 1 เวอร์ชัน
   - tag ก่อน v1.9.1 SHA ไม่เปลี่ยน (commit ก่อน 662cb88 ไม่โดนแตะ)
4. **push**: main `effeb57 → a97d099` (force-with-lease SHA เต็ม), tag `v1.9.1 b61d05c → e50a7d8`, `v1.10.0 c27704a → d984151`
5. **zip**: build ใหม่จาก source ที่ล้างแล้ว — v1.10.0 (HEAD, src = tag) และ v1.9.1 (worktree ที่ tag) · ตรวจ dll ไม่มีคำ มีแต่ design-archive · upload `--clobber` ทั้งคู่ 04:28 UTC
6. **ตรวจภายนอก**: `blob/main/src/PopupShot.cs` + raw = สะอาด · **แต่** `commit/662cb88.patch` และ raw ที่ SHA เก่ายังเห็นคำ → ต้อง GitHub Support
   - ข้อความคำขอ + รายการ 28 SHA เก่า: `docs/GITHUB_SUPPORT_PURGE_2026-10-14.md` (ไม่ commit)

## โฟลเดอร์ เก็บ/ลบ
- เก็บ: `bin\repo-backup-pre-rewrite.git` จนกว่า GitHub Support ล้างเสร็จและมั่นใจ — แล้วลบได้ (มันมีคำเดิมอยู่)
- `docs/GITHUB_SUPPORT_PURGE_2026-10-14.md` ลบได้หลังส่งแล้ว
