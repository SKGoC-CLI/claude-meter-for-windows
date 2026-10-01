# NEXT-SESSION — Session state + Cloud credit sprint

status: active

① 2026-10-02 · commit ล่าสุดตอนเขียน: `7826db4` — **ไฟล์นี้อาจเก่า เช็ค `git log --oneline -5` ก่อน**

② ขั้นตอน

| Step | งาน | ใคร | สถานะ |
|------|-----|-----|-------|
| 0 | grilling + CONTEXT.md + WORK-LOG | main | ✅ |
| 1 | `ContextMonitor`: state Working/Waiting/Idle + หน้าต่าง 30 นาที | coder | ⬜ |
| 2 | `PopupForm`: จุดสี + Idle จาง (dark+light) | main | ⬜ |
| 3 | `UsageClient`: parse `iguana_necktie` → Cloud credit | coder | ⬜ |
| 4 | `PopupForm`: แถว Cloud credit + toggle Show limits | main | ⬜ |
| 5 | `--popup-shot` dark+light ให้คุณสมกกดู | main | ⬜ |
| 6 | reviewer + rebuild portable | reviewer | ⬜ |

③ สถานะจริง: ยังไม่มีโค้ดเปลี่ยน. ไฟล์ docs/ ที่ untracked (Recording*.mp4, PROMO_DRAFTS, GITHUB_APPEAL) เป็นของเก่า **ไม่เกี่ยว อย่า commit รวม**.

④ ถัดไป: step 1 — spec อยู่ใน WORK-LOG (ข้อตัดสิน #1–3).

⑤ กับดัก
- **ห้ามใส่ backtick ใน bash heredoc** — โดนอีกรอบ 2026-10-02 (เขียน CONTEXT.md พัง) ใช้ Write/Edit
- ห้าม refresh OAuth token เอง (token family revoke) — อ่าน token ผ่าน `DesktopCredentialStore.TryRead()` เท่านั้น
- ฟิลด์ cloud credit = `iguana_necktie` (ชื่อรหัส) ไม่อยู่ใน array `limits`

⑥ log เต็ม: [WORK-LOG-2026-10-02-session-state-cloud-credit.md](WORK-LOG-2026-10-02-session-state-cloud-credit.md)
