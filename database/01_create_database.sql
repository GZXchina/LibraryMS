/* ============================================================
   图书馆管理系统 —— 数据库创建脚本（SQL Server 2012+）
   作者  : Class-Design 项目
   说明  : 非破坏性 —— 已存在的库/表/数据不会被删除
   重置  : 如需清空重建，运行 99_reset_database.sql
   对应  : 基本要求(1) 参数化+事务 / (2) 编号与用户名唯一 / (3) 关联号必须存在
   ============================================================ */

SET NOCOUNT ON;
GO

IF DB_ID(N'LibraryDB') IS NULL
BEGIN
    CREATE DATABASE LibraryDB;
END
GO

USE LibraryDB;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

/* ---------- 1. 读者类别表 ---------- */
IF OBJECT_ID(N'dbo.ReaderCategory', N'U') IS NULL
CREATE TABLE dbo.ReaderCategory
(
    CategoryId     INT          NOT NULL
        CONSTRAINT PK_ReaderCategory PRIMARY KEY,        -- 类别号（1-99，编号中显示为两位）
    CategoryName   NVARCHAR(20) NOT NULL
        CONSTRAINT UQ_ReaderCategory_Name UNIQUE,        -- 类别名不重复
    MaxBorrowCount INT          NOT NULL,                -- 可借数量
    MaxBorrowDays  INT          NOT NULL,                -- 可借天数
    CONSTRAINT CK_ReaderCategory_Count CHECK (MaxBorrowCount BETWEEN 1 AND 50),
    CONSTRAINT CK_ReaderCategory_Days  CHECK (MaxBorrowDays  BETWEEN 1 AND 365)
);
GO

/* ---------- 2. 读者信息表 ----------
   编号规则：'R' + 类别号2位 + 年份4位 + 年内序号3位，如 R012026001
   需求(2)：读者编号唯一 → 主键
   需求(3)：类别号必须已存在 → 外键 */
IF OBJECT_ID(N'dbo.Readers', N'U') IS NULL
CREATE TABLE dbo.Readers
(
    ReaderId     CHAR(10)     NOT NULL
        CONSTRAINT PK_Readers PRIMARY KEY,
    Name         NVARCHAR(20) NOT NULL
        CONSTRAINT CK_Readers_Name CHECK (LEN(Name) >= 1),
    Gender       NCHAR(1)     NOT NULL
        CONSTRAINT CK_Readers_Gender CHECK (Gender IN (N'男', N'女')),
    Phone        VARCHAR(20)  NULL,
    CategoryId   INT          NOT NULL
        CONSTRAINT FK_Readers_Category REFERENCES dbo.ReaderCategory(CategoryId),
    Status       NCHAR(2)     NOT NULL
        CONSTRAINT DF_Readers_Status DEFAULT (N'正常')
        CONSTRAINT CK_Readers_Status CHECK (Status IN (N'正常', N'注销')),
    RegisterDate DATE         NOT NULL
        CONSTRAINT DF_Readers_RegDate DEFAULT CAST(GETDATE() AS DATE)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Readers_CategoryId' AND object_id = OBJECT_ID(N'dbo.Readers'))
    CREATE INDEX IX_Readers_CategoryId ON dbo.Readers(CategoryId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Readers_Name' AND object_id = OBJECT_ID(N'dbo.Readers'))
    CREATE INDEX IX_Readers_Name ON dbo.Readers(Name);
GO

/* ---------- 3. 图书信息表 ---------- */
IF OBJECT_ID(N'dbo.Books', N'U') IS NULL
CREATE TABLE dbo.Books
(
    BookId       INT           NOT NULL
        CONSTRAINT PK_Books PRIMARY KEY IDENTITY(1,1),    -- 书号（自增）
    Title        NVARCHAR(100) NOT NULL,                  -- 图书名称
    Author       NVARCHAR(50)  NOT NULL,                  -- 作者
    Publisher    NVARCHAR(50)  NULL,                      -- 出版社
    Price        DECIMAL(10,2) NOT NULL
        CONSTRAINT CK_Books_Price CHECK (Price >= 0),     -- 单价非负
    IsAvailable  BIT           NOT NULL
        CONSTRAINT DF_Books_Available DEFAULT (1),        -- 1=在馆 0=借出（与借还事务同步）
    RegisterDate DATE          NOT NULL
        CONSTRAINT DF_Books_RegDate DEFAULT CAST(GETDATE() AS DATE)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Books_Title' AND object_id = OBJECT_ID(N'dbo.Books'))
    CREATE INDEX IX_Books_Title ON dbo.Books(Title);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Books_Author' AND object_id = OBJECT_ID(N'dbo.Books'))
    CREATE INDEX IX_Books_Author ON dbo.Books(Author);
GO

/* ---------- 4. 借阅信息表 ----------
   需求(3)：读者编号 / 书号必须已存在 → 两个外键
   状态不落冗余列：在借 = ReturnDate IS NULL；超期 = 在借 且 DueDate < 今天
   唯一过滤索引：同一本书不允许存在两条未归还记录（并发借书 DB 级兜底） */
IF OBJECT_ID(N'dbo.Borrows', N'U') IS NULL
CREATE TABLE dbo.Borrows
(
    BorrowId   INT      NOT NULL
        CONSTRAINT PK_Borrows PRIMARY KEY IDENTITY(1,1),
    ReaderId   CHAR(10) NOT NULL
        CONSTRAINT FK_Borrows_Readers REFERENCES dbo.Readers(ReaderId),
    BookId     INT      NOT NULL
        CONSTRAINT FK_Borrows_Books REFERENCES dbo.Books(BookId),
    BorrowDate DATE     NOT NULL
        CONSTRAINT DF_Borrows_BorrowDate DEFAULT CAST(GETDATE() AS DATE),
    DueDate    DATE     NOT NULL,                         -- 应还 = 借出 + 类别可借天数
    ReturnDate DATE     NULL,                             -- NULL=在借；非空=已还
    CONSTRAINT CK_Borrows_Due    CHECK (DueDate >= BorrowDate),
    CONSTRAINT CK_Borrows_Return CHECK (ReturnDate IS NULL OR ReturnDate >= BorrowDate)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Borrows_ReaderId' AND object_id = OBJECT_ID(N'dbo.Borrows'))
    CREATE INDEX IX_Borrows_ReaderId ON dbo.Borrows(ReaderId) INCLUDE (DueDate, ReturnDate);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Borrows_BookId' AND object_id = OBJECT_ID(N'dbo.Borrows'))
    CREATE INDEX IX_Borrows_BookId ON dbo.Borrows(BookId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Borrows_ActiveBook' AND object_id = OBJECT_ID(N'dbo.Borrows'))
    CREATE UNIQUE INDEX UQ_Borrows_ActiveBook ON dbo.Borrows(BookId) WHERE ReturnDate IS NULL;
GO

/* ---------- 5. 用户表 ----------
   需求(2)：用户名不允许重复 → UNIQUE
   密码：PBKDF2-SHA256 / 10 万次迭代 / 16 字节独立盐 / 32 字节哈希（见 docs/04） */
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
CREATE TABLE dbo.Users
(
    UserId        INT           NOT NULL
        CONSTRAINT PK_Users PRIMARY KEY IDENTITY(1,1),
    Username      NVARCHAR(20)  NOT NULL
        CONSTRAINT UQ_Users_Username UNIQUE
        CONSTRAINT CK_Users_Username CHECK (LEN(Username) BETWEEN 3 AND 20),
    PasswordHash  VARBINARY(32) NOT NULL,
    PasswordSalt  VARBINARY(16) NOT NULL,
    UserRole      VARCHAR(10)   NOT NULL
        CONSTRAINT CK_Users_Role CHECK (UserRole IN ('Admin', 'Reader')),
    ReaderId      CHAR(10)      NULL
        CONSTRAINT FK_Users_Readers REFERENCES dbo.Readers(ReaderId),  -- 读者账号关联；管理员为 NULL
    CreatedAt     DATETIME2     NOT NULL
        CONSTRAINT DF_Users_CreatedAt DEFAULT SYSDATETIME()
);
GO

/* ---------- 视图：借阅明细（查询/统计共用，避免应用层 N+1） ---------- */
IF OBJECT_ID(N'dbo.V_BorrowDetails', N'V') IS NOT NULL
    DROP VIEW dbo.V_BorrowDetails;
GO
CREATE VIEW dbo.V_BorrowDetails
AS
SELECT
    b.BorrowId,
    b.ReaderId, r.Name AS ReaderName, rc.CategoryName,
    b.BookId,   bk.Title AS BookTitle, bk.Author,
    b.BorrowDate, b.DueDate, b.ReturnDate,
    CASE
        WHEN b.ReturnDate IS NOT NULL                      THEN N'已还'
        WHEN b.DueDate < CAST(GETDATE() AS DATE)           THEN N'超期'
        ELSE N'在借'
    END AS Status,
    CASE
        WHEN b.ReturnDate IS NOT NULL AND DATEDIFF(DAY, b.DueDate, b.ReturnDate) > 0
            THEN DATEDIFF(DAY, b.DueDate, b.ReturnDate)
        ELSE NULL                                  -- 未超期（含提前归还）不计超期天数
    END AS OverdueDays
FROM dbo.Borrows b
JOIN dbo.Readers        r  ON r.ReaderId    = b.ReaderId
JOIN dbo.ReaderCategory rc ON rc.CategoryId = r.CategoryId
JOIN dbo.Books          bk ON bk.BookId     = b.BookId;
GO

/* ---------- 种子数据（仅在为空时插入，不覆盖已有数据） ---------- */
IF NOT EXISTS (SELECT 1 FROM dbo.ReaderCategory)
INSERT INTO dbo.ReaderCategory (CategoryId, CategoryName, MaxBorrowCount, MaxBorrowDays)
VALUES (1, N'教师',   10, 60),
       (2, N'学生',    5, 30),
       (3, N'研究生',  8, 45);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Books)
INSERT INTO dbo.Books (Title, Author, Publisher, Price)
VALUES (N'C# 程序设计教程',        N'张三',             N'清华大学出版社',   59.00),
       (N'数据库系统概论',        N'王珊',             N'高等教育出版社',   49.50),
       (N'算法导论',              N'Thomas H. Cormen', N'机械工业出版社',  128.00),
       (N'SQL Server 从入门到精通', N'李四',           N'人民邮电出版社',   79.00),
       (N'软件工程导论',          N'张海藩',           N'清华大学出版社',   45.00);
GO

PRINT N'数据库 LibraryDB 初始化完成。';
GO
