# MangaReader - Bài tập lớn

Ứng dụng đọc truyện tranh trực tuyến được xây dựng với **ASP.NET Core 8**, **SQL Server**, tích hợp **MangaDex API** và **Imgur API**.


## Tính Năng

### Frontend
- **Lưu tiến trình đọc**: Ghi nhớ chapter và trang đang đọc. Khi quay lại, ứng dụng đưa bạn đến vị trí gần nhất.
- **Lịch sử đọc truyện**: Theo dõi danh sách các truyện đã xem.
- **Yêu thích (Favorites)**: Lưu các bộ truyện vào thư viện cá nhân.
- **Giao diện**: Hỗ trợ chế độ sáng/tối tùy chỉnh theo tài khoản.
- **Tìm kiếm**: Lọc truyện theo tên, tác giả, thể loại và trạng thái.
- **Tương tác**: Hệ thống bình luận và phản ứng cho từng chapter/truyện.
### Backend/Admin
- **Đồng bộ MangaDex**: Lấy dữ liệu truyện, chapter từ MangaDex API qua Background Service.
- **Hệ thống Upload**: Upload ảnh manga lên Imgur API.
- **Thống kê**: Theo dõi lượt xem, các truyện phổ biến trong ngày/tuần/tháng.
- **Quản lý**: Phân quyền Admin để quản lý nội dung và người dùng.

## Công Nghệ Sử Dụng

- **Backend:** ASP.NET Core 8 MVC & Web API
- **ORM:** Entity Framework Core 9
- **Database:** SQL Server
- **Auth:** Cookie-based Authentication (BCrypt)
- **Storage:** Imgur API (ảnh bìa và avatar)
- **External API:** MangaDex API
- **Performance:** Sử dụng SQL Indexes và Split Queries để xử lý dữ liệu chapter.

## Hướng Dẫn Cài Đặt

### 1. Chuẩn bị
```bash
git clone <repo-url>
cd Mangarea/Mangareading
```

### 2. Cấu hình `appsettings.json`
Copy file mẫu và điền thông tin:
```bash
cp appsettings.Example.json appsettings.json
```
Các thông tin chính:
- `ConnectionStrings:DefaultConnection`: Chuỗi kết nối SQL Server.
- `ExternalServices:Imgur`: ClientId, Secret và RefreshToken.

### 3. Khởi tạo Database

Có thể chọn một trong hai cách:

#### Cách 1: Restore từ file Backup
Nếu muốn có sẵn dữ liệu mẫu (truyện, người dùng):
1. Mở **SQL Server Management Studio (SSMS)**.
2. Chuột phải vào `Databases` -> `Restore Database...`.
3. Chọn `Device` -> Tìm đến file: `Mangareading/SQL/MangaReaderDB-2025421-13-16-46.bak`.
4. Trong tab **Options**, chọn `Overwrite the existing database (WITH REPLACE)`.
5. Nhấn **OK**.

#### Cách 2: Tự động khởi tạo (Dữ liệu trống)
- Chạy app → Schema tự sinh qua EF Core Migrations.

> **Cấp quyền Admin**: Đăng ký tài khoản tại `/Account/Register`, sau đó chạy:
> ```sql
> UPDATE Users SET IsAdmin = 1 WHERE Username = 'tên_của_bạn';
> ```

### 4. Chạy Ứng dụng
```bash
dotnet run --project Mangareading
```
Truy cập: `http://localhost:5000`

## Cấu Trúc Project

```
Controllers/          — Điều hướng MVC và API
DTOs/                 — Cấu trúc truyền dữ liệu
Middleware/           — Theme, Security Headers
Models/               — EF Core Entities & ViewModels
Repositories/         — Lớp truy xuất dữ liệu
Services/             — Logic nghiệp vụ (MangaDex Sync, Statistics)
SQL/                  — Scripts SQL & Trigger
Views/                — Giao diện Razor Views
wwwroot/              — Assets (CSS, JS, Images)
```