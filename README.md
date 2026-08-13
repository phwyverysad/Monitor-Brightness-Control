<div align="center">

# Monitor Brightness Control

**โปรแกรมปรับและควบคุมความสว่างหน้าจอคอมพิวเตอร์ผ่าน System Tray บน Windows**

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D6?style=flat-square&logo=windows&logoColor=white)](https://github.com/phwyverysad/Monitor-Brightness-Control)
[![Framework](https://img.shields.io/badge/Framework-.NET%208.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Language](https://img.shields.io/badge/Language-C%23-239120?style=flat-square&logo=csharp&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![License](https://img.shields.io/badge/License-MIT-green.svg?style=flat-square)](LICENSE)
[![Download](https://img.shields.io/badge/Download-Latest%20Release-brightgreen?style=flat-square)](https://github.com/phwyverysad/Monitor-Brightness-Control/releases/tag/NobleBrightness)
[![GitHub Stars](https://img.shields.io/github/stars/phwyverysad/Monitor-Brightness-Control?style=flat-square&color=gold)](https://github.com/phwyverysad/Monitor-Brightness-Control/stargazers)
[![GitHub Issues](https://img.shields.io/github/issues/phwyverysad/Monitor-Brightness-Control?style=flat-square&color=orange)](https://github.com/phwyverysad/Monitor-Brightness-Control/issues)

[ภาพรวม](#ภาพรวม) | [ฟีเจอร์หลัก](#ฟีเจอร์หลัก) | [การติดตั้งและการใช้งาน](#การติดตั้งและการใช้งาน) | [การคอมไพล์จาก Source Code](#การคอมไพล์จาก-source-code) | [รายละเอียดทางเทคนิค](#รายละเอียดทางเทคนิค) | [สัญญาอนุญาต](#สัญญาอนุญาต)

</div>

---

## ภาพรวม

Monitor Brightness Control (NobleBrightness) คือแอปพลิเคชันสำหรับระบบปฏิบัติการ Windows ที่ช่วยให้ผู้ใช้สามารถปรับความสว่างของหน้าจอมอนิเตอร์ได้อย่างสะดวกผ่าน System Tray บริเวณมุมขวาล่างของหน้าจอ รองรับทั้งหน้าจอมอนิเตอร์ภายนอก (External Monitors) ผ่าน DDC/CI และหน้าจอโน้ตบุ๊ก (Laptop Displays) ผ่าน WMI

---

## ฟีเจอร์หลัก

### การควบคุมความสว่าง
* **ปรับความสว่างด้วยลูกกลิ้งเมาส์**: เลื่อนลูกกลิ้งเมาส์ (Mouse Wheel) ขึ้นหรือลงเหนือไอคอนบริเวณ System Tray เพื่อเพิ่มหรือลดความสว่าง (ทีละ 5%) ได้ทันที
* **การเลือกจอมอนิเตอร์และระดับความสว่าง**: คลิกขวาที่ไอคอนเพื่อเลือกจอมอนิเตอร์ที่ต้องการปรับ หรือเลือกตั้งค่าระดับความสว่างเฉพาะเจาะจง (0% - 100%)

### รองรับหน้าจอหลากหลายประเภท
* **DDC/CI (Display Data Channel / Command Interface)**: สื่อสารและปรับความสว่างฮาร์ดแวร์โดยตรงสำหรับมอนิเตอร์ต่อแยกภายนอก
* **WMI (Windows Management Instrumentation)**: รองรับการปรับความสว่างสำหรับหน้าจอโน้ตบุ๊ก

### ชุดติดตั้งสมบูรณ์แบบ (Installer Package)
* มาพร้อมไฟล์ติดตั้ง `Setup_NobleBrightness.exe` ติดตั้งลงในระบบปฏิบัติการง่ายดาย
* รองรับการตั้งค่าเปิดโปรแกรมอัตโนมัติพร้อม Windows และรองรับการถอนติดตั้งผ่าน Control Panel / Windows Settings

---

## การติดตั้งและการใช้งาน

### สิ่งที่จำเป็นต้องมี
* **ระบบปฏิบัติการ**: Windows 10 หรือ Windows 11 (64-bit)
* **หน้าจอมอนิเตอร์**: เปิดใช้งานตัวเลือก **DDC/CI** ในเมนูตั้งค่า OSD ของจอมอนิเตอร์ (สำหรับหน้าจอมอนิเตอร์ต่อแยก)

### ขั้นตอนการใช้งาน

1. ดาวน์โหลดไฟล์ติดตั้ง `Setup_NobleBrightness.exe` จาก [Releases Page](https://github.com/phwyverysad/Monitor-Brightness-Control/releases/tag/NobleBrightness)
2. เปิดไฟล์เพื่อทำการติดตั้งโปรแกรม (โปรแกรมจะถูกติดตั้งที่ `C:\Program Files\NobleBrightness`)
3. เมื่อติดตั้งเสร็จสิ้น ไอคอนจะปรากฏบริเวณ System Tray มุมขวาล่างของหน้าจอ
4. เลื่อนเมาส์ไปวางเหนือไอคอน แล้วหมุนลูกกลิ้งเมาส์ขึ้น-ลงเพื่อปรับความสว่างตามต้องการ

---

## การคอมไพล์จาก Source Code

### สิ่งที่ต้องใช้
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### 1. ทดสอบรันโปรแกรม
```powershell
dotnet restore .\NobleBrightness\NobleBrightness.csproj
dotnet run --project .\NobleBrightness\NobleBrightness.csproj
```

### 2. คอมไพล์เป็นไฟล์โปรแกรมสำเร็จรูป (.exe)
```powershell
dotnet publish .\NobleBrightness\NobleBrightness.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

### 3. คอมไพล์ชุดตัวติดตั้ง (Installer Package)
```powershell
dotnet publish .\NobleBrightnessInstaller\NobleBrightnessInstaller.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

---

## รายละเอียดทางเทคนิค

* **Global Wheel Hook**: ใช้เทคนิค Windows Hook (`WH_MOUSE_LL`) ร่วมกับ API `Shell_NotifyIconGetRect` เพื่อตรวจจับพิกัดเมาส์บริเวณไอคอน Taskbar อย่างแม่นยำ ป้องกันไม่ให้ส่งผลกระทบต่อการทำงานของโปรแกรมอื่น
* **Debounce & Asynchronous Execution**: คำสั่งส่งค่าปรับความสว่างฮาร์ดแวร์ DDC/CI และ WMI ทำงานแบบ Asynchronous ร่วมกับระบบ Debounce (80ms) เพื่อให้การปรับความสว่างลื่นไหล ตอบสนองเร็ว และไม่เกิดอาการค้างกระตุก

---

## สัญญาอนุญาต

โปรเจกต์นี้เผยแพร่ภายใต้สัญญาอนุญาต MIT License

```
MIT License - Copyright (c) 2026 phwyverysad
```
