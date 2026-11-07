# TÀI LIỆU ĐẶC TẢ NGHIỆP VỤ & KIẾN TRÚC BACKEND HỆ THỐNG BÁN LẺ IPHONE
## (Chuẩn tham chiếu: Thế Giới Di Động / TopZone / FPT Shop / Apple Authorized Reseller)
**Dự án**: IPhoneStore (Multi-UI: ASP.NET Core MVC, Blazor Server, Razor Pages)  
**Phiên bản**: 3.0 - Đặc tả Toàn diện (User Storefront & Admin Operations)  
**Ngày cập nhật**: 24/10/2026  

---

## MỤC LỤC
1. [TỔNG QUAN KIẾN TRÚC & MÔ HÌNH 2 PHÂN HỆ](#1-tổng-quan-kiến-trúc--mô-hình-2-phân-hệ)
2. [PHÂN HỆ NGƯỜI DÙNG & MUA SẮM (USER / STOREFRONT PORTAL)](#2-phân-hệ-người-dùng--mua-sắm-user--storefront-portal)
   - [2.1. Duyệt hàng & Bộ lọc Đa tiêu chí (Product Catalog Browsing)](#21-duyệt-hàng--bộ-lọc-đa-tiêu-chí-product-catalog-browsing)
   - [2.2. Trang Chi tiết Sản phẩm (Product Details)](#22-trang-chi-tiết-sản-phẩm-product-details)
   - [2.3. Giỏ hàng & Thao tác Mua sắm (Shopping Cart Flow)](#23-giỏ-hàng--thao-tác-mua-sắm-shopping-cart-flow)
   - [2.4. Quản lý Tài khoản & Định danh Khách hàng (Customer Authentication & Identity Engine)](#24-quản-lý-tài-khoản--định-danh-khách-hàng-customer-authentication--identity-engine)
     - [2.4.1. Quy chuẩn Dữ liệu Định danh & Đăng ký Khách hàng](#241-quy-chuẩn-dữ-liệu-định-danh--đăng-ký-khách-hàng)
     - [2.4.2. Luồng Đăng nhập Linh hoạt & Cơ chế Phòng vệ Brute-Force Đa tầng](#242-luồng-đăng-nhập-linh-hoạt--cơ-chế-phòng-vệ-brute-force-đa-tầng)
     - [2.4.3. Quản lý Hồ sơ & Sổ Địa chỉ Chuẩn hóa 4 Cấp độ](#243-quản-lý-hồ-sơ--sổ-địa-chỉ-chuẩn-hóa-4-cấp-độ)
     - [2.4.4. Ma trận Ràng buộc Dữ liệu & Xử lý Ngoại lệ (Validation Matrix)](#244-ma-trận-ràng-buộc-dữ-liệu--xử-lý-ngoại-lệ-validation-matrix)
   - [2.5. Luồng Thanh toán Bắt buộc Định danh (Checkout Authentication & Autofill)](#25-luồng-thanh-toán-bắt-buộc-định-danh-checkout-authentication--autofill)
   - [2.6. Tạo Đơn hàng An toàn (ACID Order Placement)](#26-tạo-đơn-hàng-an-toàn-acid-order-placement)
   - [2.7. Quản lý Đơn hàng & Tra cứu Bảo hành Cá nhân](#27-quản-lý-đơn-hàng--tra-cứu-bảo-hành-cá-nhân)
3. [PHÂN HỆ QUẢN TRỊ VẬN HÀNH (ADMIN DASHBOARD & OPERATIONS)](#3-phân-hệ-quản-trị-vận-hành-admin-dashboard--operations)
   - [3.1. Quản lý Danh mục & Sản phẩm (Product Catalog Management - CRUD)](#31-quản-lý-danh-mục--sản-phẩm-product-catalog-management---crud)
   - [3.2. Quản lý Đơn hàng (Order Management & Fulfillment)](#32-quản-lý-đơn-hàng-order-management--fulfillment)
   - [3.3. Quản lý Bảo hành & Đổi trả (Warranty & Return Management)](#33-quản-lý-bảo-hành--đổi-trả-warranty--return-management)
   - [3.4. Thống kê & Báo cáo Thông minh (Dashboard & Analytics)](#34-thống-kê--báo-cáo-thông-minh-dashboard--analytics)
   - [3.5. Quản lý Khách hàng & Tài khoản (Customer & Account Management)](#35-quản-lý-khách-hàng--tài-khoản-customer--account-management)
4. [CƠ CHẾ KỸ THUẬT CHUYÊN SÂU (ADVANCED BACKEND ENGINES)](#4-cơ-chế-kỹ-thuật-chuyên-sâu-advanced-backend-engines)
   - [4.1. Kiểm soát Đồng thời & Chống bán vượt kho (Optimistic Concurrency)](#41-kiểm-soát-đồng-thời--chống-bán-vượt-kho-optimistic-concurrency)
   - [4.2. Tiến trình Tự động Hủy đơn & Hoàn kho (Background Hosted Service)](#42-tiến-trình-tự-động-hủy-đơn--hoàn-kho-background-hosted-service)
   - [4.3. Hệ thống Nhật ký Kiểm toán (Audit Logs)](#43-hệ-thống-nhật-ký-kiểm-toán-audit-logs)
5. [THIẾT KẾ CƠ SỞ DỮ LIỆU MỞ RỘNG (DATABASE SCHEMA)](#5-thiết-kế-cơ-sở-dữ-liệu-mở-rộng-database-schema)
6. [KIẾN TRÚC TÁI SỬ DỤNG CHO 3 DỰ ÁN (MVC, BLAZOR, RAZOR)](#6-kiến-trúc-tái-sử-dụng-cho-3-dự-án-mvc-blazor-razor)

---

## 1. TỔNG QUAN KIẾN TRÚC & MÔ HÌNH 2 PHÂN HỆ

Hệ thống được thiết kế theo chuẩn phân tán 2 phân hệ rõ rệt chạy trên nền tảng .NET 9 và cơ sở dữ liệu `IPhoneStoreDb`:

```
                           HỆ THỐNG BÁN LẺ IPHONESTORE
                                       │
            ┌──────────────────────────┴──────────────────────────┐
            ▼                                                     ▼
   PHÂN HỆ NGƯỜI DÙNG (USER)                             PHÂN HỆ QUẢN TRỊ (ADMIN)
   - Duyệt & Lọc iPhone (Series/Giá/Bộ nhớ)              - Module 1: CRUD iPhone, Biến thể & Nhập IMEI
   - Xem Chi tiết & Thông số kỹ thuật                    - Module 2: Quản lý, Duyệt đơn & Gán IMEI kho
   - Giỏ hàng & Thao tác Ajax (Session/Cookie)           - Module 3: Tiếp nhận Bảo hành & Đổi trả 1-1
   - Đăng ký đa kênh (Email/SĐT/Social, Anti-Dropoff)    - Module 4: Thống kê Doanh thu & Cảnh báo kho
   - Đăng nhập 1 ô duy nhất & Chống Brute-force 3 tầng   - Module 5: Quản lý Khách hàng, Hồ sơ & Tài khoản (Customer Management)
   - Sổ địa chỉ 4 cấp & Tự động điền Checkout
   - Đặt hàng an toàn (ACID Transaction)
   - Tra cứu Đơn hàng cá nhân & Bảo hành Apple Care
```

---

## 2. PHÂN HỆ NGƯỜI DÙNG & MUA SẮM (USER / STOREFRONT PORTAL)

### 2.1. Duyệt hàng & Bộ lọc Đa tiêu chí (Product Catalog Browsing)
- Khách hàng xem danh sách các dòng iPhone mới nhất: iPhone 16 Series, 15 Series, 14 Series, 13 & Cũ.
- **Bộ lọc động**:
  - Theo Danh mục (Category/Series).
  - Theo Dung lượng bộ nhớ: 128GB, 256GB, 512GB, 1TB.
  - Theo Màu sắc Titanium / Pastel.
  - Sắp xếp: Giá tăng dần, Giá giảm dần, Mới nhất.
- Tìm kiếm tức thời theo từ khóa tên máy hoặc thông số chip (A18 Pro, A17 Pro...).

### 2.2. Trang Chi tiết Sản phẩm (Product Details)
- Hình ảnh sắc nét, mô tả Apple Intelligence, viền màn hình, cụm Camera Control.
- Bảng thông số kỹ thuật (Chipset, RAM, Màn hình OLED ProMotion 120Hz, Dung lượng pin, Khung vỏ Titan/Nhôm).
- Hiển thị trực quan tình trạng hàng: *"Còn hàng (Giao nhanh 2h)"* hoặc *"Tạm hết hàng"*.
- Lựa chọn phiên bản màu sắc và bộ nhớ trực tiếp trên giao diện.

### 2.3. Giỏ hàng & Thao tác Mua sắm (Shopping Cart Flow)
- Thêm vào giỏ hàng (`AddToCart`) bằng AJAX thời gian thực, hiển thị Badge số lượng trên Navbar.
- Điều chỉnh số lượng (`UpdateQuantity`), xóa sản phẩm (`RemoveItem`), xóa toàn bộ giỏ (`ClearCart`).
- Lưu trữ giỏ hàng độc lập qua Session / Cookie / LocalStorage, không bắt buộc đăng nhập khi xem giỏ.

### 2.4. Quản lý Tài khoản & Định danh Khách hàng (Customer Authentication & Identity Engine)

Hệ thống định danh khách hàng được thiết kế theo tiêu chuẩn thương mại điện tử hiện đại và quy chuẩn chuỗi bán lẻ Apple Authorized Reseller (AAR): tối đa hóa tỷ lệ chuyển đổi khi đăng ký, linh hoạt và thuận tiện khi đăng nhập, bảo vệ đa tầng trước các đợt tấn công dò quét brute-force, và chuẩn hóa tuyệt đối dữ liệu địa chỉ phục vụ giao vận.

```
[Khách truy cập] 
       │
       ├──► ĐĂNG KÝ (Tối ưu Drop-off rate: Không ép nhập địa chỉ)
       │      │
       │      ├─ Cách A: Số điện thoại VN (10 số, đầu 03x-09x)
       │      ├─ Cách B: Email / Gmail (Canonical Email, chống alias ảo)
       │      ├─ Cách C: Social Login (Google, Apple ID, Facebook)
       │      ├─ Mật khẩu mạnh (3/4 nhóm, chống mật khẩu phổ biến)
       │      ├─ Xác thực OTP (SMS / Email) ──► Kích hoạt tài khoản
       │      └─ Tự động tạo hồ sơ Customer & Auto-Login
       │
       └──► ĐĂNG NHẬP (Định danh 1 ô duy nhất: Username / Email / SĐT)
              │
              ├─ Bộ phân loại đầu vào tất định (Deterministic Input Classifier)
              ├─ Ghi nhớ đăng nhập (Remember Me): 24h (Session) vs 30 ngày (Refresh Token)
              ├─ Phòng vệ Brute-force 3 Tầng:
              │    ├─ Sai 3 lần: Bắt buộc CAPTCHA (Turnstile / reCAPTCHA)
              │    ├─ Sai 5 lần: Khóa tạm thời 15 phút + Cảnh báo an ninh
              │    └─ Sai 10 lần: Khóa vĩnh viễn (Yêu cầu Quên mật khẩu / Reset)
              └─ Thông báo bảo mật chung: Chống dò quét danh tính (Username Enumeration)
```

#### 2.4.1. Quy chuẩn Dữ liệu Định danh & Đăng ký Khách hàng

##### 1. Tên đăng nhập (Username)
- **Tính bắt buộc**: Bắt buộc nếu người dùng chọn phương thức đăng ký có đặt Username; Tùy chọn nếu người dùng đăng ký trực tiếp bằng Email hoặc Số điện thoại (khi đó hệ thống tự sinh `UserName = PhoneNumber` hoặc `UserName = Email`).
- **Độ dài**: Từ 6 đến 30 ký tự.
- **Ký tự hợp lệ**: Chữ cái không dấu (`a-z`, `A-Z`), chữ số (`0-9`), dấu gạch dưới (`_`) và dấu chấm (`.`). Không chứa khoảng trắng hoặc ký tự đặc biệt khác.
- **Quy tắc bắt đầu/kết thúc**: Bắt đầu và kết thúc bằng chữ cái hoặc chữ số. Không được chứa hai dấu chấm hoặc gạch dưới liên tiếp (`..` hoặc `__`).
- **Phân biệt hoa/thường**: Không phân biệt (Case-insensitive). Hệ thống tự động chuyển về chữ thường (`lowercase`) khi lưu trữ.
- **Tính duy nhất (Unique)**: Bắt buộc duy nhất trên toàn hệ thống (`NormalizedUserName`).
- **Danh sách từ cấm (Blacklist)**: Chặn tuyệt đối các từ khóa hệ thống: `admin`, `root`, `support`, `moderator`, `helpdesk`, `system`, `null`, `undefined`, `guest`, `istore`...
- **Ràng buộc quan trọng để phân biệt định danh**: Username **KHÔNG ĐƯỢC** chỉ chứa toàn chữ số (Numeric-only) và không khớp regex số điện thoại Việt Nam. Ràng buộc này đảm bảo bộ phân loại đầu vào ở bước đăng nhập nhận diện chính xác 100% giữa Username và Số điện thoại.
- **Regex chuẩn hóa**: `^(?![0-9]+$)[a-zA-Z0-9]([._](?![._])|[a-zA-Z0-9]){4,28}[a-zA-Z0-9]$`

##### 2. Địa chỉ Email / Gmail
- **Tính bắt buộc**: Bắt buộc khi đăng ký bằng Email; Dùng làm tài khoản đăng nhập, kênh nhận OTP kích hoạt, nhận hóa đơn điện tử VAT và khôi phục mật khẩu.
- **Độ dài**: Tối đa 254 ký tự (chuẩn RFC 5321). Định dạng chuẩn RFC 5322: `<local-part>@<domain>`.
- **Chuẩn hóa hoa/thường**: Tự động chuyển toàn bộ về chữ thường (`lowercase`) trước khi kiểm tra trùng lặp và lưu trữ.
- **Quy chuẩn riêng cho Gmail (Canonical Email Protection)**:
  - Google bỏ qua dấu chấm (`.` trong `ten.nguoidung@gmail.com` tương đương `tennguoidung@gmail.com`) và hỗ trợ alias dấu cộng (`+` ví dụ `user+coupon@gmail.com`).
  - Hệ thống tự động chuẩn hóa địa chỉ Gmail về dạng nguyên bản (xóa toàn bộ dấu chấm trong `local-part` và cắt bỏ đoạn từ dấu `+` đến `@`) khi đối soát trùng lặp nhằm ngăn chặn hành vi 1 người tạo hàng loạt tài khoản clone để trục lợi khuyến mãi và mã giảm giá Apple.
- **Xác thực (Verification)**: Bắt buộc gửi mã OTP (hiệu lực 5 - 10 phút) hoặc Magic Link kích hoạt trước khi tài khoản được sử dụng.
- **Regex chuẩn hóa**: `^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$`

##### 3. Số điện thoại (Phone Number)
- **Tính bắt buộc**: Bắt buộc nếu đăng ký bằng SĐT; Tùy chọn nếu đăng ký bằng Email (yêu cầu bổ sung khi Checkout để giao nhận hàng hoặc bật 2FA).
- **Định dạng áp dụng (Thị trường Việt Nam)**:
  - Độ dài: Đúng 10 chữ số (đối với định dạng nội địa bắt đầu bằng `0`).
  - Dải đầu số hợp lệ: Các dải mạng di động hiện hành tại Việt Nam (`03x`, `05x`, `07x`, `08x`, `09x`).
  - Chặn đầu số cố định: Tuyệt đối không chấp nhận số điện thoại bàn cố định (`02x`) làm tài khoản đăng nhập.
- **Chuẩn hóa lưu trữ**: Tự động chuyển đổi về chuẩn viễn thông quốc tế **E.164** khi lưu trữ trong Database (Ví dụ: `0901234567` $\rightarrow$ `+84901234567`).
- **Tính duy nhất (Unique)**: Bắt buộc duy nhất trên toàn hệ thống.
- **Xác thực (Verification) & Chống Spam OTP**:
  - Gửi mã OTP gồm 6 chữ số qua SMS Brandname / Zalo ZNS / Voice OTP. Thời gian hiệu lực: 60 - 120 giây.
  - **Rate Limiting nghiêm ngặt**: Giới hạn tối thiểu 60 giây giữa 2 lần gửi liên tiếp; Tối đa 3 lần gửi OTP / 10 phút trên mỗi số điện thoại hoặc địa chỉ IP nhằm ngăn chặn tấn công cạn kiệt tài khoản viễn thông (SMS pumping).
- **Regex chuẩn hóa**: `^(0|\+84)(3[2-9]|5[2689]|7[06-9]|8[1-9]|9[0-9])[0-9]{7}$`

##### 4. Mật khẩu (Password) & Cơ chế Băm an toàn
- **Độ dài**: Tối thiểu 8 ký tự, tối đa 32 ký tự (cho phép tối đa đến 64 ký tự để thân thiện với trình quản lý mật khẩu như Apple Keychain, 1Password, Bitwarden).
- **Độ phức tạp (Complexity)**: Bắt buộc bao gồm ít nhất 3 trong 4 nhóm ký tự sau:
  - Ít nhất 1 chữ cái in hoa (`A-Z`).
  - Ít nhất 1 chữ cái in thường (`a-z`).
  - Ít nhất 1 chữ số (`0-9`).
  - Ít nhất 1 ký tự đặc biệt (`!@#$%^&*()_+-=[]{}|;:,.<>?`).
- **Ràng buộc an toàn nâng cao**:
  - Không chứa khoảng trắng ở đầu hoặc cuối chuỗi.
  - Không được trùng hoàn toàn với Username, Email, hoặc Số điện thoại.
  - Chặn danh sách mật khẩu yếu phổ biến: `12345678`, `password`, `qwertyuiop`, `admin123`, `iloveyou`...
- **Trải nghiệm giao diện (UI/UX)**:
  - Mặc định ẩn ký tự (`type="password"`), có nút icon bật/tắt hiển thị mật khẩu (Show/Hide).
  - Thanh đo độ mạnh mật khẩu (Weak - Medium - Strong) hiển thị trực quan theo thời gian thực.
  - Trường "Xác nhận mật khẩu" (Confirm Password) phải khớp hoàn toàn với mật khẩu đã nhập.
- **Cơ chế Lưu trữ & Thuật toán Băm phía Backend**:
  - Tuyệt đối không lưu Plain-text dưới mọi hình thức.
  - **Mặc định chuẩn hóa**: Sử dụng `PasswordHasher<ApplicationUser>` của ASP.NET Core 9 Identity dựa trên thuật toán PBKDF2 với HMAC-SHA512 (100.000 vòng lặp kèm Salt ngẫu nhiên 128-bit) đạt chuẩn bảo mật NIST SP 800-63B.
  - **Mở rộng tùy chọn**: Cho phép cắm tích hợp `IPasswordHasher<ApplicationUser>` tùy biến sang thuật toán **Argon2id** (m=65536, t=3, p=4) hoặc **bcrypt** (Cost factor $\ge 12$) khi triển khai hạ tầng ngân hàng/thương mại quốc tế.

##### 5. Kịch bản Đăng ký & Tối ưu Tỷ lệ Rớt (Drop-off Rate Optimization)
1. **Bước 1: Chọn phương thức tạo tài khoản**:
   - *Cách A*: Đăng ký bằng Số điện thoại (Nhận OTP SMS).
   - *Cách B*: Đăng ký bằng Email (Nhận OTP / Link kích hoạt qua Email).
   - *Cách C*: Đăng ký một chạm qua Social Login (Google Sign-In, Apple ID, Facebook Login).
2. **Bước 2: Điền thông tin cốt lõi**:
   - Nhập thông tin định danh (Họ tên, SĐT/Email, Mật khẩu).
   - **NGUYÊN TẮC VÀNG VỀ UX**: **TUYỆT ĐỐI KHÔNG BẮT BUỘC NHẬP ĐỊA CHỈ KHI ĐĂNG KÝ**. Việc yêu cầu khách hàng điền địa chỉ chi tiết ngay lúc mở tài khoản là nguyên nhân hàng đầu khiến khách hàng rời bỏ (drop-off rate $> 60\%$). Địa chỉ sẽ chỉ được yêu cầu bổ sung khi khách hàng thực hiện Đặt hàng (Checkout) hoặc chủ động quản lý trong Sổ địa chỉ cá nhân.
3. **Bước 3: Kiểm tra Trùng lặp Thời gian thực & Xác thực (Real-time Availability Check & Instant Feedback)**:
   - **Cơ chế Kiểm tra Tức thời (Asynchronous Real-time On-Blur / Debounced Check)**:
     - Khắc phục triệt để trải nghiệm tiêu cực khi người dùng điền xong toàn bộ form rồi mới bấm nút submit và bị báo lỗi tải lại trang gây mất thời gian.
     - Hệ thống kích hoạt kiểm tra bất đồng bộ qua API `/Account/CheckAvailability` ngay khi người dùng hoàn tất nhập (sự kiện `input` với Debounce 400ms) hoặc ngay khi rời khỏi ô nhập liệu (sự kiện `blur`):
       1. **Số điện thoại (Phone Number)**: Hệ thống làm sạch và chuẩn hóa E.164, kiểm tra tức thì trong cơ sở dữ liệu. Nếu số điện thoại đã tồn tại, lập tức hiển thị viền đỏ và thông báo trực quan ngay dưới ô nhập: *"Số điện thoại này đã được sử dụng. Bạn có muốn Đăng nhập ngay không?"* kèm liên kết chuyển nhanh sang Đăng nhập. Nếu hợp lệ: hiển thị tích xanh `✓ Số điện thoại khả dụng`.
       2. **Tên đăng nhập (Username)**: Kiểm tra ngay quy cách định dạng, danh sách từ cấm (blacklist) và sự tồn tại trong CSDL. Nếu đã có người dùng, báo đỏ ngay lập tức: *"Tên đăng nhập này đã được sử dụng. Vui lòng chọn tên khác!"*. Nếu hợp lệ: hiển thị tích xanh `✓ Tên đăng nhập hợp lệ`.
       3. **Địa chỉ Email**: Chuẩn hóa Canonical Email (loại bỏ dấu chấm và bí danh `+` đối với Gmail) và đối soát CSDL. Nếu đã được liên kết: báo đỏ *"Địa chỉ email này đã được liên kết với một tài khoản khác!"*. Nếu hợp lệ: hiển thị `✓ Email khả dụng`.
     - **Chặn Submit chủ động (Client-side Guard)**: Khi bất kỳ trường định danh nào đang hiển thị trạng thái trùng lặp hoặc chưa vượt qua kiểm tra, hệ thống tự động khóa nút "Hoàn tất đăng ký" và tập trung (focus) vào ô nhập bị lỗi để người dùng sửa đổi ngay.
   - Gửi mã xác thực OTP qua kênh tương ứng kèm bộ đếm ngược thời gian (Countdown 60s) nếu kích hoạt xác thực 2 bước.
4. **Bước 4: Tạo tài khoản & Khởi tạo Hồ sơ Khách hàng**:
   - Tạo bản ghi trong `AspNetUsers` với vai trò `Customer`.
   - Nếu đăng ký không có Username riêng, tự động gán `UserName = PhoneNumber` hoặc `UserName = Email`.
   - Tạo bản ghi tương ứng trong bảng `Customers` (liên kết khóa ngoại `UserId = AspNetUsers.Id`), thiết lập cấp bậc ban đầu `MembershipTier = 'Member'`, điểm tích lũy ban đầu `RewardPoints = 0`.
   - Tự động đăng nhập (`SignInAsync`) và chuyển hướng khách hàng về trang trước đó (`returnUrl`) hoặc trang chủ.

---

#### 2.4.2. Luồng Đăng nhập Linh hoạt & Cơ chế Phòng vệ Brute-Force Đa tầng

##### 1. Định danh 1 ô duy nhất (Unified Single-Input Login)
Khách hàng không cần phải băn khoăn mình đã đăng ký bằng Username, Email hay Số điện thoại. Giao diện cung cấp **1 ô nhập duy nhất**: `Tên đăng nhập / Email / Số điện thoại`. Phía Backend sử dụng **Bộ phân loại đầu vào tất định (Deterministic Input Classifier)**:
- **Nếu chứa ký tự `@`**: Xác định là Email $\rightarrow$ Chuyển về lowercase và truy vấn chỉ mục `NormalizedEmail`.
- **Nếu toàn chữ số (10 số bắt đầu bằng `0`) hoặc bắt đầu bằng `+84`**: Xác định là Số điện thoại $\rightarrow$ Chuẩn hóa về dạng E.164 (`+84...`) và truy vấn trường `PhoneNumber`.
- **Các trường hợp còn lại**: Xác định là Username $\rightarrow$ Chuyển về lowercase và truy vấn chỉ mục `NormalizedUserName`.

Nhờ cơ chế này, câu lệnh truy vấn EF Core luôn đánh trúng index tương ứng, tránh việc quét toàn bảng (`Table Scan`) hay ghép điều kiện OR gây quá tải CPU/RAM.

##### 2. Cơ chế Ghi nhớ Phiên đăng nhập (Remember Me)
- **Nếu không tích chọn "Ghi nhớ đăng nhập"**:
  - Cookie phiên (`Session Cookie`) tạm thời.
  - Tự động hết hạn sau **24 giờ** không hoạt động hoặc bị hủy ngay khi người dùng đóng trình duyệt.
- **Nếu tích chọn "Ghi nhớ đăng nhập"**:
  - Cấp phát Persistent Auth Cookie kết hợp Refresh Token có chữ ký số an toàn.
  - Duy trì phiên đăng nhập trong vòng **30 ngày**.
  - Tự động gia hạn (Sliding Expiration) khi người dùng quay lại mua sắm thường xuyên.

##### 3. Cơ chế Phòng vệ Brute-force & Chống rà quét Đa tầng (3-Tier Security Defense)
Để ngăn chặn các công cụ tự động dò mật khẩu (Dictionary Attack, Brute-force), hệ thống kích hoạt cơ chế phòng vệ 3 cấp độ dựa trên thuộc tính `AccessFailedCount` của ASP.NET Identity:

| Cấp độ | Ngưỡng sai liên tiếp | Hành động của Hệ thống | Trải nghiệm Người dùng |
| :---: | :---: | :--- | :--- |
| **Cấp 1: Thử thách** | **3 lần** | Kích hoạt bắt buộc xác thực **CAPTCHA** (Cloudflare Turnstile hoặc Google reCAPTCHA v3/v2). Backend từ chối xử lý nếu không có Captcha Token hợp lệ. | Người dùng phải tích chọn xác nhận không phải người máy trước khi bấm nút "Đăng nhập". |
| **Cấp 2: Khóa tạm** | **5 lần** | Khóa tạm thời tài khoản trong vòng **15 phút** (`LockoutEnd = UtcNow.AddMinutes(15)`). Đồng thời tự động gửi email/tin nhắn cảnh báo đăng nhập bất thường kèm IP, thiết bị và thời gian. | Thông báo: *"Tài khoản tạm thời bị khóa 15 phút do nhập sai mật khẩu nhiều lần. Vui lòng thử lại sau hoặc dùng Quên mật khẩu."* |
| **Cấp 3: Khóa cứng** | **10 lần** | Khóa tài khoản vĩnh viễn (`LockoutEnabled = true`, `LockoutEnd = DateTimeOffset.MaxValue`). | Tài khoản bị vô hiệu hóa hoàn toàn. Bắt buộc người dùng phải dùng chức năng *"Quên mật khẩu"* để xác thực OTP qua Email/SĐT chính chủ mới được mở khóa và cấp mật khẩu mới. |

##### 4. Nguyên tắc Chống rà quét Danh tính (Username Enumeration Defense)
- Khi xác thực thất bại (dù tài khoản không tồn tại, sai mật khẩu hay đang bị khóa), hệ thống **LUÔN TRẢ VỀ MỘT THÔNG BÁO DUY NHẤT**:
  > *"Tên đăng nhập hoặc mật khẩu không chính xác!"*
- **Tuyệt đối KHÔNG hiển thị** các thông báo chi tiết như: *"Tài khoản không tồn tại"*, *"Mật khẩu sai"*, tránh để kẻ tấn công lợi dụng phân biệt tài khoản có thật trên hệ thống.
- **Phân tách ranh giới 2 phân hệ**: Nếu tài khoản nội bộ (Admin, Manager, Staff, Cashier, Shipper) đăng nhập nhầm vào trang Storefront của Khách hàng, hệ thống từ chối đăng nhập với thông báo chung như trên, **tuyệt đối KHÔNG hiển thị liên kết hay gợi ý về đường dẫn Cổng Quản trị Admin Area** (`/Admin/Account/Login`) nhằm bảo vệ an toàn cho phân hệ nội bộ.

---

#### 2.4.3. Quản lý Hồ sơ & Sổ Địa chỉ Chuẩn hóa 4 Cấp độ (Standardized Address Book)

##### 1. Khẳng định Tính Tất yếu của Địa chỉ Chuẩn hóa
Trong mô hình bán lẻ thiết bị công nghệ giá trị cao (iPhone chính hãng), nếu để trường địa chỉ dạng nhập tự do (Free-text) sẽ dẫn đến các hậu quả nghiêm trọng:
- Dữ liệu rác, sai chính tả tên phường/xã, khó đối soát bưu tá.
- Không thể tự động tích hợp API của các đơn vị chuyển phát nhanh hàng đầu (Giao Hàng Nhanh - GHN, Giao Hàng Tiết Kiệm - GHTK, Viettel Post).
- Tính sai cước phí giao hàng (Shipping Fee), không xác định được tuyến giao hàng hỏa tốc trong 2 giờ.
- Không phân tích được mật độ khách hàng theo địa bàn hành chính để quy hoạch mở thêm cửa hàng Apple Authorized Reseller mới.

##### 2. Thời điểm Thu thập Địa chỉ
- **Khi Đăng ký tài khoản (Sign Up)**: **KHÔNG THU THẬP** (loại bỏ hoàn toàn rào cản thao tác).
- **Khi Tiến hành Thanh toán (Checkout) hoặc Cập nhật Hồ sơ (`/Account/Profile`)**: **BẮT BUỘC THU THẬP ĐẦY ĐỦ** theo cấu trúc 4 cấp độ dưới đây.

##### 3. Cấu trúc Dữ liệu Địa chỉ Chuẩn hóa 4 Cấp độ

```
[CẤP 1: Tỉnh / Thành phố]  (Dropdown 63 tỉnh/thành - Chuẩn Tổng cục Thống kê)
         │
         ▼
[CẤP 2: Quận / Huyện]       (Dropdown phụ thuộc Tỉnh/Thành đã chọn)
         │
         ▼
[CẤP 3: Phường / Xã]        (Dropdown phụ thuộc Quận/Huyện đã chọn)
         │
         ▼
[CẤP 4: Địa chỉ chi tiết]   (Text Input: Số nhà, tên đường, tòa nhà, ngõ ngách - 5 đến 150 ký tự)
```

| Cấp bậc | Tên trường dữ liệu | Kiểu nhập liệu | Quy tắc nghiệp vụ & Xử lý kỹ thuật |
| :--- | :--- | :--- | :--- |
| **Cấp 1** | Tỉnh / Thành phố | Dropdown / Tìm kiếm autocomplete | Bắt buộc chọn từ danh mục chuẩn hành chính quốc gia (63 tỉnh/thành). Hệ thống lưu trữ cả Mã định danh (`ProvinceCode`) và Tên hiển thị (`ProvinceName`). Dữ liệu được cache phía Client/CDN để tải tức thì. |
| **Cấp 2** | Quận / Huyện / Thị xã | Dropdown / Tìm kiếm autocomplete | Bắt buộc chọn từ danh mục phụ thuộc trực tiếp vào Cấp 1 đã chọn. Lưu trữ cả `DistrictCode` và `DistrictName`. |
| **Cấp 3** | Phường / Xã / Thị trấn | Dropdown / Tìm kiếm autocomplete | Bắt buộc chọn từ danh mục phụ thuộc trực tiếp vào Cấp 2 đã chọn. Lưu trữ cả `WardCode` và `WardName`. |
| **Cấp 4** | Địa chỉ chi tiết (Số nhà, tên đường, căn hộ) | Text Input | - Bắt buộc nhập.<br>- Độ dài: Từ 5 đến 150 ký tự.<br>- Bộ lọc kiểm soát an toàn: Tự động loại bỏ thẻ HTML/Script (XSS), ký tự điều khiển lạ.<br>- Gợi ý trực quan: *"Ví dụ: Số 123 đường Lê Lợi, Chung cư Vincom Center, Tháp A phòng 1205"*. |

##### 4. Các Trường Thuộc tính Mở rộng của Sổ Địa chỉ (Address Book)
Khách hàng có thể lưu nhiều địa chỉ nhận hàng khác nhau trong Sổ địa chỉ cá nhân:
1. **Họ tên & Số điện thoại người nhận**: Mặc định lấy theo thông tin tài khoản đăng ký nhưng cho phép tùy chỉnh linh hoạt (rất quan trọng khi khách hàng mua iPhone đặt giao tặng người thân, bạn bè hoặc đối tác).
2. **Nhãn nhận diện địa chỉ (`AddressTag`)**: Nút bấm chọn nhanh: `Nhà riêng` / `Văn phòng` / `Khác` (kèm biểu tượng trực quan).
3. **Địa chỉ mặc định (`IsDefault`)**: Cờ boolean (`true/false`). Mỗi khách hàng chỉ có duy nhất 1 địa chỉ mặc định. Hệ thống tự động điền sẵn địa chỉ này khi vào trang Checkout để khách chốt đơn trong 30 giây.
4. **Tọa độ định vị GPS (`Latitude` / `Longitude`)**: Tích hợp công cụ Geocoding (Google Maps / Goong Map API) hỗ trợ định vị chính xác vị trí trên bản đồ, phục vụ tính khoảng cách từ kho gần nhất để kích hoạt dịch vụ *"Giao hàng siêu tốc 2 giờ"*.

##### 5. Cơ chế Lưu trữ & Tính Tương thích Ngược (Database Compatibility)
- **Tương thích ngược CSDL hiện hữu**: Cột `Customers.Address` (kiểu `NVARCHAR(255)`) lưu chuỗi tổng hợp đầy đủ theo định dạng chuẩn hóa:  
  `"{DetailedAddress}, {WardName}, {DistrictName}, {ProvinceName}"`  
  (Ví dụ: *"Số 86 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh"*). Giải pháp này đảm bảo toàn bộ mã nguồn xử lý in hóa đơn, hiển thị đơn hàng hiện có hoạt động trơn tru không lỗi.
- **Mô hình Mở rộng Sổ địa chỉ (Table `CustomerAddresses`)**: Thiết kế sẵn sàng bảng liên kết `CustomerAddresses` (1 Khách hàng có nhiều Địa chỉ) với các cột phân rã: `Id`, `CustomerId`, `ReceiverName`, `ReceiverPhone`, `ProvinceCode`, `ProvinceName`, `DistrictCode`, `DistrictName`, `WardCode`, `WardName`, `DetailedAddress`, `AddressTag`, `IsDefault`, `CreatedAt`.

---

#### 2.4.4. Ma trận Ràng buộc Dữ liệu & Xử lý Ngoại lệ (Customer Validation Matrix)

Bảng ma trận tổng hợp toàn bộ các quy tắc kiểm tra tính hợp lệ dữ liệu áp dụng cho cả Client-side (Validation Scripts / jQuery Validate) và Server-side (FluentValidation / ASP.NET Data Annotations):

| Trường thông tin | Bắt buộc | Độ dài (Ký tự) | Kiểu dữ liệu / Quy cách | Thông báo lỗi hiển thị cho người dùng |
| :--- | :---: | :---: | :--- | :--- |
| **Username** | Có* | 6 - 30 | Alphanumeric, `.` `_`, không toàn số | - "Tên đăng nhập phải từ 6-30 ký tự, không chứa ký tự đặc biệt hoặc khoảng trắng."<br>- "Tên đăng nhập không được chỉ chứa toàn chữ số."<br>- "Tên đăng nhập này đã được sử dụng. Vui lòng chọn tên khác." |
| **Email** | Có* | 6 - 254 | Email String chuẩn RFC 5322 | - "Email không đúng định dạng chuẩn (VD: tenban@gmail.com)."<br>- "Địa chỉ email này đã được liên kết với một tài khoản khác." |
| **Số điện thoại** | Tùy chọn* | Đúng 10 số | Numeric, dải di động Việt Nam | - "Số điện thoại không hợp lệ (Phải là 10 chữ số di động đầu 03, 05, 07, 08, 09)."<br>- "Số điện thoại này đã tồn tại trên hệ thống." |
| **Mật khẩu** | Có | 8 - 32 | Ký tự phức tạp (3/4 nhóm) | - "Mật khẩu tối thiểu 8 ký tự, bao gồm ít nhất 3 trong 4 nhóm: chữ hoa, chữ thường, chữ số, ký tự đặc biệt."<br>- "Mật khẩu không được chứa khoảng trắng hoặc trùng với tên đăng nhập/email/SĐT." |
| **Xác nhận mật khẩu** | Có | 8 - 32 | Khớp mật khẩu chính | - "Mật khẩu xác nhận không trùng khớp với mật khẩu đã nhập." |
| **Họ và tên khách** | Có | 2 - 100 | Chuỗi Unicode tiếng Việt | - "Vui lòng nhập họ và tên của bạn (tối thiểu 2 ký tự)." |
| **Tỉnh / Thành phố** | Bắt buộc** | - | Dropdown Mã hành chính | - "Vui lòng chọn Tỉnh/Thành phố nhận hàng hợp lệ." |
| **Quận / Huyện** | Bắt buộc** | - | Dropdown Mã hành chính | - "Vui lòng chọn Quận/Huyện nhận hàng hợp lệ." |
| **Phường / Xã** | Bắt buộc** | - | Dropdown Mã hành chính | - "Vui lòng chọn Phường/Xã nhận hàng hợp lệ." |
| **Địa chỉ chi tiết** | Bắt buộc** | 5 - 150 | Text an toàn (Lọc XSS) | - "Vui lòng nhập số nhà, tên đường cụ thể (tối thiểu 5 ký tự)." |

*(Ghi chú:*  
- `Có*` / `Tùy chọn*`: Khi Đăng ký, bắt buộc phải có ít nhất Email hoặc Số điện thoại. Username là bắt buộc nếu chọn hình thức đăng ký bằng Username, tự động sinh nếu đăng ký bằng SĐT/Email.  
- `Bắt buộc**`: Tùy chọn khi Đăng ký tài khoản mới, nhưng **BẮT BUỘC 100%** khi khách hàng thực hiện Đặt hàng (Checkout) hoặc tạo địa chỉ nhận hàng trong Sổ địa chỉ cá nhân).*

---

### 2.5. Luồng Thanh toán Bắt buộc Định danh (Checkout Authentication & Autofill)
- Khi khách hàng nhấn **"Tiến hành Đặt hàng" (Proceed to Checkout)**:
  - Hệ thống kiểm tra xác thực `[Authorize]`.
  - **Nếu chưa đăng nhập**: Lập tức chuyển hướng sang `/Account/Login?ReturnUrl=%2FCart%2FCheckout`.
  - **Hỗ trợ Đăng ký mới kèm `returnUrl`**: Khách chưa có tài khoản bấm "Đăng ký ngay", điền thông tin định danh cốt lõi (Họ tên, SĐT/Email, Mật khẩu - không ép nhập địa chỉ ngay lúc này). Sau khi tạo tài khoản xong, hệ thống tự động đăng nhập và đưa quay lại thẳng trang Checkout.
  - **Tự động điền thông minh (Autofill Engine)**:
    - Nếu khách hàng đã có địa chỉ trong Sổ địa chỉ: Tự động trích xuất Họ tên, SĐT, Email và Địa chỉ mặc định (`IsDefault = true`) điền vào form, giúp khách chốt đơn trong 30 giây.
    - Khách hàng có thể bấm chọn nhanh giữa các địa chỉ đã lưu (`Nhà riêng`, `Văn phòng`) hoặc chọn *"Thêm địa chỉ giao hàng mới"* theo đúng quy chuẩn 4 cấp hành chính ngay tại giao diện thanh toán.

### 2.6. Tạo Đơn hàng An toàn (ACID Order Placement)
- Khách hàng chọn hình thức thanh toán: **COD (Tiền mặt khi nhận)** hoặc **Chuyển khoản Ngân hàng / Cổng trực tuyến (VietQR, VNPay, Apple Pay)**.
- Mở Database Transaction nguyên tử:
  1. Kiểm tra tồn kho của từng mặt hàng: Nếu có khách khác mua trước làm `Quantity < item.Quantity` $\rightarrow$ Rollback và báo thông báo thân thiện.
  2. Khấu trừ tồn kho tức thời: `Product.Quantity -= item.Quantity`.
  3. Tạo bản ghi `Orders` trạng thái `Pending`.
  4. Nếu thanh toán online: Tự động giữ chỗ 1 mã IMEI khả dụng trong kho (`ProductImeis.Status = 'Reserved'`).
  5. Ghi nhật ký vào `AuditLogs` với hành động `PLACE_ORDER`.
- Chuyển hướng đến trang thông báo đặt hàng thành công (`/OrderSuccess`).

### 2.7. Quản lý Đơn hàng & Tra cứu Bảo hành Cá nhân
- Trang **"Đơn hàng của tôi" (`/Order/MyOrders`)**: Xem lịch sử mua sắm, trạng thái vận chuyển từng đơn (`Pending` $\rightarrow$ `Shipping` $\rightarrow$ `Completed`).
- Cổng **"Tra cứu Bảo hành Điện tử" (`/Warranty`)**: Nhập IMEI hoặc SĐT để kiểm tra thời hạn bảo hành chính hãng Apple Care còn lại.

---

## 3. PHÂN HỆ QUẢN TRỊ VẬN HÀNH (ADMIN DASHBOARD & OPERATIONS)

Phía Admin Dashboard được chuẩn hóa thành 5 module quản lý cốt lõi, bảo đảm vận hành trơn tru theo mô hình chuỗi bán lẻ chuyên nghiệp (tương tự Thế Giới Di Động / TopZone / FPT Shop):

```
                        KIẾN TRÚC PHÂN HỆ ADMIN DASHBOARD
                                       │
      ┌────────────────────────────────┼────────────────────────────────┐
      ▼                                ▼                                ▼
MODULE 1: CATALOG & IMEI         MODULE 2: ĐƠN HÀNG              MODULE 3: BẢO HÀNH & ĐỔI TRẢ
- CRUD Dòng máy (iPhone 16, 17)  - Bộ lọc đơn (Pending, Ship...) - Tra cứu máy bảo hành
- Quản lý Biến thể (Màu/ROM/Giá) - Duyệt đơn & Gán IMEI          - Nghiệp vụ Đổi 1-1 (30 ngày)
- Nhập lô IMEI hàng loạt         - Cập nhật thông tin vận chuyển - Cập nhật kết quả kỹ thuật
      │                                                                 │
      └────────────────────────────────┬────────────────────────────────┘
                                       │
                      ┌────────────────┴────────────────┐
                      ▼                                 ▼
             MODULE 4: BÁO CÁO & THỐNG KÊ      MODULE 5: NGƯỜI DÙNG & PHÂN QUYỀN
             - Doanh thu theo Ngày/Tháng/Năm   - Quản lý tài khoản nhân viên
             - Biểu đồ iPhone bán chạy nhất    - 3 Nhóm: Thủ kho, Sales, Store Manager
             - Cảnh báo tồn kho thấp (IMEI)    - Ma trận phân quyền chi tiết (RBAC)
```

---

### 3.1. Quản lý Danh mục & Sản phẩm (Product Catalog Management - CRUD)

#### 1. Thêm / Sửa / Xóa dòng iPhone mới (Series Management)
- **Tổ chức danh mục theo thế hệ iPhone**: Hỗ trợ phân nhóm linh hoạt cho các dòng máy mới nhất (*iPhone 16 Series, iPhone 17 Series, iPhone 15 Series, iPhone 14 Series, SE...*).
- **Thuộc tính quản lý**:
  - Tên dòng máy (`Name`), Mô tả công nghệ (`Description` - ví dụ: Apple Intelligence, Khung viền Titan, Camera Control).
  - Thứ tự sắp xếp hiển thị trên Storefront (`DisplayOrder`).
  - Trạng thái kích hoạt (`IsActive`): Cho phép tạm ẩn dòng máy chưa mở bán hoặc đã ngừng kinh doanh.
- **Ràng buộc an toàn (Integrity Rule)**: Ngăn chặn xóa vĩnh viễn dòng máy nếu đang có sản phẩm hoặc biến thể trực thuộc. Bắt buộc chuyển sang ẩn hoặc điều chuyển sản phẩm trước khi xóa.

#### 2. Quản lý biến thể sản phẩm (SKU & Variant Management)
- **Cấu hình đa biến thể cho từng dòng máy**:
  - **Màu sắc (Color)**: Sa Mạc Titan (Desert Titanium), Titan Tự Nhiên, Titan Đen, Titan Trắng, Xanh Lưu Ly (Teal), Hồng, Xanh Ultramarine...
  - **Dung lượng ROM / RAM (Storage)**: 128GB, 256GB, 512GB, 1TB, 2TB.
  - **Bảng giá niêm yết (Pricing)**: Giá bán gốc niêm yết của hãng, Giá khuyến mãi bán lẻ.
  - **Thông số kỹ thuật chuyên sâu (Specifications)**: Chip vi xử lý (A18 Pro / A18 Bionic), Màn hình Super Retina XDR OLED ProMotion 120Hz, Viền mỏng chuẩn Apple, Cụm camera 48MP Fusion, Chuẩn kết nối USB-C 3.0, Chuẩn kháng nước bụi IP68.
  - **Đa phương tiện (Media Gallery)**: Ảnh đại diện sản phẩm sắc nét, bộ sưu tập ảnh chi tiết theo từng góc cạnh và màu sắc.
  - **Thời hạn bảo hành chuẩn hãng (`WarrantyMonths`)**: Mặc định 12 tháng Apple Care chính hãng (hoặc mở rộng 24 tháng theo gói bảo hành VIP).
- **Cơ chế Xóa thông minh (Soft-Delete Preservation)**:
  - Nếu biến thể sản phẩm đã từng phát sinh giao dịch trong bảng `OrderDetails`: Hệ thống tự động chuyển `IsActive = false` (ẩn khỏi giao diện bán lẻ của khách).
  - Nghiêm cấm xóa vật lý (Hard-Delete) các sản phẩm đã có đơn hàng nhằm bảo toàn lịch sử hóa đơn tài chính và dữ liệu đối soát bảo hành sau này.

#### 3. Thêm mới lô hàng & Nhập danh sách mã IMEI/Serial hàng loạt vào kho (Batch IMEI Ingestion)
- **Bản chất định danh thiết bị Apple**: Mỗi máy iPhone thực tế là một thực thể vật lý độc nhất, được nhận diện qua mã **IMEI** (15 chữ số tiêu chuẩn GSMA) và mã **Serial Number** (10-12 ký tự của Apple).
- **Giao diện nhập lô (Batch Input UI)**:
  - Cho phép thủ kho chọn Dòng máy & Biến thể cụ thể.
  - Cung cấp ô nhập liệu đa dòng (`Textarea`): Cho phép dán (paste) danh sách hàng chục, hàng trăm mã IMEI từ file Excel của nhà phân phối (Synnex FPT, Digiworld, Viettel Distribution) hoặc quét liên tục bằng súng đọc mã vạch Barcode/QR 2D.
- **Cơ chế kiểm duyệt & Validation tự động**:
  1. Tự động loại bỏ ký tự trắng thừa, ngắt dòng rác.
  2. Kiểm tra định dạng chuẩn 15 chữ số đối với mã IMEI.
  3. Kiểm tra tính duy nhất nội bộ: Phát hiện và cảnh báo nếu người dùng quét trùng 2 lần trong cùng một đợt nhập.
  4. Kiểm tra tính duy nhất trên toàn hệ thống: Tự động so sánh với CSDL (`UNIQUE KEY (Imei)`), báo lỗi cụ thể nếu IMEI đã tồn tại trong kho hoặc đã từng bán.
- **Đồng bộ tồn kho tự động**:
  - Bản ghi được thêm vào bảng `ProductImeis` với trạng thái ban đầu: `Status = 'InStock'`.
  - Hệ thống tự động cập nhật số lượng tồn khả dụng của sản phẩm: `Products.Quantity += [Số lượng IMEI nhập thành công]`.

---

### 3.2. Quản lý Đơn hàng (Order Management & Fulfillment)

#### 1. Danh sách toàn bộ đơn hàng & Bộ lọc đa trạng thái
- **Giao diện quản lý trực quan**: Hiển thị danh sách toàn bộ đơn hàng trên hệ thống, sắp xếp theo thứ tự thời gian đặt mới nhất (`OrderDate DESC`).
- **Bộ lọc trạng thái nghiệp vụ chuẩn**:
  - `Pending`: Đơn hàng mới tạo, đang chờ xác nhận hoặc đang chờ khách thanh toán trực tuyến.
  - `Confirmed`: Đơn hàng đã được nhân viên bán hàng gọi điện xác thực thông tin và duyệt đơn.
  - `Shipping`: Hàng đã được đóng gói, dán tem vận chuyển và bàn giao cho đơn vị giao vận.
  - `Completed`: Giao hàng thành công, khách đã nhận máy và thanh toán đủ (hoàn tất đơn).
  - `Cancelled`: Đơn hàng bị hủy (do khách chủ động hủy hoặc do hệ thống tự hủy vì quá hạn thanh toán).
- **Bộ công cụ tìm kiếm nhanh**: Tra cứu tức thì theo Mã đơn hàng (`#OrderId`), Họ tên người nhận hoặc Số điện thoại đặt hàng.
- **Xem chi tiết đơn hàng (Order Inspection)**:
  - Hiển thị danh sách sản phẩm đặt mua, số lượng, đơn giá, tổng tiền thanh toán.
  - Địa chỉ nhận hàng, phương thức thanh toán (COD hoặc Chuyển khoản QR), thông tin người đặt.
  - Hiển thị mã IMEI cụ thể đã được gán cho từng sản phẩm trong đơn.

#### 2. Thao tác duyệt đơn, gán mã IMEI cụ thể cho từng sản phẩm & Cập nhật vận chuyển
- **Bước 1: Duyệt đơn (Approve Order)**:
  - Nhân viên bán hàng liên hệ khách hàng xác nhận địa chỉ và thời gian nhận máy.
  - Chuyển trạng thái đơn hàng sang `Confirmed`.
- **Bước 2: Gán mã IMEI xuất kho (Fulfillment / Barcode Scanning)**:
  - Nhân viên kho mở chi tiết đơn hàng, cầm hộp iPhone thực tế trên kệ kho.
  - Dùng súng quét mã vạch (Barcode Scanner) quét tem hộp IMEI của máy.
  - **Hệ thống xác thực nghiêm ngặt**:
    + Mã IMEI quét phải thuộc đúng `ProductId` trong đơn hàng.
    + Trạng thái IMEI phải là `InStock` (hoặc `Reserved` nếu là đơn đặt cọc thanh toán trước).
  - Hệ thống tự động gán `ProductImeiId` và `ImeiNumber` vào chi tiết đơn hàng `OrderDetails`.
  - Cập nhật trạng thái mã IMEI từ `InStock` $\rightarrow$ `Sold`, ghi nhận ngày xuất kho `ExportDate = GETDATE()`.
- **Bước 3: Cập nhật thông tin vận chuyển (Shipping Dispatch)**:
  - Chọn đơn vị vận chuyển đối tác: *Giao Hàng Nhanh (GHN), Viettel Post, GHTK, Hỏa tốc 2H (AhaMove/GrabExpress)...*
  - Nhập **Mã vận đơn (Tracking Number / Waybill No)** để khách hàng và nhân viên có thể theo dõi tiến độ di chuyển của kiện hàng.
  - Thiết lập Ngày giao hàng dự kiến (`EstimatedDeliveryDate`).
  - Chuyển trạng thái đơn hàng sang `Shipping`.
- **Bước 4: Hoàn tất đơn hàng & Kích hoạt Bảo hành điện tử tự động**:
  - Khi đối tác vận chuyển xác nhận "Giao hàng thành công", nhân viên chuyển trạng thái sang `Completed`.
  - Hệ thống tự động kích hoạt bảo hành điện tử chính hãng Apple Care:
    + Tạo bản ghi mới trong bảng `Warranties`.
    + `StartDate = GETDATE()` (Ngày khách chính thức nhận máy).
    + `EndDate = StartDate + Product.WarrantyMonths` (Ví dụ: 12 tháng).
    + `Status = 'Active'`.
    + Khách hàng có thể dùng ngay số IMEI này để tra cứu trên trang Web công khai.

---

### 3.3. Quản lý Bảo hành & Đổi trả (Warranty & Return Management)

#### 1. Tra cứu danh sách máy đang bảo hành & Theo dõi vòng đời
- Cung cấp thanh công cụ tra cứu tức thì cho nhân viên kỹ thuật: Tìm kiếm bằng 15 số IMEI, Apple Serial Number hoặc Số điện thoại khách hàng.
- Bảng danh sách hiển thị đầy đủ thông tin:
  - Model máy, Biến thể màu sắc/dung lượng.
  - Khách hàng sở hữu, Số điện thoại liên hệ.
  - Ngày kích hoạt bảo hành, Ngày hết hạn bảo hành.
  - Số ngày còn lại (kèm thanh trạng thái trực quan: Màu xanh lá nếu còn hạn > 60 ngày, Màu vàng nếu còn hạn < 30 ngày, Màu đỏ nếu đã hết hạn).
  - Trạng thái bảo hành: `Active` (Đang được bảo hành), `Expired` (Đã hết hạn tiêu chuẩn), `Voided` (Bị từ chối bảo hành do vi phạm chính sách).

#### 2. Nghiệp vụ "1 Đổi 1 trong 30 ngày" (30-Day Hardware Exchange Policy)
- **Chính sách cam kết chất lượng AAR**: Trong vòng 30 ngày đầu tiên kể từ ngày nhận hàng, nếu iPhone phát sinh lỗi phần cứng do nhà sản xuất (lỗi nguồn, lỗi bo mạch chủ, màn hình điểm chết, camera lỗi...), khách hàng được đổi ngay một thân máy mới 100% cùng loại.
- **Quy trình xử lý tự động trên Admin Dashboard**:
  1. Nhân viên kỹ thuật tra cứu IMEI trên hệ thống $\rightarrow$ Hệ thống tự động kiểm tra điều kiện `DATEDIFF(day, StartDate, GETDATE()) <= 30`.
  2. Nhân viên tạo phiếu yêu cầu đổi máy và chọn hành động `ActionTaken = 'ExchangeDevice'`.
  3. **Thu hồi máy lỗi**: Hệ thống tự động chuyển trạng thái của mã IMEI cũ từ `Sold` sang `Defective` (Hàng lỗi, khóa vĩnh viễn không cho xuất bán tiếp, chuyển sang kho chờ Apple kiểm định).
  4. **Xuất máy mới thay thế**: Hệ thống liệt kê danh sách các mã IMEI cùng dòng máy đang ở trạng thái `InStock`. Nhân viên quét chọn 1 mã IMEI mới để cấp cho khách. Mã IMEI mới này lập tức chuyển thành `Sold`.
  5. **Kế thừa và lưu vết bảo hành**:
     - Bản ghi `Warranties` của khách được cập nhật sang mã IMEI mới.
     - Hệ thống tự động lưu số IMEI cũ vào trường `OriginalImei` để đảm bảo tính minh bạch và lịch sử đối soát kỹ thuật.
     - Thời hạn bảo hành của máy mới được kế thừa thời hạn còn lại của máy cũ (hoặc tối thiểu 90 ngày theo chính sách Apple).

#### 3. Cập nhật kết quả kỹ thuật & Quản lý phiếu bảo hành (`WarrantyClaims`)
- Mỗi lần khách hàng gửi máy bảo hành, hệ thống tạo một phiếu ghi nhận `WarrantyClaims` với mã số duy nhất.
- Ghi nhận thông tin ban đầu: Mô tả triệu chứng lỗi của khách (Sọc màn hình, camera không lấy nét, pin báo bảo trì, mất Face ID, loa rè...).
- **Cập nhật 3 kịch bản kết quả kỹ thuật**:
  1. **Đổi máy mới (Exchange Device)**: Áp dụng cho trường hợp đủ điều kiện 1 đổi 1 trong 30 ngày (như mục 2 ở trên).
  2. **Thay thế linh kiện chính hãng (Component Replacement)**:
     - Gửi máy sang Trung tâm Bảo hành Ủy quyền Apple (AASP).
     - Ghi nhận kết quả: Đã thay Màn hình Super Retina XDR Zin, Đã thay Cụm Camera Zin, Đã thay Pin chuẩn hãng.
     - Chuyển trạng thái phiếu: `Tiếp nhận` $\rightarrow$ `Đang xử lý tại Apple` $\rightarrow$ `Đã hoàn tất` $\rightarrow$ `Đã bàn giao cho khách`.
  3. **Từ chối bảo hành (Void / Reject Warranty)**:
     - Phát hiện máy có dấu hiệu hư hỏng do tác động vật lý của người dùng: Rơi vỡ màn hình, cấn móp sườn vỏ, có dấu hiệu ngấm nước làm đổi màu quỳ tím chỉ thị độ ẩm bên trong, tự ý bung máy can thiệp phần cứng.
     - Nhân viên chọn kết quả `Từ chối bảo hành`, đính kèm ảnh chụp hiện trường vi phạm và lý do chi tiết vào biên bản kỹ thuật. Cập nhật trạng thái bảo hành thành `Voided`.

---

### 3.4. Thống kê & Báo cáo Thông minh (Dashboard & Analytics)

#### 1. Thống kê tổng doanh thu đa kỳ hạn
- **Chỉ số kinh doanh tức thời (Real-time KPIs)**:
  - Doanh thu Hôm nay (so sánh với Hôm qua và cùng kỳ tuần trước).
  - Doanh thu Tháng này (so sánh với Tháng trước và chỉ tiêu đề ra).
  - Doanh thu Lũy kế Năm tài chính.
- **Phân tích đơn hàng**:
  - Tổng số đơn đặt hàng toàn hệ thống.
  - Tỷ lệ đơn hoàn tất thành công (Fulfillment Rate).
  - Tỷ lệ đơn bị hủy và nguyên nhân chính (Khách đổi ý, Hết hàng, Quá hạn thanh toán).
  - Giá trị đơn hàng trung bình (AOV - Average Order Value).

#### 2. Biểu đồ & Xếp hạng iPhone bán chạy nhất theo phiên bản/dung lượng (Best Sellers)
- **Bảng xếp hạng sản phẩm tiêu thụ mạnh nhất**:
  - Xếp hạng theo Doanh số số lượng máy bán ra (Units Sold) và Doanh thu thực thu (Gross Revenue).
  - Ví dụ xếp hạng mẫu:
    1. *iPhone 16 Pro Max 256GB Titan Sa Mạc* (Chiếm 42% doanh số phân khúc Pro).
    2. *iPhone 16 Pro 128GB Titan Tự Nhiên* (Chiếm 28%).
    3. *iPhone 16 128GB Xanh Lưu Ly* (Chiếm 18%).
    4. *iPhone 15 128GB Hồng Pastel* (Dẫn đầu phân khúc giá tầm trung).
- **Phân tích cơ cấu phiên bản**:
  - Biểu đồ tỷ lệ dung lượng: So sánh thị phần bán ra của các mức bộ nhớ (128GB vs 256GB vs 512GB vs 1TB).
  - Biểu đồ tỷ lệ màu sắc: Phản ánh xu hướng màu sắc hot nhất trên thị trường để bộ phận nhập hàng điều chỉnh cơ cấu tồn kho.

#### 3. Cảnh báo tồn kho thấp & Nguy cơ đứt gãy hàng hóa (Low Stock Alerts)
- **Hệ thống cảnh báo hai cấp độ (Stock Warning Engine)**:
  - **Mức Cảnh báo Vàng (Warning)**: Số lượng tồn kho `Quantity <= 5` máy.
  - **Mức Cảnh báo Đỏ (Critical / Out-of-Stock)**: Số lượng tồn kho `Quantity == 0` hoặc số lượng mã IMEI có trạng thái `InStock` bằng 0.
- **Thống kê trạng thái mã IMEI toàn kho**:
  - Tổng số lượng IMEI nhập vào hệ thống.
  - Số lượng đang sẵn sàng bán (`InStock`).
  - Số lượng đang bị giữ chỗ cho đơn thanh toán online (`Reserved`).
  - Số lượng đã bán và bàn giao (`Sold`).
  - Số lượng hàng lỗi đang chờ đổi trả thẩm định (`Defective`).
- Bảng thông báo tồn kho hiển thị ngay tại trang chủ Dashboard để Quản lý cửa hàng chủ động lên đơn đặt hàng nhập mới với nhà phân phối trước khi cháy hàng.

---

### 3.5. Quản lý Khách hàng & Tài khoản (Customer & Account Management)

> **Quy chuẩn Phạm vi Đồ án Giữa kỳ (Midterm Architecture Scope)**:  
> Theo định hướng từ Giảng viên hướng dẫn, phân hệ Quản trị Admin được tối ưu hóa tập trung: **Duy nhất 1 vai trò Quản trị viên (Admin) nắm toàn quyền điều hành**, loại bỏ việc chia nhỏ các vai trò nhân sự nội bộ không cần thiết ở giai đoạn giữa kỳ. Hệ thống chỉ gồm **2 vai trò duy nhất: `Admin` và `Customer`**.  
> Toàn bộ module được quy hoạch lại thành **Trung tâm Quản lý Khách hàng & Tài khoản Apple AAR**, loại bỏ các cơ chế phức tạp không cần thiết (không phân cấp bậc thành viên, không quản lý điểm tích lũy, không đa tài khoản/hợp nhất, không tạo khách hàng thủ công do khách hàng tự động khởi tạo khi đăng ký tại Storefront hoặc mua hàng), thiết lập chuẩn hóa mô hình định danh 1-1 tinh gọn (`1 Customer = 1 ApplicationUser` hoặc `UserId = null` đối với khách vãng lai). Module tập trung xuất sắc vào **3 nghiệp vụ quản trị cốt lõi**.
>
> **Quy chuẩn Mật khẩu & Danh sách 5 Khách hàng Thành viên Mẫu Phục vụ Chấm Đồ án & Kiểm thử**:  
> - **Tuân thủ Tuyệt đối Quy tắc An ninh Mật khẩu (Mục 2.4.1.4)**:  
>   Mật khẩu cho các tài khoản kiểm thử khách hàng được thiết lập chuẩn hóa là **`Customer@123`**. Mật khẩu này thỏa mãn 100% ràng buộc nghiệp vụ:  
>   1. Độ dài: **12 ký tự** (vượt chuẩn tối thiểu 8 ký tự).  
>   2. Đạt đầy đủ **4/4 nhóm ký tự**: Chữ hoa (`C`), chữ thường (`ustomer`), chữ số (`123`), ký tự đặc biệt (`@`).  
>   3. Không nằm trong danh bạ mật khẩu yếu (`CommonPasswords`).  
>   4. Được băm an toàn theo thuật toán PBKDF2 HMAC-SHA512 (ASP.NET Core Identity).  
>
> | STT | Họ và tên Khách hàng | Username | Mật khẩu mẫu | Số điện thoại | Địa chỉ Email | Địa chỉ giao hàng mặc định | Số đơn hàng mẫu |
> | :---: | :--- | :--- | :---: | :---: | :--- | :--- | :---: |
> | 1 | **Võ Văn Nghé** | `nghecute` | `Customer@123` | `0982726262` | `nghecute@gmail.com` | 123 Nguyễn Huệ, P. Bến Nghé, Q. 1, TP. HCM | 4 đơn |
> | 2 | **Nguyễn Thị Mai** | `mainguyen` | `Customer@123` | `0912345678` | `mainguyen@gmail.com` | 456 Lê Duẩn, P. Bến Nghé, Q. 1, TP. HCM | 3 đơn |
> | 3 | **Trần Văn Hùng** | `hungtran` | `Customer@123` | `0988776655` | `hungtran@gmail.com` | 789 Nguyễn Văn Linh, P. Tân Phong, Q. 7, TP. HCM | 3 đơn |
> | 4 | **Lê Hoàng Long** | `longle` | `Customer@123` | `0903284116` | `longle@gmail.com` | 246 Hai Bà Trưng, P. Tân Định, Q. 1, TP. HCM | 3 đơn |
> | 5 | **Phạm Thu Thảo** | `thaopham` | `Customer@123` | `0918650293` | `thaopham@gmail.com` | 135 Võ Văn Tần, P. Võ Thị Sáu, Q. 3, TP. HCM | 3 đơn |
>
> *(Tài khoản Quản trị viên tối cao: Username: `admin` | Mật khẩu: `admin123` | Vai trò: `Admin`)*. Toàn bộ các dữ liệu thử nghiệm cũ rác/sai quy cách trước đây đã được thanh lọc toàn diện khỏi CSDL, mã nguồn Seeder và SQL Script.

---

#### 1. Nghiệp vụ 1: Xem chi tiết & Chỉnh sửa thông tin khách hàng trực tiếp trên Pop-up Modal (Inline Modal Edit)
- **Bản chất nghiệp vụ**: Cung cấp cho Quản trị viên góc nhìn toàn diện về từng khách hàng, đồng thời cho phép cập nhật thông tin nhanh chóng thông qua cửa sổ bật lên (Pop-up Modal) mà không cần chuyển trang.
- **Xem chi tiết hồ sơ khách hàng (Customer Inspection)**:
  - **Thông tin định danh & liên hệ**: Mã khách hàng (`CustomerId`), Tên đăng nhập (`UserName`), Họ và tên (`FullName`), Địa chỉ Email, Số điện thoại liên hệ, Địa chỉ nhận hàng mặc định.
  - **Lịch sử hoạt động**:
    + Tổng số đơn hàng đã đặt và danh sách liên kết nhanh sang chi tiết đơn.
    + Danh sách thiết bị iPhone đang được bảo hành điện tử chính hãng (gắn với số IMEI).
  - **Trạng thái tài khoản**: `Đang hoạt động (Active)` hoặc `Bị vô hiệu hóa / Tạm khóa (Locked)` (hoặc `Chưa có tài khoản` đối với khách vãng lai).
- **Chỉnh sửa thông tin khách hàng trực tiếp qua Pop-up Modal**:
  - Khi Admin click nút **"Chi tiết"** trên từng dòng khách hàng: Hệ thống mở Modal Form và tải trực tiếp dữ liệu hiện tại lên các ô nhập liệu.
  - **Các trường dữ liệu cho phép sửa đổi (tập trung thông tin khách hàng)**:
    1. *Họ và tên*: Chuỗi ký tự độ dài 2 - 100 ký tự.
    2. *Số điện thoại*: Bắt buộc đúng định dạng di động Việt Nam (10 số, đầu `03x - 09x`).
    3. *Địa chỉ Email*: Chuẩn RFC 5322.
    4. *Địa chỉ nhận hàng*: Tối đa 255 ký tự (số nhà, đường, phường/xã, quận/huyện, tỉnh/thành).
  - **Ma trận ràng buộc & Chống trùng lặp dữ liệu (Đồng bộ với Ma trận Đăng ký mục 2.4.4)**:
    + **Kiểm tra Họ và tên**: Bắt buộc, độ dài 2 - 100 ký tự. Lỗi trả về: *"Vui lòng nhập họ và tên của bạn (tối thiểu 2 ký tự)!"*.
    + **Kiểm tra định dạng Số điện thoại**: Bắt buộc đúng 10 số di động Việt Nam (đầu `03x, 05x, 07x, 08x, 09x`). Lỗi trả về: *"Số điện thoại không hợp lệ (Phải là 10 chữ số di động đầu 03, 05, 07, 08, 09)!"*.
    + **Kiểm tra trùng Số điện thoại (Phone Unique Guard)**: Truy vấn kiểm tra trên toàn bộ CSDL (`Customers` và `AspNetUsers`, ngoại trừ chính hồ sơ đang sửa). Lỗi trả về: *"Số điện thoại {stdPhone} này đã tồn tại trên hệ thống (đang được liên kết với khách hàng #{id} - {FullName})!"*.
    + **Kiểm tra định dạng Email**: Chuẩn RFC 5322 regex. Lỗi trả về: *"Địa chỉ email không đúng định dạng chuẩn (VD: tenban@gmail.com)!"*.
    + **Kiểm tra trùng Email (Email Unique Guard)**: Đảm bảo địa chỉ email chưa được liên kết với bất kỳ khách hàng hay tài khoản nào khác trên hệ thống. Lỗi trả về: *"Địa chỉ email {cleanEmail} này đã được liên kết với một tài khoản khác trên hệ thống!"*.
    + **Kiểm tra Địa chỉ nhận hàng**: Nếu có nhập thì phải tối thiểu 5 ký tự (chuẩn hóa số nhà, tên đường). Lỗi trả về: *"Vui lòng nhập số nhà, tên đường cụ thể (tối thiểu 5 ký tự)!"*.
    + **Cơ chế phản hồi Realtime trên Pop-up Modal (Ajax Inline Error Feedback)**:
      - Khi Admin bấm "Lưu thay đổi", form được submit ngầm qua Ajax (`X-Requested-With: XMLHttpRequest`).
      - Nếu phát hiện vi phạm nghiệp vụ (trùng SĐT, trùng Email, sai định dạng), Backend trả về `{ success: false, message: "..." }`. Hệ thống **giữ nguyên Modal** và hiển thị thông báo lỗi màu đỏ ngay phía trên các ô nhập liệu giúp Admin nắm bắt và chỉnh sửa tức thời mà không bị mất dữ liệu đã nhập.
      - Nếu hợp lệ: Backend thực thi **Đồng bộ nguyên tử (Atomic Synchronization)** trên cả thực thể `Customers` và `AspNetUsers` (`PhoneNumber`, `Email`, `NormalizedEmail`), lưu vết `AuditLogs` và phản hồi `{ success: true }` để tự động làm mới giao diện.

---

#### 2. Nghiệp vụ 2: Đổi mật khẩu tài khoản khách hàng (Admin Password Reset)
- **Tình huống nghiệp vụ**: Khách hàng quên mật khẩu và gọi hotline hoặc đến trực tiếp cửa hàng nhờ Admin hỗ trợ cấp lại mật khẩu mới; hoặc khi tài khoản bị phát hiện có dấu hiệu lộ lọt thông tin.
- **Quy trình thực thi trên giao diện**:
  1. Trên danh sách khách hàng, Admin nhấn nút **"Đổi MK"** tại dòng tài khoản tương ứng.
  2. Modal "Cấp lại mật khẩu khách hàng" xuất hiện:
     - Hiển thị rõ: *Tên khách hàng*, *Username* và *Email/SĐT định danh*.
     - Ô nhập: *Mật khẩu mới* (có nút icon mắt ẩn/hiện mật khẩu).
     - Nút tiện ích: *"Tạo mật khẩu ngẫu nhiên an toàn"* (Random Strong Password Generator).
  3. Admin bấm "Xác nhận cập nhật".
- **Cơ chế kỹ thuật & Bảo mật phía Backend**:
  - **Kiểm tra hợp lệ mật khẩu mới**: Độ dài tối thiểu 6 ký tự theo quy định bảo mật Identity.
  - **Thực thi đặt lại an toàn qua ASP.NET Core Identity API**:
    ```csharp
    var user = await _userManager.FindByIdAsync(userId);
    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
    var result = await _userManager.ResetPasswordAsync(user, token, newPassword.Trim());
    ```
  - **Cơ chế Thu hồi Phiên tức thời (Security Stamp Revocation)**:
    - Ngay sau khi đặt lại mật khẩu, hệ thống bắt buộc gọi:
      ```csharp
      await _userManager.UpdateSecurityStampAsync(user);
      ```
    - *Ý nghĩa an ninh*: Mã `SecurityStamp` mới sẽ khiến toàn bộ các phiên Cookie / Session đăng nhập cũ của tài khoản này trên tất cả các trình duyệt và thiết bị khác bị vô hiệu hóa ngay lập tức. Khách hàng bắt buộc phải đăng nhập lại bằng mật khẩu mới được cấp.
  - **Ghi vết kiểm toán**: Tạo bản ghi trong `AuditLogs`: `Action = "RESET_PASSWORD"`, đối tượng bị tác động `UserId`, ghi nhận thời gian và Admin thực hiện.

---

#### 3. Nghiệp vụ 3: Khóa / Mở khóa tài khoản khách hàng (Account Lock & Unlock)
- **Tình huống nghiệp vụ**:
  - Tài khoản khách hàng vi phạm chính sách: Spam đặt đơn ảo, bom hàng nhiều lần khi chọn COD.
  - Khách hàng báo mất điện thoại/thiết bị và yêu cầu cửa hàng tạm khóa tài khoản để bảo vệ thông tin cá nhân và đơn hàng.
- **Giao diện thao tác**:
  - Cột trạng thái hiển thị rõ: Huy hiệu xanh `● Đang hoạt động` hoặc Huy hiệu đỏ `● Bị vô hiệu hóa` (hoặc nhãn `Chưa có tài khoản` đối với khách vãng lai).
  - Cột thao tác cung cấp nút bấm chuyển đổi trạng thái trực tiếp:
    + Nếu tài khoản đang mở: Nút **"Khóa"** (kèm hộp thoại xác nhận an toàn trước khi khóa).
    + Nếu tài khoản đang khóa: Nút **"Mở"** (phục hồi quyền truy cập tức thì).
- **Cơ chế kỹ thuật phòng vệ 2 lớp (Dual-Layer Lockout Mechanism)**:
  - **Lớp 1: Cờ ứng dụng (`ApplicationUser.IsActive`)**:
    - Khi Khóa: Chuyển `IsActive = false`.
    - Khi Mở: Chuyển `IsActive = true`.
  - **Lớp 2: Cơ chế khóa chuẩn của ASP.NET Core Identity (`LockoutEnd`)**:
    - Khi Khóa:
      ```csharp
      user.IsActive = false;
      user.LockoutEnd = DateTimeOffset.MaxValue; // Khóa vĩnh viễn cho đến khi Admin mở
      user.LockoutEnabled = true;
      await _userManager.UpdateAsync(user);
      await _userManager.UpdateSecurityStampAsync(user); // Đá văng phiên đăng nhập hiện tại
      ```
    - Khi Mở:
      ```csharp
      user.IsActive = true;
      user.LockoutEnd = null;
      user.AccessFailedCount = 0;
      await _userManager.UpdateAsync(user);
      ```
- **Hệ quả tức thời khi tài khoản bị khóa**:
  1. **Chặn đăng nhập**: Khi người dùng cố gắng đăng nhập tại Storefront (`AccountController.Login`), hệ thống kiểm tra `!user.IsActive || await _userManager.IsLockedOutAsync(user)` và từ chối truy cập ngay lập tức, trả về thông báo: *"Tài khoản của bạn đã bị khóa. Vui lòng liên hệ bộ phận hỗ trợ khách hàng!"*.
  2. **Đá văng phiên hiện tại (Active Session Eviction)**: Nếu khách hàng đang đăng nhập trên trình duyệt, `SecurityStampValidator` tích hợp trong ASP.NET Core sẽ phát hiện tem an ninh thay đổi và hủy Cookie xác thực ở request kế tiếp, chuyển hướng khách về trang Login.
  3. Ghi vết `AuditLogs` đầy đủ để phục vụ kiểm toán đối soát.

---

## 4. CƠ CHẾ KỸ THUẬT CHUYÊN SÂU (ADVANCED BACKEND ENGINES)

### 4.1. Kiểm soát Đồng thời & Chống bán vượt kho (Optimistic Concurrency)
- Cột `RowVersion ROWVERSION NOT NULL` trên cả bảng `Products` và `ProductImeis`.
- Bắt ngoại lệ `DbUpdateConcurrencyException` khi có nhiều khách cùng tranh mua máy cuối cùng trong đợt Flash Sale.
- Thực hiện cập nhật số lượng tồn kho bên trong Database Transaction có mức cô lập an toàn.

### 4.2. Tiến trình Tự động Hủy đơn & Hoàn kho (Background Hosted Service)
- Lớp `OrderHoldCleanupWorker : BackgroundService` chạy ngầm độc lập trong .NET 9.
- Định kỳ mỗi **60 giây**, worker quét tự động:
  ```csharp
  OrderStatus == "Pending" && PaymentMethod != "COD" && OrderDate < DateTime.Now.AddMinutes(-15)
  ```
- **Hành động nguyên tử**:
  1. Chuyển `OrderStatus = 'Cancelled'`.
  2. Hoàn lại số lượng tồn kho `Products.Quantity += item.Quantity`.
  3. Giải phóng các mã IMEI bị giữ chỗ từ `Reserved` trở về `InStock`.
  4. Ghi bản ghi vào `AuditLogs` với tác nhân `OrderHoldCleanupWorker`.

### 4.3. Hệ thống Nhật ký Kiểm toán (Audit Logs)
- Ghi vết mọi biến động nhạy cảm trên hệ thống: Tạo đơn hàng, duyệt đơn, xuất kho IMEI, hủy đơn tự động, đổi máy bảo hành.
- Lưu trữ: `UserId`, `UserName`, `Action`, `EntityName`, `RecordId`, `OldValues`, `NewValues` (JSON), `IpAddress` và `Timestamp`.
- Liên kết chuẩn khóa ngoại `FK_AuditLogs_AspNetUsers` với `AspNetUsers(Id)`.

---

## 5. THIẾT KẾ CƠ SỞ DỮ LIỆU MỞ RỘNG (DATABASE SCHEMA)

```
                            SƠ ĐỒ THỰC THỂ CƠ SỞ DỮ LIỆU
                                    (IPhoneStoreDb)

     ┌───────────────┐                  ┌──────────────────┐ 1              * ┌───────────────────┐
     │  AspNetUsers  │◄─────────────────┤    Customers     ├─────────────────┤ CustomerAddresses │
     └───────┬───────┘                  └────────┬─────────┘                  └───────────────────┘
             │                                   │ 1                           (Sổ địa chỉ 4 cấp)
             │ 1                                 │
             ▼ *                                 ▼ *
     ┌───────────────┐                  ┌──────────────────┐
     │   AuditLogs   │                  │      Orders      │
     └───────────────┘                  └────────┬─────────┘
                                                 │ 1
                                                 │
                                                 ▼ *
     ┌───────────────┐ 1              * ┌──────────────────┐ 1              * ┌───────────────┐
     │  Categories   ├─────────────────┤     Products     ├─────────────────┤  ProductImeis │
     └───────────────┘                  └────────┬─────────┘                  └───────┬───────┘
                                                 │ 1                                  │
                                                 │                                    │
                                                 ▼ *                                  │
                                        ┌──────────────────┐                          │
                                        │   OrderDetails   │◄─────────────────────────┘
                                        └────────┬─────────┘
                                                 │ 1
                                                 │
                                                 ▼ *
                                        ┌──────────────────┐ 1              * ┌───────────────┐
                                        │    Warranties    ├─────────────────┤ WarrantyClaims│
                                        └──────────────────┘                  └───────────────┘
```

---

## 6. KIẾN TRÚC TÁI SỬ DỤNG CHO 3 DỰ ÁN (MVC, BLAZOR, RAZOR)

Cả 3 dự án đều sử dụng chung một nguồn dữ liệu và logic nghiệp vụ:
- **`IPhoneStore_Mvc`**: Giao diện Cửa hàng bán lẻ chuyên nghiệp & Toàn bộ trang Quản trị Admin Area (`/Admin/Products`, `/Admin/Categories`, `/Admin/Orders`, `/Admin/Accounts`).
- **`IPhoneStore_Blazor`**: Ứng dụng Single-Page Application (SPA) tương tác thời gian thực, quản lý giỏ hàng Blazor Service, giao diện mua hàng và tra cứu bảo hành `/warranty`.
- **`IPhoneStore_Razor`**: Giao diện Razor Pages tối ưu SEO, danh mục sản phẩm, đặt hàng bảo mật `[Authorize]` và tra cứu bảo hành `/Warranty`.
