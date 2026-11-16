# 📋 BorrowService 还书事务 —— 审查契约与清单

- **作者**: @engineering-code-reviewer
- **日期**: 2026-10-26
- **对象**: `src/LibraryMS/BLL/BorrowService.cs` 还书部分（待创建）

---

## 审查状态：待审对象尚未创建

仓库当前无任何 C# 代码。按审查纪律：**不审不存在的代码**。本文件交付两样东西：

1. 还书事务的**参考契约**——写代码时照此实现
2. 代码就绪后的**审查清单**——届时再 `@engineering-code-reviewer 审查 BorrowService 还书事务` 执行正式审查

---

## 一、还书事务参考契约（实现时必须满足）

```csharp
public ReturnResult ReturnBook(int borrowId)
{
    using var conn = DbHelper.CreateConnection();
    conn.Open();
    using var tran = conn.BeginTransaction();          // 事务覆盖两个写操作
    try
    {
        // (1) 条件更新借阅记录 —— 幂等核心：已还则 0 行受影响
        int rows;
        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tran;
            cmd.CommandText = @"UPDATE dbo.Borrows
                                SET ReturnDate = CAST(GETDATE() AS DATE)
                                WHERE BorrowId = @id AND ReturnDate IS NULL";
            cmd.Parameters.Add("@id", SqlDbType.Int).Value = borrowId;
            rows = cmd.ExecuteNonQuery();
        }
        if (rows == 0)
        {
            tran.Rollback();                            // 无写入，安全退出
            return ReturnResult.AlreadyReturned;        // 幂等：提示"已归还"，不算错误
        }

        // (2) 恢复图书在馆状态
        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tran;
            cmd.CommandText = @"UPDATE dbo.Books
                                SET IsAvailable = 1
                                WHERE BookId = (SELECT BookId FROM dbo.Borrows WHERE BorrowId = @id)";
            cmd.Parameters.Add("@id", SqlDbType.Int).Value = borrowId;
            cmd.ExecuteNonQuery();
        }

        tran.Commit();
        return ReturnResult.Success;                    // 附带超期天数（DueDate 与今天比较）
    }
    catch (SqlException ex)
    {
        tran.Rollback();                                // 部分失败 → 全部回滚，不留幽灵状态
        throw new BizException("归还失败，数据已回滚", ex);
    }
}
```

要点提炼：

1. **事务边界** = (1) + (2)，两个 UPDATE 同生共死
2. **幂等**：`WHERE ReturnDate IS NULL` + 受影响行数判定 → 重复还书 / 并发双还均安全
3. **超期天数** = 今天 − DueDate，仅展示（假设 A4：不罚款）
4. **回滚**：任何 SqlException → Rollback → 结构化业务异常上抛，UI 不见原始 SQL 错误

---

## 二、审查清单（代码就绪后逐条执行）

### 🔴 阻塞项（不修复不予通过）

| # | 检查项 |
|---|-------|
| B1 | 事务同时覆盖"更新借阅记录"与"恢复图书状态"两个写操作 |
| B2 | 条件更新 `ReturnDate IS NULL` 且检查受影响行数（幂等/并发双还） |
| B3 | 异常路径必然 Rollback（try/catch 语义完整，无吞异常） |
| B4 | 全部 SQL 参数化（无拼接、无内插字符串进 CommandText） |
| B5 | 借阅记录不存在时返回明确业务错误，**不误报"已归还"** |

### 🟡 建议项

| # | 检查项 |
|---|-------|
| B6 | 连接 / 命令 / 事务均 using 释放，无资源泄漏 |
| B7 | 超期天数基于数据库日期（GETDATE）而非应用机本地时钟 |
| B8 | 结果对象四态区分：成功 / 已归还 / 记录不存在 / 系统错误 |
| B9 | 层次不越界：UI 无 SQL，DAL 无业务判断 |

### 💭 小改进

- B10 命名与注释清晰（`ReturnBook` 而非 `doReturn`）
- B11 关键分支有测试覆盖（见下）

---

## 三、验收测试用例（M5 阶段执行）

| 用例 | 步骤 | 预期 |
|---|---|---|
| T1 正常还书 | 借书 → 还书 | 借阅 ReturnDate 非空；图书 IsAvailable=1 |
| T2 重复还书 | 对同一记录还两次 | 第二次提示"已归还"，数据无变化 |
| T3 超期还书 | 手工将 DueDate 改为昨天 → 还书 | 成功并展示超期 1 天 |
| T4 事务回滚 | 借阅更新成功后人为让图书更新抛异常 | 借阅表 ReturnDate 仍为 NULL |
| T5 并发双还 | 两个线程同时对同一记录还书 | 仅一个"成功"，另一个"已归还" |
