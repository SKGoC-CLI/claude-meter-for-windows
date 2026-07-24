# CONTEXT — ubiquitous language

คำที่ใช้ในโปรเจกต์นี้ให้ตรงกันทั้งใน UI, เมนู, โค้ด และเอกสาร (กัน section ในป๊อปอัป
กับ label ในเมนูเรียกคนละชื่อเหมือนที่เคยเกิด "SESSION GRAPH" vs "Usage graph").

## กราฟในป๊อปอัป

- **Session graph** — กราฟเส้น "usage remaining" ของ session 5 ชม. บนแกนเวลา 12/24 ชม.
  section header ในป๊อปอัป = `SESSION GRAPH (24H)`; เมนู = `Session graph` (Show/Range/Now position);
  setting = `ShowRemainingGraph`, key ประวัติ = `five_hour`. **ห้ามเรียก "Usage graph" อีก** —
  ชื่อเดิมกำกวมกับ "extra usage" / usage credits.
- **Credit graph** — กราฟเส้นสะสม "usage-credit spend" เดือนปัจจุบันเทียบเพดานรายเดือน.
  section header = `CREDIT (THIS MONTH)`; เมนู = `Credit graph` (Show); setting = `ShowCreditGraph`,
  key ประวัติ = `credit_spend` (เก็บ dollars, retention 45 วัน). แต่ละกราฟเป็นคนละ section →
  มี submenu ของตัวเอง (หลักการเดิม: "each popup section owns its toggle + its options").

## Wallet / เครดิต

- **Wallet row / "Extra usage" row** — แถวเดียวในป๊อปอัปที่รวม `extra_usage` กับ `spend`
  จาก API (มันคือกระเป๋าเดียวกัน) โชว์เป็นเงิน `$used / $limit`. key = `extra_usage`.
- **used / limit** — เงินที่ใช้ไปเดือนนี้ (server-side cumulative, รีเซ็ต $0 วันที่ 1) เทียบ
  เพดานรายเดือน. อ่านสด monthly_limit จาก API เสมอ — **ห้าม hardcode** (เคยเป็น $50 ตอนนี้ $100).
- **Balance / promotional credit** — ยอดเครดิตคงเหลือ ($99+) **ไม่มีใน OAuth endpoint ใดๆ**
  ที่แอปเข้าถึงได้ จึงไม่แสดง.
