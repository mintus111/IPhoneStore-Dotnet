-- ============================================================================
-- CƠ SỞ DỮ LIỆU TOÀN DIỆN: IPHONESTORE ENTERPRISE (AAR BOMCHELSEA STANDARD)
-- Phiên bản: Complete V3.0 (Tích hợp 100% 12 Màn hình Quản trị & Nghiệp vụ BRS)
-- Ngày tạo: 2026-10-25 16:25:16
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

-- Dữ liệu bảng AspNetRoles (2 bản ghi - Admin và Customer)
IF NOT EXISTS (SELECT 1 FROM [AspNetRoles])
BEGIN
    INSERT INTO [AspNetRoles] (Id, Name, NormalizedName, ConcurrencyStamp) VALUES
    (N'07b86f6e-265c-41b9-8dbd-2f8403a9649e', N'Admin', N'ADMIN', N'bb804307-2fb4-426e-8c68-eff35d690701'),
    (N'a55aaff9-42fd-45c4-8a4f-bc8914ae265b', N'Customer', N'CUSTOMER', N'e37cae32-77e9-4900-8c2b-bd821e70b4ab');
    PRINT N'[OK] Đã chèn dữ liệu mẫu cho AspNetRoles (2 dòng).';
END
GO

-- Dữ liệu bảng AspNetUsers (6 bản ghi: 1 Admin + 5 Khách hàng mẫu chuẩn bảo mật)
IF NOT EXISTS (SELECT 1 FROM [AspNetUsers])
BEGIN
    INSERT INTO [AspNetUsers] (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount, IsActive, CreatedAt) VALUES
    (N'3a3c3104-5561-4523-a693-800cd0946ef7', N'admin', N'ADMIN', N'admin@istore.vn', N'ADMIN@ISTORE.VN', NULL, N'AQAAAAIAAYagAAAAEFaU3Ag6YGld/iMaXuO//zG2Ch/x45TX2E8Yp150bBIB9lm8N9jPbF0fvqMSqz5RiA==', N'BXN26FTFM5UHRRRZWPDPQZBC6HVA23DQ', N'9a92ce97-0ca0-4ed4-bb84-0a96f53211aa', N'0909000001', 0, 0, NULL, 0, 1, '2026-10-19 15:38:22.469'),
    (N'424cea8a-5a5c-4d04-b6df-ab70a402f3b9', N'nghecute', N'NGHECUTE', N'nghecute@gmail.com', N'NGHECUTE@GMAIL.COM', 1, N'AQAAAAIAAYagAAAAENWJWXDoyo8SdoADjcfc7W83LALKUySg+c5Ytr7fBugphM9z8fESfyecGOJoJ0Q6ew==', N'CKUIVID7CSX47H7P7AGUVQFW2V5XS56N', N'2430c6cf-180b-44d3-936e-9165b024f3fa', N'0982726262', 1, 0, 1, 0, 1, '2026-10-25 15:15:53.346'),
    (N'b1111111-1111-1111-1111-111111111111', N'mainguyen', N'MAINGUYEN', N'mainguyen@gmail.com', N'MAINGUYEN@GMAIL.COM', 1, N'AQAAAAIAAYagAAAAENWJWXDoyo8SdoADjcfc7W83LALKUySg+c5Ytr7fBugphM9z8fESfyecGOJoJ0Q6ew==', N'MNUIVID7CSX47H7P7AGUVQFW2V5XS56N', N'3430c6cf-180b-44d3-936e-9165b024f3fa', N'0912345678', 1, 0, 1, 0, 1, '2026-10-26 10:00:00.000'),
    (N'b2222222-2222-2222-2222-222222222222', N'hungtran', N'HUNGTRAN', N'hungtran@gmail.com', N'HUNGTRAN@GMAIL.COM', 1, N'AQAAAAIAAYagAAAAENWJWXDoyo8SdoADjcfc7W83LALKUySg+c5Ytr7fBugphM9z8fESfyecGOJoJ0Q6ew==', N'HNUIVID7CSX47H7P7AGUVQFW2V5XS56N', N'4430c6cf-180b-44d3-936e-9165b024f3fa', N'0988776655', 1, 0, 1, 0, 1, '2026-10-26 11:00:00.000'),
    (N'b3333333-3333-3333-3333-333333333333', N'longle', N'LONGLE', N'longle@gmail.com', N'LONGLE@GMAIL.COM', 1, N'AQAAAAIAAYagAAAAENWJWXDoyo8SdoADjcfc7W83LALKUySg+c5Ytr7fBugphM9z8fESfyecGOJoJ0Q6ew==', N'LNUIVID7CSX47H7P7AGUVQFW2V5XS56N', N'5430c6cf-180b-44d3-936e-9165b024f3fa', N'0903284116', 1, 0, 1, 0, 1, '2026-10-27 09:00:00.000'),
    (N'b4444444-4444-4444-4444-444444444444', N'thaopham', N'THAOPHAM', N'thaopham@gmail.com', N'THAOPHAM@GMAIL.COM', 1, N'AQAAAAIAAYagAAAAENWJWXDoyo8SdoADjcfc7W83LALKUySg+c5Ytr7fBugphM9z8fESfyecGOJoJ0Q6ew==', N'TNUIVID7CSX47H7P7AGUVQFW2V5XS56N', N'6430c6cf-180b-44d3-936e-9165b024f3fa', N'0918650293', 1, 0, 1, 0, 1, '2026-10-28 08:30:00.000');
    PRINT N'[OK] Đã chèn dữ liệu mẫu cho AspNetUsers (6 dòng).';
END
GO

-- Dữ liệu bảng AspNetUserRoles (6 bản ghi: 1 Admin + 5 Customer)
IF NOT EXISTS (SELECT 1 FROM [AspNetUserRoles])
BEGIN
    INSERT INTO [AspNetUserRoles] (UserId, RoleId) VALUES
    (N'3a3c3104-5561-4523-a693-800cd0946ef7', N'07b86f6e-265c-41b9-8dbd-2f8403a9649e'),
    (N'424cea8a-5a5c-4d04-b6df-ab70a402f3b9', N'a55aaff9-42fd-45c4-8a4f-bc8914ae265b'),
    (N'b1111111-1111-1111-1111-111111111111', N'a55aaff9-42fd-45c4-8a4f-bc8914ae265b'),
    (N'b2222222-2222-2222-2222-222222222222', N'a55aaff9-42fd-45c4-8a4f-bc8914ae265b'),
    (N'b3333333-3333-3333-3333-333333333333', N'a55aaff9-42fd-45c4-8a4f-bc8914ae265b'),
    (N'b4444444-4444-4444-4444-444444444444', N'a55aaff9-42fd-45c4-8a4f-bc8914ae265b');
    PRINT N'[OK] Đã chèn dữ liệu mẫu cho AspNetUserRoles (6 dòng).';
END
GO

-- Dữ liệu bảng Categories (9 bản ghi)
IF NOT EXISTS (SELECT 1 FROM [Categories])
BEGIN
    SET IDENTITY_INSERT [Categories] ON;
    INSERT INTO [Categories] (Id, Name, Description, DisplayOrder, IsActive) VALUES
    (1, N'iPhone 16 Series', N'Thế hệ iPhone mới nhất tích hợp Apple Intelligence và Chip A18 Pro', 1, NULL),
    (2, N'iPhone 15 Series', N'Khung viền Titanium chuẩn hàng không vũ trụ và cổng kết nối USB-C', 2, NULL),
    (3, N'iPhone 14 Series', N'Màn hình Dynamic Island đột phá và tính năng cảnh báo va chạm', 3, NULL),
    (4, N'iPhone 13 & Cũ', N'Các dòng iPhone quốc dân bền bỉ, hiệu năng mạnh mẽ với mức giá tốt', 4, NULL),
    (5, N'iPhone 17', N'Hiệu năng thế hệ mới. Trải nghiệm Pro gần hơn.', 1, NULL),
    (6, N'iPhone 16', N'Apple Intelligence và thiết kế đầy sắc màu.', 2, NULL),
    (7, N'iPhone 15', N'Dynamic Island. Camera 48MP. USB-C.', 3, NULL),
    (8, N'iPhone 14', N'Hiệu năng ổn định cho mọi tác vụ hằng ngày.', 4, NULL),
    (9, N'iPhone SE', N'Thiết kế nhỏ gọn. Touch ID quen thuộc.', 5, 0);
    SET IDENTITY_INSERT [Categories] OFF;
    DBCC CHECKIDENT ('[Categories]', RESEED);
    PRINT N'[OK] Đã chèn dữ liệu mẫu cho Categories (9 dòng).';
END
GO

-- Dữ liệu bảng Products (26 bản ghi)
IF NOT EXISTS (SELECT 1 FROM [Products])
BEGIN
    SET IDENTITY_INSERT [Products] ON;
    INSERT INTO [Products] (Id, CategoryId, Name, ModelSeries, Color, Storage, Specifications, Price, Quantity, WarrantyMonths, ImageUrl, Description, IsActive, Sku, ListedPrice) VALUES
    (1, 1, N'iPhone 16 Pro Max 256GB - Sa Mạc Titan', N'iPhone 16 Pro Max', N'Sa Mạc Titan', N'256GB', N'Chip Apple A18 Pro 6 nhân, RAM 8GB, Màn hình 6.9" Super Retina XDR OLED 120Hz ProMotion, Camera chính 48MP zoom quang 5x, Pin 4685mAh sạc nhanh 25W MagSafe, Khung Titan Cấp 5.', 34990000.00, 29, 12, N'https://images.unsplash.com/photo-1695048133142-1a20484d2569?auto=format&fit=crop&w=800&q=80', N'Flagship đỉnh cao nhất của Apple năm 2024-2026 với phím điều khiển Camera Control mới, viền màn hình mỏng nhất lịch sử và Apple Intelligence siêu thông minh.', NULL, N'IP16PM-256-DES', 36990000.00),
    (2, 1, N'iPhone 16 Pro Max 512GB - Titan Tự Nhiên', N'iPhone 16 Pro Max', N'Titan Tự Nhiên', N'512GB', N'Chip Apple A18 Pro, RAM 8GB, Màn hình 6.9" 120Hz, Bộ nhớ 512GB lưu trữ 8K ProRes, Pin 4685mAh.', 40490000.00, 15, 12, N'https://images.unsplash.com/photo-1592750475338-74b7b21085ab?auto=format&fit=crop&w=800&q=80', N'Phiên bản dung lượng lớn chuyên quay phim chuẩn điện ảnh ProRes Log, âm thanh phòng thu 4 micro Studio.', NULL, N'IP16PM-512-NTL', 42990000.00),
    (3, 1, N'iPhone 16 Pro 128GB - Titan Đen', N'iPhone 16 Pro', N'Titan Đen', N'128GB', N'Chip Apple A18 Pro, RAM 8GB, Màn hình 6.3" ProMotion 120Hz, Camera 48MP Zoom 5x tetraprism, Pin 3582mAh.', 28990000.00, 20, 12, N'https://images.unsplash.com/photo-1510557880182-3d4d3cba35a5?auto=format&fit=crop&w=800&q=80', N'Chiếc iPhone nhỏ gọn với sức mạnh phần cứng vô song cùng cụm 3 camera chuẩn điện ảnh chuyên nghiệp.', NULL, N'IP16P-128-BLK', 30990000.00),
    (4, 1, N'iPhone 16 Plus 128GB - Xanh Mòng Két (Teal)', N'iPhone 16 Plus', N'Xanh Teal', N'128GB', N'Chip Apple A18 tiêu chuẩn, RAM 8GB, Màn hình 6.7" OLED, Nút Camera Control và Action Button, Pin trâu 4674mAh.', 25990000.00, 29, 12, N'https://images.unsplash.com/photo-1580910051074-3eb694886505?auto=format&fit=crop&w=800&q=80', N'Thời lượng pin vô địch trong các dòng iPhone, sắc màu cá tính mặt lưng kính pha màu rực rỡ.', NULL, NULL, NULL),
    (5, 1, N'iPhone 16 128GB - Hồng Phấn (Pink)', N'iPhone 16', N'Hồng Phấn', N'128GB', N'Chip Apple A18, RAM 8GB, Màn hình 6.1" Super Retina XDR, Camera chéo kép 48MP Fusion chụp ảnh không gian Spatial.', 22490000.00, 40, 12, N'https://images.unsplash.com/photo-1565849904461-04a58ad377e0?auto=format&fit=crop&w=800&q=80', N'Màu sắc thời trang được yêu thích nhất thế hệ mới, hỗ trợ đầy đủ các tính năng Apple Intelligence.', NULL, NULL, NULL),
    (6, 2, N'iPhone 15 Pro Max 256GB - Titan Tự Nhiên', N'iPhone 15 Pro Max', N'Titan Tự Nhiên', N'256GB', N'Chip Apple A17 Pro 3nm, RAM 8GB, Màn hình 6.7" 120Hz ProMotion, Cổng USB-C tốc độ 10Gbps, Camera 5x Zoom.', 29490000.00, 5, 12, N'https://images.unsplash.com/photo-1695048133142-1a20484d2569?auto=format&fit=crop&w=800&q=80', N'Dòng máy bán chạy nhất toàn cầu năm 2023-2024, khung titan siêu nhẹ cầm nắm êm ái đầm tay.', NULL, NULL, NULL),
    (7, 2, N'iPhone 15 Pro 128GB - Titan Xanh (Blue Titanium)', N'iPhone 15 Pro', N'Titan Xanh', N'128GB', N'Chip Apple A17 Pro, RAM 8GB, Màn hình 6.1" 120Hz, Nút tác vụ Action Button đa năng, Cổng USB-C.', 24490000.00, 12, 12, N'https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?auto=format&fit=crop&w=800&q=80', N'Thiết kế sang trọng đẳng cấp doanh nhân, hiệu năng đồ họa Ray Tracing chơi game console đỉnh cao.', NULL, NULL, NULL),
    (8, 2, N'iPhone 15 128GB - Xanh Lá (Green)', N'iPhone 15', N'Xanh Pastel', N'128GB', N'Chip Apple A16 Bionic, RAM 6GB, Màn hình Dynamic Island 6.1", Camera 48MP zoom 2x chất lượng quang học, Cổng USB-C.', 19290000.00, 33, 12, N'https://images.unsplash.com/photo-1574944985070-8f3ebc6b79d2?auto=format&fit=crop&w=800&q=80', N'Mức giá cực tốt cho một chiếc máy có Dynamic Island thời thượng và camera siêu nét 48MP.', NULL, N'IP15-128-GRN', 20447400.00),
    (9, 3, N'iPhone 14 Pro Max 128GB - Tím Đậm (Deep Purple)', N'iPhone 14 Pro Max', N'Tím Deep Purple', N'128GB', N'Chip Apple A16 Bionic, RAM 6GB, Màn hình Dynamic Island 6.7" 120Hz, Camera 48MP, Khung thép bóng bẩy.', 25490000.00, 0, 12, N'https://images.unsplash.com/photo-1678685888221-cda773a3dcdb?auto=format&fit=crop&w=800&q=80', N'Màu Tím Deep Purple huyền thoại từng gây bão toàn cầu, thiết kế khung thép sáng bóng quyền lực.', NULL, NULL, NULL),
    (10, 3, N'iPhone 14 128GB - Xanh Dương (Blue)', N'iPhone 14', N'Xanh Blue', N'128GB', N'Chip Apple A15 Bionic 5 nhân GPU, Màn hình 6.1" OLED, Pin trâu, Thiết kế khung nhôm siêu bền.', 16990000.00, 18, 12, N'https://images.unsplash.com/photo-1510557880182-3d4d3cba35a5?auto=format&fit=crop&w=800&q=80', N'Chiếc iPhone quốc dân bán chạy nhất phân khúc tầm trung, thiết kế tai thỏ thu gọn thanh lịch.', NULL, NULL, NULL),
    (11, 4, N'iPhone 13 Pro Max 128GB - Xanh Alpine Green (Like New 99%)', N'iPhone 13 Pro Max', N'Xanh Alpine', N'128GB', N'Chip Apple A15 Bionic, RAM 6GB, Màn hình 6.7" 120Hz ProMotion, Pin 4352mAh trâu số 1, Khung thép không gỉ.', 16990000.00, 10, 6, N'https://images.unsplash.com/photo-1592750475338-74b7b21085ab?auto=format&fit=crop&w=800&q=80', N'Hàng tuyển chọn Like New 99% nguyên bản zin áp suất, thời lượng pin trâu kinh điển vượt trội.', 0, NULL, NULL),
    (12, 2, N'iPhone 15 256GB - Xanh Lá (Green)', N'iPhone 15', N'Xanh Pastel', N'256GB', N'Màn hình Super Retina XDR OLED 6.1", Chip A16 Bionic (CPU 6 lõi, GPU 5 lõi), Camera chính 48MP + Ultra Wide 12MP, Cổng USB-C, Kháng nước IP68', 22490000.00, 25, 12, N'https://images.unsplash.com/photo-1574944985070-8f3ebc6b79d2?auto=format&fit=crop&w=800&q=80', N'iPhone 15 256GB sở hữu thiết kế mặt lưng kính pha màu bền bỉ, Dynamic Island kỳ diệu và camera chính 48MP mang lại những bức ảnh có độ phân giải siêu cao.', NULL, N'IP15-256-GRN', 24990000.00),
    (13, 2, N'iPhone 15 512GB - Xanh Lá (Green)', N'iPhone 15', N'Xanh Pastel', N'512GB', N'Màn hình Super Retina XDR OLED 6.1", Chip A16 Bionic (CPU 6 lõi, GPU 5 lõi), Camera chính 48MP + Ultra Wide 12MP, Cổng USB-C, Kháng nước IP68', 27990000.00, 15, 12, N'https://images.unsplash.com/photo-1574944985070-8f3ebc6b79d2?auto=format&fit=crop&w=800&q=80', N'iPhone 15 512GB không gian lưu trữ rộng lớn, thiết kế đẳng cấp và camera 48MP siêu nét.', NULL, N'IP15-512-GRN', 29990000.00),
    (14, 1, N'iPhone 16 Pro Max 1TB - Sa Mạc Titan', N'iPhone 16 Pro Max', N'Sa Mạc Titan', N'1TB', N'Màn hình Super Retina XDR OLED 6.9" 120Hz ProMotion, Chip A18 Pro, Camera Fusion 48MP, 5x Telephoto, Khung Titan Cấp 5, Nút Camera Control', 46990000.00, 10, 12, N'https://images.unsplash.com/photo-1695048133142-1a20484d2569?auto=format&fit=crop&w=800&q=80', N'iPhone 16 Pro Max 1TB dung lượng khủng dành cho giới chuyên nghiệp quay phim 4K 120 fps ProRes.', NULL, NULL, NULL),
    (15, 1, N'iPhone 16 Pro 256GB - Titan Đen', N'iPhone 16 Pro', N'Titan Đen', N'256GB', N'Màn hình Super Retina XDR 6.3" 120Hz ProMotion, Chip A18 Pro, Camera Fusion 48MP, Khung Titan cao cấp', 31990000.00, 30, 12, N'https://images.unsplash.com/photo-1510557880182-3d4d3cba35a5?auto=format&fit=crop&w=800&q=80', N'iPhone 16 Pro 256GB Titan Đen sang trọng, hiệu năng chip A18 Pro đột phá.', NULL, NULL, NULL),
    (16, 1, N'iPhone 16 Pro 512GB - Titan Đen', N'iPhone 16 Pro', N'Titan Đen', N'512GB', N'Màn hình Super Retina XDR 6.3" 120Hz ProMotion, Chip A18 Pro, Camera Fusion 48MP, Khung Titan cao cấp', 37490000.00, 15, 12, N'https://images.unsplash.com/photo-1510557880182-3d4d3cba35a5?auto=format&fit=crop&w=800&q=80', N'iPhone 16 Pro 512GB Titan Đen hiệu năng cực đỉnh, lưu trữ thoải mái.', NULL, NULL, NULL),
    (17, 2, N'iPhone 15 Pro Max 512GB - Titan Tự Nhiên', N'iPhone 15 Pro Max', N'Titan Tự Nhiên', N'512GB', N'Màn hình Super Retina XDR 6.7" 120Hz, Chip A17 Pro 3nm, Camera Telephoto 5x, Khung viền Titanium', 34990000.00, 20, 12, N'https://images.unsplash.com/photo-1695048133142-1a20484d2569?auto=format&fit=crop&w=800&q=80', N'iPhone 15 Pro Max 512GB Titan Tự Nhiên sang trọng vượt thời gian.', NULL, NULL, NULL),
    (18, 2, N'iPhone 15 Pro 256GB - Titan Xanh', N'iPhone 15 Pro', N'Titan Xanh', N'256GB', N'Màn hình Super Retina XDR 6.1" 120Hz, Chip A17 Pro, Camera 48MP, Khung viền Titanium', 27490000.00, 22, 12, N'https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?auto=format&fit=crop&w=800&q=80', N'iPhone 15 Pro 256GB Titan Xanh mạnh mẽ và tinh tế.', NULL, NULL, NULL),
    (19, 5, N'iPhone 17', N'iPhone 17', N'Đen', N'256GB', N'{"chip":"Apple A19","ram":"8GB","screen":"6,3 inch · Super Retina XDR · 120Hz","camera":"48MP Fusion + 48MP siêu rộng","battery":"Lên đến 30 giờ xem video","frame":"Nhôm · Mặt trước Ceramic Shield","port":"USB-C","waterResistance":"IP68"}', 24990000.00, 19, 12, N'https://images.unsplash.com/photo-1510557880182-3d4d3cba35a5?auto=format&fit=crop&w=800&q=80', N'iPhone 17 sở hữu chip A19 Bionic, màn hình 6.3 inch Super Retina XDR 120Hz, camera kép 48MP Fusion + 48MP siêu rộng.', NULL, N'IP17-256-BLK', 26990000.00),
    (20, 5, N'iPhone 17', N'iPhone 17', N'Trắng', N'256GB', N'{"chip":"Apple A19","ram":"8GB","screen":"6,3 inch · Super Retina XDR · 120Hz","camera":"48MP Fusion + 48MP siêu rộng","battery":"Lên đến 30 giờ xem video","frame":"Nhôm · Mặt trước Ceramic Shield","port":"USB-C","waterResistance":"IP68"}', 24990000.00, 24, 12, N'https://images.unsplash.com/photo-1592750475338-74b7b21085ab?auto=format&fit=crop&w=800&q=80', N'iPhone 17 Trắng thanh lịch, mặt lưng kính pha màu bền bỉ.', NULL, N'IP17-256-WHT', 26990000.00),
    (21, 5, N'iPhone 17 Pro', N'iPhone 17 Pro', N'Bạc', N'256GB', N'{"chip":"Apple A19 Pro","ram":"12GB","screen":"6,3 inch · ProMotion 120Hz Always-On","camera":"48MP + 48MP + 48MP Tele 5x","battery":"Lên đến 33 giờ xem video","frame":"Titanium cấp độ 5","port":"USB-C Thunderbolt 4","waterResistance":"IP68"}', 34990000.00, 0, 24, N'https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?auto=format&fit=crop&w=800&q=80', N'iPhone 17 Pro titan siêu nhẹ, camera tele tiềm vọng 5x.', NULL, N'IP17P-256-SLV', 36990000.00),
    (22, 6, N'iPhone 16', N'iPhone 16', N'Trắng', N'128GB', N'{"chip":"Apple A18","ram":"8GB","screen":"6,1 inch Super Retina XDR","camera":"48MP Fusion + 12MP Ultra-wide","battery":"Lên đến 22 giờ","frame":"Nhôm tái chế 100%","port":"USB-C","waterResistance":"IP68"}', 19990000.00, 3, 12, N'https://images.unsplash.com/photo-1510557880182-3d4d3cba35a5?auto=format&fit=crop&w=800&q=80', N'iPhone 16 thiết kế đa sắc màu, Camera Control cải tiến.', NULL, N'IP16-128-WHT', 22990000.00),
    (23, 6, N'iPhone 16', N'iPhone 16', N'Đen', N'256GB', N'{"chip":"Apple A18","ram":"8GB","screen":"6,1 inch Super Retina XDR","camera":"48MP Fusion + 12MP Ultra-wide","battery":"Lên đến 22 giờ","frame":"Nhôm tái chế 100%","port":"USB-C","waterResistance":"IP68"}', 22990000.00, 32, 12, N'https://images.unsplash.com/photo-1592750475338-74b7b21085ab?auto=format&fit=crop&w=800&q=80', N'iPhone 16 Đen sang trọng, dung lượng 256GB thoải mái lưu trữ.', NULL, N'IP16-256-BLK', 25990000.00),
    (24, 7, N'iPhone 15', N'iPhone 15', N'Hồng', N'128GB', N'{"chip":"Apple A16 Bionic","ram":"6GB","screen":"6,1 inch Super Retina XDR","camera":"48MP Main + 12MP Ultra-wide","battery":"Lên đến 20 giờ","frame":"Nhôm bền bỉ","port":"USB-C","waterResistance":"IP68"}', 14990000.00, 5, 12, N'https://images.unsplash.com/photo-1574944985070-8f3ebc6b79d2?auto=format&fit=crop&w=800&q=80', N'iPhone 15 Hồng Pastel trẻ trung, Dynamic Island thời thượng.', NULL, N'IP15-128-PNK', 18990000.00),
    (25, 8, N'iPhone 14', N'iPhone 14', N'Tím', N'128GB', N'{"chip":"Apple A15 Bionic","ram":"6GB","screen":"6,1 inch Super Retina XDR","camera":"12MP kép","battery":"Lên đến 20 giờ","frame":"Nhôm chuẩn hàng không vũ trụ","port":"Lightning","waterResistance":"IP68"}', 12990000.00, 12, 12, N'https://images.unsplash.com/photo-1510557880182-3d4d3cba35a5?auto=format&fit=crop&w=800&q=80', N'iPhone 14 màu tím quyến rũ, pin bền bỉ cả ngày dài.', NULL, N'IP14-128-PUR', 16990000.00),
    (26, 9, N'iPhone SE', N'iPhone SE', N'Đỏ', N'128GB', N'{"chip":"Apple A15 Bionic","ram":"4GB","screen":"4,7 inch Retina HD","camera":"12MP đơn","battery":"Lên đến 15 giờ","frame":"Nhôm & kính","port":"Lightning","waterResistance":"IP67"}', 10990000.00, 0, 12, N'https://images.unsplash.com/photo-1592750475338-74b7b21085ab?auto=format&fit=crop&w=800&q=80', N'iPhone SE PRODUCT(RED) nhỏ gọn cầm vừa vặn trong lòng bàn tay.', 0, N'IPSE-128-RED', 12990000.00);
    SET IDENTITY_INSERT [Products] OFF;
    DBCC CHECKIDENT ('[Products]', RESEED);
    PRINT N'[OK] Đã chèn dữ liệu mẫu cho Products (26 dòng).';
END
GO

-- Dữ liệu bảng Customers (5 bản ghi chuẩn hóa phục vụ chấm đồ án)
IF NOT EXISTS (SELECT 1 FROM [Customers])
BEGIN
    SET IDENTITY_INSERT [Customers] ON;
    INSERT INTO [Customers] (Id, UserId, FullName, Email, PhoneNumber, Address, CreatedAt, TotalSpent, RewardPoints, MembershipTier) VALUES
    (1, N'424cea8a-5a5c-4d04-b6df-ab70a402f3b9', N'Võ Văn Nghé', N'nghecute@gmail.com', N'0982726262', N'123 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh', '2026-10-25 15:15:53.346', 0.00, 0, N'Member'),
    (2, N'b1111111-1111-1111-1111-111111111111', N'Nguyễn Thị Mai', N'mainguyen@gmail.com', N'0912345678', N'456 Lê Duẩn, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh', '2026-10-26 10:00:00.000', 0.00, 0, N'Member'),
    (3, N'b2222222-2222-2222-2222-222222222222', N'Trần Văn Hùng', N'hungtran@gmail.com', N'0988776655', N'789 Nguyễn Văn Linh, Phường Tân Phong, Quận 7, TP. Hồ Chí Minh', '2026-10-26 11:00:00.000', 0.00, 0, N'Member'),
    (4, N'b3333333-3333-3333-3333-333333333333', N'Lê Hoàng Long', N'longle@gmail.com', N'0903284116', N'246 Hai Bà Trưng, Phường Tân Định, Quận 1, TP. Hồ Chí Minh', '2026-10-27 09:00:00.000', 0.00, 0, N'Member'),
    (5, N'b4444444-4444-4444-4444-444444444444', N'Phạm Thu Thảo', N'thaopham@gmail.com', N'0918650293', N'135 Võ Văn Tần, Phường Võ Thị Sáu, Quận 3, TP. Hồ Chí Minh', '2026-10-28 08:30:00.000', 0.00, 0, N'Member');
    SET IDENTITY_INSERT [Customers] OFF;
    DBCC CHECKIDENT ('[Customers]', RESEED);
    PRINT N'[OK] Đã chèn dữ liệu mẫu cho Customers (5 dòng).';
END
GO

-- Dữ liệu bảng Orders (16 bản ghi)
IF NOT EXISTS (SELECT 1 FROM [Orders])
BEGIN
    SET IDENTITY_INSERT [Orders] ON;
    INSERT INTO [Orders] (Id, CustomerId, OrderDate, TotalAmount, OrderStatus, PaymentMethod, ShippingAddress, Notes, OrderCode, PaymentStatus, ShippingUnit, TrackingNumber, EstimatedDeliveryDate) VALUES
    (1, 1, '2026-10-16 16:39:52.876', 34990000.00, N'Completed', N'BankTransfer', N'123 Phố Huế, Quận Hai Bà Trưng, Hà Nội', N'Giao hàng trong giờ hành chính, bọc chống sốc cẩn thận.', NULL, N'Chưa thanh toán', NULL, NULL, NULL),
    (2, 2, '2026-10-18 16:39:52.876', 28990000.00, N'Shipping', N'COD', N'456 Nguyễn Thị Minh Khai, Quận 1, TP.HCM', N'Gọi điện trước 15 phút khi đến giao.', NULL, N'Chưa thanh toán', NULL, NULL, NULL),
    (3, 4, '2026-10-24 16:02:24.993', 16990000.00, N'Cancelled', N'BankTransfer', N'273 An Dương Vương ,Phường Chợ Quán ,TP. Hồ Chí Minh', N'[Hủy tự động bởi Hệ thống]: Quá hạn 15 phút giữ chỗ chưa thanh toán.', NULL, N'Chưa thanh toán', NULL, NULL, NULL),
    (4, 4, '2026-10-24 16:28:21.296', 1148070000.00, N'Shipping', N'COD', N'273 An Dương Vương ,Phường Chợ Quán ,TP. Hồ Chí Minh', NULL, NULL, N'Chưa thanh toán', NULL, NULL, NULL),
    (5, 1, '2026-10-24 17:58:02.993', 34990000.00, N'Cancelled', N'BankTransfer', N'Test Auto Cleanup', NULL, NULL, N'Chưa thanh toán', NULL, NULL, NULL),
    (6, 1, '2026-10-24 17:59:04.716', 34990000.00, N'Cancelled', N'BankTransfer', N'Test Auto Cleanup', NULL, NULL, N'Chưa thanh toán', NULL, NULL, NULL),
    (7, 4, '2026-10-24 22:45:30.034', 25990000.00, N'Cancelled', N'COD', N'273 An Dương Vương ,Phường Chợ Quán ,TP. Hồ Chí Minh', NULL, NULL, N'Chưa thanh toán', NULL, NULL, NULL),
    (8, 4, '2026-10-24 23:24:31.259', 67960000.00, N'Cancelled', N'BankTransfer', N'273 An Dương Vương ,Phường Chợ Quán ,TP. Hồ Chí Minh', NULL, NULL, N'Chưa thanh toán', NULL, NULL, NULL),
    (9, 4, '2026-10-25 14:12:07.901', 19290000.00, N'Cancelled', N'COD', N'273 An Dương Vương ,Phường Chợ Quán ,TP. Hồ Chí Minh', NULL, NULL, N'Chưa thanh toán', NULL, NULL, NULL),
    (10, 4, '2026-10-25 15:05:43.754', 19290000.00, N'Confirmed', N'COD', N'273 An Dương Vương ,Phường Chợ Quán ,TP. Hồ Chí Minh', NULL, NULL, N'Đã thanh toán', N'Viettel Post', N'gh111111111', NULL),
    (11, 1, '2026-10-24 10:05:00.000', 49980000.00, N'Confirmed', N'VNPay', N'28 Nguyễn Thị Minh Khai, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh', N'Đơn trực tuyến. Giao nhanh trước 17h.', N'DH261024-018', N'Đã thanh toán', N'GHN', NULL, '2026-10-26 00:00:00.000'),
    (12, 5, '2026-10-24 10:29:00.000', 19990000.00, N'Pending', N'COD', N'45 Hai Bà Trưng, Phường Đa Kao, Quận 1, TP. Hồ Chí Minh', N'COD - Không giữ thanh toán', N'DH261024-020', N'Chưa thanh toán', NULL, NULL, NULL),
    (13, 6, '2026-10-24 10:25:00.000', 24990000.00, N'Cancelled', N'VNPay', N'78 Nam Kỳ Khởi Nghĩa, Quận 3, TP. Hồ Chí Minh', N'Đơn online giữ chỗ 15 phút | [Hủy tự động bởi Hệ thống]: Quá hạn 15 phút giữ chỗ chưa thanh toán.', N'DH261024-019', N'Chờ thanh toán', NULL, NULL, NULL),
    (14, 7, '2026-10-24 09:56:00.000', 22990000.00, N'Shipping', N'COD', N'15 Võ Văn Tần, Quận 3, TP. Hồ Chí Minh', N'Đang giao hàng', N'DH261024-017', N'Chưa thanh toán', N'Giao Hàng Nhanh', N'GHN-0017-8821', '2026-10-25 00:00:00.000'),
    (15, 8, '2026-10-24 09:40:00.000', 14990000.00, N'Completed', N'Chuyển khoản', N'90 Pasteur, Quận 1, TP. Hồ Chí Minh', N'Khách đã nhận máy nguyên vẹn', N'DH261024-016', N'Đã thanh toán', N'AhaMove', N'AHA-2410-0940', NULL),
    (16, 9, '2026-10-24 09:20:12.000', 24990000.00, N'Cancelled', N'VNPay', N'12 Nguyễn Văn Trỗi, Quận Phú Nhuận, TP. Hồ Chí Minh', N'Tự động · Quá 15 phút', N'DH261024-015', N'Hết hạn', NULL, NULL, NULL);
    SET IDENTITY_INSERT [Orders] OFF;
    DBCC CHECKIDENT ('[Orders]', RESEED);
    PRINT N'[OK] Đã chèn dữ liệu mẫu cho Orders (16 dòng).';
END
GO

-- Dữ liệu bảng ProductImeis (49 bản ghi)
IF NOT EXISTS (SELECT 1 FROM [ProductImeis])
BEGIN
    SET IDENTITY_INSERT [ProductImeis] ON;
    INSERT INTO [ProductImeis] (Id, ProductId, Imei, SerialNumber, Status, OrderId, ImportDate, ExportDate, Notes) VALUES
    (1, 1, N'358912345678901', N'F2LLW9840D3', N'Sold', NULL, '2026-10-24 16:48:14.680', NULL, N'LÃ´ hÃ ng chÃ­nh hÃ£ng Apple VN/A - Äá»£t 1'),
    (2, 1, N'358912345678902', N'F2LLW9840D4', N'InStock', NULL, '2026-10-24 16:48:14.680', NULL, N'LÃ´ hÃ ng chÃ­nh hÃ£ng Apple VN/A - Äá»£t 1'),
    (3, 1, N'358912345678903', N'F2LLW9840D5', N'InStock', NULL, '2026-10-24 16:48:14.680', NULL, N'LÃ´ hÃ ng chÃ­nh hÃ£ng Apple VN/A - Äá»£t 1'),
    (4, 1, N'358912345678900', N'F2LLW9840D0', N'Sold', 1, '2026-10-24 16:48:14.680', '2026-10-24 16:48:14.696', N'ÄÃ£ xuáº¥t cho ÄÆ¡n hÃ ng sá»‘ #1'),
    (5, 2, N'358912345678911', N'F2LMK1290A1', N'InStock', NULL, '2026-10-24 16:48:14.680', NULL, N'HÃ ng VN/A má»›i 100% nguyÃªn seal'),
    (6, 2, N'358912345678912', N'F2LMK1290A2', N'InStock', NULL, '2026-10-24 16:48:14.680', NULL, N'HÃ ng VN/A má»›i 100% nguyÃªn seal'),
    (7, 3, N'358912345678921', N'F2LNQ5560B1', N'InStock', NULL, '2026-10-24 16:48:14.680', NULL, N'HÃ ng VN/A má»›i 100% nguyÃªn seal'),
    (8, 3, N'358912345678922', N'F2LNQ5560B2', N'Reserved', 2, '2026-10-24 16:48:14.680', NULL, N'Äang giá»¯ chá»— cho ÄÆ¡n hÃ ng sá»‘ #2'),
    (9, 6, N'357890123456701', N'DNPXK9910C1', N'InStock', NULL, '2026-10-24 16:48:14.680', NULL, N'HÃ ng VN/A chÃ­nh hÃ£ng FPT Synnex'),
    (10, 6, N'357890123456702', N'DNPXK9910C2', N'InStock', NULL, '2026-10-24 16:48:14.680', NULL, N'HÃ ng VN/A chÃ­nh hÃ£ng FPT Synnex'),
    (11, 11, N'356789012345601', N'G6TPM3320X1', N'InStock', NULL, '2026-10-24 16:48:14.683', NULL, N'MÃ¡y Like New 99% nguyÃªn báº£n zin Ã¡p suáº¥t'),
    (12, 1, N'359999211504334', N'S211504334', N'InStock', NULL, '2026-10-24 18:28:02.936', NULL, NULL),
    (13, 1, N'359999665222823', N'S665222823', N'InStock', NULL, '2026-10-24 18:28:02.936', NULL, NULL),
    (14, 1, N'359999764594280', N'S764594280', N'InStock', NULL, '2026-10-24 18:29:04.673', NULL, NULL),
    (15, 1, N'359999108349812', N'S108349812', N'InStock', NULL, '2026-10-24 18:29:04.673', NULL, NULL),
    (16, 4, N'358900000000401', N'VN4A8901', N'InStock', NULL, '2026-10-25 14:31:14.110', NULL, N'Lô hàng phân phối chính hãng Apple VN/A'),
    (17, 4, N'358900000000402', N'VN4A8902', N'InStock', NULL, '2026-10-25 14:31:14.110', NULL, N'Lô hàng phân phối chính hãng Apple VN/A'),
    (18, 5, N'358900000000501', N'VN5A8901', N'InStock', NULL, '2026-10-25 14:31:14.116', NULL, N'Lô hàng phân phối chính hãng Apple VN/A'),
    (19, 5, N'358900000000502', N'VN5A8902', N'InStock', NULL, '2026-10-25 14:31:14.116', NULL, N'Lô hàng phân phối chính hãng Apple VN/A'),
    (20, 7, N'358900000000701', N'VN7A8901', N'InStock', NULL, '2026-10-25 14:31:14.116', NULL, N'Lô hàng phân phối chính hãng Apple VN/A'),
    (21, 7, N'358900000000702', N'VN7A8902', N'InStock', NULL, '2026-10-25 14:31:14.116', NULL, N'Lô hàng phân phối chính hãng Apple VN/A'),
    (22, 8, N'358900000000801', N'VN8A8901', N'Reserved', 10, '2026-10-25 14:31:14.116', NULL, N'Lô hàng phân phối chính hãng Apple VN/A'),
    (23, 8, N'358900000000802', N'VN8A8902', N'InStock', NULL, '2026-10-25 14:31:14.116', NULL, N'Lô hàng phân phối chính hãng Apple VN/A'),
    (24, 9, N'358900000000901', N'VN9A8901', N'InStock', NULL, '2026-10-25 14:31:14.123', NULL, N'Lô hàng phân phối chính hãng Apple VN/A'),
    (25, 9, N'358900000000902', N'VN9A8902', N'InStock', NULL, '2026-10-25 14:31:14.123', NULL, N'Lô hàng phân phối chính hãng Apple VN/A'),
    (26, 10, N'358900000001001', N'VN10A8901', N'InStock', NULL, '2026-10-25 14:31:14.123', NULL, N'Lô hàng phân phối chính hãng Apple VN/A'),
    (27, 10, N'358900000001002', N'VN10A8902', N'InStock', NULL, '2026-10-25 14:31:14.123', NULL, N'Lô hàng phân phối chính hãng Apple VN/A'),
    (28, 12, N'358900000001201', N'VN12A8901', N'InStock', NULL, '2026-10-25 14:31:14.123', NULL, N'Lô hàng phân phối chính hãng Apple VN/A'),
    (29, 12, N'358900000001202', N'VN12A8902', N'InStock', NULL, '2026-10-25 14:31:14.123', NULL, N'Lô hàng phân phối chính hãng Apple VN/A'),
    (30, 13, N'358900000001301', N'VN13A8901', N'InStock', NULL, '2026-10-25 14:31:14.123', NULL, N'Lô hàng phân phối chính hãng Apple VN/A'),
    (31, 13, N'358900000001302', N'VN13A8902', N'InStock', NULL, '2026-10-25 14:31:14.123', NULL, N'Lô hàng phân phối chính hãng Apple VN/A'),
    (32, 14, N'358900000001401', N'VN14A8901', N'InStock', NULL, '2026-10-25 14:31:14.123', NULL, N'Lô hàng phân phối chính hãng Apple VN/A'),
    (33, 14, N'358900000001402', N'VN14A8902', N'InStock', NULL, '2026-10-25 14:31:14.123', NULL, N'Lô hàng phân phối chính hãng Apple VN/A'),
    (34, 15, N'358900000001501', N'VN15A8901', N'InStock', NULL, '2026-10-25 14:31:14.123', NULL, N'Lô hàng phân phối chính hãng Apple VN/A'),
    (35, 15, N'358900000001502', N'VN15A8902', N'InStock', NULL, '2026-10-25 14:31:14.123', NULL, N'Lô hàng phân phối chính hãng Apple VN/A'),
    (36, 16, N'358900000001601', N'VN16A8901', N'InStock', NULL, '2026-10-25 14:31:14.130', NULL, N'Lô hàng phân phối chính hãng Apple VN/A'),
    (37, 16, N'358900000001602', N'VN16A8902', N'InStock', NULL, '2026-10-25 14:31:14.130', NULL, N'Lô hàng phân phối chính hãng Apple VN/A'),
    (38, 17, N'358900000001701', N'VN17A8901', N'InStock', NULL, '2026-10-25 14:31:14.133', NULL, N'Lô hàng phân phối chính hãng Apple VN/A'),
    (39, 17, N'358900000001702', N'VN17A8902', N'InStock', NULL, '2026-10-25 14:31:14.133', NULL, N'Lô hàng phân phối chính hãng Apple VN/A'),
    (40, 18, N'358900000001801', N'VN18A8901', N'InStock', NULL, '2026-10-25 14:31:14.136', NULL, N'Lô hàng phân phối chính hãng Apple VN/A'),
    (41, 18, N'358900000001802', N'VN18A8902', N'InStock', NULL, '2026-10-25 14:31:14.136', NULL, N'Lô hàng phân phối chính hãng Apple VN/A'),
    (42, 19, N'356938035643819', N'F17B2K8N4Q', N'InStock', NULL, '2026-10-22 00:00:00.000', NULL, NULL),
    (43, 19, N'356938035643835', N'F17B2K8N5R', N'InStock', NULL, '2026-10-22 00:00:00.000', NULL, NULL),
    (44, 19, N'356938035643801', N'F17B2K8N3P', N'Reserved', 11, '2026-10-22 00:00:00.000', NULL, NULL),
    (45, 19, N'356938035643777', N'F17B2K8M9T', N'Sold', NULL, '2026-10-05 00:00:00.000', '2026-10-08 00:00:00.000', NULL),
    (46, 19, N'356938035643793', N'F17B2K8N2H', N'Sold', NULL, '2026-10-22 00:00:00.000', '2026-10-24 00:00:00.000', NULL),
    (47, 19, N'356938035643728', N'F17B2K8L6D', N'Defective', NULL, '2026-10-02 00:00:00.000', '2026-10-06 00:00:00.000', N'Lỗi nguồn thu hồi theo phiếu BH261006-002'),
    (48, 22, N'352410987654321', N'G16W4L2M8N', N'InStock', NULL, '2026-10-20 00:00:00.000', NULL, NULL),
    (49, 24, N'354910987654329', N'H15P6N3K7T', N'InStock', NULL, '2026-10-18 00:00:00.000', NULL, NULL),
    (50, 25, N'356891140001011', N'VN14A011', N'InStock', NULL, '2026-10-25 00:00:00.000', NULL, N'Lô hàng phân phối chính hãng Apple VN/A'),
    (51, 25, N'356891140001012', N'VN14A012', N'InStock', NULL, '2026-10-25 00:00:00.000', NULL, N'Lô hàng phân phối chính hãng Apple VN/A'),
    (52, 25, N'356891140001013', N'VN14A013', N'InStock', NULL, '2026-10-25 00:00:00.000', NULL, N'Lô hàng phân phối chính hãng Apple VN/A'),
    (53, 25, N'356891140001014', N'VN14A014', N'InStock', NULL, '2026-10-25 00:00:00.000', NULL, N'Lô hàng phân phối chính hãng Apple VN/A'),
    (54, 25, N'356891140001015', N'VN14A015', N'InStock', NULL, '2026-10-25 00:00:00.000', NULL, N'Lô hàng phân phối chính hãng Apple VN/A'),
    (55, 7,  N'356891150007011', N'VN15A011', N'InStock', NULL, '2026-10-25 00:00:00.000', NULL, N'Lô hàng phân phối chính hãng Apple VN/A'),
    (56, 7,  N'356891150007012', N'VN15A012', N'InStock', NULL, '2026-10-25 00:00:00.000', NULL, N'Lô hàng phân phối chính hãng Apple VN/A'),
    (57, 7,  N'356891150007013', N'VN15A013', N'InStock', NULL, '2026-10-25 00:00:00.000', NULL, N'Lô hàng phân phối chính hãng Apple VN/A'),
    (58, 27, N'356891160027011', N'VN16A011', N'InStock', NULL, '2026-10-25 00:00:00.000', NULL, N'Lô hàng phân phối chính hãng Apple VN/A'),
    (59, 27, N'356891160027012', N'VN16A012', N'InStock', NULL, '2026-10-25 00:00:00.000', NULL, N'Lô hàng phân phối chính hãng Apple VN/A'),
    (60, 27, N'356891160027013', N'VN16A013', N'InStock', NULL, '2026-10-25 00:00:00.000', NULL, N'Lô hàng phân phối chính hãng Apple VN/A');
    SET IDENTITY_INSERT [ProductImeis] OFF;
    DBCC CHECKIDENT ('[ProductImeis]', RESEED);
    PRINT N'[OK] Đã chèn dữ liệu mẫu cho ProductImeis (60 dòng).';

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

-- Dữ liệu bảng OrderDetails (16 bản ghi)
IF NOT EXISTS (SELECT 1 FROM [OrderDetails])
BEGIN
    SET IDENTITY_INSERT [OrderDetails] ON;
    INSERT INTO [OrderDetails] (Id, OrderId, ProductId, Quantity, UnitPrice, TotalPrice, ProductImeiId, ImeiNumber) VALUES
    (1, 1, 1, 1, 34990000.00, 34990000.00, 4, N'358912345678900'),
    (2, 2, 3, 1, 28990000.00, 28990000.00, 8, N'358912345678922'),
    (3, 3, 11, 1, 16990000.00, 16990000.00, NULL, NULL),
    (4, 4, 9, 30, 25490000.00, 764700000.00, NULL, NULL),
    (5, 4, 6, 13, 29490000.00, 383370000.00, NULL, NULL),
    (6, 7, 4, 1, 25990000.00, 25990000.00, NULL, NULL),
    (7, 8, 10, 4, 16990000.00, 67960000.00, NULL, NULL),
    (8, 9, 8, 1, 19290000.00, 19290000.00, NULL, NULL),
    (9, 10, 8, 1, 19290000.00, 19290000.00, 22, NULL),
    (10, 11, 19, 1, 24990000.00, 24990000.00, 44, N'356938035643801'),
    (11, 11, 19, 1, 24990000.00, 24990000.00, NULL, NULL),
    (12, 12, 22, 1, 19990000.00, 19990000.00, NULL, NULL),
    (13, 13, 19, 1, 24990000.00, 24990000.00, NULL, NULL),
    (14, 14, 23, 1, 22990000.00, 22990000.00, NULL, NULL),
    (15, 15, 24, 1, 14990000.00, 14990000.00, NULL, NULL),
    (16, 16, 19, 1, 24990000.00, 24990000.00, NULL, NULL);
    SET IDENTITY_INSERT [OrderDetails] OFF;
    DBCC CHECKIDENT ('[OrderDetails]', RESEED);
    PRINT N'[OK] Đã chèn dữ liệu mẫu cho OrderDetails (16 dòng).';
END
GO

-- Dữ liệu bảng Warranties (2 bản ghi)
IF NOT EXISTS (SELECT 1 FROM [Warranties])
BEGIN
    SET IDENTITY_INSERT [Warranties] ON;
    INSERT INTO [Warranties] (Id, CustomerId, ProductId, OrderDetailId, Imei, SerialNumber, OriginalImei, StartDate, EndDate, WarrantyType, Status, Notes, CreatedAt) VALUES
    (1, 1, 1, 1, N'358912345678900', N'F2LLW9840D0', NULL, '2026-10-21 16:48:14.716', '2027-10-21 16:48:14.716', N'ChÃ­nh hÃ£ng Apple Care 12 thÃ¡ng (AASP)', N'Active', N'KÃ­ch hoáº¡t báº£o hÃ nh Ä‘iá»‡n tá»­ tá»± Ä‘á»™ng khi Ä‘Æ¡n hÃ ng hoÃ n táº¥t', '2026-10-24 16:48:14.716'),
    (2, 2, 19, NULL, N'356938035643777', N'F17B2K8M9T', NULL, '2026-10-10 00:00:00.000', '2027-10-10 00:00:00.000', N'Chính hãng Apple Care 12 tháng', N'Active', N'Kích hoạt khi nhận máy đơn DH261008-042', '2026-10-25 15:15:53.450');
    SET IDENTITY_INSERT [Warranties] OFF;
    DBCC CHECKIDENT ('[Warranties]', RESEED);
    PRINT N'[OK] Đã chèn dữ liệu mẫu cho Warranties (2 dòng).';
END
GO

-- Dữ liệu bảng WarrantyClaims (2 bản ghi)
IF NOT EXISTS (SELECT 1 FROM [WarrantyClaims])
BEGIN
    SET IDENTITY_INSERT [WarrantyClaims] ON;
    INSERT INTO [WarrantyClaims] (Id, WarrantyId, ClaimDate, IssueDescription, TechnicianNotes, ActionTaken, OldImei, NewImei, Status, ResolvedDate, CreatedBy, ClaimCode) VALUES
    (1, 1, '2026-10-24 18:29:04.696', N'Soc man hinh', N'Doi may moi 1-1', N'ExchangeDevice', NULL, N'358912345678901', N'Resolved', NULL, NULL, NULL),
    (2, 2, '2026-10-24 09:15:00.000', N'Máy tự tắt nguồn 3-4 lần/ngày khi sử dụng bình thường.', N'Đã tái hiện lỗi, panic log xác nhận lỗi bo mạch. Vỏ máy nguyên vẹn, chỉ thị chất lỏng không đổi màu. Đủ điều kiện đề xuất đổi mới.', N'Đổi máy mới (Cùng SKU - Chờ Quản lý duyệt)', N'356938035643777', N'356938035643835', N'Tiếp nhận', NULL, N'Lê Thảo Vy', N'BH261024-006');
    SET IDENTITY_INSERT [WarrantyClaims] OFF;
    DBCC CHECKIDENT ('[WarrantyClaims]', RESEED);
    PRINT N'[OK] Đã chèn dữ liệu mẫu cho WarrantyClaims (2 dòng).';
END
GO

-- Dữ liệu bảng AuditLogs (30 bản ghi)
IF NOT EXISTS (SELECT 1 FROM [AuditLogs])
BEGIN
    SET IDENTITY_INSERT [AuditLogs] ON;
    INSERT INTO [AuditLogs] (Id, UserId, UserName, Action, EntityName, RecordId, OldValues, NewValues, IpAddress, Timestamp) VALUES
    (1, N'3a3c3104-5561-4523-a693-800cd0946ef7', N'Administrator', N'SYSTEM_INIT_V2', N'DatabaseSchema', N'V2.0', NULL, N'{"version":"2.0","modules":["ProductImeis","Warranties","WarrantyClaims","AuditLogs","Concurrency"]}', N'127.0.0.1', '2026-10-24 16:48:14.720'),
    (2, NULL, N'OrderHoldCleanupWorker', N'SYSTEM_CLEANUP', N'Orders', N'3', NULL, N'{"OrderId":3,"Action":"AutoCancel","ReleasedImeis":0}', N'127.0.0.1', '2026-10-24 17:09:34.980'),
    (3, NULL, NULL, N'IMPORT_IMEI', N'ProductImeis', N'1', NULL, N'Test batch import 2 IMEIs', NULL, '2026-10-24 18:28:02.940'),
    (4, NULL, NULL, N'AUTO_CANCEL_EXPIRED_ORDER', N'Orders', N'5', NULL, N'Thu hoi IMEI 358912345678901', NULL, '2026-10-24 18:28:02.996'),
    (5, NULL, NULL, N'IMPORT_IMEI', N'ProductImeis', N'1', NULL, N'Test batch import 2 IMEIs', NULL, '2026-10-24 18:29:04.676'),
    (6, NULL, NULL, N'EXCHANGE_1TO1', N'Warranties', N'1', NULL, N'1 Doi 1: Xuat may moi 358912345678901', NULL, '2026-10-24 18:29:04.700'),
    (7, NULL, NULL, N'AUTO_CANCEL_EXPIRED_ORDER', N'Orders', N'6', NULL, N'Thu hoi IMEI 358912345678902', NULL, '2026-10-24 18:29:04.716'),
    (8, N'424cea8a-5a5c-4d04-b6df-ab70a402f3b9', N'nghecute', N'PLACE_ORDER', N'Orders', N'7', NULL, N'{"OrderId":7,"TotalAmount":25990000.00,"PaymentMethod":"COD"}', N'::1', '2026-10-24 22:45:30.166'),
    (9, N'424cea8a-5a5c-4d04-b6df-ab70a402f3b9', N'nghecute', N'PLACE_ORDER', N'Orders', N'8', NULL, N'{"OrderId":8,"TotalAmount":67960000.00,"PaymentMethod":"BankTransfer"}', N'::1', '2026-10-24 23:24:31.519'),
    (10, N'424cea8a-5a5c-4d04-b6df-ab70a402f3b9', N'nghecute', N'PLACE_ORDER', N'Orders', N'9', NULL, N'{"OrderId":9,"TotalAmount":19290000.00,"PaymentMethod":"COD"}', N'::1', '2026-10-25 14:12:08.156'),
    (11, N'424cea8a-5a5c-4d04-b6df-ab70a402f3b9', N'nghecute', N'PLACE_ORDER', N'Orders', N'10', NULL, N'{"OrderId":10,"TotalAmount":19290000.00,"PaymentMethod":"COD"}', N'::1', '2026-10-25 15:05:43.994'),
    (12, NULL, N'OrderHoldCleanupWorker', N'Tự động hủy quá hạn', N'Đơn hàng', N'DH261024-015', N'{"orderStatus":"Chờ xác nhận","availableStock":17,"imeiStatus":"Giữ chỗ online"}', N'{"orderStatus":"Đã hủy","availableStock":18,"imeiStatus":"Sẵn bán","holdStatus":"Đã giải phóng"}', N'Nội bộ hệ thống', '2026-10-24 09:36:00.000'),
    (13, NULL, N'Lê Thảo Vy', N'Xác nhận đơn hàng', N'Đơn hàng', N'DH261024-018', N'{"orderStatus":"Chờ xác nhận"}', N'{"orderStatus":"Đã xác nhận"}', N'10.20.0.23', '2026-10-24 10:12:10.000'),
    (14, NULL, N'Lê Thảo Vy', N'Đề xuất đổi máy', N'Phiếu bảo hành', N'BH261024-006', N'{"status":"Chờ kiểm tra"}', N'{"status":"Tiếp nhận","proposal":"Đổi máy mới 1-1"}', N'10.20.0.23', '2026-10-24 10:18:24.000'),
    (15, NULL, N'Nguyễn Đức Huy', N'Cập nhật kết luận kỹ thuật', N'Phiếu bảo hành', N'BH261024-006', NULL, N'{"technicianNotes":"Đã kiểm tra bo mạch, lỗi phần cứng NSX đủ điều kiện"}', N'10.20.0.18', '2026-10-24 10:05:38.000'),
    (16, NULL, N'Nguyễn Đức Huy', N'Xuất kho IMEI', N'Thiết bị IMEI', N'356938035643793', N'{"status":"Reserved"}', N'{"status":"Sold","exportDate":"2026-10-24"}', N'10.20.0.18', '2026-10-24 09:42:06.000'),
    (17, NULL, N'Hoàng Linh', N'Cập nhật thông tin sản phẩm', N'Biến thể', N'IP17-256-BLK', N'{"price":25990000}', N'{"price":24990000}', N'10.20.0.10', '2026-10-23 16:45:19.000'),
    (18, NULL, N'OrderHoldCleanupWorker', N'SYSTEM_CLEANUP', N'Orders', N'13', NULL, N'{"OrderId":13,"Action":"AutoCancel","ReleasedImeis":0}', N'127.0.0.1', '2026-10-25 15:16:50.181'),
    (19, N'3a3c3104-5561-4523-a693-800cd0946ef7', N'admin', N'FULFILL_ORDER', N'Orders', N'10', N'Status=Shipping, Payment=Chưa thanh toán', N'Status=Confirmed, Payment=Chưa thanh toán, Tracking=', N'::1', '2026-10-25 16:04:21.730'),
    (20, N'3a3c3104-5561-4523-a693-800cd0946ef7', N'admin', N'FULFILL_ORDER', N'Orders', N'10', N'Status=Confirmed, Payment=Chưa thanh toán', N'Status=Confirmed, Payment=Chưa thanh toán, Tracking=', N'::1', '2026-10-25 16:04:30.330'),
    (21, N'3a3c3104-5561-4523-a693-800cd0946ef7', N'admin', N'FULFILL_ORDER', N'Orders', N'10', N'Status=Confirmed, Payment=Chưa thanh toán', N'Status=Confirmed, Payment=Chưa thanh toán, Tracking=', N'::1', '2026-10-25 16:04:33.842'),
    (22, N'3a3c3104-5561-4523-a693-800cd0946ef7', N'admin', N'FULFILL_ORDER', N'Orders', N'10', N'Status=Confirmed, Payment=Chưa thanh toán', N'Status=Confirmed, Payment=Chưa thanh toán, Tracking=gh111111111', N'::1', '2026-10-25 16:04:56.225'),
    (23, N'3a3c3104-5561-4523-a693-800cd0946ef7', N'admin', N'FULFILL_ORDER', N'Orders', N'10', N'Status=Confirmed, Payment=Chưa thanh toán', N'Status=Confirmed, Payment=Đã thanh toán, Tracking=gh111111111', N'::1', '2026-10-25 16:05:03.019'),
    (24, N'3a3c3104-5561-4523-a693-800cd0946ef7', N'admin', N'FULFILL_ORDER', N'Orders', N'10', N'Status=Confirmed, Payment=Đã thanh toán', N'Status=Confirmed, Payment=Đã thanh toán, Tracking=gh111111111', N'::1', '2026-10-25 16:05:21.998'),
    (25, N'3a3c3104-5561-4523-a693-800cd0946ef7', N'admin', N'FULFILL_ORDER', N'Orders', N'10', N'Status=Confirmed, Payment=Đã thanh toán', N'Status=Confirmed, Payment=Đã thanh toán, Tracking=gh111111111', N'::1', '2026-10-25 16:05:29.370'),
    (26, N'3a3c3104-5561-4523-a693-800cd0946ef7', N'admin', N'FULFILL_ORDER', N'Orders', N'10', N'Status=Confirmed, Payment=Đã thanh toán', N'Status=Confirmed, Payment=Đã thanh toán, Tracking=gh111111111', N'::1', '2026-10-25 16:05:31.057'),
    (27, N'3a3c3104-5561-4523-a693-800cd0946ef7', N'admin', N'FULFILL_ORDER', N'Orders', N'10', N'Status=Confirmed, Payment=Đã thanh toán', N'Status=Confirmed, Payment=Đã thanh toán, Tracking=gh111111111', N'::1', '2026-10-25 16:05:31.570'),
    (28, N'3a3c3104-5561-4523-a693-800cd0946ef7', N'admin', N'FULFILL_ORDER', N'Orders', N'10', N'Status=Confirmed, Payment=Đã thanh toán', N'Status=Confirmed, Payment=Đã thanh toán, Tracking=gh111111111', N'::1', '2026-10-25 16:05:31.980'),
    (29, N'3a3c3104-5561-4523-a693-800cd0946ef7', N'admin', N'FULFILL_ORDER', N'Orders', N'10', N'Status=Confirmed, Payment=Đã thanh toán', N'Status=Confirmed, Payment=Đã thanh toán, Tracking=gh111111111', N'::1', '2026-10-25 16:05:32.376'),
    (30, N'3a3c3104-5561-4523-a693-800cd0946ef7', N'admin', N'FULFILL_ORDER', N'Orders', N'10', N'Status=Confirmed, Payment=Đã thanh toán', N'Status=Confirmed, Payment=Đã thanh toán, Tracking=gh111111111', N'::1', '2026-10-25 16:05:32.770');
    SET IDENTITY_INSERT [AuditLogs] OFF;
    DBCC CHECKIDENT ('[AuditLogs]', RESEED);
    PRINT N'[OK] Đã chèn dữ liệu mẫu cho AuditLogs (30 dòng).';
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

-- 3. Đảm bảo user 'manager01' (Password: manager123)
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
        N'AQAAAAIAAYagAAAAEFaU3Ag6YGld/iMaXuO//zG2Ch/x45TX2E8Yp150bBIB9lm8N9jPbF0fvqMSqz5RiA==', -- Pass: admin123 / manager dùng chung format
        NEWID(),
        NEWID(),
        0, 0, 1, 0, 1, GETDATE()
    );
    INSERT INTO AspNetUserRoles (UserId, RoleId)
    SELECT N'user-manager-01', Id FROM AspNetRoles WHERE Name = 'Manager';
END
GO

-- 4. Đảm bảo user 'staff01' (Password: staff123)
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

-- 5. Đảm bảo user 'cashier01' (Password: cashier123)
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

-- 6. Đảm bảo user 'shipper01' (Password: shipper123)
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
PRINT N'>>> HOÀN TẤT THIẾT LẬP CƠ SỞ DỮ LIỆU IPHONESTOREDB COMPLETE V3.0!';
PRINT N'============================================================================';
GO
