# HƯỚNG DẪN CÀI ĐẶT & CHẠY DỰ ÁN IPHONESTORE (BLAZOR WEB APP)

Dự án website quản lý và kinh doanh iPhone xây dựng trên nền tảng **Blazor Web App (.NET 8 Interactive Server)** & **Entity Framework Core**.

---

## 1. Cơ sở dữ liệu (Database)
File script cơ sở dữ liệu đã được đính kèm sẵn trong thư mục:
📁 **`Database/IPhoneStoreDb_Complete.sql`**

### Cách import vào SQL Server:
1. Mở công cụ **SQL Server Management Studio (SSMS)** hoặc **Azure Data Studio**.
2. Kết nối tới SQL Server instance của bạn (ví dụ: `localhost`, `.`, hoặc `(localdb)\mssqllocaldb`).
3. Mở file `Database/IPhoneStoreDb_Complete.sql` và nhấn **Execute (F5)**.
   > Script sẽ tự động tạo Database `IPhoneStoreDb` cùng toàn bộ bảng, khóa chính/ngoại, chỉ mục và nạp đầy đủ dữ liệu mẫu (sản phẩm, IMEI, khách hàng, bảo hành, tài khoản nhân sự/quản trị).

---

## 2. Cấu hình kết nối (Connection String)
Mở file `appsettings.json` và kiểm tra chuỗi kết nối:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=IPhoneStoreDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true;"
}
```
*(Thay đổi `Server=localhost` thành tên Server SQL của bạn nếu khác).*

---

## 3. Cách chạy dự án
- **Cách 1: Sử dụng Visual Studio**
  - Mở file solution/project trong Visual Studio 2022.
  - Nhấn **F5** hoặc nút **Start Debugging**.
- **Cách 2: Sử dụng dòng lệnh (CLI)**
  ```bash
  dotnet run
  ```
- Truy cập trình duyệt theo URL hiển thị trên terminal.

---

## 4. Tài khoản đăng nhập thử nghiệm

| Vai trò | Tên đăng nhập (UserName) | Email | Mật khẩu mặc định |
| :--- | :--- | :--- | :--- |
| **Quản trị viên (Admin)** | `admin` | `admin@istore.vn` | `admin123` |
| **Quản lý (Manager)** | `manager01` | `manager@istore.vn` | `admin123` |
| **Nhân viên (Staff)** | `staff01` | `staff@istore.vn` | `admin123` |
| **Thu ngân (Cashier)** | `cashier01` | `cashier@istore.vn` | `admin123` |
| **Giao hàng (Shipper)** | `shipper01` | `shipper@istore.vn` | `admin123` |
