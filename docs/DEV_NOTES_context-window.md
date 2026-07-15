# Dev Note — แก้ SESSION CONTEXT โชว์ % ผิด (context window)

**วันที่:** 2026-07-15
**ไฟล์ที่แก้:** `src/ContextMonitor.cs`

## อาการ
Session context โชว์ **84%** ทั้งที่ Claude เองโชว์ **17%** (168.8k / 1.0M)

## สาเหตุ
transcript (`.jsonl`) **ไม่ได้บอกขนาด context window** มาเลย โค้ดเก่าเลยเดาจากจำนวน token:

```csharp
long window = tokens > 200_000 ? 1_000_000 : 200_000;   // ← เดาผิด
```

- token ที่อ่านได้ = 2 + 25 + 168,766 = **168,793**
- 168,793 < 200k → เดา window = 200k → `168,793 / 200,000 = 84%` ❌
- เช็ก `[1m]` ในชื่อโมเดลก็ไม่เจอ เพราะโมเดลคือ `claude-opus-4-8` เฉยๆ

การเดาจาก token **ผิดทุกครั้ง**ที่ session ยังใช้ไม่ถึง 200k บน account ที่ได้ window 1M (คือกรณีปกติ)

## วิธีแก้
เดาจากชื่อโมเดลแทน — Opus/Sonnet ปัจจุบัน = 1M, Haiku = 200k:

```csharp
long window = modelId.Contains("haiku", ...) ? 200_000 : 1_000_000;
if (tokens > window) window = 1_000_000;   // safety กัน % เกิน 100
```

ผลลัพธ์: session เดียวกันโชว์ **17%** ตรงกับ Claude ✅

## หมายเหตุ
- ถ้าอนาคตมี account ที่ Opus/Sonnet ยังเป็น 200k จะโชว์ % ต่ำกว่าจริง — ถ้าเจอค่อยเพิ่มเป็น setting ให้เลือก window เอง
- **"1h old"** ไม่ใช่บั๊ก — มันนับอายุตั้งแต่ข้อความแรกของ session (12:07 น.) ไม่ใช่ตั้งแต่พิมพ์ล่าสุด เพราะ resume session เก่า
