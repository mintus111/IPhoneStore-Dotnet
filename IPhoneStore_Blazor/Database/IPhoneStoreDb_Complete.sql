-- ============================================================================
-- CƠ SỞ DỮ LIỆU TOÀN DIỆN: IPHONESTORE ENTERPRISE (AAR BOMCHELSEA STANDARD)
-- Phiên bản: Complete V3.1 (Xuất trực tiếp từ Database IPhoneStoreDb hiện hành)
-- Ngày tạo: 2026-11-01 17:30:00
-- Tương thích: Microsoft SQL Server 2019 / 2022 / LocalDB / Azure SQL
-- Hướng dẫn: Mở file trong SSMS (SQL Server Management Studio) và nhấn F5 để thực thi.
-- ============================================================================

USE [master];
GO
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'IPhoneStoreDb')
BEGIN
    CREATE DATABASE [IPhoneStoreDb];
    PRINT N'[OK] Đã tạo cơ sở dữ liệu IPhoneStoreDb.';
END
GO

USE [IPhoneStoreDb];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- ============================================================================
-- PHẦN 1: CẤU TRÚC BẢNG (SCHEMA DDL)
-- ============================================================================

-- 1. Bảng AspNetRoles
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AspNetRoles')
BEGIN
    CREATE TABLE [dbo].[AspNetRoles] (
        [Id] NVARCHAR(450) NOT NULL,
        [Name] NVARCHAR(256) NULL,
        [NormalizedName] NVARCHAR(256) NULL,
        [ConcurrencyStamp] NVARCHAR(MAX) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE UNIQUE NONCLUSTERED INDEX [RoleNameIndex] ON [dbo].[AspNetRoles] ([NormalizedName] ASC) WHERE ([NormalizedName] IS NOT NULL);
END
GO

-- 2. Bảng AspNetUsers
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AspNetUsers')
BEGIN
    CREATE TABLE [dbo].[AspNetUsers] (
        [Id] NVARCHAR(450) NOT NULL,
        [UserName] NVARCHAR(256) NULL,
        [NormalizedUserName] NVARCHAR(256) NULL,
        [Email] NVARCHAR(256) NULL,
        [NormalizedEmail] NVARCHAR(256) NULL,
        [EmailConfirmed] BIT NOT NULL,
        [PasswordHash] NVARCHAR(MAX) NULL,
        [SecurityStamp] NVARCHAR(MAX) NULL,
        [ConcurrencyStamp] NVARCHAR(MAX) NULL,
        [PhoneNumber] NVARCHAR(MAX) NULL,
        [PhoneNumberConfirmed] BIT NOT NULL,
        [TwoFactorEnabled] BIT NOT NULL,
        [LockoutEnd] DATETIMEOFFSET NULL,
        [LockoutEnabled] BIT NOT NULL,
        [AccessFailedCount] INT NOT NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_AspNetUsers_IsActive] DEFAULT 1,
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_AspNetUsers_CreatedAt] DEFAULT GETDATE(),
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE NONCLUSTERED INDEX [EmailIndex] ON [dbo].[AspNetUsers] ([NormalizedEmail] ASC);
    CREATE UNIQUE NONCLUSTERED INDEX [UserNameIndex] ON [dbo].[AspNetUsers] ([NormalizedUserName] ASC) WHERE ([NormalizedUserName] IS NOT NULL);
END
GO

-- 3. Bảng AspNetRoleClaims
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AspNetRoleClaims')
BEGIN
    CREATE TABLE [dbo].[AspNetRoleClaims] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [RoleId] NVARCHAR(450) NOT NULL,
        [ClaimType] NVARCHAR(MAX) NULL,
        [ClaimValue] NVARCHAR(MAX) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[AspNetRoles] ([Id]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_AspNetRoleClaims_RoleId] ON [dbo].[AspNetRoleClaims] ([RoleId] ASC);
END
GO

-- 4. Bảng AspNetUserClaims
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AspNetUserClaims')
BEGIN
    CREATE TABLE [dbo].[AspNetUserClaims] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] NVARCHAR(450) NOT NULL,
        [ClaimType] NVARCHAR(MAX) NULL,
        [ClaimValue] NVARCHAR(MAX) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_AspNetUserClaims_UserId] ON [dbo].[AspNetUserClaims] ([UserId] ASC);
END
GO

-- 5. Bảng AspNetUserLogins
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AspNetUserLogins')
BEGIN
    CREATE TABLE [dbo].[AspNetUserLogins] (
        [LoginProvider] NVARCHAR(450) NOT NULL,
        [ProviderKey] NVARCHAR(450) NOT NULL,
        [ProviderDisplayName] NVARCHAR(MAX) NULL,
        [UserId] NVARCHAR(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY CLUSTERED ([LoginProvider] ASC, [ProviderKey] ASC),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_AspNetUserLogins_UserId] ON [dbo].[AspNetUserLogins] ([UserId] ASC);
END
GO

-- 6. Bảng AspNetUserRoles
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AspNetUserRoles')
BEGIN
    CREATE TABLE [dbo].[AspNetUserRoles] (
        [UserId] NVARCHAR(450) NOT NULL,
        [RoleId] NVARCHAR(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY CLUSTERED ([UserId] ASC, [RoleId] ASC),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_AspNetUserRoles_RoleId] ON [dbo].[AspNetUserRoles] ([RoleId] ASC);
END
GO

-- 7. Bảng AspNetUserTokens
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AspNetUserTokens')
BEGIN
    CREATE TABLE [dbo].[AspNetUserTokens] (
        [UserId] NVARCHAR(450) NOT NULL,
        [LoginProvider] NVARCHAR(450) NOT NULL,
        [Name] NVARCHAR(450) NOT NULL,
        [Value] NVARCHAR(MAX) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY CLUSTERED ([UserId] ASC, [LoginProvider] ASC, [Name] ASC),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END
GO

-- 8. Bảng Categories (Dòng máy)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'Categories')
BEGIN
    CREATE TABLE [dbo].[Categories] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Name] NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(255) NULL,
        [DisplayOrder] INT NOT NULL CONSTRAINT [DF_Categories_DisplayOrder] DEFAULT 0,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_Categories_IsActive] DEFAULT 1,
        CONSTRAINT [PK_Categories] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

-- 9. Bảng Products (Sản phẩm & Biến thể SKU)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'Products')
BEGIN
    CREATE TABLE [dbo].[Products] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [CategoryId] INT NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [ModelSeries] NVARCHAR(100) NOT NULL,
        [Color] NVARCHAR(50) NOT NULL,
        [Storage] NVARCHAR(20) NOT NULL,
        [Specifications] NVARCHAR(MAX) NULL,
        [Price] DECIMAL(18,2) NOT NULL,
        [Quantity] INT NOT NULL CONSTRAINT [DF_Products_Quantity] DEFAULT 0,
        [WarrantyMonths] INT NOT NULL CONSTRAINT [DF_Products_WarrantyMonths] DEFAULT 12,
        [ImageUrl] NVARCHAR(500) NULL,
        [Description] NVARCHAR(MAX) NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_Products_IsActive] DEFAULT 1,
        [RowVersion] ROWVERSION NOT NULL,
        [Sku] NVARCHAR(50) NULL,
        [ListedPrice] DECIMAL(18,2) NULL,
        CONSTRAINT [PK_Products] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Products_Categories] FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[Categories] ([Id]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_Products_CategoryId] ON [dbo].[Products] ([CategoryId] ASC);
    CREATE NONCLUSTERED INDEX [IX_Products_Sku] ON [dbo].[Products] ([Sku] ASC);
END
GO

-- 10. Bảng Customers (Khách hàng & Thành viên)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'Customers')
BEGIN
    CREATE TABLE [dbo].[Customers] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] NVARCHAR(450) NULL,
        [FullName] NVARCHAR(100) NOT NULL,
        [Email] NVARCHAR(100) NULL,
        [PhoneNumber] NVARCHAR(20) NOT NULL,
        [Address] NVARCHAR(255) NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_Customers_CreatedAt] DEFAULT GETDATE(),
        [TotalSpent] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_Customers_TotalSpent] DEFAULT 0,
        [RewardPoints] INT NOT NULL CONSTRAINT [DF_Customers_RewardPoints] DEFAULT 0,
        [MembershipTier] NVARCHAR(50) NOT NULL CONSTRAINT [DF_Customers_MembershipTier] DEFAULT N'Member',
        CONSTRAINT [PK_Customers] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Customers_AspNetUsers] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE SET NULL
    );
    CREATE NONCLUSTERED INDEX [IX_Customers_UserId] ON [dbo].[Customers] ([UserId] ASC);
END
GO

-- 11. Bảng Orders (Đơn hàng)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'Orders')
BEGIN
    CREATE TABLE [dbo].[Orders] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [CustomerId] INT NOT NULL,
        [OrderDate] DATETIME2 NOT NULL CONSTRAINT [DF_Orders_OrderDate] DEFAULT GETDATE(),
        [TotalAmount] DECIMAL(18,2) NOT NULL,
        [OrderStatus] NVARCHAR(50) NOT NULL CONSTRAINT [DF_Orders_OrderStatus] DEFAULT 'Pending',
        [PaymentMethod] NVARCHAR(50) NOT NULL,
        [ShippingAddress] NVARCHAR(255) NOT NULL,
        [Notes] NVARCHAR(500) NULL,
        [OrderCode] NVARCHAR(50) NULL,
        [PaymentStatus] NVARCHAR(50) NOT NULL CONSTRAINT [DF_Orders_PaymentStatus] DEFAULT N'Chưa thanh toán',
        [ShippingUnit] NVARCHAR(100) NULL,
        [TrackingNumber] NVARCHAR(100) NULL,
        [EstimatedDeliveryDate] DATETIME2 NULL,
        CONSTRAINT [PK_Orders] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Orders_Customers] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_Orders_CustomerId] ON [dbo].[Orders] ([CustomerId] ASC);
    CREATE NONCLUSTERED INDEX [IX_Orders_OrderCode] ON [dbo].[Orders] ([OrderCode] ASC);
END
GO

-- 12. Bảng ProductImeis (Kho cá thể IMEI / Serial Number)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'ProductImeis')
BEGIN
    CREATE TABLE [dbo].[ProductImeis] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [ProductId] INT NOT NULL,
        [Imei] NVARCHAR(20) NOT NULL,
        [SerialNumber] NVARCHAR(50) NULL,
        [Status] NVARCHAR(30) NOT NULL CONSTRAINT [DF_ProductImeis_Status] DEFAULT 'InStock',
        [OrderId] INT NULL,
        [ImportDate] DATETIME2 NOT NULL CONSTRAINT [DF_ProductImeis_ImportDate] DEFAULT GETDATE(),
        [ExportDate] DATETIME2 NULL,
        [Notes] NVARCHAR(255) NULL,
        [RowVersion] ROWVERSION NOT NULL,
        CONSTRAINT [PK_ProductImeis] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_ProductImeis_Products] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProductImeis_Orders] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders] ([Id])
    );
    CREATE UNIQUE NONCLUSTERED INDEX [UQ_ProductImeis_Imei] ON [dbo].[ProductImeis] ([Imei] ASC);
    CREATE NONCLUSTERED INDEX [IX_ProductImeis_ProductId] ON [dbo].[ProductImeis] ([ProductId] ASC);
    CREATE NONCLUSTERED INDEX [IX_ProductImeis_Status] ON [dbo].[ProductImeis] ([Status] ASC);
    CREATE NONCLUSTERED INDEX [IX_ProductImeis_OrderId] ON [dbo].[ProductImeis] ([OrderId] ASC);
END
GO

-- 13. Bảng OrderDetails (Chi tiết đơn hàng & Gán IMEI)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'OrderDetails')
BEGIN
    CREATE TABLE [dbo].[OrderDetails] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [OrderId] INT NOT NULL,
        [ProductId] INT NOT NULL,
        [Quantity] INT NOT NULL,
        [UnitPrice] DECIMAL(18,2) NOT NULL,
        [TotalPrice] DECIMAL(18,2) NOT NULL,
        [ProductImeiId] INT NULL,
        [ImeiNumber] NVARCHAR(20) NULL,
        CONSTRAINT [PK_OrderDetails] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_OrderDetails_Orders] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_OrderDetails_Products] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products] ([Id]),
        CONSTRAINT [FK_OrderDetails_ProductImeis] FOREIGN KEY ([ProductImeiId]) REFERENCES [dbo].[ProductImeis] ([Id]) ON DELETE SET NULL
    );
    CREATE NONCLUSTERED INDEX [IX_OrderDetails_OrderId] ON [dbo].[OrderDetails] ([OrderId] ASC);
    CREATE NONCLUSTERED INDEX [IX_OrderDetails_ProductId] ON [dbo].[OrderDetails] ([ProductId] ASC);
    CREATE NONCLUSTERED INDEX [IX_OrderDetails_ProductImeiId] ON [dbo].[OrderDetails] ([ProductImeiId] ASC);
END
GO

-- 14. Bảng Warranties (Sổ Bảo hành Điện tử Apple)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'Warranties')
BEGIN
    CREATE TABLE [dbo].[Warranties] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [CustomerId] INT NOT NULL,
        [ProductId] INT NOT NULL,
        [OrderDetailId] INT NULL,
        [Imei] NVARCHAR(20) NOT NULL,
        [SerialNumber] NVARCHAR(50) NULL,
        [OriginalImei] NVARCHAR(20) NULL,
        [StartDate] DATETIME2 NOT NULL,
        [EndDate] DATETIME2 NOT NULL,
        [WarrantyType] NVARCHAR(50) NOT NULL CONSTRAINT [DF_Warranties_WarrantyType] DEFAULT N'Chính hãng Apple 12 tháng',
        [Status] NVARCHAR(30) NOT NULL CONSTRAINT [DF_Warranties_Status] DEFAULT 'Active',
        [Notes] NVARCHAR(500) NULL,
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_Warranties_CreatedAt] DEFAULT GETDATE(),
        [RowVersion] ROWVERSION NOT NULL,
        CONSTRAINT [PK_Warranties] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Warranties_Customers] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id]),
        CONSTRAINT [FK_Warranties_Products] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products] ([Id]),
        CONSTRAINT [FK_Warranties_OrderDetails] FOREIGN KEY ([OrderDetailId]) REFERENCES [dbo].[OrderDetails] ([Id]) ON DELETE SET NULL
    );
    CREATE NONCLUSTERED INDEX [IX_Warranties_Imei] ON [dbo].[Warranties] ([Imei] ASC);
    CREATE NONCLUSTERED INDEX [IX_Warranties_CustomerId] ON [dbo].[Warranties] ([CustomerId] ASC);
    CREATE NONCLUSTERED INDEX [IX_Warranties_Status] ON [dbo].[Warranties] ([Status] ASC);
END
GO

-- 15. Bảng WarrantyClaims (Phiếu Tiếp nhận & 1 Đổi 1)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'WarrantyClaims')
BEGIN
    CREATE TABLE [dbo].[WarrantyClaims] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [WarrantyId] INT NOT NULL,
        [ClaimDate] DATETIME2 NOT NULL CONSTRAINT [DF_WarrantyClaims_ClaimDate] DEFAULT GETDATE(),
        [IssueDescription] NVARCHAR(MAX) NOT NULL,
        [TechnicianNotes] NVARCHAR(MAX) NULL,
        [ActionTaken] NVARCHAR(100) NULL,
        [OldImei] NVARCHAR(20) NULL,
        [NewImei] NVARCHAR(20) NULL,
        [Status] NVARCHAR(50) NOT NULL CONSTRAINT [DF_WarrantyClaims_Status] DEFAULT N'Tiếp nhận',
        [ResolvedDate] DATETIME2 NULL,
        [CreatedBy] NVARCHAR(100) NULL,
        [ClaimCode] NVARCHAR(50) NULL,
        CONSTRAINT [PK_WarrantyClaims] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_WarrantyClaims_Warranties] FOREIGN KEY ([WarrantyId]) REFERENCES [dbo].[Warranties] ([Id]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_WarrantyClaims_WarrantyId] ON [dbo].[WarrantyClaims] ([WarrantyId] ASC);
    CREATE NONCLUSTERED INDEX [IX_WarrantyClaims_Status] ON [dbo].[WarrantyClaims] ([Status] ASC);
    CREATE NONCLUSTERED INDEX [IX_WarrantyClaims_ClaimCode] ON [dbo].[WarrantyClaims] ([ClaimCode] ASC);
END
GO

-- 16. Bảng AuditLogs (Nhật ký Kiểm toán Hệ thống)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AuditLogs')
BEGIN
    CREATE TABLE [dbo].[AuditLogs] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] NVARCHAR(450) NULL,
        [UserName] NVARCHAR(256) NULL,
        [Action] NVARCHAR(50) NOT NULL,
        [EntityName] NVARCHAR(100) NOT NULL,
        [RecordId] NVARCHAR(50) NOT NULL,
        [OldValues] NVARCHAR(MAX) NULL,
        [NewValues] NVARCHAR(MAX) NULL,
        [IpAddress] NVARCHAR(50) NULL,
        [Timestamp] DATETIME2 NOT NULL CONSTRAINT [DF_AuditLogs_Timestamp] DEFAULT GETDATE(),
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_AuditLogs_AspNetUsers] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE SET NULL
    );
    CREATE NONCLUSTERED INDEX [IX_AuditLogs_UserId] ON [dbo].[AuditLogs] ([UserId] ASC);
    CREATE NONCLUSTERED INDEX [IX_AuditLogs_EntityName_RecordId] ON [dbo].[AuditLogs] ([EntityName] ASC, [RecordId] ASC);
    CREATE NONCLUSTERED INDEX [IX_AuditLogs_Timestamp] ON [dbo].[AuditLogs] ([Timestamp] ASC);
END
GO

-- ============================================================================
-- PHẦN 2: DỮ LIỆU MẪU CHUẨN AAR (MASTER SEED DATA)
-- ============================================================================

-- Dữ liệu bảng AspNetRoles (6 bản ghi)
IF NOT EXISTS (SELECT 1 FROM [AspNetRoles])
BEGIN
    INSERT INTO [AspNetRoles] (Id, Name, NormalizedName, ConcurrencyStamp) VALUES
    (N'07b86f6e-265c-41b9-8dbd-2f8403a9649e', N'Admin', N'ADMIN', N'bb804307-2fb4-426e-8c68-eff35d690701'),
    (N'a55aaff9-42fd-45c4-8a4f-bc8914ae265b', N'Customer', N'CUSTOMER', N'e37cae32-77e9-4900-8c2b-bd821e70b4ab'),
    (N'D5E1C767-7403-4345-A19A-922D4121624C', N'Staff', N'STAFF', N'A3DCD7B0-6809-490D-97DE-D391F9F84527'),
    (N'role-cashier-id', N'Cashier', N'CASHIER', N'B7543BE5-5B04-4E70-89BD-4787CE5C1670'),
    (N'role-manager-id', N'Manager', N'MANAGER', N'F7E418C1-91D0-486A-A15F-1BEBAA09189B'),
    (N'role-shipper-id', N'Shipper', N'SHIPPER', N'E557DD27-F594-4A13-98FC-CAB4715482D2');
    PRINT N'[OK] Đã chèn dữ liệu mẫu cho AspNetRoles (6 dòng).';
END
GO

-- Dữ liệu bảng AspNetUsers (6 bản ghi)
IF NOT EXISTS (SELECT 1 FROM [AspNetUsers])
BEGIN
    INSERT INTO [AspNetUsers] (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount, IsActive, CreatedAt) VALUES
    (N'3a3c3104-5561-4523-a693-800cd0946ef7', N'admin', N'ADMIN', N'admin@istore.vn', N'ADMIN@ISTORE.VN', 1, N'AQAAAAIAAYagAAAAEFaU3Ag6YGld/iMaXuO//zG2Ch/x45TX2E8Yp150bBIB9lm8N9jPbF0fvqMSqz5RiA==', N'BXN26FTFM5UHRRRZWPDPQZBC6HVA23DQ', N'449d9ea6-cab5-432d-b8af-6d2911787642', N'0909000001', 0, 0, NULL, 1, 0, 1, '2026-10-19 15:38:22.469'),
    (N'527cef66-3ba2-4a82-a2a2-5770a24bdd2c', N'nghecute', N'NGHECUTE', N'nghecute@gmail.com', N'NGHECUTE@GMAIL.COM', 0, N'AQAAAAIAAYagAAAAEMLqR96ABsNznlmEawvol+O9MtW2+b6CUYNBmdlT52ir+4jZHkcdEufRXhV+vkf1Ig==', N'TBJR7AEX56VEV5OLDBQQUGSJW3CXPGV2', N'0dd23d04-139f-4249-a947-fde0553a82be', N'0982726260', 0, 0, NULL, 1, 0, 1, '2026-10-30 23:10:26.925'),
    (N'6a300f77-0a2f-462a-ac88-ba0ba7c84fcd', N'mainguyen', N'MAINGUYEN', N'mainguyen@gmail.com', N'MAINGUYEN@GMAIL.COM', 1, N'AQAAAAIAAYagAAAAEFFST/NKTySNd/viTSa6YA1wP3Mbcy9ajmOQtLZHPJZoIdpuQdoidwuHPmbgNn+mTg==', N'A5VJJ6GBXWUE5N3MN2CHCM5WNHCB4JGI', N'bcf1c2ac-3f6a-4c5b-8209-9df7a6c9d641', N'0912345678', 0, 0, NULL, 1, 0, 1, '2026-10-30 23:47:04.331'),
    (N'e8abd37a-cbbd-4e01-b285-05508151a96a', N'hungtran', N'HUNGTRAN', N'hungtran@gmail.com', N'HUNGTRAN@GMAIL.COM', 1, N'AQAAAAIAAYagAAAAEIzX4KQmz/K0xhgLqAQOG3eJdKwspu58yGypY1HrX+M2uyb2ti9ekhp3X5D2Q4TIOw==', N'YFSQHZPMXZXAOGEZIYPMG7AJBHEXM6DS', N'7a625398-9dd2-43ce-89df-e547f6f1eb01', N'0988776655', 0, 0, '9999-12-31 23:59:59.9999999 +00:00', 1, 0, 1, '2026-10-30 23:47:04.513'),
    (N'f317b5dc-ba47-40fd-8282-93926ec3915d', N'longle', N'LONGLE', N'longle@gmail.com', N'LONGLE@GMAIL.COM', 1, N'AQAAAAIAAYagAAAAEKwuOcR8lxuXG7zEEdxEoSEGdvjAi40a8oPqqJmhEKHv6wLX6/AQoiglMHUbiaP34g==', N'FF22RFQCGU4NLDSNJWMOWKRC7ZXVGCMS', N'a3d095e7-34cd-4ce5-8011-ed441e5f266d', N'0903284116', 0, 0, '2026-11-01 10:23:41.9406240 +00:00', 1, 0, 1, '2026-10-30 23:47:04.590'),
    (N'6fc8d2af-14e3-435b-9866-648cb4619d51', N'thaopham', N'THAOPHAM', N'thaopham@gmail.com', N'THAOPHAM@GMAIL.COM', 1, N'AQAAAAIAAYagAAAAELe0/+IAe0y0DNJozbSZNghHS9CLiC7HigRbMWvvlKOFOj72pOzzi/mzMy6OsEsbwA==', N'J5RBRGGMJCSMP2ZL6E6YG5UCSUFFMS2H', N'1478dae2-eff4-48e7-91cb-324adc38fb42', N'0918650293', 0, 0, NULL, 1, 0, 1, '2026-10-30 23:47:04.661');
    PRINT N'[OK] Đã chèn dữ liệu mẫu cho AspNetUsers (6 dòng).';
END
GO

-- Dữ liệu bảng AspNetUserRoles (6 bản ghi)
IF NOT EXISTS (SELECT 1 FROM [AspNetUserRoles])
BEGIN
    INSERT INTO [AspNetUserRoles] (UserId, RoleId) VALUES
    (N'3a3c3104-5561-4523-a693-800cd0946ef7', N'07b86f6e-265c-41b9-8dbd-2f8403a9649e'),
    (N'527cef66-3ba2-4a82-a2a2-5770a24bdd2c', N'a55aaff9-42fd-45c4-8a4f-bc8914ae265b'),
    (N'6a300f77-0a2f-462a-ac88-ba0ba7c84fcd', N'a55aaff9-42fd-45c4-8a4f-bc8914ae265b'),
    (N'6fc8d2af-14e3-435b-9866-648cb4619d51', N'a55aaff9-42fd-45c4-8a4f-bc8914ae265b'),
    (N'e8abd37a-cbbd-4e01-b285-05508151a96a', N'a55aaff9-42fd-45c4-8a4f-bc8914ae265b'),
    (N'f317b5dc-ba47-40fd-8282-93926ec3915d', N'a55aaff9-42fd-45c4-8a4f-bc8914ae265b');
    PRINT N'[OK] Đã chèn dữ liệu mẫu cho AspNetUserRoles (6 dòng).';
END
GO

-- Dữ liệu bảng Categories (3 bản ghi)
IF NOT EXISTS (SELECT 1 FROM [Categories])
BEGIN
    SET IDENTITY_INSERT [Categories] ON;
    INSERT INTO [Categories] (Id, Name, Description, DisplayOrder, IsActive) VALUES
    (1, N'iPhone 16 Series', N'Dòng flagship Apple mới nhất với chip A18 Pro, nút Camera Control và khung Titan siêu nhẹ', 1, 1),
    (2, N'iPhone 15 Series', N'Thế hệ iPhone khung viền Titan và cổng kết nối USB-C chuẩn mực toàn cầu', 2, 1),
    (3, N'iPhone 14 Series', N'Dòng iPhone kinh điển bền bỉ, màn hình Dynamic Island đỉnh cao', 3, 1);
    SET IDENTITY_INSERT [Categories] OFF;
    DBCC CHECKIDENT ('[Categories]', RESEED);
    PRINT N'[OK] Đã chèn dữ liệu mẫu cho Categories (3 dòng).';
END
GO

-- Dữ liệu bảng Products (7 bản ghi)
IF NOT EXISTS (SELECT 1 FROM [Products])
BEGIN
    SET IDENTITY_INSERT [Products] ON;
    INSERT INTO [Products] (Id, CategoryId, Name, ModelSeries, Color, Storage, Specifications, Price, Quantity, WarrantyMonths, ImageUrl, Description, IsActive, Sku, ListedPrice) VALUES
    (1, 1, N'iPhone 16 Pro Max 256GB Titan Sa Mạc', N'iPhone 16 Series', N'Titan Sa Mạc', N'256GB', N'Màn hình 6.9 inch Super Retina XDR OLED 120Hz; Chip A18 Pro 3nm; Camera chính 48MP; Pin 4685 mAh; Sạc MagSafe 25W.', 34990000.00, 6, 12, N'/images/hero-iphone16.jpg', N'iPhone 16 Pro Max với chip A18 Pro, nút Điều khiển Camera hoàn toàn mới, camera tiềm vọng 5x và thời lượng pin xuất sắc nhất trên iPhone.', 1, N'IP16PM-256-DES', 36990000.00),
    (2, 1, N'iPhone 16 Pro 128GB Titan Tự Nhiên', N'iPhone 16 Series', N'Titan Tự Nhiên', N'128GB', N'Màn hình 6.3 inch Super Retina XDR OLED 120Hz; Chip A18 Pro; Nút Camera Control; Cổng USB-C tốc độ 10Gbps.', 28990000.00, 4, 12, N'/images/camera-iphone16.jpg', N'iPhone 16 Pro thiết kế viền mỏng nhất từng có, hiệu năng Pro trong kích thước nhỏ gọn 6.3 inch.', 1, N'IP16P-128-NAT', 30990000.00),
    (3, 1, N'iPhone 16 128GB Hồng', N'iPhone 16 Series', N'Hồng', N'128GB', N'Màn hình 6.1 inch Super Retina XDR; Chip A18; Nút Action & Camera Control; Kính pha màu Ceramic Shield bền bỉ.', 22490000.00, 4, 12, N'/images/hero-iphone16.jpg', N'iPhone 16 màu Hồng cá tính, sở hữu nút Tác Vụ (Action Button) và cụm camera kép đặt dọc hỗ trợ quay Spatial Video.', 1, N'IP16-128-PNK', 24990000.00),
    (4, 1, N'iPhone 16 128GB Xanh Lưu Ly', N'iPhone 16 Series', N'Xanh Lưu Ly', N'128GB', N'Màn hình 6.1 inch Super Retina XDR; Chip A18; 2 camera sau 48MP Fusion & 12MP Ultra Wide.', 22490000.00, 4, 12, N'/images/camera-iphone16.jpg', N'iPhone 16 phiên bản Xanh Lưu Ly (Ultramarine) độc quyền đầy cuốn hút, công nghệ kính pha màu siêu bền.', 1, N'IP16-128-ULT', 24990000.00),
    (5, 2, N'iPhone 15 128GB Xanh Pastel', N'iPhone 15 Series', N'Xanh Pastel', N'128GB', N'Màn hình 6.1 inch OLED Dynamic Island; Chip A16 Bionic; Camera 48MP; Cổng sạc USB-C.', 19490000.00, 5, 12, N'/images/hero-iphone16.jpg', N'iPhone 15 mang màn hình Dynamic Island hiện đại, camera 48MP sắc nét và cổng sạc USB-C tiện dụng.', 1, N'IP15-128-BLU', 21990000.00),
    (6, 2, N'iPhone 15 Pro 128GB Titan Xanh', N'iPhone 15 Series', N'Titan Xanh', N'128GB', N'Màn hình 6.1 inch ProMotion 120Hz; Chip A17 Pro 3nm; Cổng USB-C 3.0; Cụm 3 camera 48MP.', 25490000.00, 3, 12, N'/images/camera-iphone16.jpg', N'iPhone 15 Pro khung viền Titan chuẩn hàng không vũ trụ siêu bền nhẹ, chip A17 Pro đỉnh cao đồ họa.', 1, N'IP15P-128-BLU', 27990000.00),
    (7, 3, N'iPhone 14 Pro Max 128GB Tím Deep Purple', N'iPhone 14 Series', N'Tím Deep Purple', N'128GB', N'Màn hình 6.7 inch Super Retina XDR 120Hz; Chip A16 Bionic; Camera 48MP; Khung viền thép cao cấp.', 22990000.00, 0, 12, N'/images/hero-iphone16.jpg', N'iPhone 14 Pro Max phiên bản Tím Deep Purple huyền thoại, màn hình 120Hz và camera 48MP đầu tiên của Apple.', 1, N'IP14PM-128-PUR', 24990000.00);
    SET IDENTITY_INSERT [Products] OFF;
    DBCC CHECKIDENT ('[Products]', RESEED);
    PRINT N'[OK] Đã chèn dữ liệu mẫu cho Products (7 dòng).';
END
GO

-- Dữ liệu bảng Customers (5 bản ghi)
IF NOT EXISTS (SELECT 1 FROM [Customers])
BEGIN
    SET IDENTITY_INSERT [Customers] ON;
    INSERT INTO [Customers] (Id, UserId, FullName, Email, PhoneNumber, Address, CreatedAt, TotalSpent, RewardPoints, MembershipTier) VALUES
    (12, N'527cef66-3ba2-4a82-a2a2-5770a24bdd2c', N'Võ Văn Bê', N'nghecute@gmail.com', N'0982726260', N'123 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh', '2026-10-30 23:10:27.324', 0.00, 350, N'VIP'),
    (13, N'6a300f77-0a2f-462a-ac88-ba0ba7c84fcd', N'Nguyễn Thị Mai', N'mainguyen@gmail.com', N'0912345678', N'456 Lê Duẩn, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh', '2026-10-22 23:47:04.468', 22490000.00, 120, N'Member'),
    (14, N'e8abd37a-cbbd-4e01-b285-05508151a96a', N'Trần Văn Hùng', N'hungtran@gmail.com', N'0988776655', N'789 Nguyễn Văn Linh, Phường Tân Phong, Quận 7, TP. Hồ Chí Minh', '2026-10-24 23:47:04.585', 0.00, 80, N'Member'),
    (15, N'f317b5dc-ba47-40fd-8282-93926ec3915d', N'Lê Hoàng Long', N'longle@gmail.com', N'0903284116', N'246 Hai Bà Trưng, Phường Tân Định, Quận 1, TP. Hồ Chí Minh', '2026-10-25 23:47:04.657', 69980000.00, 450, N'VIP'),
    (16, N'6fc8d2af-14e3-435b-9866-648cb4619d51', N'Phạm Thu Thảo', N'thaopham@gmail.com', N'0918650293', N'135 Võ Văn Tần, Phường Võ Thị Sáu, Quận 3, TP. Hồ Chí Minh', '2026-10-17 23:47:04.727', 0.00, 50, N'Member');
    SET IDENTITY_INSERT [Customers] OFF;
    DBCC CHECKIDENT ('[Customers]', RESEED);
    PRINT N'[OK] Đã chèn dữ liệu mẫu cho Customers (5 dòng).';
END
GO

-- Dữ liệu bảng Orders (6 bản ghi)
IF NOT EXISTS (SELECT 1 FROM [Orders])
BEGIN
    SET IDENTITY_INSERT [Orders] ON;
    INSERT INTO [Orders] (Id, CustomerId, OrderDate, TotalAmount, OrderStatus, PaymentMethod, ShippingAddress, Notes, OrderCode, PaymentStatus, ShippingUnit, TrackingNumber, EstimatedDeliveryDate) VALUES
    (1, 12, '2026-11-01 17:11:20.205', 34990000.00, N'Cancelled', N'VietQR', N'123 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh', N'Khách chọn quét mã VietQR, đang chờ thanh toán xác nhận giữ kho. | [Hủy tự động bởi Hệ thống]: Quá hạn 15 phút giữ chỗ chưa thanh toán.', N'DH261024-001', N'Chưa thanh toán', NULL, NULL, NULL),
    (2, 16, '2026-11-01 16:14:20.283', 28990000.00, N'Confirmed', N'COD', N'135 Võ Văn Tần, Phường Võ Thị Sáu, Quận 3, TP. Hồ Chí Minh', N'Đã xác nhận điện thoại với khách, chờ xuất kho gán mã IMEI.', N'DH261024-002', N'Chưa thanh toán', NULL, NULL, NULL),
    (3, 14, '2026-11-01 12:14:20.320', 22490000.00, N'Shipping', N'COD', N'789 Nguyễn Văn Linh, Phường Tân Phong, Quận 7, TP. Hồ Chí Minh', N'Hàng đã đóng gói tem niêm phong Apple AAR, bàn giao tài xế GHN.', N'DH261024-003', N'Chưa thanh toán', N'Giao Hàng Nhanh (GHN)', N'GHN-88901234VN', NULL),
    (4, 13, '2026-10-22 17:14:20.345', 22490000.00, N'Completed', N'BankTransfer', N'456 Lê Duẩn, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh', N'Khách đã nhận máy nguyên seal và kích hoạt bảo hành điện tử thành công.', N'DH261024-004', N'Đã thanh toán', N'Viettel Post', N'VTP-77889900VN', NULL),
    (5, 15, '2026-10-12 17:14:20.353', 69980000.00, N'Completed', N'VietQR', N'246 Hai Bà Trưng, Phường Tân Định, Quận 1, TP. Hồ Chí Minh', N'Giao hỏa tốc 2H tại TP.HCM. Khách kiểm tra cả 2 máy hoàn hảo.', N'DH261024-005', N'Đã thanh toán', N'BomChelsea Apple Direct Delivery', N'BOM-EXPRESS-005', NULL),
    (6, 12, '2026-10-30 17:14:20.392', 19490000.00, N'Cancelled', N'COD', N'123 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh', N'Khách đổi ý muốn chuyển sang bản màu Titan Sa Mạc nên hủy đơn này.', N'DH261024-006', N'Chưa thanh toán', NULL, NULL, NULL);
    SET IDENTITY_INSERT [Orders] OFF;
    DBCC CHECKIDENT ('[Orders]', RESEED);
    PRINT N'[OK] Đã chèn dữ liệu mẫu cho Orders (6 dòng).';
END
GO

-- Dữ liệu bảng ProductImeis (33 bản ghi)
IF NOT EXISTS (SELECT 1 FROM [ProductImeis])
BEGIN
    SET IDENTITY_INSERT [ProductImeis] ON;
    INSERT INTO [ProductImeis] (Id, ProductId, Imei, SerialNumber, Status, OrderId, ImportDate, ExportDate, Notes) VALUES
    (1, 1, N'358916010001001', N'S16PM001001', N'InStock', NULL, '2026-10-22 17:14:20.050', NULL, NULL),
    (2, 1, N'358916010001002', N'S16PM001002', N'InStock', NULL, '2026-10-22 17:14:20.050', NULL, NULL),
    (3, 1, N'358916010001003', N'S16PM001003', N'InStock', NULL, '2026-10-24 17:14:20.050', NULL, NULL),
    (4, 1, N'358916010001004', N'S16PM001004', N'Sold', NULL, '2026-10-27 17:14:20.050', NULL, NULL),
    (5, 1, N'358916010001005', N'S16PM001005', N'InStock', NULL, '2026-10-29 17:14:20.050', NULL, NULL),
    (6, 1, N'358916010001101', N'S16PM001101', N'InStock', NULL, '2026-10-30 17:14:20.050', NULL, NULL),
    (7, 1, N'358916010001201', N'S16PM001201', N'Defective', 5, '2026-10-07 17:14:20.050', '2026-10-12 17:14:20.050', NULL),
    (8, 1, N'358916010001202', N'S16PM001202', N'Sold', 5, '2026-10-07 17:14:20.050', '2026-10-12 17:14:20.050', NULL),
    (9, 1, N'358916010001999', N'S16PM001999', N'Defective', NULL, '2026-10-02 17:14:20.050', NULL, N'Lỗi sọc xanh màn hình OLED (Đã thu hồi đổi máy mới)'),
    (10, 2, N'358916020002001', N'S16P002001', N'InStock', NULL, '2026-10-20 17:14:20.050', NULL, NULL),
    (11, 2, N'358916020002002', N'S16P002002', N'InStock', NULL, '2026-10-22 17:14:20.050', NULL, NULL),
    (12, 2, N'358916020002003', N'S16P002003', N'InStock', NULL, '2026-10-24 17:14:20.050', NULL, NULL),
    (13, 2, N'358916020002004', N'S16P002004', N'InStock', NULL, '2026-10-28 17:14:20.050', NULL, NULL),
    (14, 3, N'358916030003001', N'S16003001', N'InStock', NULL, '2026-10-17 17:14:20.050', NULL, NULL),
    (15, 3, N'358916030003002', N'S16003002', N'InStock', NULL, '2026-10-20 17:14:20.050', NULL, NULL),
    (16, 3, N'358916030003003', N'S16003003', N'InStock', NULL, '2026-10-22 17:14:20.050', NULL, NULL),
    (17, 3, N'358916030003004', N'S16003004', N'InStock', NULL, '2026-10-27 17:14:20.050', NULL, NULL),
    (18, 3, N'358916030003201', N'S16003201', N'Sold', 4, '2026-10-12 17:14:20.050', '2026-10-22 17:14:20.050', NULL),
    (19, 4, N'358916040004001', N'S16004001', N'InStock', NULL, '2026-10-17 17:14:20.050', NULL, NULL),
    (20, 4, N'358916040004002', N'S16004002', N'InStock', NULL, '2026-10-20 17:14:20.050', NULL, NULL),
    (21, 4, N'358916040004003', N'S16004003', N'InStock', NULL, '2026-10-22 17:14:20.050', NULL, NULL),
    (22, 4, N'358916040004004', N'S16004004', N'InStock', NULL, '2026-10-27 17:14:20.050', NULL, NULL),
    (23, 4, N'358916040004201', N'S16004201', N'Sold', 3, '2026-10-26 17:14:20.050', '2026-11-01 12:14:20.050', NULL),
    (24, 5, N'358915050005001', N'S15005001', N'InStock', NULL, '2026-10-12 17:14:20.050', NULL, NULL),
    (25, 5, N'358915050005002', N'S15005002', N'InStock', NULL, '2026-10-14 17:14:20.050', NULL, NULL),
    (26, 5, N'358915050005003', N'S15005003', N'InStock', NULL, '2026-10-17 17:14:20.050', NULL, NULL),
    (27, 5, N'358915050005004', N'S15005004', N'InStock', NULL, '2026-10-22 17:14:20.050', NULL, NULL),
    (28, 5, N'358915050005005', N'S15005005', N'InStock', NULL, '2026-10-27 17:14:20.050', NULL, N'Nhả giữ kho tự động do đơn hàng bị hủy'),
    (29, 6, N'358915060006001', N'S15P006001', N'InStock', NULL, '2026-10-17 17:14:20.050', NULL, NULL),
    (30, 6, N'358915060006002', N'S15P006002', N'InStock', NULL, '2026-10-20 17:14:20.050', NULL, NULL),
    (31, 6, N'358915060006003', N'S15P006003', N'InStock', NULL, '2026-10-24 17:14:20.050', NULL, NULL),
    (32, 7, N'358914070007201', N'S14PM007201', N'Sold', NULL, '2024-11-01 17:14:20.050', '2024-11-01 17:14:20.050', NULL),
    (33, 7, N'358914070007999', N'S14PM007999', N'Defective', NULL, '2025-11-01 17:14:20.050', NULL, N'Máy ngấm nước hư hỏng, từ chối bảo hành');
    SET IDENTITY_INSERT [ProductImeis] OFF;
    DBCC CHECKIDENT ('[ProductImeis]', RESEED);
    PRINT N'[OK] Đã chèn dữ liệu mẫu cho ProductImeis (33 dòng).';

    -- Đồng bộ Products.Quantity bằng đúng số lượng InStock IMEIs thực tế trong kho
    UPDATE Products 
    SET Quantity = (
        SELECT COUNT(*) 
        FROM ProductImeis 
        WHERE ProductImeis.ProductId = Products.Id 
          AND ProductImeis.Status = 'InStock'
    );
    PRINT N'[OK] Đã đồng bộ Products.Quantity theo số lượng IMEI InStock thực tế.';
END
GO

-- Dữ liệu bảng OrderDetails (7 bản ghi)
IF NOT EXISTS (SELECT 1 FROM [OrderDetails])
BEGIN
    SET IDENTITY_INSERT [OrderDetails] ON;
    INSERT INTO [OrderDetails] (Id, OrderId, ProductId, Quantity, UnitPrice, TotalPrice, ProductImeiId, ImeiNumber) VALUES
    (1, 1, 1, 1, 34990000.00, 34990000.00, NULL, NULL),
    (2, 2, 2, 1, 28990000.00, 28990000.00, NULL, NULL),
    (3, 3, 4, 1, 22490000.00, 22490000.00, 23, N'358916040004201'),
    (4, 4, 3, 1, 22490000.00, 22490000.00, 18, N'358916030003201'),
    (5, 5, 1, 1, 34990000.00, 34990000.00, 7, N'358916010001201'),
    (6, 5, 1, 1, 34990000.00, 34990000.00, 8, N'358916010001202'),
    (7, 6, 5, 1, 19490000.00, 19490000.00, NULL, NULL);
    SET IDENTITY_INSERT [OrderDetails] OFF;
    DBCC CHECKIDENT ('[OrderDetails]', RESEED);
    PRINT N'[OK] Đã chèn dữ liệu mẫu cho OrderDetails (7 dòng).';
END
GO

-- Dữ liệu bảng Warranties (4 bản ghi)
IF NOT EXISTS (SELECT 1 FROM [Warranties])
BEGIN
    SET IDENTITY_INSERT [Warranties] ON;
    INSERT INTO [Warranties] (Id, CustomerId, ProductId, OrderDetailId, Imei, SerialNumber, OriginalImei, StartDate, EndDate, WarrantyType, Status, Notes, CreatedAt) VALUES
    (1, 13, 3, 4, N'358916030003201', N'S16003201', NULL, '2026-10-22 17:14:20.404', '2027-10-22 17:14:20.404', N'Chính hãng Apple 12 tháng', N'Active', N'Kích hoạt bảo hành điện tử chính hãng Apple AAR. Còn trong thời hạn 30 ngày 1 đổi 1.', '2026-10-22 17:14:20.404'),
    (2, 15, 1, 6, N'358916010001202', N'S16PM001202', NULL, '2026-08-28 17:14:20.435', '2027-08-28 17:14:20.435', N'AppleCare+ 1 Đổi 1 VIP', N'Active', N'Gói AppleCare+ 1 đổi 1 VIP được bảo vệ toàn diện nguồn và màn hình.', '2026-08-28 17:14:20.435'),
    (3, 15, 1, 5, N'358916010001004', N'S16PM001004', N'358916010001999', '2026-10-12 17:14:20.435', '2027-10-12 17:14:20.435', N'Chính hãng Apple 12 tháng', N'Active', N'Đã thực hiện chính sách 1 đổi 1 sang máy mới mã 358916010001201.', '2026-10-12 17:14:20.435'),
    (4, 14, 7, NULL, N'358914070007201', N'S14PM007201', NULL, '2024-11-01 17:14:20.499', '2025-11-01 17:14:20.499', N'Chính hãng Apple 12 tháng', N'Voided', N'Máy bị vào nước ngấm chất lỏng, giấy quỳ tím bên trong đổi màu đỏ. Từ chối bảo hành theo quy định Apple.', '2024-11-01 17:14:20.499');
    SET IDENTITY_INSERT [Warranties] OFF;
    DBCC CHECKIDENT ('[Warranties]', RESEED);
    PRINT N'[OK] Đã chèn dữ liệu mẫu cho Warranties (4 dòng).';
END
GO

-- Dữ liệu bảng WarrantyClaims (3 bản ghi)
IF NOT EXISTS (SELECT 1 FROM [WarrantyClaims])
BEGIN
    SET IDENTITY_INSERT [WarrantyClaims] ON;
    INSERT INTO [WarrantyClaims] (Id, WarrantyId, ClaimDate, IssueDescription, TechnicianNotes, ActionTaken, OldImei, NewImei, Status, ResolvedDate, CreatedBy, ClaimCode) VALUES
    (1, 3, '2026-10-27 17:14:20.488', N'Màn hình OLED xuất hiện 1 đường sọc xanh dọc, cảm ứng chập chờn khi sử dụng.', N'Kỹ thuật viên Apple Care kiểm tra: Lỗi phần cứng do nhà sản xuất, máy không trầy xước va đập. Đạt tiêu chuẩn 1 đổi 1 theo chính sách AAR.', N'Đổi máy mới (Exchange)', N'358916010001999', N'358916010001201', N'Hoàn tất', '2026-10-28 17:14:20.488', N'admin', N'YC-261024-001'),
    (2, 1, '2026-11-01 17:15:01.565', N'Camera rung / mờ không lấy nét', N'Đủ điều kiện xem xét 1 Đổi 1 trong 30 ngày (Tiếp nhận ban đầu)', N'Gửi TTBH Apple AASP sửa chữa', NULL, NULL, N'Gửi Apple', NULL, N'admin', N'BH261101-001'),
    (3, 3, '2026-11-01 17:15:26.563', N'Face ID không khả dụng', N'Đủ điều kiện xem xét 1 Đổi 1 trong 30 ngày (Tiếp nhận ban đầu)', N'Đổi mới 1-1 (AAR Instant Replacement)', N'358916010001201', N'358916010001004', N'Hoàn tất', '2026-11-01 17:15:44.009', N'admin', N'BH261101-002');
    SET IDENTITY_INSERT [WarrantyClaims] OFF;
    DBCC CHECKIDENT ('[WarrantyClaims]', RESEED);
    PRINT N'[OK] Đã chèn dữ liệu mẫu cho WarrantyClaims (3 dòng).';
END
GO

-- Dữ liệu bảng AuditLogs (8 bản ghi)
IF NOT EXISTS (SELECT 1 FROM [AuditLogs])
BEGIN
    SET IDENTITY_INSERT [AuditLogs] ON;
    INSERT INTO [AuditLogs] (Id, UserId, UserName, Action, EntityName, RecordId, OldValues, NewValues, IpAddress, Timestamp) VALUES
    (1, N'3a3c3104-5561-4523-a693-800cd0946ef7', N'admin', N'SYSTEM_INIT', N'System', N'0', NULL, N'Khởi tạo hệ thống dữ liệu mẫu AAR BomChelsea Enterprise.', N'127.0.0.1', '2026-10-02 17:14:20.523'),
    (2, N'3a3c3104-5561-4523-a693-800cd0946ef7', N'admin', N'IMPORT_IMEI', N'ProductImeis', N'1', NULL, N'Nhập kho lô 10 máy iPhone 16 Pro Max từ nhà phân phối Synnex FPT.', N'127.0.0.1', '2026-10-22 17:14:20.523'),
    (3, N'3a3c3104-5561-4523-a693-800cd0946ef7', N'admin', N'FULFILL_ORDER', N'Orders', N'4', NULL, N'Hoàn tất đơn hàng #DH261024-004, xuất kho IMEI 358916030003201 và kích hoạt bảo hành điện tử.', N'127.0.0.1', '2026-10-22 17:14:20.523'),
    (4, N'3a3c3104-5561-4523-a693-800cd0946ef7', N'admin', N'WARRANTY_EXCHANGE', N'WarrantyClaims', N'1', N'OldImei=358916010001999', N'NewImei=358916010001201, Status=Hoàn tất', N'127.0.0.1', '2026-10-28 17:14:20.523'),
    (5, N'3a3c3104-5561-4523-a693-800cd0946ef7', N'admin', N'WARRANTY_AASP', N'WarrantyClaims', N'2', N'Status=Tiếp nhận, Action=', N'Chuyển tiếp TTBH Apple AASP', N'::1', '2026-11-01 17:15:09.398'),
    (6, N'3a3c3104-5561-4523-a693-800cd0946ef7', N'admin', N'WARRANTY_AASP', N'WarrantyClaims', N'2', N'Status=Gửi Apple, Action=Gửi TTBH Apple AASP sửa chữa', N'Chuyển tiếp TTBH Apple AASP', N'::1', '2026-11-01 17:15:20.145'),
    (7, N'3a3c3104-5561-4523-a693-800cd0946ef7', N'admin', N'WARRANTY_REPLACE_1TO1', N'WarrantyClaims', N'3', N'OldImei=358916010001201', N'NewImei=358916010001004, Status=Hoàn tất', N'::1', '2026-11-01 17:15:44.041'),
    (8, NULL, N'OrderHoldCleanupWorker', N'SYSTEM_CLEANUP', N'Orders', N'1', NULL, N'{"OrderId":1,"Action":"AutoCancel","ReleasedImeis":1}', N'127.0.0.1', '2026-11-01 17:26:21.090');
    SET IDENTITY_INSERT [AuditLogs] OFF;
    DBCC CHECKIDENT ('[AuditLogs]', RESEED);
    PRINT N'[OK] Đã chèn dữ liệu mẫu cho AuditLogs (8 dòng).';
END
GO

-- ============================================================================
-- ĐẢM BẢO CÁC TÀI KHOẢN NỘI BỘ (ADMIN, MANAGER, STAFF, CASHIER, SHIPPER)
-- ============================================================================
-- 1. Bổ sung các Role nội bộ nếu chưa có
IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Name = 'Admin')
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES (N'role-admin-id', 'Admin', 'ADMIN', NEWID());
IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Name = 'Manager')
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES (N'role-manager-id', 'Manager', 'MANAGER', NEWID());
IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Name = 'Staff')
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES (N'role-staff-id', 'Staff', 'STAFF', NEWID());
IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Name = 'Cashier')
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES (N'role-cashier-id', 'Cashier', 'CASHIER', NEWID());
IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Name = 'Shipper')
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES (N'role-shipper-id', 'Shipper', 'SHIPPER', NEWID());
IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Name = 'Customer')
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES (N'role-customer-id', 'Customer', 'CUSTOMER', NEWID());
GO

-- 2. Đảm bảo user 'admin' (Password: admin123)
IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE UserName = 'admin')
BEGIN
    INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount, IsActive, CreatedAt)
    VALUES (
        N'user-admin-01',
        N'admin',
        N'ADMIN',
        N'admin@istore.vn',
        N'ADMIN@ISTORE.VN',
        1,
        N'AQAAAAIAAYagAAAAEFaU3Ag6YGld/iMaXuO//zG2Ch/x45TX2E8Yp150bBIB9lm8N9jPbF0fvqMSqz5RiA==',
        N'BXN26FTFM5UHRRRZWPDPQZBC6HVA23DQ',
        NEWID(),
        0, 0, 1, 0, 1, GETDATE()
    );
    INSERT INTO AspNetUserRoles (UserId, RoleId)
    SELECT N'user-admin-01', Id FROM AspNetRoles WHERE Name = 'Admin';
END
GO

-- 3. Đảm bảo user 'manager01' (Password: admin123)
IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE UserName = 'manager01')
BEGIN
    INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount, IsActive, CreatedAt)
    VALUES (
        N'user-manager-01',
        N'manager01',
        N'MANAGER01',
        N'manager@istore.vn',
        N'MANAGER@ISTORE.VN',
        1,
        N'AQAAAAIAAYagAAAAEFaU3Ag6YGld/iMaXuO//zG2Ch/x45TX2E8Yp150bBIB9lm8N9jPbF0fvqMSqz5RiA==',
        NEWID(),
        NEWID(),
        0, 0, 1, 0, 1, GETDATE()
    );
    INSERT INTO AspNetUserRoles (UserId, RoleId)
    SELECT N'user-manager-01', Id FROM AspNetRoles WHERE Name = 'Manager';
END
GO

-- 4. Đảm bảo user 'staff01' (Password: admin123)
IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE UserName = 'staff01')
BEGIN
    INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount, IsActive, CreatedAt)
    VALUES (
        N'user-staff-01',
        N'staff01',
        N'STAFF01',
        N'staff@istore.vn',
        N'STAFF@ISTORE.VN',
        1,
        N'AQAAAAIAAYagAAAAEFaU3Ag6YGld/iMaXuO//zG2Ch/x45TX2E8Yp150bBIB9lm8N9jPbF0fvqMSqz5RiA==',
        NEWID(),
        NEWID(),
        0, 0, 1, 0, 1, GETDATE()
    );
    INSERT INTO AspNetUserRoles (UserId, RoleId)
    SELECT N'user-staff-01', Id FROM AspNetRoles WHERE Name = 'Staff';
END
GO

-- 5. Đảm bảo user 'cashier01' (Password: admin123)
IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE UserName = 'cashier01')
BEGIN
    INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount, IsActive, CreatedAt)
    VALUES (
        N'user-cashier-01',
        N'cashier01',
        N'CASHIER01',
        N'cashier@istore.vn',
        N'CASHIER@ISTORE.VN',
        1,
        N'AQAAAAIAAYagAAAAEFaU3Ag6YGld/iMaXuO//zG2Ch/x45TX2E8Yp150bBIB9lm8N9jPbF0fvqMSqz5RiA==',
        NEWID(),
        NEWID(),
        0, 0, 1, 0, 1, GETDATE()
    );
    INSERT INTO AspNetUserRoles (UserId, RoleId)
    SELECT N'user-cashier-01', Id FROM AspNetRoles WHERE Name = 'Cashier';
END
GO

-- 6. Đảm bảo user 'shipper01' (Password: admin123)
IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE UserName = 'shipper01')
BEGIN
    INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount, IsActive, CreatedAt)
    VALUES (
        N'user-shipper-01',
        N'shipper01',
        N'SHIPPER01',
        N'shipper@istore.vn',
        N'SHIPPER@ISTORE.VN',
        1,
        N'AQAAAAIAAYagAAAAEFaU3Ag6YGld/iMaXuO//zG2Ch/x45TX2E8Yp150bBIB9lm8N9jPbF0fvqMSqz5RiA==',
        NEWID(),
        NEWID(),
        0, 0, 1, 0, 1, GETDATE()
    );
    INSERT INTO AspNetUserRoles (UserId, RoleId)
    SELECT N'user-shipper-01', Id FROM AspNetRoles WHERE Name = 'Shipper';
END
GO

PRINT N'============================================================================';
PRINT N'>>> HOÀN TẤT THIẾT LẬP CƠ SỞ DỮ LIỆU IPHONESTOREDB COMPLETE V3.1!';
PRINT N'============================================================================';
GO
