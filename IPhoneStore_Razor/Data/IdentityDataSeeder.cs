using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using IPhoneStore_Razor.Models.Entities;

namespace IPhoneStore_Razor.Data;

public static class IdentityDataSeeder
{
    // Cờ kiểm soát: Đặt true để tự động làm sạch và nạp lại Dữ liệu mẫu chuẩn AAR khi khởi động
    public static bool ForceCleanResetOnStartup = true;

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IPhoneStoreDbContext>();

        // Kiểm tra xem có cần tự động Reset dữ liệu mẫu không (khi cờ bật hoặc phát hiện sản phẩm test cũ)
        bool hasTestJunk = await db.Products.AnyAsync(p => p.Name.Contains("Kim Ji Won") || p.Name.Contains("ip 100") || p.Sku == "Tp78" || p.Sku == "KJW-1");

        if (ForceCleanResetOnStartup || hasTestJunk)
        {
            await CleanAndSeedAllAsync(serviceProvider, forceReset: true);
            ForceCleanResetOnStartup = false; // Chỉ chạy 1 lần khi khởi động
            return;
        }

        // Nếu dữ liệu đã chuẩn thì chỉ đảm bảo tài khoản Admin và Khách hàng tồn tại
        await EnsureIdentityUsersAsync(serviceProvider);
    }

    /// <summary>
    /// Xóa sạch dữ liệu test và nạp lại toàn bộ Dữ liệu mẫu chuẩn Apple Authorized Reseller (AAR)
    /// </summary>
    public static async Task CleanAndSeedAllAsync(IServiceProvider serviceProvider, bool forceReset = true)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<IPhoneStoreDbContext>();

        // =========================================================================
        // 1. TẠO ROLES VÀ TÀI KHOẢN ĐĂNG NHẬP CHUẨN
        // =========================================================================
        string[] requiredRoles = { "Admin", "Customer" };
        foreach (var role in requiredRoles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Admin User
        var adminUser = await userManager.FindByNameAsync("admin");
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "admin",
                Email = "admin@istore.vn",
                PhoneNumber = "0909000001",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            var createAdmin = await userManager.CreateAsync(adminUser, "admin123");
            if (createAdmin.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
        else
        {
            adminUser.Email = "admin@istore.vn";
            adminUser.PhoneNumber = "0909000001";
            adminUser.IsActive = true;
            await userManager.UpdateAsync(adminUser);
            if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
            if (await userManager.IsInRoleAsync(adminUser, "Customer"))
            {
                await userManager.RemoveFromRoleAsync(adminUser, "Customer");
            }
        }

        // 5 Khách hàng thành viên chuẩn hóa (Mật khẩu chuẩn BRS: Customer@123)
        const string standardPassword = "Customer@123";
        var customerDefs = new[]
        {
            new {
                Username = "nghecute",
                FullName = "Võ Văn Nghé",
                Email = "nghecute@gmail.com",
                Phone = "0982726262",
                Address = "123 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh",
                Tier = "VIP",
                Points = 350
            },
            new {
                Username = "mainguyen",
                FullName = "Nguyễn Thị Mai",
                Email = "mainguyen@gmail.com",
                Phone = "0912345678",
                Address = "456 Lê Duẩn, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh",
                Tier = "Member",
                Points = 120
            },
            new {
                Username = "hungtran",
                FullName = "Trần Văn Hùng",
                Email = "hungtran@gmail.com",
                Phone = "0988776655",
                Address = "789 Nguyễn Văn Linh, Phường Tân Phong, Quận 7, TP. Hồ Chí Minh",
                Tier = "Member",
                Points = 80
            },
            new {
                Username = "longle",
                FullName = "Lê Hoàng Long",
                Email = "longle@gmail.com",
                Phone = "0903284116",
                Address = "246 Hai Bà Trưng, Phường Tân Định, Quận 1, TP. Hồ Chí Minh",
                Tier = "VIP",
                Points = 450
            },
            new {
                Username = "thaopham",
                FullName = "Phạm Thu Thảo",
                Email = "thaopham@gmail.com",
                Phone = "0918650293",
                Address = "135 Võ Văn Tần, Phường Võ Thị Sáu, Quận 3, TP. Hồ Chí Minh",
                Tier = "Member",
                Points = 50
            }
        };

        var seededCustomerMap = new Dictionary<string, Customer>();

        foreach (var def in customerDefs)
        {
            var user = await userManager.FindByNameAsync(def.Username);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = def.Username,
                    Email = def.Email,
                    PhoneNumber = def.Phone,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-30)
                };
                var createRes = await userManager.CreateAsync(user, standardPassword);
                if (createRes.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Customer");
                }
            }
            else
            {
                user.Email = def.Email;
                user.PhoneNumber = def.Phone;
                user.IsActive = true;
                await userManager.UpdateAsync(user);

                if (!await userManager.IsInRoleAsync(user, "Customer"))
                {
                    await userManager.AddToRoleAsync(user, "Customer");
                }

                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                await userManager.ResetPasswordAsync(user, token, standardPassword);
            }
        }

        // =========================================================================
        // 2. DỌN SẠCH CÁC BẢNG DỮ LIỆU ĐỂ RESET VỀ TRẠNG THÁI MẪU CHUẨN
        // =========================================================================
        try
        {
            await db.Database.ExecuteSqlRawAsync(@"
                IF OBJECT_ID('dbo.WarrantyClaims', 'U') IS NOT NULL DELETE FROM WarrantyClaims;
                IF OBJECT_ID('dbo.Warranties', 'U') IS NOT NULL DELETE FROM Warranties;
                IF OBJECT_ID('dbo.OrderDetails', 'U') IS NOT NULL DELETE FROM OrderDetails;
                IF OBJECT_ID('dbo.ProductImeis', 'U') IS NOT NULL DELETE FROM ProductImeis;
                IF OBJECT_ID('dbo.Orders', 'U') IS NOT NULL DELETE FROM Orders;
                IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL DELETE FROM Products;
                IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DELETE FROM Categories;
                IF OBJECT_ID('dbo.AuditLogs', 'U') IS NOT NULL DELETE FROM AuditLogs;
            ");

            // Reseed Identity tự động nếu SQL Server hỗ trợ
            try
            {
                await db.Database.ExecuteSqlRawAsync(@"
                    IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL DBCC CHECKIDENT ('Products', RESEED, 0);
                    IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DBCC CHECKIDENT ('Categories', RESEED, 0);
                    IF OBJECT_ID('dbo.Orders', 'U') IS NOT NULL DBCC CHECKIDENT ('Orders', RESEED, 0);
                    IF OBJECT_ID('dbo.OrderDetails', 'U') IS NOT NULL DBCC CHECKIDENT ('OrderDetails', RESEED, 0);
                    IF OBJECT_ID('dbo.ProductImeis', 'U') IS NOT NULL DBCC CHECKIDENT ('ProductImeis', RESEED, 0);
                    IF OBJECT_ID('dbo.Warranties', 'U') IS NOT NULL DBCC CHECKIDENT ('Warranties', RESEED, 0);
                    IF OBJECT_ID('dbo.WarrantyClaims', 'U') IS NOT NULL DBCC CHECKIDENT ('WarrantyClaims', RESEED, 0);
                    IF OBJECT_ID('dbo.AuditLogs', 'U') IS NOT NULL DBCC CHECKIDENT ('AuditLogs', RESEED, 0);
                ");
            }
            catch { /* Ignore reseed if table is empty or driver limitation */ }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Seeder Raw SQL Warning]: {ex.Message}");
            // Fallback EF Core nếu raw SQL gặp vấn đề
            try
            {
                db.WarrantyClaims.RemoveRange(db.WarrantyClaims);
                db.Warranties.RemoveRange(db.Warranties);
                db.OrderDetails.RemoveRange(db.OrderDetails);
                db.ProductImeis.RemoveRange(db.ProductImeis);
                await db.SaveChangesAsync();

                db.Orders.RemoveRange(db.Orders);
                db.Products.RemoveRange(db.Products);
                await db.SaveChangesAsync();

                db.Categories.RemoveRange(db.Categories);
                db.AuditLogs.RemoveRange(db.AuditLogs);
                await db.SaveChangesAsync();
            }
            catch (Exception ex2)
            {
                Console.WriteLine($"[Seeder EF Fallback Warning]: {ex2.Message}");
            }
        }

        // Đồng bộ 5 hồ sơ Customer trong bảng Customers
        foreach (var def in customerDefs)
        {
            var user = await userManager.FindByNameAsync(def.Username);
            var profile = await db.Customers.FirstOrDefaultAsync(c => c.UserId == user!.Id || c.PhoneNumber == def.Phone);
            if (profile == null)
            {
                profile = new Customer
                {
                    UserId = user?.Id,
                    FullName = def.FullName,
                    Email = def.Email,
                    PhoneNumber = def.Phone,
                    Address = def.Address,
                    MembershipTier = def.Tier,
                    RewardPoints = def.Points,
                    TotalSpent = 0,
                    CreatedAt = DateTime.Now.AddDays(-20)
                };
                db.Customers.Add(profile);
            }
            else
            {
                profile.UserId = user?.Id;
                profile.FullName = def.FullName;
                profile.Email = def.Email;
                profile.PhoneNumber = def.Phone;
                profile.Address = def.Address;
                profile.MembershipTier = def.Tier;
                profile.RewardPoints = def.Points;
            }
            await db.SaveChangesAsync();
            seededCustomerMap[def.Username] = profile;
        }

        // Xóa khách hàng rác không thuộc 5 tài khoản mẫu
        var validCustomerIds = seededCustomerMap.Values.Select(v => v.Id).ToList();
        var junkCustomers = await db.Customers.Where(c => !validCustomerIds.Contains(c.Id)).ToListAsync();
        if (junkCustomers.Any())
        {
            db.Customers.RemoveRange(junkCustomers);
            await db.SaveChangesAsync();
        }

        // =========================================================================
        // 3. SEED DANH MỤC DÒNG MÁY (CATEGORIES)
        // =========================================================================
        var cat16 = new Category
        {
            Name = "iPhone 16 Series",
            Description = "Dòng flagship Apple mới nhất với chip A18 Pro, nút Camera Control và khung Titan siêu nhẹ",
            DisplayOrder = 1,
            IsActive = true
        };
        var cat15 = new Category
        {
            Name = "iPhone 15 Series",
            Description = "Thế hệ iPhone khung viền Titan và cổng kết nối USB-C chuẩn mực toàn cầu",
            DisplayOrder = 2,
            IsActive = true
        };
        var cat14 = new Category
        {
            Name = "iPhone 14 Series",
            Description = "Dòng iPhone kinh điển bền bỉ, màn hình Dynamic Island đỉnh cao",
            DisplayOrder = 3,
            IsActive = true
        };

        db.Categories.AddRange(cat16, cat15, cat14);
        await db.SaveChangesAsync();

        // =========================================================================
        // 4. SEED SẢN PHẨM & BIẾN THỂ (PRODUCTS)
        // Gồm cả: Sản phẩm sẵn hàng, 1 sản phẩm Tồn kho thấp (3 máy), 1 sản phẩm Hết hàng (0 máy)
        // =========================================================================
        var p1 = new Product
        {
            CategoryId = cat16.Id,
            Name = "iPhone 16 Pro Max 256GB Titan Sa Mạc",
            ModelSeries = "iPhone 16 Series",
            Sku = "IP16PM-256-DES",
            Color = "Titan Sa Mạc",
            Storage = "256GB",
            ListedPrice = 36990000,
            Price = 34990000,
            Quantity = 0, // Sẽ được đồng bộ tự động theo IMEI InStock
            WarrantyMonths = 12,
            ImageUrl = "/images/hero-iphone16.jpg",
            Description = "iPhone 16 Pro Max với chip A18 Pro, nút Điều khiển Camera hoàn toàn mới, camera tiềm vọng 5x và thời lượng pin xuất sắc nhất trên iPhone.",
            Specifications = "Màn hình 6.9 inch Super Retina XDR OLED 120Hz; Chip A18 Pro 3nm; Camera chính 48MP; Pin 4685 mAh; Sạc MagSafe 25W.",
            IsActive = true
        };

        var p2 = new Product
        {
            CategoryId = cat16.Id,
            Name = "iPhone 16 Pro 128GB Titan Tự Nhiên",
            ModelSeries = "iPhone 16 Series",
            Sku = "IP16P-128-NAT",
            Color = "Titan Tự Nhiên",
            Storage = "128GB",
            ListedPrice = 30990000,
            Price = 28990000,
            Quantity = 0,
            WarrantyMonths = 12,
            ImageUrl = "/images/camera-iphone16.jpg",
            Description = "iPhone 16 Pro thiết kế viền mỏng nhất từng có, hiệu năng Pro trong kích thước nhỏ gọn 6.3 inch.",
            Specifications = "Màn hình 6.3 inch Super Retina XDR OLED 120Hz; Chip A18 Pro; Nút Camera Control; Cổng USB-C tốc độ 10Gbps.",
            IsActive = true
        };

        var p3 = new Product
        {
            CategoryId = cat16.Id,
            Name = "iPhone 16 128GB Hồng",
            ModelSeries = "iPhone 16 Series",
            Sku = "IP16-128-PNK",
            Color = "Hồng",
            Storage = "128GB",
            ListedPrice = 24990000,
            Price = 22490000,
            Quantity = 0,
            WarrantyMonths = 12,
            ImageUrl = "/images/hero-iphone16.jpg",
            Description = "iPhone 16 màu Hồng cá tính, sở hữu nút Tác Vụ (Action Button) và cụm camera kép đặt dọc hỗ trợ quay Spatial Video.",
            Specifications = "Màn hình 6.1 inch Super Retina XDR; Chip A18; Nút Action & Camera Control; Kính pha màu Ceramic Shield bền bỉ.",
            IsActive = true
        };

        var p4 = new Product
        {
            CategoryId = cat16.Id,
            Name = "iPhone 16 128GB Xanh Lưu Ly",
            ModelSeries = "iPhone 16 Series",
            Sku = "IP16-128-ULT",
            Color = "Xanh Lưu Ly",
            Storage = "128GB",
            ListedPrice = 24990000,
            Price = 22490000,
            Quantity = 0,
            WarrantyMonths = 12,
            ImageUrl = "/images/camera-iphone16.jpg",
            Description = "iPhone 16 phiên bản Xanh Lưu Ly (Ultramarine) độc quyền đầy cuốn hút, công nghệ kính pha màu siêu bền.",
            Specifications = "Màn hình 6.1 inch Super Retina XDR; Chip A18; 2 camera sau 48MP Fusion & 12MP Ultra Wide.",
            IsActive = true
        };

        var p5 = new Product
        {
            CategoryId = cat15.Id,
            Name = "iPhone 15 128GB Xanh Pastel",
            ModelSeries = "iPhone 15 Series",
            Sku = "IP15-128-BLU",
            Color = "Xanh Pastel",
            Storage = "128GB",
            ListedPrice = 21990000,
            Price = 19490000,
            Quantity = 0,
            WarrantyMonths = 12,
            ImageUrl = "/images/hero-iphone16.jpg",
            Description = "iPhone 15 mang màn hình Dynamic Island hiện đại, camera 48MP sắc nét và cổng sạc USB-C tiện dụng.",
            Specifications = "Màn hình 6.1 inch OLED Dynamic Island; Chip A16 Bionic; Camera 48MP; Cổng sạc USB-C.",
            IsActive = true
        };

        // Sản phẩm Tồn kho thấp (Low Stock Alert - Đúng 3 máy InStock)
        var p6 = new Product
        {
            CategoryId = cat15.Id,
            Name = "iPhone 15 Pro 128GB Titan Xanh",
            ModelSeries = "iPhone 15 Series",
            Sku = "IP15P-128-BLU",
            Color = "Titan Xanh",
            Storage = "128GB",
            ListedPrice = 27990000,
            Price = 25490000,
            Quantity = 0,
            WarrantyMonths = 12,
            ImageUrl = "/images/camera-iphone16.jpg",
            Description = "iPhone 15 Pro khung viền Titan chuẩn hàng không vũ trụ siêu bền nhẹ, chip A17 Pro đỉnh cao đồ họa.",
            Specifications = "Màn hình 6.1 inch ProMotion 120Hz; Chip A17 Pro 3nm; Cổng USB-C 3.0; Cụm 3 camera 48MP.",
            IsActive = true
        };

        // Sản phẩm Hết hàng (Out of Stock Alert - Đúng 0 máy InStock)
        var p7 = new Product
        {
            CategoryId = cat14.Id,
            Name = "iPhone 14 Pro Max 128GB Tím Deep Purple",
            ModelSeries = "iPhone 14 Series",
            Sku = "IP14PM-128-PUR",
            Color = "Tím Deep Purple",
            Storage = "128GB",
            ListedPrice = 24990000,
            Price = 22990000,
            Quantity = 0,
            WarrantyMonths = 12,
            ImageUrl = "/images/hero-iphone16.jpg",
            Description = "iPhone 14 Pro Max phiên bản Tím Deep Purple huyền thoại, màn hình 120Hz và camera 48MP đầu tiên của Apple.",
            Specifications = "Màn hình 6.7 inch Super Retina XDR 120Hz; Chip A16 Bionic; Camera 48MP; Khung viền thép cao cấp.",
            IsActive = true
        };

        db.Products.AddRange(p1, p2, p3, p4, p5, p6, p7);
        await db.SaveChangesAsync();

        // =========================================================================
        // 5. SEED KHO MÃ IMEI CHUẨN 15 CHỮ SỐ (PRODUCT_IMEIS)
        // Đầy đủ trạng thái: InStock, Reserved, Sold, Defective
        // =========================================================================
        var imeiList = new List<ProductImei>();

        // P1: iPhone 16 Pro Max 256GB Titan Sa Mạc
        // 5 máy InStock (để thầy quét barcode/chọn thoải mái)
        imeiList.Add(new ProductImei { ProductId = p1.Id, Imei = "358916010001001", SerialNumber = "S16PM001001", Status = "InStock", ImportDate = DateTime.Now.AddDays(-10) });
        imeiList.Add(new ProductImei { ProductId = p1.Id, Imei = "358916010001002", SerialNumber = "S16PM001002", Status = "InStock", ImportDate = DateTime.Now.AddDays(-10) });
        imeiList.Add(new ProductImei { ProductId = p1.Id, Imei = "358916010001003", SerialNumber = "S16PM001003", Status = "InStock", ImportDate = DateTime.Now.AddDays(-8) });
        imeiList.Add(new ProductImei { ProductId = p1.Id, Imei = "358916010001004", SerialNumber = "S16PM001004", Status = "InStock", ImportDate = DateTime.Now.AddDays(-5) });
        imeiList.Add(new ProductImei { ProductId = p1.Id, Imei = "358916010001005", SerialNumber = "S16PM001005", Status = "InStock", ImportDate = DateTime.Now.AddDays(-3) });
        // 1 máy Reserved cho đơn Pending
        imeiList.Add(new ProductImei { ProductId = p1.Id, Imei = "358916010001101", SerialNumber = "S16PM001101", Status = "Reserved", ImportDate = DateTime.Now.AddDays(-2) });
        // 2 máy Sold cho đơn Completed (đơn 2 máy của Long Lê)
        imeiList.Add(new ProductImei { ProductId = p1.Id, Imei = "358916010001201", SerialNumber = "S16PM001201", Status = "Sold", ImportDate = DateTime.Now.AddDays(-25), ExportDate = DateTime.Now.AddDays(-20) });
        imeiList.Add(new ProductImei { ProductId = p1.Id, Imei = "358916010001202", SerialNumber = "S16PM001202", Status = "Sold", ImportDate = DateTime.Now.AddDays(-25), ExportDate = DateTime.Now.AddDays(-20) });
        // 1 máy Defective (lỗi màn hình để demo đổi trả 1-1)
        imeiList.Add(new ProductImei { ProductId = p1.Id, Imei = "358916010001999", SerialNumber = "S16PM001999", Status = "Defective", ImportDate = DateTime.Now.AddDays(-30), Notes = "Lỗi sọc xanh màn hình OLED (Đã thu hồi đổi máy mới)" });

        // P2: iPhone 16 Pro 128GB Titan Tự Nhiên (4 máy InStock)
        imeiList.Add(new ProductImei { ProductId = p2.Id, Imei = "358916020002001", SerialNumber = "S16P002001", Status = "InStock", ImportDate = DateTime.Now.AddDays(-12) });
        imeiList.Add(new ProductImei { ProductId = p2.Id, Imei = "358916020002002", SerialNumber = "S16P002002", Status = "InStock", ImportDate = DateTime.Now.AddDays(-10) });
        imeiList.Add(new ProductImei { ProductId = p2.Id, Imei = "358916020002003", SerialNumber = "S16P002003", Status = "InStock", ImportDate = DateTime.Now.AddDays(-8) });
        imeiList.Add(new ProductImei { ProductId = p2.Id, Imei = "358916020002004", SerialNumber = "S16P002004", Status = "InStock", ImportDate = DateTime.Now.AddDays(-4) });

        // P3: iPhone 16 128GB Hồng (4 máy InStock, 1 máy Sold)
        imeiList.Add(new ProductImei { ProductId = p3.Id, Imei = "358916030003001", SerialNumber = "S16003001", Status = "InStock", ImportDate = DateTime.Now.AddDays(-15) });
        imeiList.Add(new ProductImei { ProductId = p3.Id, Imei = "358916030003002", SerialNumber = "S16003002", Status = "InStock", ImportDate = DateTime.Now.AddDays(-12) });
        imeiList.Add(new ProductImei { ProductId = p3.Id, Imei = "358916030003003", SerialNumber = "S16003003", Status = "InStock", ImportDate = DateTime.Now.AddDays(-10) });
        imeiList.Add(new ProductImei { ProductId = p3.Id, Imei = "358916030003004", SerialNumber = "S16003004", Status = "InStock", ImportDate = DateTime.Now.AddDays(-5) });
        imeiList.Add(new ProductImei { ProductId = p3.Id, Imei = "358916030003201", SerialNumber = "S16003201", Status = "Sold", ImportDate = DateTime.Now.AddDays(-20), ExportDate = DateTime.Now.AddDays(-10) });

        // P4: iPhone 16 128GB Xanh Lưu Ly (4 máy InStock, 1 máy Sold cho đơn Shipping)
        imeiList.Add(new ProductImei { ProductId = p4.Id, Imei = "358916040004001", SerialNumber = "S16004001", Status = "InStock", ImportDate = DateTime.Now.AddDays(-15) });
        imeiList.Add(new ProductImei { ProductId = p4.Id, Imei = "358916040004002", SerialNumber = "S16004002", Status = "InStock", ImportDate = DateTime.Now.AddDays(-12) });
        imeiList.Add(new ProductImei { ProductId = p4.Id, Imei = "358916040004003", SerialNumber = "S16004003", Status = "InStock", ImportDate = DateTime.Now.AddDays(-10) });
        imeiList.Add(new ProductImei { ProductId = p4.Id, Imei = "358916040004004", SerialNumber = "S16004004", Status = "InStock", ImportDate = DateTime.Now.AddDays(-5) });
        imeiList.Add(new ProductImei { ProductId = p4.Id, Imei = "358916040004201", SerialNumber = "S16004201", Status = "Sold", ImportDate = DateTime.Now.AddDays(-6), ExportDate = DateTime.Now.AddHours(-5) });

        // P5: iPhone 15 128GB Xanh Pastel (5 máy InStock, bao gồm 1 máy nhả lại từ đơn Cancelled)
        imeiList.Add(new ProductImei { ProductId = p5.Id, Imei = "358915050005001", SerialNumber = "S15005001", Status = "InStock", ImportDate = DateTime.Now.AddDays(-20) });
        imeiList.Add(new ProductImei { ProductId = p5.Id, Imei = "358915050005002", SerialNumber = "S15005002", Status = "InStock", ImportDate = DateTime.Now.AddDays(-18) });
        imeiList.Add(new ProductImei { ProductId = p5.Id, Imei = "358915050005003", SerialNumber = "S15005003", Status = "InStock", ImportDate = DateTime.Now.AddDays(-15) });
        imeiList.Add(new ProductImei { ProductId = p5.Id, Imei = "358915050005004", SerialNumber = "S15005004", Status = "InStock", ImportDate = DateTime.Now.AddDays(-10) });
        imeiList.Add(new ProductImei { ProductId = p5.Id, Imei = "358915050005005", SerialNumber = "S15005005", Status = "InStock", ImportDate = DateTime.Now.AddDays(-5), Notes = "Nhả giữ kho tự động do đơn hàng bị hủy" });

        // P6: iPhone 15 Pro 128GB Titan Xanh (Đúng 3 máy InStock -> Bật Low Stock Alert màu vàng)
        imeiList.Add(new ProductImei { ProductId = p6.Id, Imei = "358915060006001", SerialNumber = "S15P006001", Status = "InStock", ImportDate = DateTime.Now.AddDays(-15) });
        imeiList.Add(new ProductImei { ProductId = p6.Id, Imei = "358915060006002", SerialNumber = "S15P006002", Status = "InStock", ImportDate = DateTime.Now.AddDays(-12) });
        imeiList.Add(new ProductImei { ProductId = p6.Id, Imei = "358915060006003", SerialNumber = "S15P006003", Status = "InStock", ImportDate = DateTime.Now.AddDays(-8) });

        // P7: iPhone 14 Pro Max (0 máy InStock, 1 máy Sold đã hết hạn bảo hành, 1 máy Defective ngấm nước)
        imeiList.Add(new ProductImei { ProductId = p7.Id, Imei = "358914070007201", SerialNumber = "S14PM007201", Status = "Sold", ImportDate = DateTime.Now.AddYears(-2), ExportDate = DateTime.Now.AddYears(-2) });
        imeiList.Add(new ProductImei { ProductId = p7.Id, Imei = "358914070007999", SerialNumber = "S14PM007999", Status = "Defective", ImportDate = DateTime.Now.AddYears(-1), Notes = "Máy ngấm nước hư hỏng, từ chối bảo hành" });

        db.ProductImeis.AddRange(imeiList);
        await db.SaveChangesAsync();

        // Đồng bộ tồn kho Product.Quantity bằng số lượng IMEI InStock thực tế
        p1.Quantity = 5;
        p2.Quantity = 4;
        p3.Quantity = 4;
        p4.Quantity = 4;
        p5.Quantity = 5;
        p6.Quantity = 3; // Low stock alert
        p7.Quantity = 0; // Out of stock alert
        await db.SaveChangesAsync();

        // =========================================================================
        // 6. SEED ĐƠN HÀNG (ORDERS) BAO PHỦ ĐỦ 5 TRẠNG THÁI
        // 5 trạng thái: Pending, Confirmed, Shipping, Completed, Cancelled
        // =========================================================================
        var cNge = seededCustomerMap["nghecute"];
        var cThao = seededCustomerMap["thaopham"];
        var cHung = seededCustomerMap["hungtran"];
        var cMai = seededCustomerMap["mainguyen"];
        var cLong = seededCustomerMap["longle"];

        // Đơn 1: PENDING - Chờ duyệt & giữ kho (Đếm ngược 15 phút)
        var orderPending = new Order
        {
            OrderCode = "DH261024-001",
            CustomerId = cNge.Id,
            OrderDate = DateTime.Now.AddMinutes(-3), // Vừa đặt 3 phút trước -> Còn 12 phút giữ kho
            TotalAmount = p1.Price,
            OrderStatus = "Pending",
            PaymentStatus = "Chưa thanh toán",
            PaymentMethod = "VietQR",
            ShippingAddress = cNge.Address,
            Notes = "Khách chọn quét mã VietQR, đang chờ thanh toán xác nhận giữ kho."
        };
        db.Orders.Add(orderPending);
        await db.SaveChangesAsync();

        var odPending = new OrderDetail
        {
            OrderId = orderPending.Id,
            ProductId = p1.Id,
            Quantity = 1,
            UnitPrice = p1.Price,
            TotalPrice = p1.Price
        };
        db.OrderDetails.Add(odPending);
        await db.SaveChangesAsync();

        // Gán IMEI đang giữ kho (Reserved)
        var imeiPending = imeiList.First(i => i.Imei == "358916010001101");
        imeiPending.OrderId = orderPending.Id;

        // Đơn 2: CONFIRMED - Đã duyệt, Đang chờ thủ kho quét/gán mã IMEI
        var orderConfirmed = new Order
        {
            OrderCode = "DH261024-002",
            CustomerId = cThao.Id,
            OrderDate = DateTime.Now.AddHours(-1),
            TotalAmount = p2.Price,
            OrderStatus = "Confirmed",
            PaymentStatus = "Chưa thanh toán",
            PaymentMethod = "COD",
            ShippingAddress = cThao.Address,
            Notes = "Đã xác nhận điện thoại với khách, chờ xuất kho gán mã IMEI."
        };
        db.Orders.Add(orderConfirmed);
        await db.SaveChangesAsync();

        var odConfirmed = new OrderDetail
        {
            OrderId = orderConfirmed.Id,
            ProductId = p2.Id,
            Quantity = 1,
            UnitPrice = p2.Price,
            TotalPrice = p2.Price
        };
        db.OrderDetails.Add(odConfirmed);
        await db.SaveChangesAsync();

        // Đơn 3: SHIPPING - Đang giao hàng (Đã gán IMEI, có mã vận đơn GHN)
        var imeiShipping = imeiList.First(i => i.Imei == "358916040004201");
        var orderShipping = new Order
        {
            OrderCode = "DH261024-003",
            CustomerId = cHung.Id,
            OrderDate = DateTime.Now.AddHours(-5),
            TotalAmount = p4.Price,
            OrderStatus = "Shipping",
            PaymentStatus = "Chưa thanh toán",
            PaymentMethod = "COD",
            ShippingUnit = "Giao Hàng Nhanh (GHN)",
            TrackingNumber = "GHN-88901234VN",
            ShippingAddress = cHung.Address,
            Notes = "Hàng đã đóng gói tem niêm phong Apple AAR, bàn giao tài xế GHN."
        };
        db.Orders.Add(orderShipping);
        await db.SaveChangesAsync();

        imeiShipping.OrderId = orderShipping.Id;

        var odShipping = new OrderDetail
        {
            OrderId = orderShipping.Id,
            ProductId = p4.Id,
            Quantity = 1,
            UnitPrice = p4.Price,
            TotalPrice = p4.Price,
            ProductImeiId = imeiShipping.Id,
            ImeiNumber = imeiShipping.Imei
        };
        db.OrderDetails.Add(odShipping);
        await db.SaveChangesAsync();

        // Đơn 4: COMPLETED - Hoàn tất giao hàng (Đơn 1 máy - ĐÃ KHÓA FORM CHỈNH SỬA)
        var imeiCompleted1 = imeiList.First(i => i.Imei == "358916030003201");
        var orderCompleted1 = new Order
        {
            OrderCode = "DH261024-004",
            CustomerId = cMai.Id,
            OrderDate = DateTime.Now.AddDays(-10),
            TotalAmount = p3.Price,
            OrderStatus = "Completed",
            PaymentStatus = "Đã thanh toán",
            PaymentMethod = "BankTransfer",
            ShippingUnit = "Viettel Post",
            TrackingNumber = "VTP-77889900VN",
            ShippingAddress = cMai.Address,
            Notes = "Khách đã nhận máy nguyên seal và kích hoạt bảo hành điện tử thành công."
        };
        db.Orders.Add(orderCompleted1);
        await db.SaveChangesAsync();

        imeiCompleted1.OrderId = orderCompleted1.Id;

        var odCompleted1 = new OrderDetail
        {
            OrderId = orderCompleted1.Id,
            ProductId = p3.Id,
            Quantity = 1,
            UnitPrice = p3.Price,
            TotalPrice = p3.Price,
            ProductImeiId = imeiCompleted1.Id,
            ImeiNumber = imeiCompleted1.Imei
        };
        db.OrderDetails.Add(odCompleted1);
        await db.SaveChangesAsync();

        // Đơn 5: COMPLETED - Hoàn tất giao hàng (ĐƠN 2 MÁY - DEMO MULTI-IMEI SLOTS)
        var imeiCompleted2A = imeiList.First(i => i.Imei == "358916010001201");
        var imeiCompleted2B = imeiList.First(i => i.Imei == "358916010001202");
        var orderCompleted2 = new Order
        {
            OrderCode = "DH261024-005",
            CustomerId = cLong.Id,
            OrderDate = DateTime.Now.AddDays(-20),
            TotalAmount = p1.Price * 2,
            OrderStatus = "Completed",
            PaymentStatus = "Đã thanh toán",
            PaymentMethod = "VietQR",
            ShippingUnit = "BomChelsea Apple Direct Delivery",
            TrackingNumber = "BOM-EXPRESS-005",
            ShippingAddress = cLong.Address,
            Notes = "Giao hỏa tốc 2H tại TP.HCM. Khách kiểm tra cả 2 máy hoàn hảo."
        };
        db.Orders.Add(orderCompleted2);
        await db.SaveChangesAsync();

        imeiCompleted2A.OrderId = orderCompleted2.Id;
        imeiCompleted2B.OrderId = orderCompleted2.Id;

        var odCompleted2A = new OrderDetail
        {
            OrderId = orderCompleted2.Id,
            ProductId = p1.Id,
            Quantity = 1,
            UnitPrice = p1.Price,
            TotalPrice = p1.Price,
            ProductImeiId = imeiCompleted2A.Id,
            ImeiNumber = imeiCompleted2A.Imei
        };
        var odCompleted2B = new OrderDetail
        {
            OrderId = orderCompleted2.Id,
            ProductId = p1.Id,
            Quantity = 1,
            UnitPrice = p1.Price,
            TotalPrice = p1.Price,
            ProductImeiId = imeiCompleted2B.Id,
            ImeiNumber = imeiCompleted2B.Imei
        };
        db.OrderDetails.AddRange(odCompleted2A, odCompleted2B);
        await db.SaveChangesAsync();

        // Đơn 6: CANCELLED - Đã hủy đơn hàng (Kho đã nhả IMEI về InStock, giao diện bị khóa)
        var orderCancelled = new Order
        {
            OrderCode = "DH261024-006",
            CustomerId = cNge.Id,
            OrderDate = DateTime.Now.AddDays(-2),
            TotalAmount = p5.Price,
            OrderStatus = "Cancelled",
            PaymentStatus = "Chưa thanh toán",
            PaymentMethod = "COD",
            ShippingAddress = cNge.Address,
            Notes = "Khách đổi ý muốn chuyển sang bản màu Titan Sa Mạc nên hủy đơn này."
        };
        db.Orders.Add(orderCancelled);
        await db.SaveChangesAsync();

        var odCancelled = new OrderDetail
        {
            OrderId = orderCancelled.Id,
            ProductId = p5.Id,
            Quantity = 1,
            UnitPrice = p5.Price,
            TotalPrice = p5.Price
        };
        db.OrderDetails.Add(odCancelled);
        await db.SaveChangesAsync();

        // Cập nhật TotalSpent & RewardPoints tích lũy cho 5 khách hàng chuẩn
        cMai.TotalSpent = p3.Price;
        cLong.TotalSpent = p1.Price * 2;
        await db.SaveChangesAsync();

        // =========================================================================
        // 7. SEED HỒ SƠ BẢO HÀNH & ĐỔI TRẢ 1-1 (WARRANTIES & CLAIMS)
        // =========================================================================
        // Hồ sơ 1: Đang trong 30 ngày đầu (Đủ điều kiện 1 Đổi 1)
        var w1 = new Warranty
        {
            CustomerId = cMai.Id,
            ProductId = p3.Id,
            OrderDetailId = odCompleted1.Id,
            Imei = imeiCompleted1.Imei,
            SerialNumber = imeiCompleted1.SerialNumber,
            StartDate = DateTime.Now.AddDays(-10),
            EndDate = DateTime.Now.AddDays(-10).AddYears(1),
            WarrantyType = "Chính hãng Apple 12 tháng",
            Status = "Active",
            Notes = "Kích hoạt bảo hành điện tử chính hãng Apple AAR. Còn trong thời hạn 30 ngày 1 đổi 1.",
            CreatedAt = DateTime.Now.AddDays(-10)
        };
        db.Warranties.Add(w1);

        // Hồ sơ 2: Đã kích hoạt trên 60 ngày (Trạng thái Active màu xanh lá)
        var w2 = new Warranty
        {
            CustomerId = cLong.Id,
            ProductId = p1.Id,
            OrderDetailId = odCompleted2B.Id,
            Imei = imeiCompleted2B.Imei,
            SerialNumber = imeiCompleted2B.SerialNumber,
            StartDate = DateTime.Now.AddDays(-65),
            EndDate = DateTime.Now.AddDays(-65).AddYears(1),
            WarrantyType = "AppleCare+ 1 Đổi 1 VIP",
            Status = "Active",
            Notes = "Gói AppleCare+ 1 đổi 1 VIP được bảo vệ toàn diện nguồn và màn hình.",
            CreatedAt = DateTime.Now.AddDays(-65)
        };
        db.Warranties.Add(w2);

        // Hồ sơ 3: Đã thực hiện Đổi máy mới 1-1 thành công (Demo kịch bản đổi trả)
        var w3 = new Warranty
        {
            CustomerId = cLong.Id,
            ProductId = p1.Id,
            OrderDetailId = odCompleted2A.Id,
            Imei = imeiCompleted2A.Imei,
            SerialNumber = imeiCompleted2A.SerialNumber,
            OriginalImei = "358916010001999", // IMEI cũ lỗi đã thu hồi
            StartDate = DateTime.Now.AddDays(-20),
            EndDate = DateTime.Now.AddDays(-20).AddYears(1),
            WarrantyType = "Chính hãng Apple 12 tháng",
            Status = "Active",
            Notes = "Đã thực hiện chính sách 1 đổi 1 sang máy mới mã 358916010001201.",
            CreatedAt = DateTime.Now.AddDays(-20)
        };
        db.Warranties.Add(w3);
        await db.SaveChangesAsync();

        // Kèm phiếu yêu cầu bảo hành đã xử lý đổi máy
        var claim = new WarrantyClaim
        {
            ClaimCode = "YC-261024-001",
            WarrantyId = w3.Id,
            ClaimDate = DateTime.Now.AddDays(-5),
            IssueDescription = "Màn hình OLED xuất hiện 1 đường sọc xanh dọc, cảm ứng chập chờn khi sử dụng.",
            TechnicianNotes = "Kỹ thuật viên Apple Care kiểm tra: Lỗi phần cứng do nhà sản xuất, máy không trầy xước va đập. Đạt tiêu chuẩn 1 đổi 1 theo chính sách AAR.",
            ActionTaken = "Đổi máy mới (Exchange)",
            OldImei = "358916010001999",
            NewImei = imeiCompleted2A.Imei,
            Status = "Hoàn tất",
            ResolvedDate = DateTime.Now.AddDays(-4),
            CreatedBy = "admin"
        };
        db.WarrantyClaims.Add(claim);

        // Hồ sơ 4: Hết hạn hoặc Từ chối bảo hành (Voided - Màu đỏ)
        var w4 = new Warranty
        {
            CustomerId = cHung.Id,
            ProductId = p7.Id,
            Imei = "358914070007201",
            SerialNumber = "S14PM007201",
            StartDate = DateTime.Now.AddYears(-2),
            EndDate = DateTime.Now.AddYears(-1),
            WarrantyType = "Chính hãng Apple 12 tháng",
            Status = "Voided",
            Notes = "Máy bị vào nước ngấm chất lỏng, giấy quỳ tím bên trong đổi màu đỏ. Từ chối bảo hành theo quy định Apple.",
            CreatedAt = DateTime.Now.AddYears(-2)
        };
        db.Warranties.Add(w4);
        await db.SaveChangesAsync();

        // =========================================================================
        // 8. SEED NHẬT KÝ KIỂM TOÁN (AUDIT_LOGS)
        // =========================================================================
        var auditLogs = new List<AuditLog>
        {
            new AuditLog
            {
                UserId = adminUser.Id,
                UserName = "admin",
                Action = "SYSTEM_INIT",
                EntityName = "System",
                RecordId = "0",
                NewValues = "Khởi tạo hệ thống dữ liệu mẫu AAR BomChelsea Enterprise.",
                Timestamp = DateTime.Now.AddDays(-30),
                IpAddress = "127.0.0.1"
            },
            new AuditLog
            {
                UserId = adminUser.Id,
                UserName = "admin",
                Action = "IMPORT_IMEI",
                EntityName = "ProductImeis",
                RecordId = p1.Id.ToString(),
                NewValues = "Nhập kho lô 10 máy iPhone 16 Pro Max từ nhà phân phối Synnex FPT.",
                Timestamp = DateTime.Now.AddDays(-10),
                IpAddress = "127.0.0.1"
            },
            new AuditLog
            {
                UserId = adminUser.Id,
                UserName = "admin",
                Action = "FULFILL_ORDER",
                EntityName = "Orders",
                RecordId = orderCompleted1.Id.ToString(),
                NewValues = $"Hoàn tất đơn hàng #{orderCompleted1.OrderCode}, xuất kho IMEI {imeiCompleted1.Imei} và kích hoạt bảo hành điện tử.",
                Timestamp = DateTime.Now.AddDays(-10),
                IpAddress = "127.0.0.1"
            },
            new AuditLog
            {
                UserId = adminUser.Id,
                UserName = "admin",
                Action = "WARRANTY_EXCHANGE",
                EntityName = "WarrantyClaims",
                RecordId = claim.Id.ToString(),
                OldValues = $"OldImei={claim.OldImei}",
                NewValues = $"NewImei={claim.NewImei}, Status=Hoàn tất",
                Timestamp = DateTime.Now.AddDays(-4),
                IpAddress = "127.0.0.1"
            }
        };

        db.AuditLogs.AddRange(auditLogs);
        await db.SaveChangesAsync();
    }

    private static async Task EnsureIdentityUsersAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roleManager.RoleExistsAsync("Admin"))
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        if (!await roleManager.RoleExistsAsync("Customer"))
            await roleManager.CreateAsync(new IdentityRole("Customer"));

        var admin = await userManager.FindByNameAsync("admin");
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = "admin",
                Email = "admin@istore.vn",
                PhoneNumber = "0909000001",
                EmailConfirmed = true,
                IsActive = true
            };
            await userManager.CreateAsync(admin, "admin123");
            await userManager.AddToRoleAsync(admin, "Admin");
        }
    }
}
