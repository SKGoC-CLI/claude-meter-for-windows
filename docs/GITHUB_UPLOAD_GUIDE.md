# วิธีอัพโปรเจกต์ขึ้น GitHub (ทีละขั้น)

> เอกสารนี้สำหรับคุณเอง ไม่ต้อง push ขึ้น repo ก็ได้ (อยู่ใน docs/ เผื่ออ้างอิง)

## สิ่งที่เตรียมไว้ให้แล้ว

| ไฟล์ | หน้าที่ |
|---|---|
| `README.md` | หน้าแรกของ repo (ภาษาอังกฤษ พร้อมรูป badge/ตาราง) |
| `LICENSE` | MIT |
| `.gitignore` | กันไฟล์ build (`publish/`, `portable/`, `*.zip`) ไม่ให้ขึ้น repo |
| `docs/RELEASE_NOTES_v1.0.1.md` | ข้อความสำหรับหน้า Release |
| git repository | init + commit แรกให้แล้ว |

## ขั้นตอน (ครั้งแรกครั้งเดียว)

### 1. เพิ่ม screenshot (ก่อน push จะสวย)
แคปหน้าจอ popup (แบบที่เคยส่งในแชท) แล้วบันทึกเป็น
`docs/screenshot.png` — README ชี้ไปที่ไฟล์นี้อยู่แล้ว
จากนั้น:
```powershell
git add docs/screenshot.png
git commit -m "Add screenshot"
```

### 2. สร้าง repo บน GitHub
ไปที่ https://github.com/new
- Repository name: `claude-meter-for-windows`
- Public ✔
- **อย่า**ติ๊ก "Add a README" (เรามีแล้ว)
- กด Create repository

### 3. Push โค้ดขึ้นไป
GitHub จะโชว์คำสั่งให้ copy — หรือใช้ชุดนี้ (แทน `<USERNAME>` ด้วยชื่อบัญชีคุณ):
```powershell
cd "D:\Onedrive\Desktop\App Claude Meter"
git remote add origin https://github.com/SKGoC-CLI/claude-meter-for-windows.git
git push -u origin main
```
(ครั้งแรก Windows จะเด้งหน้าต่างให้ login GitHub)

### 4. สร้าง Release แนบไฟล์ zip
1. ในหน้า repo → แท็บ **Releases** → **Create a new release**
2. Tag: `v1.0.1` (พิมพ์แล้วเลือก "Create new tag on publish")
3. Title: `Claude Meter v1.0.1`
4. Description: copy เนื้อหาจาก `docs/RELEASE_NOTES_v1.0.1.md` มาวาง
5. ลากไฟล์ `ClaudeMeter-portable.zip` (อยู่ในโฟลเดอร์โปรเจกต์) ไปวางในช่อง attach
6. กด **Publish release**

เสร็จแล้ว! ลิงก์ **Download** ใน README จะชี้ไปหา release ล่าสุดโดยอัตโนมัติ

## ครั้งถัดไป (เมื่อแก้โค้ดเพิ่ม)

```powershell
git add -A
git commit -m "อธิบายสิ่งที่แก้"
git push
```
ถ้าจะออกเวอร์ชันใหม่: build zip ใหม่ → สร้าง Release ใหม่ (tag v1.1.0, v1.2.0, ...)
