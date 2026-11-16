# 项目启动说明（Quick Start）

- **Version**: 1.1
- **Date**: 2026-11-16
- **适用对象**：首次在本机运行本项目的同学 / 验收教师
- **项目**：学校图书馆管理系统（C# / WinForms + SQL Server）

---

## 1. 环境要求

| 组件 | 最低要求 | 说明 |
|---|---|---|
| 操作系统 | Windows 7 SP1 / Windows 10 | 已在 Windows 10 1909 64 位实测 |
| .NET Framework | **4.8 运行时** | 程序目标框架为 `net48`，运行必需（705 教室实测版本 4.8.03752） |
| 编译工具 | 二选一：**.NET SDK**（含 `dotnet` CLI）或 **VS2015 的 MSBuild 14 `csc.exe`** | 都没有时可直接拷贝 710 教室编译好的 `bin\Debug\net48\` 目录运行 |
| 数据库 | **SQL Server 2008 R2 及以上** | 已在 2008 R2 Express、2012 SP1 上验证；Express 版即可 |
| 客户端工具 | `sqlcmd`（Express 自带）或 SSMS | 仅用于执行建库脚本，非运行必需 |

> 本项目**不依赖任何 NuGet 包**（数据库驱动为 .NET Framework 内置的 `System.Data.SqlClient`），因此**无需联网还原**，离线教室也能编译运行。

---

## 2. 五分钟启动步骤

### 第 1 步：初始化数据库

在项目根目录打开 PowerShell / CMD，执行：

```powershell
sqlcmd -S localhost -E -f 65001 -i database\01_create_database.sql
```

- `-E` 表示用当前 Windows 账号连接，不需要密码；若实例允许 `sa` 登录，可改成 `-U sa -P 你的密码`。
- 实例名按本机实际情况替换：**705 教室为命名实例 `localhost\SQLEXPRESS`**，710 教室为默认实例 `localhost`。
- 若改用 `sa`，请确认 `src\LibraryMS\App.config` 里的连接字符串与本机一致（见第 6 节）。
- **必须保留 `-f 65001`**：脚本为 UTF-8 无 BOM，SQL Server 2008 R2 自带的 sqlcmd 10.0 不加该参数会按本地代码页解析中文，导致建表语句被吞掉。
- 脚本**非破坏性**：库/表已存在时不会删除数据，可安全重复执行。
- 也可用 SSMS 打开 `database\01_create_database.sql` 直接执行（F5）。

执行成功的标志：输出末尾出现 `数据库 LibraryDB 初始化完成。`，且无红色错误；可用下面的语句抽查：

```sql
USE LibraryDB;
SELECT COUNT(*) AS Tables FROM sys.tables;   -- 期望 5：Books / Borrows / ReaderCategory / Readers / Users
```

### 第 2 步：启动程序

**双击项目根目录的 `run.bat`**（推荐）。

`run.bat` 的构建顺序：

1. 若 `src\LibraryMS\bin\Debug\net48\LibraryMS.exe` 已存在且体积合理（40 KB～600 KB，且不超过 `obj` 里刚编译产物的一倍）→ 直接运行，不重复编译；
2. 产物缺失或体积异常（多为被文件感染型病毒改写）→ 删除后尝试 `dotnet build`（本机装有 .NET SDK 时）；
3. 失败或没有 .NET SDK → 自动回退到 VS2015 的 `csc.exe`（**C# 6**）逐个编译 `src\LibraryMS` 下的 `.cs`，并把 `App.config` 复制为 `LibraryMS.exe.config`；
4. 两者都不存在 → 提示安装 .NET Framework 4.8 Developer Pack，或拷贝他人编译好的 `bin\Debug\net48\` 目录。

> ⚠️ **不要直接运行 `obj\Debug\net48\LibraryMS.exe`**：那是编译中间产物，没有配套 `.exe.config`，启动即报「未找到名为 LibraryDB 的连接字符串」。

### 第 3 步：创建管理员账号（仅首次）

程序启动后先做数据库连通性自检，若 `Users` 表为空，会进入管理员创建流程：

- 用户名：3-20 位字母 / 数字 / 下划线
- 密码：6-32 位（输入时不回显）
- 密码以 **PBKDF2-SHA256（10 万次迭代 + 每用户随机盐）** 存储，不存明文

创建完成后即可用该账号登录；之后每次启动都不会再要求创建。

### 第 4 步：登录并进入主界面

登录窗口左侧是品牌区，右侧是表单，三个按钮：**登 录** / **读者注册（新读者自助注册）** / **退出系统**。

- **管理员**：输入用户名 + 密码 → 进入主窗体，左侧「功能菜单」列出图书管理、读者类别、读者管理、借书登记、还书登记、借阅查询、借阅统计 7 个模块，右上角显示当前账号并可「退出登录」。
- **读者**：
  - 在登录页点「读者注册」自助注册（同事务内创建读者记录 + 登录账号，注册成功会显示自动生成的读者编号）；
  - 或由管理员在「读者管理」里选中读者后为其开通账号。
  登录后左侧只有查询图书、我的借阅、修改密码 3 个模块。

> 主窗体底部状态栏会显示当前模块与数据库名，方便验收时确认所处位置。

---

## 3. 首次使用建议路径

按下面顺序录入数据，可最快看到完整效果：

1. **读者类别**：确认或新增类别（可借数量 1-50、可借天数 1-365）
2. **图书管理 → 录入图书**：书名 / 作者 / 出版社 / 单价
3. **读者管理 → 录入读者**：选择类别，系统自动生成编号（`R` + 类别号 2 位 + 年份 4 位 + 序号 3 位，如 `R012026001`）
4. **借书登记**：输入读者编号 + 书号 → 回执含借阅号与应还日期
5. **还书登记**：按借阅号归还，或按读者查询在借清单后归还
6. **借阅记录查询 / 借阅统计**：核对在借、超期、热门图书等

---

## 4. 启动自检清单（验收用）

| # | 检查项 | 期望结果 |
|---|---|---|
| 1 | 建库脚本执行 | 无错误，`sys.tables` 为 5 张业务表 + 视图 `V_BorrowDetails` |
| 2 | 启动程序 | 弹出登录窗口，标题「图书馆管理系统 - 登录」，无连接字符串/连接失败报错 |
| 3 | 首次创建管理员 | 提示创建成功，随后可用新账号登录 |
| 4 | 登录后主窗体 | 左侧 7 个菜单项，右侧默认打开「图书管理」并列出馆藏，底部状态栏显示「当前模块：图书管理」 |
| 5 | 借书 | 借书登记页填写读者编号 + 书号，回执含借阅号、读者名、书名、应还日期（日期由数据库 `GETDATE()` 计算） |
| 6 | 同一本书再借 | 提示 `[E1004] 该书当前不在馆（已被借出）。` |
| 7 | 重复还书 | 提示「已归还，无需重复操作。」（幂等，不报错） |
| 8 | 借阅统计 | 四类报表均有数据或明确的空态提示 |

> 边界用例（BC-01..BC-10、RC-01..RC-06）的逐条实测记录见 `docs/02-workflow-borrow.md` 的「执行记录」小节。

---

## 5. 数据库维护

### 重置（清空全部业务数据，危险）

```powershell
sqlcmd -S localhost -E -f 65001 -i database\99_reset_database.sql
sqlcmd -S localhost -E -f 65001 -i database\01_create_database.sql
```

`99_reset_database.sql` 会 `DROP DATABASE LibraryDB`（先置 `SINGLE_USER` 强制断开连接），随后重跑建库脚本即可得到全新库。**演示前建议先备份**：

```sql
BACKUP DATABASE LibraryDB TO DISK = N'D:\backup\LibraryDB.bak' WITH INIT;
```

### 常用核对 SQL

```sql
USE LibraryDB;
SELECT * FROM dbo.Books;
SELECT * FROM dbo.Readers;
SELECT * FROM dbo.V_BorrowDetails ORDER BY BorrowId DESC;
SELECT * FROM dbo.Users;
```

---

## 6. 修改数据库连接

连接字符串位于 [App.config](../src/LibraryMS/App.config)：

```xml
<add name="LibraryDB"
     connectionString="Server=localhost;Database=LibraryDB;Integrated Security=True;Connect Timeout=5;"
     providerName="System.Data.SqlClient" />
```

修改后注意：

- **必须重新生成/复制配置**：`run.bat` 只在 `LibraryMS.exe.config` 不存在时才复制。已改过 `App.config` 而程序行为未变时，删除 `src\LibraryMS\bin\Debug\net48\LibraryMS.exe.config` 后再运行 `run.bat`。
- 默认是 **Windows 集成身份验证**（`Integrated Security=True`）：不存明文密码，用当前 Windows 账号连库。若本机 SQL Server 只允许 `sa` 登录，把该段换成 `User Id=sa;Password=你的密码`，并确认服务端已开启混合身份验证。
- 命名实例把 `Server` 改为 `localhost\SQLEXPRESS`。

> 安全说明：`sa` 方式需要把密码明文写进配置文件，**仅限本地课设环境**；真实部署应改用最小权限账号并把凭据移出源码目录。

---

## 7. 常见故障排查

| 现象 | 原因 | 处理 |
|---|---|---|
| 未找到名为 `LibraryDB` 的连接字符串 | 运行了 `obj\Debug\net48\LibraryMS.exe` | 改用 `run.bat` 或 `bin\Debug\net48\LibraryMS.exe` |
| 无法连接数据库 / 提示「用户 'sa' 登录失败」 | SQL Server 服务未启动、实例名不符、建库脚本未执行；或服务端只允许 Windows 身份验证而连接串用了 `sa` | 启动 SQL Server 服务；核对 `App.config` 的 `Server`；改用 `Integrated Security=True`，或在 SSMS 开启混合模式并启用 `sa` |
| 建库脚本执行后表很少或语法错误 | 未加 `-f 65001`，中文注释被按本地代码页解析 | 加 `-f 65001` 重新执行 |
| `INSERT` 报「SET 选项 QUOTED_IDENTIFIER 设置不正确」（错误 1934） | 手工用 sqlcmd 写借阅表，且连接未开启 QUOTED_IDENTIFIER（表上有过滤索引） | sqlcmd 加 `-I` 参数，或改用程序界面操作 |
| `run.bat` 提示 Neither .NET SDK nor MSBuild 14 csc.exe was found | 本机既无 `dotnet` 也无 VS2015 | 安装 .NET Framework 4.8 Developer Pack，或拷贝 710 教室编译好的 `bin\Debug\net48\` 整个目录 |
| 编译报 `MSB3027 / MSB3021`（文件被占用，无法写入 exe） | 程序仍在运行，占用输出文件 | 关闭程序窗口后重试 |
| 代码改完运行无变化 | 已存在旧 exe 且体积正常，`run.bat` 直接运行不重新编译 | 删除 `src\LibraryMS\bin\Debug\net48\LibraryMS.exe` 后再运行 `run.bat` |
| 双击 exe 弹出 `Execution Processing Message (0xc0000005) - Unexpected parameters` 或 "This application could not be started." | 文件已不是有效托管程序集——**被文件感染型病毒改写**（病毒把 exe 包成 Delphi 外壳，体积从约 150 KB 膨胀到 700 KB～1 MB） | 正常产物 154112 字节；异常时删除该 exe 用 `run.bat` 重编译，并全盘杀毒。开发机实测感染源为 `C:\ProgramData\Synaptics\Synaptics.exe`（无签名，自启动项伪装成 "Synaptics Pointing Device Driver"） |
| 读者无法登录 | 该读者尚未开通登录账号 | 登录页点「读者注册」自助注册，或管理员 → 读者管理 → 为该读者开通账号 |

---

## 8. 项目结构与文档索引

```
Class-Design/
├── run.bat                     # 一键构建 + 启动
├── database/
│   ├── 01_create_database.sql  # 建库建表 + 种子数据（非破坏性）
│   └── 99_reset_database.sql   # 删除重建（危险）
├── docs/                       # 设计文档
└── src/LibraryMS/              # 源码（Models / DAL / BLL / UI）
```

| 文档 | 内容 |
|---|---|
| [README.md](../README.md) | 项目总览、技术栈、功能清单、设计要点 |
| [01-implementation-plan.md](01-implementation-plan.md) | 实施计划、里程碑、风险与实测记录 |
| [02-workflow-borrow.md](02-workflow-borrow.md) | 借书工作流、边界用例定义与 16 条执行记录 |
| [03-database-schema.sql](03-database-schema.sql) | 数据库结构设计稿 |
| [04-login-security-review.md](04-login-security-review.md) | 登录与密码安全评审 |
| [05-borrowservice-review-checklist.md](05-borrowservice-review-checklist.md) | 借还服务审查清单 |
