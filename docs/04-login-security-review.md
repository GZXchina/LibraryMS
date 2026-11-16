# 🔒 登录模块安全规范与审查清单

- **作者**: @security-senior-secops
- **日期**: 2026-10-26
- **对象**: AuthService / LoginService / UserDao（待创建）

---

## 🔍 SECURITY SCAN —— 扫描跳过（0 项发现）

**原因**：项目仓库当前不含任何源代码（尚未开始编码）。按规程声明：本次无法执行代码扫描，改为交付**安全基线规范**与**落地后审查清单**。代码完成后，再次 `@security-senior-secops 审查登录模块` 触发逐行扫描。

---

## 一、密码存储规范（CRITICAL —— 课设最常见失分点）

### 必须做
- **PBKDF2**（.NET 内置 `Rfc2898DeriveBytes`），参数集中为常量：
  - 算法 SHA-256、迭代 ≥ 100,000、盐 16 字节（`RandomNumberGenerator.GetBytes(16)`）、输出 32 字节
  - 注册与登录使用**完全相同**的参数（防两处不一致）
- 盐**每用户独立随机**，与哈希分列存储（表结构见 docs/03）
- 校验用**常量时间比较**：`CryptographicOperations.FixedTimeEquals`

### 绝不做（🔴 一票否决）
- ❌ 明文存储密码
- ❌ MD5 / SHA-1（可快速碰撞）
- ❌ 无盐哈希 / 全局共用一个盐
- ❌ `==` 直接比较字节数组（时序侧信道；报告中说明即可加分）

```csharp
// 参考：注册（写入）
const int Iterations = 100_000;
byte[] salt = RandomNumberGenerator.GetBytes(16);
using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
byte[] hash = pbkdf2.GetBytes(32);   // → PasswordHash / PasswordSalt

// 参考：登录（校验）
using var kdf = new Rfc2898DeriveBytes(input, user.PasswordSalt, Iterations, HashAlgorithmName.SHA256);
byte[] inputHash = kdf.GetBytes(32);
bool ok = CryptographicOperations.FixedTimeEquals(inputHash, user.PasswordHash);
```

## 二、SQL 注入防护规范（CRITICAL —— 需求(5) 明确要求）

- **所有 SQL 一律 `SqlParameter` 参数化，零拼接**：

```csharp
// ✅ 正确
cmd.CommandText = "SELECT UserId, PasswordHash, PasswordSalt, Role FROM dbo.Users WHERE Username = @u";
cmd.Parameters.Add("@u", SqlDbType.NVarChar, 20).Value = username;

// 🔴 一票否决
cmd.CommandText = $"SELECT ... WHERE Username = '{username}'";
```

- 登录用户名作为**数据**传递：输入 `' OR '1'='1` 只会查不到 → 统一失败提示
- 模糊查询（图书/读者搜索）LIKE：`%` `_` `[` 在**参数值内**转义，仍走参数
- 不可参数化的部分（排序字段等）→ **白名单映射**，绝不拼接用户输入
- 连接串写入 `App.config`，**不硬编码在 .cs 中**

## 三、登录流程规范（HIGH）

- **统一错误消息**：「用户名或密码错误」——不区分"用户不存在/密码错误"，防账号枚举
- 失败锁定（可选加分项）：同一用户名连续失败 5 次锁 5 分钟
- 登录成功仅向 UI 返回 UserId + Role；**密码哈希/盐不得传出 DAL**
- 日志：记录 UserId、时间、成败；**绝不记录密码原文或哈希**

## 四、凭据与连接（CRITICAL）

- ⚠️ 本项目按用户要求使用 `sa` 账号（仅限本地课设演示环境）；真实环境必须改用最小权限账号（`db_datareader` + `db_datawriter`），并避免在源码目录明文保存密码
- 含密码的连接串不打印到控制台/日志

---

## 五、落地后审查清单（代码就绪后逐条执行）

| # | 检查项 | 级别 | 判定方法 |
|---|-------|------|---------|
| S1 | 全部 SQL 参数化，无字符串拼接 | 🔴 | 全文搜索 `CommandText` 附近的 `+` 拼接与 `$"..."` 内插 |
| S2 | PBKDF2 参数达标（≥10 万迭代 / 每用户独立盐） | 🔴 | 对照常量定义与 Users 表数据 |
| S3 | 使用 FixedTimeEquals 比较 | 🟡 | 代码走查 |
| S4 | 登录失败统一消息 | 🔴 | 分别输错用户名/密码，观察输出一致 |
| S5 | 日志无密码/哈希 | 🔴 | 检查所有日志输出点 |
| S6 | 连接串在 App.config 且非 sa | 🔴 | 检查配置文件 |
| S7 | 用户名重复注册触发 UNIQUE 时有友好提示 | 🟡 | 注册重名测试（SqlException 2601 捕获） |
| S8 | 已注销读者对应的读者用户登录后的可见性 | 🟡 | 关联业务规则确认 |
