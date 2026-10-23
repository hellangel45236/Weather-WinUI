# 🌤️ Weather WinUI (Version 3.6.0)
### Hệ Điều Hành Vi Khí Hậu Cá Nhân Hóa & Trực Quan Hóa Đô Thị Dành Cho Windows 10 & 11
> **Nền tảng:** Windows App SDK (WinUI 3) • .NET 8 • C# 12 • Fluent Design System 2.0 & Nothing Design System 3.6  
> **Nhánh phát triển chính thức:** `main` | **Phiên bản:** `v3.6.0 (Bản Phát Hành Chính Thức)`  
> **Bản phát hành tiền nhiệm:** `v3.5.2` • `v3.5.1` • `v3.5.0` • `v3.4.1` • `v3.4.0` • `v3.3.1` • `v3.3.0` • `v3.0.0`

---

## 📑 Mục Lục
1. [Giới Thiệu Tổng Quan & Tầm Nhìn Dự Án](#-1-giới-thiệu-tổng-quan--tầm-nhìn-dự-án)
2. [Ngăn Xếp Công Nghệ (Tech Stack) & Kiến Trúc Phần Mềm](#-2-ngăn-xếp-công-nghệ-tech-stack--kiến-trúc-phần-mềm)
3. [Điểm Mới Đột Phá Trên Phiên Bản 3.6.0](#-3-điểm-mới-đột-phá-trên-phiên-bản-360)
   - [Giao Diện Chính Zero-Chrome & Spatial Bento (Unified Command Header & Minimal Rail)](#1-giao-diện-chính-zero-chrome--spatial-bento)
   - [Trung Tâm Lệnh Nhanh Command Palette (`Ctrl + K`) & Bộ Đệm `0ms` Stale-While-Revalidate](#2-command-palette-ctrl--k--tối-ưu-hiệu-năng-0ms)
   - [Dự Báo Mưa 60 Phút Tới (60-Min Precipitation Nowcast) & Đường Cong Khí Tượng 24 Giờ Tương Tác](#3-60-minute-precipitation-nowcast--interactive-24h-telemetry-curve)
   - [Zen Mode 2.0: Nothing OS StandBy Dual-Matrix, Auto-Hide Chrome & Pomodoro Focus Timer](#4-zen-mode-20-standby-dual-matrix--pomodoro-focus-sanctuary)
   - [Desktop Widgets 3.0: Dynamic Island, Teenage Engineering Field Deck, Pure Typo HUD & Ghost Mode](#5-desktop-widgets-30-dynamic-island-field-deck-pure-typo--ghost-mode)
4. [Khám Phá Chi Tiết 7 Phân Hệ Chính (7 Core Modules)](#-4-khám-phá-chi-tiết-7-phân-hệ-chính-7-core-modules)
5. [Hướng Dẫn Cài Đặt & Khởi Chạy](#-5-hướng-dẫn-cài-đặt--khởi-chạy)
6. [Biên Niên Sử Phát Triển Toàn Diện (Changelog)](#-6-biên-niên-sử-phát-triển-toàn-diện)

---

## 🌟 1. Giới Thiệu Tổng Quan & Tầm Nhìn Dự Án
**Weather WinUI** là ứng dụng thời tiết hiện đại, được xây dựng hoàn toàn trên nền tảng **Windows App SDK (WinUI 3)** và **.NET 8**. Ứng dụng mang sứ mệnh tái định nghĩa trải nghiệm theo dõi thời tiết trên hệ điều hành Windows 10 & Windows 11: không dừng lại ở việc xem nhiệt độ đơn thuần, mà trở thành một **trợ lý vi khí hậu toàn diện** bảo vệ sức khỏe, tối ưu hóa trang phục đời thường, cảnh báo ngập úng đô thị và đồng hành cùng không gian làm việc tập trung của người dùng.

Ứng dụng đáp ứng chuẩn mực thẩm mỹ cao cấp với khả năng chuyển đổi tức thì giữa **Microsoft Fluent Design System 2.0** (vật liệu Mica Alt, Acrylic mờ, chuyển động Fluent Motion) và **Nothing Design System 3.6** (ngôn ngữ thiết kế tối giản biểu tượng của Nothing Tech & Teenage Engineering: ma trận điểm `Ndot77JPExtended` tự đóng gói, tương phản cao Monochrome Stealth & CMF Chalk, Glyph Interface LED).

---

## 🛠️ 2. Ngăn Xếp Công Nghệ (Tech Stack) & Kiến Trúc Phần Mềm

| Phân tầng | Công nghệ chủ đạo | Mục đích & Đặc tính kỹ thuật |
| :--- | :--- | :--- |
| **Giao diện (UI)** | **WinUI 3 (Windows App SDK 1.8+)** | XAML gốc hiện đại, tối ưu đồ họa DirectX 12 Compositor, tương thích 100% Windows 10 & 11 |
| **Nền tảng chạy** | **.NET 8 (C# 12)** | Biên dịch Self-Contained, tự đóng gói phông chữ `Ndot77JPExtended`, không đòi hỏi cài đặt trước .NET Runtime |
| **Kiến trúc mã** | **MVVM (CommunityToolkit.Mvvm)** | Phân tách ObservableObject, RelayCommand, bộ nhớ đệm `0ms` Stale-While-Revalidate |
| **Đồ họa động** | **Win2D & Canvas GPU Composition** | Hệ thống hạt khí quyển, đường cong Bézier 24h tương tác, la bàn số, biểu đồ thủy văn |
| **Nguồn dữ liệu** | **Open-Meteo & WMO Meteorological API** | Cập nhật vi khí hậu độ trễ mili-giây, miễn phí vĩnh viễn, không cần API Key |
| **Đóng gói** | **Inno Setup 6 (Self-Contained Installer)** | Tự động tạo Shortcut, biểu tượng AppUserModelId, tích hợp gỡ cài đặt sạch |

---

## 🚀 3. Điểm Mới Đột Phá Trên Phiên Bản 3.6.0

### 1. Giao Diện Chính Zero-Chrome & Spatial Bento
- **Single-Row Unified Command Header**: Hợp nhất toàn bộ khung điều khiển phía trên thành 1 dải Header 50px duy nhất, tinh gọn và hiện đại:
  - Cụm trái: Biểu tượng ứng dụng + Huy hiệu phiên bản `v3.6.0` / `NDOT // 3.6` + Dải **Smart Pinned Cities** (GPS + 4 thành phố ưu tiên + nút `📍 +12` mở bảng chọn nhanh 16 tỉnh thành).
  - Cụm giữa: **Command Capsule (`Ctrl + K`)** kết hợp tìm kiếm thành phố toàn cầu và bảng lệnh nhanh.
  - Cụm phải: Các nút hành động tinh gọn (So sánh đối kháng, Chia sẻ ảnh HD, Làm mới, Zen Mode).
- **Minimal Navigation Rail**: Thu gọn thanh điều hướng bên trái (`OpenPaneLength="216"`, `CompactPaneLength="52"`), thay thế các hộp màu rườm rà bằng chỉ báo viền mảnh và chữ số thứ tự Monospace (`01`..`07`) chuẩn Nothing OS.

### 2. Command Palette (`Ctrl + K`) & Tối Ưu Hiệu Năng `0ms`
- **Quick Command Palette (`Ctrl + K`)**: Bấm `Ctrl + K` từ bất kỳ đâu hoặc gõ lệnh `> nothing`, `> fluent`, `> dark`, `> light`, `> zen`, `> focus`, `> widget`, `> radar`, `> flood`, `> duel` ngay trên thanh tìm kiếm để thực thi lệnh tức thì mà không cần rời bàn phím.
- **`0ms` Stale-While-Revalidate City Cache**: Lưu trữ dữ liệu thời tiết & AQI trong bộ nhớ đệm thông minh theo từng tọa độ giúp việc chuyển đổi qua lại giữa các thành phố diễn ra tức thì (`0ms`) không cần chờ tải lại.
- **Lazy Offscreen Export & XAML De-bloating**: Tắt kết xuất cây giao diện `OffscreenExportHost` (`1920x1080`) khi không xuất ảnh và rút gọn hơn 1.240 dòng XAML dư thừa trong `MainPage.xaml`, giảm mạnh dung lượng RAM và tăng tốc độ đổi chủ đề Sáng/Tối.

### 3. 60-Minute Precipitation Nowcast & Interactive 24h Telemetry Curve
- **Dự Báo Mưa 60 Phút Tới (60-Min Precipitation Nowcast)**: Thanh biểu đồ 12 cột cường độ mưa theo từng mốc 5 phút (`+5m` đến `+60m`) kèm thông điệp cảnh báo mưa thời gian thực cho cả Fluent 2 và Nothing Design.
- **Đường Cong Khí Tượng 24 Giờ Tương Tác (`NothingHourlyCurveCanvas`)**: Biểu đồ spline chấm nét đứt đỏ Nothing Red `#D71921` đồng bộ 1:1 với 24 thẻ dự báo giờ; di chuột để xem con trỏ ngắm (crosshair) và thông số `GIỜ // NHIỆT ĐỘ // MƯA // GIÓ` thời gian thực.

### 4. Zen Mode 2.0: StandBy Dual-Matrix & Pomodoro Focus Sanctuary
- **Nothing OS StandBy Dual-Matrix**: Chế độ màn hình chờ lấy cảm hứng từ iOS 17 StandBy & Nothing OS với nền đen OLED tuyệt đối `#000000`, đồng hồ Ma Trận Điểm `Ndot77JPExtended` khổng lồ bên trái và cụm đồng hồ tròn khí tượng bên phải.
- **Auto-Hide Chrome (2.6s)**: Tự động làm mờ thanh điều khiển trên và dưới xuống `8%` sau 2.6 giây không di chuột để tôn vinh trọn vẹn không gian thiên nhiên và tránh lưu ảnh màn hình OLED.
- **Tích Hợp Đồng Hồ Tập Trung Pomodoro (`25:00` / `05:00`)**: Biến Zen Mode thành không gian làm việc sâu (Deep Work Sanctuary) kết hợp tiếng mưa rơi / gió rừng ASMR cùng bộ đếm Pomodoro 25 phút tập trung / 5 phút nghỉ ngơi ngay trên thanh dock.

### 5. Desktop Widgets 3.0: Dynamic Island, Field Deck, Pure Typo & Ghost Mode
Nâng tổng số kiểu dáng trong **Widget Studio** lên **20 kiểu dáng**, bổ sung 3 siêu phẩm mới và chế độ xuyên thấu chuột:
1. **`Dynamic Island Capsule` (`DynamicIsland`)**: Viên thuốc đen tuyền nhỏ gọn (`254×50 px`) ở trạng thái nghỉ, tự động bung rộng mượt mà (`324×152 px`) khi rê chuột vào để lộ diện dự báo 4 giờ tới và chỉ số độ ẩm/gió.
2. **`Teenage Engineering Field Deck` (`TeenageDeck`)**: Cảm hứng thiết bị âm thanh công nghiệp OP-1 Field với khung vỏ Titanium xám đen `#18181C`, thước đo kỹ thuật, núm xoay cam `#FF5500` và màn hình OLED sắc nét.
3. **`Pure Typography Wallpaper HUD` (`PureTypoHud`)**: Giao diện chữ nghệ thuật trong suốt 100% không khung viền, hòa quyện trực tiếp vào hình nền Desktop như một tác phẩm Rainmeter hiện đại.
4. **Ghost Mode (Click-Through `WS_EX_TRANSPARENT`)**: Công tắc cho phép chuột bấm xuyên hoàn toàn qua mọi Widget trên màn hình Desktop, không bao giờ che khuất hay cản trở thao tác làm việc.

---

## 🧭 4. Khám Phá Chi Tiết 7 Phân Hệ Chính (7 Core Modules)

### Tab 1: Tổng Quan Vi Khí Hậu, 60-Min Nowcast & 24h Interactive Scrubber
- Cấu trúc Spatial Bento kết hợp thời tiết trực tiếp, chỉ số cảm nhận, độ ẩm, áp suất, UV, tốc độ gió và chất lượng không khí AQI/PM2.5.
- Biểu đồ dự báo lượng mưa 60 phút tới (60-Minute Precipitation Nowcast) và đường cong nhiệt độ 24 giờ tương tác kéo thả mượt mà.

### Tab 2: Cảnh Báo Ngập Úng & Triều Cường Đô Thị (Urban Flood Hub)
- Theo dõi mực nước triều sông Sài Gòn, sông Hồng thời gian thực và biểu đồ sóng triều hình sin 24h.
- Đánh giá nguy cơ ngập úng các tuyến đường đô thị trọng yếu theo lượng mưa tích lũy và mực nước triều dâng.

### Tab 3: Bản Đồ & Radar Khí Tượng 360° Realtime (Radar Hub)
- Mô phỏng chùm tia quét Radar Doppler 360 độ chân thực cùng bản đồ tương tác nhiều lớp (Lượng mưa, Gió, Nhiệt độ, Mây, Áp suất).

### Tab 4: Trợ Lý Trang Phục "Hôm Nay Mặc Gì?" & Đời Sống (OOTD Hub)
- Phân tích kết hợp nhiệt độ, độ ẩm, gió và tia UV để gợi ý trang phục đi làm, đi chơi, thể thao và chỉ số hoạt động ngoài trời.

### Tab 5: Lịch Vạn Niên Âm Dương & Kế Hoạch Cá Nhân (Calendar Hub)
- Tính toán lịch Âm thiên văn chuẩn xác theo múi giờ Việt Nam (UTC+7), Can Chi ngày/tháng/năm, Giờ Hoàng Đạo và 24 Tiết khí.

### Tab 6: Widget Studio — Bộ Sưu Tập 20 Kiểu Dáng Desktop Widget & Ghost Mode
- Ghim ngay lập tức 20 kiểu dáng widget ra màn hình Desktop (bao gồm Dynamic Island, Teenage Engineering Field Deck, Pure Typography HUD, bộ tứ Nothing OS Widgets, Bento Pop/Pilot, Retro LCD và 8 mẫu Lễ Hội 4 Mùa).
- Tùy chỉnh độ mờ đục (20%–100%), Always on Top và **Ghost Mode (Click-Through)**.

### Tab 7: Trung Tâm Cài Đặt & Cá Nhân Hóa Toàn Trang (Settings Hub)
- Chuyển đổi tức thì giữa **Fluent 2 (Mặc định)** và **Nothing Design (OS)** cùng chế độ Sáng / Tối / Theo hệ thống hợp nhất.

---

## 📦 5. Hướng Dẫn Cài Đặt & Khởi Chạy

### Dành Cho Người Dùng (End Users)
1. **Tải bộ cài đặt chính thức**:
   - Tải về tệp cài đặt: `WeatherApp_Setup_v3.6.0.exe` từ thư mục phát hành.
   - Chạy tệp tin và làm theo hướng dẫn trên màn hình. Trình cài đặt sẽ tự động tạo Shortcut trên Desktop và Start Menu (tương thích hoàn toàn với cả Windows 10 và Windows 11).
2. **Khởi chạy ứng dụng**:
   - Mở Weather WinUI từ màn hình chính, nhấn `Ctrl + K` bất kỳ lúc nào để mở nhanh Bảng Lệnh Thông Minh (Command Palette).

### Dành Cho Lập Trình Viên (Developers)
```powershell
# 1. Khôi phục gói thư viện và biên dịch Release
& 'C:\Users\LMT\AppData\Local\Microsoft\dotnet\dotnet.exe' build "WeatherApp.csproj" -c Release -p:Platform=x64

# 2. Xuất bản phiên bản độc lập Self-Contained
& 'C:\Users\LMT\AppData\Local\Microsoft\dotnet\dotnet.exe' publish "WeatherApp.csproj" -c Release -p:Platform=x64 -o "publish"

# 3. Đóng gói bộ cài đặt Inno Setup (WeatherApp_Setup_v3.6.0.exe)
& "C:\Users\LMT\AppData\Local\Programs\Inno Setup 6\ISCC.exe" "installer.iss"
```

---

## 📜 6. Biên Niên Sử Phát Triển Toàn Diện

### Phiên Bản 3.6.0 (23/10/2026) — The Spatial, Command & Sanctuary Update
- **Single-Row Unified Command Header & Minimal Rail**: Hợp nhất thanh công cụ phía trên thành 1 hàng duy nhất với Smart Pinned Cities (4 thành phố ghim + menu 16 tỉnh thành) và Command Capsule (`Ctrl + K`).
- **Command Palette (`Ctrl + K`) & `0ms` City Cache**: Hỗ trợ lệnh nhanh bàn phím (`> nothing`, `> fluent`, `> dark`, `> light`, `> zen`, `> focus`, `> widget`) và bộ đệm `Stale-While-Revalidate` chuyển thành phố tức thì `0ms`.
- **60-Minute Precipitation Nowcast & Interactive 24h Curve Scrubber**: Thêm biểu đồ dự báo mưa 60 phút tới (mỗi mốc 5 phút) và đường cong nhiệt độ 24 giờ tương tác cho cả Fluent 2 và Nothing Design.
- **Zen Mode 2.0**: Bổ sung chế độ **Nothing OS StandBy Dual-Matrix**, tự động ẩn khung điều khiển sau 2.6 giây và đồng hồ tập trung **Pomodoro Focus Timer (`25:00` / `05:00`)**.
- **Desktop Widgets 3.0**: Ra mắt 3 kiểu widget mới (`Dynamic Island Capsule`, `Teenage Engineering Field Deck`, `Pure Typography Wallpaper HUD`) và chế độ **Ghost Mode (Click-Through `WS_EX_TRANSPARENT`)**.

### Phiên Bản 3.5.2 (21/10/2026)
- Tích hợp phông chữ tự đóng gói `Ndot77JPExtended` cùng bộ đôi widget `Nothing Circular Dial` và `Nothing Glance Pill`.
- Chuẩn hóa tương thích đa nền tảng Windows 10 & Windows 11.

### Phiên Bản 3.5.1 (21/10/2026)
- Hợp nhất bộ chọn giao diện Sáng/Tối duy nhất tại Cài đặt, sửa triệt để xung đột resource brush khi chuyển đổi giữa Nothing Design và Fluent 2.

---
*Bản quyền (C) 2026 Weather WinUI Team. Phát triển với đam mê dành cho cộng đồng Windows.*
