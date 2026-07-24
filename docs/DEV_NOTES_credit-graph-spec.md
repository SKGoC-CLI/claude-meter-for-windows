# Spec: กราฟ credit + รวมแถว Extra usage/Spend (จาก grilling session 2026-07-24)

## ข้อเท็จจริงที่พิสูจน์แล้ว (เรียก API สดวันนี้)

จาก `GET /api/oauth/usage` (token Desktop, ผ่าน probe ใน scratchpad):

- `extra_usage` และ `spend` คือ **กระเป๋าเดียวกัน 100%** — ทั้งคู่คือยอดใช้ usage-credit
  เทียบเพดานรายเดือน ตัวอย่างจริง: `extra_usage = {monthly_limit: 5000, used_credits: 313,
  utilization: 6.26}` กับ `spend = {used: 313¢, limit: 5000¢, percent: 6}` —
  ต่างแค่ extra_usage ให้ทศนิยม, spend ปัดเศษ + มี `severity`
- หน่วยเป็น minor units (cents), `decimal_places: 2` → 5000 = $50.00
- `used_credits` เป็น **ยอดสะสมฝั่ง server ของเดือนปัจจุบัน** — poll เมื่อไหร่ก็ได้ค่าจริง
  ไม่ต้องสะสมเอง ปิดแอปข้ามวันก็ไม่เพี้ยน รีเซ็ตเป็น 0 ทุกวันที่ 1
- **ยอด balance/promotional credit ($99.04) ไม่มีในทุก endpoint ที่ OAuth token เข้าถึงได้**
  — `spend.balance` = null, `/api/oauth/profile` และ `/api/oauth/account` ก็ไม่มีตัวเลขเงิน
  (Desktop ดึงจาก API ภายใน claude.ai ที่ใช้ cookie คนละระบบ)
- เครดิตถูกกินได้แม้ session bar ต่ำ (วันนี้ $1.92→$3.13 ระหว่าง session ~1%) —
  ผู้ใช้ไม่มีทางรู้ตัวถ้าไม่ดูจอ Desktop settings
- บัญชีผู้ใช้: Auto-reload = Off + balance เกือบทั้งหมดเป็น promotional credit
  (หมดอายุ 19 ก.ย. 2026) → เครดิตหมด = หยุด ไม่ตัดบัตร ("ไม่เข้าเนื้อ" โดยโครงสร้างอยู่แล้ว)

## มติ (ตอบโดย Khun Somgok ทีละข้อ)

1. **รวม Extra usage + Spend เป็นแถวเดียว โชว์เป็นเงิน** — เช่น `Extra usage: $3.13 / $50.00 (6.3%)`
   ใช้ตัวเลขจาก extra_usage (ละเอียดกว่า) + severity จาก spend; **อย่า hardcode $50** —
   อ่าน `monthly_limit` จาก API
2. **ไม่ทำ "free remaining"** — API ไม่มีข้อมูล, auto-reload off คุ้มครองอยู่แล้ว (YAGNI)
3. **เพิ่มกราฟใหม่ 1 อันเฉพาะ credit** — month-to-date: แกน X = วันที่ 1 → สิ้นเดือน,
   แกน Y = $0 → monthly_limit, plot `used_credits` ตรงๆ, **เส้น limit ขีดที่ $50** (จาก API)
4. **วางซ้อนใต้กราฟ session เดิม** ใน popup (ไม่ทำปุ่มสลับ)
5. **แจ้งเตือน balloon ครั้งเดียวเมื่อ "เริ่มกินเครดิต"** หลังจากนิ่งมานาน —
   เช่น "เริ่มใช้ usage credit แล้ว ($3.13 เดือนนี้)" — ต่อยอดระบบ balloon + Notify ที่มีอยู่

## สถานะ (อัปเดต 2026-07-24)

- [x] implement ครบทั้ง 5 มติ → v1.7.0 (coder ทำ, reviewer ตรวจ, แก้ตาม review แล้ว)
- แก้เพิ่มจาก review: (1) balloon ไม่เด้งซ้ำตอนเปิดเครื่อง — poll แรกเป็น baseline เงียบ,
  (2) block ที่ disabled ไม่ถูกดึง field มาปน (กัน % กับ $ มาจากคนละกระเป๋า),
  (3) exponent ของ used/limit อ่านแยกกัน, (4) สีกราฟ fallback ตาม % เมื่อไม่มี severity,
  (5) กันแถว "spend" โผล่ซ้ำใน legacy shape
- ที่ "ยอมรับไม่แก้": throttle balloon ไม่ persist ข้าม restart (ต้องรีสตาร์ทถี่ๆ
  ระหว่างเครดิตกำลังไหลถึงจะเห็นเด้งเกิน 1 ครั้ง/2ชม. — ไม่คุ้มเพิ่ม state)
- วิธีตรวจภาพโดยไม่ต้องเปิดแอป: scratchpad `popuprender\` — render PopupForm เป็น PNG
  ด้วยข้อมูล synthetic (inject credit_spend ผ่าน reflection) มีประโยชน์ต่อรอบหน้า
- เครื่อง Khun Somgok ตอนนี้รัน debug build v1.7.0 แทน portable v1.6.2 เดิม
  (portable\ClaudeMeter.exe ยังเป็น 1.6.2 จนกว่าจะ build release ใหม่)
- fix รอบสอง (e9a9d46): กราф credit ค้าง "Collecting data…" หลังติดตั้ง/รีสตาร์ท
  เพราะ downsample 30 นาทียุบตัวอย่างใหม่ทันที → เหลือจุดเดียวไม่ถึงเกณฑ์ 2 จุด
  แก้: downsample เฉพาะข้อมูลเก่ากว่า 24 ชม. + กราฟต่อจุดค่าปัจจุบันจาก snapshot
  เข้าท้ายเส้นเสมอ (เส้นถึง "Now" และวาดได้ตั้งแต่ poll แรก)
- fix รอบสาม (grilling 2026-07-24): แทน "Collecting data…" / เส้นสั้นๆ ด้วยเส้นประนำ
  ลากจากซ้ายสุด + พื้นเติมต่อเนื่อง + เส้นทึบช่วงที่บันทึกจริง. เลิก "Collecting data…" ถาวร.
- fix รอบสี่ (grilling 2026-07-24, เปลี่ยนใจจากรอบสาม): จุดยึดซ้ายของเส้นประ
  เปลี่ยนจาก $0 (แนวทแยงไต่ขึ้น) เป็น **แบนที่ระดับจุดจริงแรก** (`baseline.Y = recPts[0].Y`).
  เหตุผล user: เน้นกราฟสวย > ตรงความจริง — "ไม่มีข้อมูลก็ assume ว่าใช้เท่านี้มาแต่แรก".
  ผล: ไม่มีประวัติ → เส้นประแบนที่ค่าปัจจุบันเต็มความกว้าง; มีประวัติ → เส้นประนำอยู่
  ระดับต่ำสุดที่บันทึกได้ (มักใกล้ $0 ริมล่าง) แล้วโชว์เส้นทึบ curve จริง.
- fix รอบห้า (grilling 2026-07-24): ย้าย "$X used" จากหัวมุมขวาบน (ลบทิ้ง) ลงมาเป็น
  ป้าย 2 บรรทัด "Now / $X used" เกาะจุดล่าสุด — ชิดซ้ายเส้น now เหนือจุด, flip
  ขวา/ล่างถ้าชนขอบ. "Now" ใช้ Theme.NowText, "$X used" ใช้สี severity เดิม.
- ยืนยันตอนนี้: limit ขึ้นจาก $50 → $100 (เพิ่มเครดิต) กราฟอ่านจาก API เลยขึ้นถูกเอง
  = พิสูจน์ว่าการไม่ hardcode $50 คุ้มค่า.
