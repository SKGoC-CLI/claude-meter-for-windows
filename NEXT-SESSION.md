# NEXT-SESSION — Session state + Cloud credit sprint

status: done

① 2026-10-02 · commit ล่าสุดตอนเขียน: `a862368` (+ commit ปิด sprint นี้) — **ไฟล์นี้อาจเก่า เช็ค `git log --oneline -5` ก่อน**

② ขั้นตอน

| Step | งาน | ใคร | สถานะ |
|------|-----|-----|-------|
| 0 | grilling + CONTEXT.md + WORK-LOG | main | ✅ |
| 1 | `ContextMonitor`: state Working/Waiting/Idle + หน้าต่าง 30 นาที | coder | ✅ |
| 2 | `PopupForm`: จุดสี + Idle จาง (dark+light) | main | ✅ |
| 3 | `UsageClient`: parse `iguana_necktie` → Cloud credit (กันออกจาก tray/balloon) | coder | ✅ |
| 4 | `PopupForm`: แถว Cloud credit (toggle ใน Show limits มาเองอัตโนมัติ) | main | ✅ |
| 5 | `--popup-shot` dark+light (fixture เพิ่ม cloud credit + 3 สถานะ) | main | ✅ |
| 6 | reviewer + แก้ 5 ข้อ + ตัด subagent ออก + rebuild portable | reviewer/main | ✅ |

③ สถานะจริง: build Release 0 error 0 warning. commits `872a5be` (ฟีเจอร์), `a862368` (แก้ตาม review).
portable สลับเป็น build ใหม่แล้วและรันอยู่ — log ขึ้น `usage ok: Session (5h) 72%, Weekly 34%, Cloud credit 1%`.
exe เก่า v1.9.1 อยู่ `bin\portable-v1.9.1-backup\`. **ยังไม่ release** (csproj ยัง 1.9.1).
`docs/PROMO_TODO.md` (modified) + ไฟล์ docs/ untracked เป็นของเก่า **ไม่เกี่ยว อย่า commit รวม**.

④ ถัดไป: รอคุณสมกกดูของจริงบนจอ แล้วปรับขนาด/สีจุดถ้าต้องการ. ถ้าจะ release = v1.10.0 + CHANGELOG + README + รูปใหม่.

⑤ กับดัก
- **ห้ามใส่ backtick ใน bash heredoc** — โดน 2 ครั้งใน sprint นี้: ครั้งที่ 2 backtick `git revert ...` ในข้อความ WORK-LOG
  ถูก shell รันจริง สร้าง revert commit 2 อัน (ไม่ได้ push) กู้ด้วย `git reset --keep a862368`. เขียน md ด้วย Write/Edit เท่านั้น
- ห้าม refresh OAuth token เอง (token family revoke) — อ่าน token ผ่าน `DesktopCredentialStore.TryRead()` เท่านั้น
- ฟิลด์ cloud credit = `iguana_necktie` (ชื่อรหัส) ไม่อยู่ใน array `limits`
- transcript ของ subagent (`.../subagents/*.jsonl`) จบด้วย tool call เสมอ → ถูกตัดออกจากลิสต์แล้ว อย่าเอากลับ

⑥ log เต็ม: [WORK-LOG-2026-10-02-session-state-cloud-credit.md](WORK-LOG-2026-10-02-session-state-cloud-credit.md)
