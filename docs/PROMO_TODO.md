# โปรโมต Claude Meter — สิ่งที่ต้องทำต่อ

_อัปเดต 2026-07-17 (หลัง release v1.6.2). ไฟล์นี้ไว้กลับมาสานงานโปรโมตต่อ — โค้ด/release เสถียรดีแล้ว เหลือแค่กระจายให้คนรู้จัก_

## สถานะปัจจุบัน (เช็คจริง 17 ก.ค.)

| ช่องทาง | สถานะ | ทำอะไรต่อ |
|---------|-------|-----------|
| **r/ClaudeCode** | โพสต์เดิม (13 ก.ค. id `1uux0l8`) **ยังโดน automod filter** — `approved: null`, robot index ไม่ได้ = คนทั่วไปมองไม่เห็น ผ่านมา 4 วันไม่ approve เอง | โพสต์ตายแล้ว ต้องเลือกทางใหม่ (ดูล่าง) |
| **awesome-claude-code #2215** | ยัง open เฉยๆ อัปเดตล่าสุด 12 ก.ค. รอ maintainer ตัดสินใจ (เขาบอกเองอาจหลายสัปดาห์) | **รออย่างเดียว** — ทำอะไรไม่ได้ อย่าไปจี้ (maintainer ไม่ชอบ promo-driven) |
| **Built with Claude Megathread** (r/ClaudeAI) | คอมเมนต์ไว้แล้ว (u/Overall_Rate_8351) | ปล่อยไว้ |

**อุปสรรคหลัก:** karma ของ u/Overall_Rate_8351 ต่ำ (~1) → ซับใหญ่ auto-filter โพสต์ทันที นี่คือเหตุผลที่ r/ClaudeCode ตกทุกที

## ทางเลือก (เรียงตามที่แนะนำ)

### 1. เปิดช่องใหม่ที่ยังไม่เคยโพสต์ ⭐ แนะนำ
ยังไม่เคยแตะ — ลองทีละอัน:
- [ ] **r/SideProject** — สายโปรเจกต์ส่วนตัว รับ story "สร้างเครื่องมือใช้เอง" ดี
- [ ] **r/dotnet** หรือ **r/csharp** — เป็น .NET WinForms พอดี สาย dev น่าสนใจ tray app จริง
- [ ] **r/opensource** — MIT + public repo ตรงสเปก
- [ ] **X (Twitter)** — โพสต์สั้น + demo GIF + ลิงก์ release

### 2. กู้ r/ClaudeCode
- [ ] ลบโพสต์เก่า แล้วโพสต์ใหม่ (เสี่ยงโดน filter ซ้ำเพราะ karma) — หรือ
- [ ] ส่งข้อความหา mods ขอ approve: reddit.com/message/compose?to=r/ClaudeCode
- [ ] ทางยั่งยืน: ไปเพิ่ม karma ในซับเล็กสัก 20–30 ก่อน ค่อยกลับมาโพสต์ซับใหญ่

### 3. พักโปรโมต
ปล่อย in-app update กระจาย v1.6.2 เงียบๆ ค่อยลุยตอนมีฟีเจอร์ใหม่ให้เล่า

## Pitch หลัก (พร้อมใช้ ดึงจาก README)

> **Story:** ชนลิมิต Claude บ่อยตอนสร้างบอทคอนโด อยากรู้ว่าเหลือเท่าไหร่ แต่มีแต่แอป Mac / Chrome extension — เลยสร้างเองสำหรับ Windows

**จุดขาย:**
- Live usage อยู่ใน system tray (5h / weekly / per-model)
- Popup กราฟ usage + session context (ทุก session ที่ active)
- Portable ไม่ต้องลง .NET, ฟรี, MIT, open source
- อ่าน token แบบ read-only ทั้ง Claude Code CLI และ Claude Desktop

**ลิงก์:** https://github.com/SKGoC-CLI/claude-meter-for-windows/releases/latest
**Demo GIF:** `docs/demo-multi-session.gif`, `docs/demo-no-data.gif`

## Gotcha ตอน fetch Reddit (กันลืม)
- WebFetch + Browser pane โดน block สำหรับ reddit.com
- curl ไป www/old/api.reddit.com → 403 (TLS fingerprint)
- **ใช้ได้:** Arctic Shift API (`arctic-shift.photon-reddit.com/api/posts/ids?ids=<id>`) — เช็คสถานะโพสต์ได้ ไม่ต้อง auth
- **ใช้ได้:** Claude in Chrome (คุณล็อกอิน u/Overall_Rate_8351 อยู่แล้ว) — โพสต์/อ่านได้เต็ม

---
_บอกผมได้เลยว่าจะเอาช่องไหน เดี๋ยวร่างโพสต์เต็มให้ดูก่อนโพสต์จริง_
