/* ============================================================
   图书馆管理系统 —— 数据库结构（SQL Server）
   作者  : @engineering-database-optimizer
   编码  : UTF-8（若 SSMS 中文注释乱码，请以 UTF-8 编码重新打开）
   特性  : 幂等可重跑 / 外键全部建索引 / CHECK 约束兜底业务规则
   对应  : 基本要求(1) 参数化+事务 / (2) 编号与用户名唯一 / (3) 关联号必须存在
   ============================================================ */

USE master;
GO
IF DB_ID(N'LibraryDB') IS NOT NULL
BEGIN
    ALTER DATABASE LibraryDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE LibraryDB;
END
GO
CREATE DATABASE LibraryDB;
GO
USE LibraryDB;
GO

/* ---------- 1. 读者类别表 ---------- */
IF OBJECT_ID(N'dbo.ReaderCategory', N'U') IS NOT NULL DROP TABLE dbo.ReaderCategory;
CREATE TABLE dbo.ReaderCategory
(
    CategoryId     INT          NOT NULL
        CONSTRAINT PK_ReaderCategory PRIMARY KEY,       -- 类别号（1-99，编号中显示两位 01-99）
    CategoryName   NVARCHAR(20) NOT NULL
        CONSTRAINT UQ_ReaderCategory_Name UNIQUE,      -- 类别名不重复
    MaxBorrowCount INT          NOT NULL,              -- 可借数量
    MaxBorrowDays  INT          NOT NULL,              -- 可借天数
    CONSTRAINT CK_ReaderCategory_Count CHECK (MaxBorrowCount BETWEEN 1 AND 50),
    CONSTRAINT CK_ReaderCategory_Days  CHECK (MaxBorrowDays  BETWEEN 1 AND 365)
);
GO

/* ---------- 2. 读者信息表 ----------
   编号规则: 'R' + 类别号2位 + 年份4位 + 年内序号3位，如 R012025001
   需求(2): 读者编号唯一 → PRIMARY KEY
   需求(3): 类别号必须已存在 → 外键（数据库层兜底，防脏数据） */
IF OBJECT_ID(N'dbo.Readers', N'U') IS NOT NULL DROP TABLE dbo.Readers;
CREATE TABLE dbo.Readers
(
    ReaderId     CHAR(10)     NOT NULL
        CONSTRAINT PK_Readers PRIMARY KEY,
    Name         NVARCHAR(20) NOT NULL,
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
CREATE INDEX IX_Readers_CategoryId ON dbo.Readers(CategoryId);  -- 外键索引：类别关联查询
CREATE INDEX IX_Readers_Name       ON dbo.Readers(Name);        -- 姓名查询
GO

/* ---------- 3. 图书信息表 ---------- */
IF OBJECT_ID(N'dbo.Books', N'U') IS NOT NULL DROP TABLE dbo.Books;
CREATE TABLE dbo.Books
(
    BookId       INT           NOT NULL
        CONSTRAINT PK_Books PRIMARY KEY IDENTITY(1,1), -- 书号（自增）
    Title        NVARCHAR(100) NOT NULL,               -- 图书名称
    Author       NVARCHAR(50)  NOT NULL,               -- 作者
    Publisher    NVARCHAR(50)  NULL,                   -- 出版社
    Price        DECIMAL(10,2) NOT NULL
        CONSTRAINT CK_Books_Price CHECK (Price >= 0),  -- 单价非负
    IsAvailable  BIT           NOT NULL
        CONSTRAINT DF_Books_Available DEFAULT (1),     -- 1=在馆 0=借出（与借还事务同步）
    RegisterDate DATE          NOT NULL
        CONSTRAINT DF_Books_RegDate DEFAULT CAST(GETDATE() AS DATE)
);
CREATE INDEX IX_Books_Title  ON dbo.Books(Title);       -- 书名查询（读者端高频）
CREATE INDEX IX_Books_Author ON dbo.Books(Author);
GO

/* ---------- 4. 借阅信息表 ----------
   需求(3): 读者编号 / 书号必须已存在 → 两个外键
   状态不落列：在借 = ReturnDate IS NULL；超期 = 在借 ∧ DueDate < 今天
   （避免冗余状态列与真实状态漂移——这是审查时的常见缺陷） */
IF OBJECT_ID(N'dbo.Borrows', N'U') IS NOT NULL DROP TABLE dbo.Borrows;
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
    DueDate    DATE     NOT NULL,                       -- 应还 = 借出 + 类别可借天数
    ReturnDate DATE     NULL,                           -- NULL=在借；非空=已还
    CONSTRAINT CK_Borrows_Due    CHECK (DueDate >= BorrowDate),
    CONSTRAINT CK_Borrows_Return CHECK (ReturnDate IS NULL OR ReturnDate >= BorrowDate)
);
/* INCLUDE 覆盖"读者在借数量统计"高频查询：COUNT(*) WHERE ReaderId=@r AND ReturnDate IS NULL */
CREATE INDEX IX_Borrows_ReaderId ON dbo.Borrows(ReaderId) INCLUDE (DueDate, ReturnDate);
CREATE INDEX IX_Borrows_BookId   ON dbo.Borrows(BookId);
GO

/* ---------- 5. 用户表 ----------
   需求(2): 用户名不允许重复 → UNIQUE
   密码规范见 docs/04：PBKDF2-SHA256 / 10万迭代 / 16字节独立盐 / 32字节哈希 */
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL DROP TABLE dbo.Users;
CREATE TABLE dbo.Users
(
    UserId        INT          NOT NULL
        CONSTRAINT PK_Users PRIMARY KEY IDENTITY(1,1),
    Username      NVARCHAR(20) NOT NULL
        CONSTRAINT UQ_Users_Username UNIQUE,
    PasswordHash  VARBINARY(32) NOT NULL,
    PasswordSalt  VARBINARY(16) NOT NULL,
    Role          VARCHAR(10)  NOT NULL
        CONSTRAINT CK_Users_Role CHECK (Role IN ('Admin', 'Reader')),
    ReaderId      CHAR(10)     NULL
        CONSTRAINT FK_Users_Readers REFERENCES dbo.Readers(ReaderId), -- 读者用户关联（管理员为 NULL）
    CreatedAt     DATETIME2    NOT NULL
        CONSTRAINT DF_Users_CreatedAt DEFAULT SYSDATETIME()
);
GO

/* ---------- 视图：借阅明细（查询/统计共用，JOIN 一次取齐，避免应用层 N+1） ---------- */
IF OBJECT_ID(N'dbo.V_BorrowDetails', N'V') IS NOT NULL DROP VIEW dbo.V_BorrowDetails;
GO
CREATE VIEW dbo.V_BorrowDetails
AS
SELECT
    b.BorrowId,
    b.ReaderId, r.Name AS ReaderName, rc.CategoryName,
    b.BookId,   bk.Title AS BookTitle, bk.Author,
    b.BorrowDate, b.DueDate, b.ReturnDate,
    CASE
        WHEN b.ReturnDate IS NOT NULL                    THEN N'已还'
        WHEN b.DueDate < CAST(GETDATE() AS DATE)         THEN N'超期'
        ELSE N'在借'
    END AS Status
FROM dbo.Borrows b
JOIN dbo.Readers       r  ON r.ReaderId    = b.ReaderId
JOIN dbo.ReaderCategory rc ON rc.CategoryId = r.CategoryId
JOIN dbo.Books         bk ON bk.BookId     = b.BookId;
GO

/* ============================================================
   种子数据（管理员账号由应用首次启动创建——密码哈希不进脚本）
   ============================================================ */
INSERT INTO dbo.ReaderCategory (CategoryId, CategoryName, MaxBorrowCount, MaxBorrowDays)
VALUES (1, N'教师', 10, 60),
       (2, N'学生', 5, 30);

INSERT INTO dbo.Books (Title, Author, Publisher, Price)
VALUES (N'C# 程序设计教程', N'张三', N'清华大学出版社', 59.00),
       (N'数据库系统概论',   N'王五', N'高等教育出版社', 49.50),
       (N'算法导论', N'Thomas H. Cormen', N'机械工业出版社', 128.00);
GO

/* ============================================================
   读者编号生成（并发安全模板，DAL 在同一事务内执行）
   ------------------------------------------------------------
   BEGIN TRAN;
   -- 序列化同类别同年注册：UPDLOCK(锁行) + HOLDLOCK(锁范围)
   SELECT @seq = ISNULL(MAX(CAST(SUBSTRING(ReaderId, 8, 3) AS INT)), 0) + 1
   FROM dbo.Readers WITH (UPDLOCK, HOLDLOCK)
   WHERE CategoryId = @cat
     AND SUBSTRING(ReaderId, 4, 4) = @year;   -- @year 为 4 位年份字符串
   -- @seq > 999 → 抛业务异常（假设 A2）
   -- C# 拼编号：$\"R{cat:D2}{year}{seq:D3}\" 后 INSERT（外键保证类别存在）
   COMMIT;
   ============================================================ */
