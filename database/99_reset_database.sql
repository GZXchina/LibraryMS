/* ============================================================
   图书馆管理系统 —— 数据库重置脚本（危险操作）
   作用：删除并重建 LibraryDB（清空全部业务数据）
   使用：仅在需要演示重置或结构升级时手工执行
   ============================================================ */

USE master;
GO
IF DB_ID(N'LibraryDB') IS NOT NULL
BEGIN
    ALTER DATABASE LibraryDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE LibraryDB;
    PRINT N'已删除旧数据库 LibraryDB。';
END
GO
