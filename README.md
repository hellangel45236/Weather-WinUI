# 🌤️ Weather WinUI (Version 3.7.0)
### Hệ Điều Hành Không Gian Khí Quyển (Atmospheric Spatial Canvas & Modular Dock OS) Dành Cho Windows 10 & 11
> **Nền tảng:** Windows App SDK (WinUI 3) • .NET 8 • C# 12 • Tri-Design Engine (Fluent 2 • Spatial Vision 3.7 • Nothing OS 3.7)  
> **Nhánh phát triển chính thức:** `main` | **Phiên bản:** `v3.7.0 (Bản Phát Hành Chính Thức)`  
> **Bản phát hành tiền nhiệm:** `v3.6.1` • `v3.5.2` • `v3.5.1` • `v3.5.0` • `v3.4.1` • `v3.4.0` • `v3.3.1` • `v3.3.0` • `v3.0.0`

---

## 📑 Mục Lục
1. [Giới Thiệu Tổng Quan & Tầm Nhìn Dự Án](#-1-giới-thiệu-tổng-quan--tầm-nhìn-dự-án)
2. [Ngăn Xếp Công Nghệ (Tech Stack) & Kiến Trúc Phần Mềm](#-2-ngăn-xếp-công-nghệ-tech-stack--kiến-trúc-phần-mềm)
3. [6 Trụ Cột Đột Phá Trên Phiên Bản 3.7.0](#-3-6-trụ-cột-đột-phá-trên-phiên-bản-370)
   - [Floating Bottom Spatial Dock & Không Gian Tràn Viền Edge-to-Edge](#1-floating-bottom-spatial-dock--không-gian-tràn-viền-edge-to-edge)
   - [Ngôn Ngữ Thiết Kế Thứ 3: Spatial Vision 3.7 (Aurora Glass)](#2-ngôn-ngữ-thiết-kế-thứ-3-spatial-vision-37-aurora-glass)
   - [Nothing OS 3.7: Ma Trận Điểm Động 13×13 (`NothingLiveMatrixCanvas`) & Chế Độ Giấy Điện Tử E-Ink (`#F4F1EA`)](#3-nothing-os-37-ma-trận-điểm-động-1313--chế-độ-giấy-điện-tử-e-ink-f4f1ea)
   - [Cỗ Máy Thời Gian 24 Giờ (24h + 7-Day Time-Machine Ribbon)](#4-cỗ-máy-thời-gian-24-giờ-24h--7-day-time-machine-ribbon)
   - [Desktop Widgets 3.7: Thanh Taskbar Siêu Mỏng (`TaskbarStrip`) & Khối Kính Không Gian (`SpatialCube`)](#5-desktop-widgets-37-taskbar-strip--spatial-aurora-cube)
   - [Tương Thích Tuyệt Đối 100% Windows 10 & Windows 11](#6-tương-thích-tuyệt-đối-100-windows-10--windows-11)
4. [Khám Phá Chi Tiết 7 Phân Hệ Chính (7 Core Modules)](#-4-khám-phá-chi-tiết-7-phân-hệ-chính-7-core-modules)
5. [Hướng Dẫn Cài Đặt & Khởi Chạy](#-5-hướng-dẫn-cài-đặt--khởi-chạy)
6. [Biên Niên Sử Phát Triển Toàn Diện (Changelog)](#-6-biên-niên-sử-phát-triển-toàn-diện)

---

## 🌟 1. Giới Thiệu Tổng Quan & Tầm Nhìn Dự Án
**Weather WinUI** là ứng dụng thời tiết hiện đại, được xây dựng hoàn toàn trên nền tảng **Windows App SDK (WinUI 3)** và **.NET 8**. Ở cột mốc **Version 3.7.0**, ứng dụng lột xác hoàn toàn khỏi bố cục Sidebar truyền thống để trở thành một **Hệ Điều Hành Không Gian Khí Quyển (Atmospheric Spatial Canvas & Modular Dock OS)** trên cả **Windows 10** và **Windows 11**.

Ứng dụng tiên phong trang bị **Bộ Ba Ngôn Ngữ Thiết Kế (Tri-Design Engine)** chuyển đổi tức thì `0ms`:
- **Microsoft Fluent Design System 2.0**: Vật liệu Mica Alt, Acrylic mờ và chuyển động Fluent Motion.
- **Spatial Vision 3.7 (Aurora Glass)**: Cảm hứng từ Apple visionOS & Raycast với vầng hào quang cực quang đa tầng và viền kính phản quang chéo (Specular Border Highlight) hoạt động đồng nhất trên cả Windows 10 và Windows 11.
- **Nothing Design System 3.7**: Thẩm mỹ tối giản cơ học của Nothing Tech & Teenage Engineering với phông chữ ma trận điểm `Ndot77JPExtended`, **Ma Trận LED 13×13 Tương Tác Trực Tiếp** và chế độ **Giấy Điện Tử E-Ink (`#F4F1EA`)**.

---

## 🛠️ 2. Ngăn Xếp Công Nghệ (Tech Stack) & Kiến Trúc Phần Mềm

| Phân tầng | Công nghệ chủ đạo | Mục đích & Đặc tính kỹ thuật |
| :--- | :--- | :--- |
| **Giao diện (UI)** | **WinUI 3 (Windows App SDK 1.8+)** | XAML gốc hiện đại, tối ưu đồ họa DirectX 12 Compositor, tương thích 100% Windows 10 & 11 |
| **Nền tảng chạy** | **.NET 8 (C# 12)** | Biên dịch Self-Contained, tự đóng gói phông chữ `Ndot77JPExtended`, không đòi hỏi cài đặt trước .NET Runtime |
| **Kiến trúc mã** | **MVVM (CommunityToolkit.Mvvm)** | Phân tách ObservableObject, RelayCommand, bộ nhớ đệm `0ms` Stale-While-Revalidate |
| **Đồ họa động** | **Win2D & Canvas GPU Composition** | Hệ thống hạt khí quyển, đường cong Bézier 24h đa ngày, ma trận LED 13×13, la bàn số, biểu đồ thủy văn |
| **Nguồn dữ liệu** | **Open-Meteo & WMO Meteorological API** | Cập nhật vi khí hậu độ trễ mili-giây, miễn phí vĩnh viễn, không cần API Key |
| **Đóng gói** | **Inno Setup 6 (Self-Contained Installer)** | Tự động tạo Shortcut, biểu tượng AppUserModelId, tích hợp gỡ cài đặt sạch |

---

## 🚀 3. 6 Trụ Cột Đột Phá Trên Phiên Bản 3.7.0

### 1. Floating Bottom Spatial Dock & Không Gian Tràn Viền Edge-to-Edge
- **Loại bỏ sự gò bó của Sidebar trái truyền thống**: Ở chế độ mặc định **Floating Dock**, thanh điều hướng bên trái được thu ẩn hoàn toàn, giải phóng **100% chiều ngang cửa sổ** cho bức tranh bầu trời khí quyển và lưới thẻ Bento.
- **Thanh Dock Nổi Không Gian (`FloatingBottomDock`)**: Neo lơ lửng tại trung tâm đáy màn hình như macOS / visionOS / Nothing Launcher, tích hợp 7 viên thuốc điều hướng (`01 Tổng Quan` .. `07 Cài Đặt`), huy hiệu vi khí hậu trực tiếp (`30°C`, `LIVE`, `3.7`) và nút chuyển đổi 1 chạm giữa **Dock Nổi (Floating Dock)** và **Sidebar Trái (Left Sidebar)**.

### 2. Ngôn Ngữ Thiết Kế Thứ 3: Spatial Vision 3.7 (Aurora Glass)
- **Hào Quang Cực Quang Đa Tầng (`SpatialAuroraBackdropGrid`)**: Kết xuất bầu trời đêm cực quang sâu thẳm với các khối cầu ánh sáng xanh băng (`#38BDF8`), tím tinh vân (`#A855F7`) và ngọc lục bảo (`#10B981`) chạy mượt 100% trên cả Windows 10 và Windows 11 mà không phụ thuộc vào DWM Mica của hệ điều hành.
- **Thẻ Kính Phản Quang (Specular Glass Cards)**: Viền thẻ ánh sáng chéo mô phỏng khúc xạ ánh sáng trên mép kính vát 3D cao cấp.

### 3. Nothing OS 3.7: Ma Trận Điểm Động 13×13 & Chế Độ Giấy Điện Tử E-Ink (`#F4F1EA`)
- **Interactive 13×13 Live Glyph Matrix (`NothingLiveMatrixCanvas`)**: Nằm ngay bên phải thẻ Hero của giao diện Nothing OS là cụm màn hình **169 bóng LED ma trận điểm (13×13)** hoạt họa thời gian thực theo thời tiết (hạt mưa rơi chéo, mặt trời xoay vòng với tâm đỏ `#D71921`, tia chớp giông bão) và tạo **gợn sóng đỏ Nothing Red** tương tác trực tiếp khi người dùng rê chuột lướt qua.
- **Chế Độ Giấy Điện Tử Monochrome E-Ink Paper Mode (`#F4F1EA`)**: Bấm nút `E-INK` trên thanh Header hoặc gõ lệnh `> eink` để chuyển toàn bộ giao diện Nothing OS sang chất liệu giấy đọc sách Kindle / Teenage Engineering ấm áp (`#F4F1EA`) với mực in đen tuyền (`#141413`), dịu mắt tuyệt đối khi làm việc lâu.

### 4. Cỗ Máy Thời Gian 24 Giờ (24h + 7-Day Time-Machine Ribbon)
- **Liên kết trực tiếp Dự báo 7 ngày và Đường cong 24 giờ**: Bấm vào **bất kỳ ngày nào** trong bảng Dự báo 7 ngày tới (ở cả Fluent 2, Spatial Vision và Nothing OS), biểu đồ đường cong 24 giờ phía trên sẽ lập tức chuyển sang **nhá trước trọn vẹn 24 khung giờ (`00:00` – `23:00`)** của ngày hôm đó.
- **Huy hiệu `TIME-MACHINE 24H` & Nút Quay Về Hiện Tại**: Hiển thị rõ ngày đang khám phá cùng nút **"Quay về 24h hiện tại"** chỉ với 1 cú nhấp chuột.

### 5. Desktop Widgets 3.7: Taskbar Strip & Spatial Aurora Cube
Nâng tổng số kiểu dáng trong **Widget Studio** lên **22 kiểu dáng**, bổ sung 2 thiết kế đột phá cho phiên bản 3.7.0:
1. **`Taskbar Weather Strip 3.7` (`TaskbarStrip` — `236×42 px`)**: Thanh trạng thái siêu mảnh nằm gọn sát mép Taskbar hoặc cạnh màn hình (`[Chấm Xanh Live] [Icon] 30°C | TP.HCM | AQI 42`), kết hợp hoàn hảo với **Ghost Mode (Click-Through)**.
2. **`Spatial Aurora Glass Cube 3.7` (`SpatialCube` — `276×220 px`)**: Khối kính không gian 3D mang phong cách visionOS với viền kính phát sáng và **đường cong nhiệt độ Bezier 12 giờ tới (`WidgetSpatialCurveCanvas`)** được vẽ trực tiếp ngay bên trong Desktop Widget.

### 6. Tương Thích Tuyệt Đối 100% Windows 10 & Windows 11
- Tự động nhận diện phiên bản hệ điều hành và hỗ trợ cờ kiểm thử `--win10`.
- Trên **Windows 10**: Tự động kích hoạt lớp nền `SpatialAuroraBackdropGrid` đa sắc, bo góc cửa sổ thông minh và sử dụng `SetWindowLongPtr` (`WS_EX_TOOLWINDOW | WS_EX_TRANSPARENT`) chuẩn Win32 giúp mọi tính năng từ Spatial Vision 3.7, Floating Dock đến Ghost Mode Widget hoạt động mượt mà không một lỗi nhỏ.

---

## 🧭 4. Khám Phá Chi Tiết 7 Phân Hệ Chính (7 Core Modules)

### Tab 1 (`01`): Tổng Quan Vi Khí Hậu, Time-Machine 24h & Live Glyph Matrix
- Cấu trúc Spatial Bento kết hợp thời tiết trực tiếp, chỉ số cảm nhận, độ ẩm, áp suất, UV, tốc độ gió và chất lượng không khí AQI/PM2.5.
- Biểu đồ dự báo mưa 60 phút tới (60-Min Precipitation Nowcast), đường cong Bézier 24 giờ tương tác đa ngày (Time-Machine 24h) và màn hình LED ma trận 13×13 trên Nothing OS.

### Tab 2 (`02`): Cảnh Báo Ngập Úng & Triều Cường Đô Thị (Urban Flood Hub)
- Theo dõi mực nước triều sông Sài Gòn, sông Hồng thời gian thực và biểu đồ sóng triều hình sin 24h cùng hơn 40 tuyến đường cảnh báo ngập.

### Tab 3 (`03`): Bản Đồ & Radar Khí Tượng 360° Realtime (Radar Hub)
- Mô phỏng chùm tia quét Radar Doppler 360 độ chân thực cùng bản đồ tương tác nhiều lớp (Lượng mưa, Gió, Nhiệt độ, Mây, Áp suất).

### Tab 4 (`04`): Lịch Vạn Niên Âm Dương & Kế Hoạch Cá Nhân (Calendar Hub)
- Tính toán lịch Âm thiên văn chuẩn xác theo múi giờ Việt Nam (UTC+7), Can Chi ngày/tháng/năm, Giờ Hoàng Đạo và 24 Tiết khí.

### Tab 5 (`05`): Trợ Lý Trang Phục "Hôm Nay Mặc Gì?" & Thẻ Thời Tiết HD (OOTD Hub)
- Phân tích nhiệt độ, độ ẩm, gió và tia UV để gợi ý trang phục đi làm, đi chơi, thể thao và xuất ảnh thẻ thời tiết Full HD.

### Tab 6 (`06`): Widget Studio — Bộ Sưu Tập 22 Kiểu Dáng Desktop Widget & Ghost Mode
- Ghim ngay lập tức 22 kiểu dáng widget ra màn hình Desktop (bao gồm **Taskbar Strip 3.7**, **Spatial Aurora Cube 3.7**, Dynamic Island, Teenage Engineering Field Deck, Pure Typography HUD, bộ tứ Nothing OS Widgets, Bento Pop/Pilot, Retro LCD và 8 mẫu Lễ Hội 4 Mùa).

### Tab 7 (`07`): Trung Tâm Cài Đặt & Bộ Ba Ngôn Ngữ Thiết Kế (Settings Hub)
- Chuyển đổi tức thì giữa **Fluent 2**, **Spatial Vision 3.7 (Aurora Glass)** và **Nothing Design (OS)**, bật/tắt **Chế độ giấy điện tử E-Ink (`#F4F1EA`)** và lựa chọn giữa **Thanh Dock Nổi (Floating Dock)** hoặc **Sidebar Trái**.

---

## 📦 5. Hướng Dẫn Cài Đặt & Khởi Chạy

### Dành Cho Người Dùng (End Users)
1. **Tải bộ cài đặt chính thức**:
   - Tải về tệp cài đặt: `WeatherApp_Setup_v3.7.0.exe` từ thư mục `Output/`.
   - Chạy tệp tin và làm theo hướng dẫn trên màn hình. Trình cài đặt tự động tạo Shortcut trên Desktop và Start Menu, tương thích hoàn toàn với cả **Windows 10** và **Windows 11**.
2. **Phím tắt & Lệnh nhanh (`Ctrl + K`)**:
   - Nhấn `Ctrl + K` bất kỳ lúc nào để mở Command Palette và gõ nhanh: `> spatial`, `> nothing`, `> fluent`, `> eink`, `> dock`, `> sidebar`, `> zen`, `> focus`, `> widget`.

### Dành Cho Lập Trình Viên (Developers)
```powershell
# 1. Khôi phục gói thư viện và biên dịch Release
& 'C:\Users\LMT\AppData\Local\Microsoft\dotnet\dotnet.exe' build "WeatherApp.csproj" -c Release -p:Platform=x64

# 2. Xuất bản phiên bản độc lập Self-Contained
& 'C:\Users\LMT\AppData\Local\Microsoft\dotnet\dotnet.exe' publish "WeatherApp.csproj" -c Release -p:Platform=x64 -o "publish"

# 3. Đóng gói bộ cài đặt Inno Setup (WeatherApp_Setup_v3.7.0.exe)
& "C:\Users\LMT\AppData\Local\Programs\Inno Setup 6\ISCC.exe" "installer.iss"
```

---

## 📜 6. Biên Niên Sử Phát Triển Toàn Diện

### Phiên Bản 3.7.0 (23/10/2026) — Atmospheric Spatial Canvas & Modular Dock OS
- **Floating Bottom Spatial Dock & Sidebar-Free Canvas**: Thay thế Sidebar trái truyền thống bằng thanh Dock nổi hiện đại dưới đáy màn hình với 7 viên thuốc điều hướng và huy hiệu nhiệt độ trực tiếp; hỗ trợ chuyển đổi 1 chạm về Sidebar trái bất kỳ lúc nào.
- **Ngôn Ngữ Thiết Kế Thứ 3 — Spatial Vision 3.7 (Aurora Glass)**: Bổ sung chủ đề kính không gian cực quang lấy cảm hứng từ Apple visionOS & Raycast, hoạt động mượt mà 100% trên cả Windows 10 và Windows 11.
- **Nothing OS 3.7 — Interactive 13×13 Live Glyph Matrix & E-Ink Paper Mode**: Tích hợp màn hình 169 điểm LED hoạt họa thời tiết và phản hồi gợn sóng đỏ khi di chuột (`NothingLiveMatrixCanvas`), cùng chế độ giấy điện tử **Monochrome E-Ink (`#F4F1EA`)** chống mỏi mắt.
- **24h + 7-Day Time-Machine Ribbon**: Cho phép bấm vào bất kỳ ngày nào trong bảng Dự báo 7 ngày để xem đường cong nhiệt độ/mưa 24 giờ chi tiết của ngày tương lai đó.
- **Desktop Widgets 3.7**: Ra mắt 2 mẫu widget mới — **Taskbar Weather Strip (`236×42`)** siêu mảnh và **Spatial Aurora Glass Cube (`276×220`)** tích hợp đường cong nhiệt độ Bezier 12 giờ ngay trên Desktop.

### Phiên Bản 3.6.1 (23/10/2026) — The Spatial, Command & Sanctuary Update
- **Single-Row Unified Command Header & Command Palette (`Ctrl + K`)**: Hợp nhất thanh công cụ phía trên thành 1 hàng duy nhất với Smart Pinned Cities và bộ đệm `0ms` Stale-While-Revalidate.
- **60-Minute Precipitation Nowcast & Interactive 24h Curve Scrubber**: Thêm biểu đồ dự báo mưa 60 phút tới và đường cong nhiệt độ 24 giờ tương tác cho cả Fluent 2 và Nothing Design.
- **Zen Mode 2.0 & Desktop Widgets 3.0**: Bổ sung chế độ **Nothing OS StandBy Dual-Matrix**, đồng hồ **Pomodoro Focus Timer (`25:00` / `05:00`)**, 3 kiểu widget (`Dynamic Island Capsule`, `Teenage Engineering Field Deck`, `Pure Typography Wallpaper HUD`) và **Ghost Mode (Click-Through)**.

### Phiên Bản 3.5.2 (21/10/2026)
- Tích hợp phông chữ tự đóng gói `Ndot77JPExtended` cùng bộ đôi widget `Nothing Circular Dial` và `Nothing Glance Pill`.

---
*Bản quyền (C) 2026 Weather WinUI Team. Phát triển với đam mê dành cho cộng đồng Windows 10 & Windows 11.*
