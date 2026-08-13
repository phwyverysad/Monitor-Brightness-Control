# 🖥️ Monitor Brightness Control (Noble Brightness)

โปรแกรมควบคุมความสว่างหน้าจอคอมพิวเตอร์ผ่าน System Tray บน Windows ออกแบบมาให้ใช้งานง่าย รวดเร็ว และกินทรัพยากรเครื่องน้อยที่สุด

---

## 🌟 คุณสมบัติเด่น

- 🖱️ **ปรับความสว่างด้วยลูกกลิ้งเมาส์**: เลื่อนลูกกลิ้งเมาส์ (Mouse Wheel) ขึ้น-ลง บนไอคอนบริเวณ System Tray มุมขวาล่างเพื่อปรับความสว่าง (ทีละ 5%)
- 🖥️ **รองรับหน้าจอหลากหลายประเภท**:
  - **DDC/CI**: สำหรับหน้าจอมอนิเตอร์แยก (External Monitors)
  - **WMI**: รองรับหน้าจอโน้ตบุ๊ก (Laptop Displays)
- ⚡ **น้ำหนักเบาและประหยัดทรัพยากร**: ตัวติดตั้งขนาดเพียง 3.2 MB ทำงานรวดเร็ว ไม่หน่วงเครื่อง
- 🔒 **ระบบ Hook ปลอดภัย**: ตรวจสอบตำแหน่งเมาส์เฉพาะบนไอคอนโปรแกรม ไม่กระทบการเลื่อนหน้าเว็บหรือเอกสารอื่นๆ
- ⚙️ **เมนูทางเลือก**: คลิกขวาที่ไอคอนเพื่อเลือกจอมอนิเตอร์ หรือเลือกระดับความสว่าง (0% - 100%) ได้ทันที
- 📦 **ตัวติดตั้งสมบูรณ์แบบ**: มาพร้อมชุดติดตั้งที่รองรับการเปิดอัตโนมัติพร้อม Windows และการถอนติดตั้งจาก Control Panel

---

## 🚀 การติดตั้งและการใช้งาน

### ความต้องการของระบบ
* **ระบบปฏิบัติการ**: Windows 10 / Windows 11 (64-bit)
* **หน้าจอมอนิเตอร์**: เปิดใช้งานฟังก์ชัน **DDC/CI** ในเมนูตั้งค่าของหน้าจอ (สำหรับมอนิเตอร์ต่อแยก)

### ขั้นตอนการใช้งาน
1. ดาวน์โหลดไฟล์ `Setup_NobleBrightness.exe`
2. ดับเบิลคลิกเพื่อติดตั้งโปรแกรม (ติดตั้งลงใน `C:\Program Files\NobleBrightness`)
3. ไอคอนจะปรากฏบริเวณ System Tray มุมขวาล่างของหน้าจอ
4. เลื่อนเมาส์ไปวางเหนือไอคอน แล้วหมุนลูกกลิ้งเมาส์เพื่อปรับความสว่างตามต้องการ

---

## 🛠️ การคอมไพล์ซอร์สโค้ด (Build from Source)

### สิ่งที่ต้องใช้
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### 1. ทดสอบรันโปรแกรม
```powershell
dotnet restore .\NobleBrightness\NobleBrightness.csproj
dotnet run --project .\NobleBrightness\NobleBrightness.csproj
```

### 2. คอมไพล์เป็นไฟล์โปรแกรม (.exe)
```powershell
dotnet publish .\NobleBrightness\NobleBrightness.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

### 3. คอมไพล์ชุดตัวติดตั้ง (Installer Package)
```powershell
dotnet publish .\NobleBrightnessInstaller\NobleBrightnessInstaller.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

---

## ⚙️ รายละเอียดทางเทคนิค

* **Global Wheel Hook**: ใช้เทคนิค Windows Hook (`WH_MOUSE_LL`) ร่วมกับ API `Shell_NotifyIconGetRect` เพื่อตรวจสอบพิกัดเมาส์กับไอคอนบน Taskbar ป้องกันไม่ให้ส่งผลกระทบต่อการทำงานของโปรแกรมอื่น
* **Debounce & Asynchronous Execution**: คำสั่งส่งค่าปรับความสว่างฮาร์ดแวร์ DDC/CI และ WMI ทำงานแบบ Asynchronous ร่วมกับระบบ Debounce (80ms) เพื่อให้การปรับความสว่างตอบสนองได้ลื่นไหลและไม่ค้างกระตุก

---

## 📄 ลิขสิทธิ์
โปรเจกต์นี้เป็นซอฟต์แวร์โอเพ่นซอร์สภายใต้ใบอนุญาต MIT License
