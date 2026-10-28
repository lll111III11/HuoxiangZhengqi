# 藿香正气水 · Huoxiang Zhengqi Water

> SCP:SL（SCP Secret Laboratory）LabAPI / EXILED 整活插件 —— 一瓶会拉嗓子的“药”
> (A fun SCP:SL LabAPI / EXILED plugin — a "medicine" that burns your throat.)

[![状态](https://img.shields.io/badge/status-stable-brightgreen)](#版本状态-version-status)
[![平台](https://img.shields.io/badge/platform-SCP%3ASL%20LabAPI%20%7C%20EXILED-blue)](#环境要求-requirements)
[![授权](https://img.shields.io/badge/license-CC0--1.0-lightgrey)](LICENSE)

---

## 版本状态 (Version Status)

> **非官方第三方插件。** 同时提供 **LabAPI** 与 **EXILED** 两个构建。
> (**Unofficial third-party plugin.** Ships two builds: **LabAPI** and **EXILED**.)

- 两个 DLL 都安装时：**EXILED 版优先**，LabAPI 版自动让位，不会重复触发
  (When both DLLs are installed, **the EXILED build takes priority** and the LabAPI build stands down, so nothing fires twice.)
- 只装其中一个：正常单框架运行
  (With only one installed, it runs on that framework alone.)
- 语言、物品、指令、配置完全一致
  (Language, items, commands and config are identical across both builds.)

---

## 这是什么 (What is this)

藿香正气水是挂在 **SCP-207（可乐）** 载体上的自定义物品，插件用物品 Serial 表把它和普通可乐区分开。
(Huoxiang Zhengqi Water is a custom item carried on **SCP-207 (cola)**; the plugin uses an item-serial table to tell it apart from ordinary cola.)

- 拾取时屏幕 Hint：**这是什么**
  (Pickup hint: **"What is this?"**)
- 喝下时屏幕 Hint：
  (Hint shown when drunk:)
  ```
  你饮用了
  藿香正气水!
  (拉嗓子!)
  ```
  (You drank / Huoxiang Zhengqi Water! / (Throat burn!))
- 喝下后给予 **Metal 30 秒 + Spicy 30 秒**
  (On drinking: **Metal for 30 s + Spicy for 30 s**.)
- 若当前游戏版本没有 `Metal` 效果，自动回退为 **Invigorated 30 秒 + FogControl 12 级 30 秒**
  (If the current game version has no `Metal` effect, it falls back to **Invigorated 30 s + FogControl level 12 for 30 s**.)

### Hint 输出优先级 (Hint Output Priority)

1. **HintChorus**（本系列 Hint 框架 / this series' hint framework）
2. 社区 Hint 框架 (community hint frameworks)：**HintServiceMeow** → **RueI**
3. 框架原生 (framework native)：LabAPI `SendHint` / EXILED `ShowHint`

没装的框架会自动跳过，不会报错。
(Missing frameworks are skipped automatically without errors.)

---

## 获取方式 (How to Obtain)

| 方式 (Method) | 说明 (Description) |
|---|---|
| SCP-914 | 把 **反 SCP-207（粉可）** 用 **超精加工 / Very Fine** 加工 → 藿香正气水 (Process **Anti SCP-207 (pink cola)** with **Very Fine** → Huoxiang Zhengqi Water) |
| 储物柜 (Locker) | 每回合开始，每个储物柜独立 **20%** 概率在柜内生成一瓶 (At the start of every round, each locker independently has a **20%** chance to contain one bottle) |
| 管理指令 (Admin command) | `UCP!` 直接发放到背包 (`UCP!` gives it straight into the inventory) |

### UCP! 指令 (UCP! Command)

```
UCP! 2.3.4      给 2、3、4 号玩家各 1 瓶
UCP! 2 3        给 2 号玩家 3 瓶
UCP! 2.3.4 3    给 2、3、4 号玩家各 3 瓶
```

- `UCP! 2.3.4`：给 2、3、4 号玩家各 1 瓶
  (`UCP! 2.3.4` — one bottle each for players #2, #3 and #4)
- `UCP! 2 3`：给 2 号玩家 3 瓶
  (`UCP! 2 3` — three bottles for player #2)
- `UCP! 2.3.4 3`：给 2、3、4 号玩家各 3 瓶
  (`UCP! 2.3.4 3` — three bottles each for players #2, #3 and #4)

同时注册了别名 `UCP` 与 `hxzqs`；RA 面板与服务器控制台都可用。
(The aliases `UCP` and `hxzqs` are registered as well; it works from the RA panel and the server console.)

---

## 语言 (Language)

内置中英文，按服务器地区自动切换：
(Built-in Chinese and English, switched automatically based on the server's region:)

- 优先看系统时区（`China Standard Time` / `Asia/Shanghai` 等）
  (The system time zone is checked first, e.g. `China Standard Time` / `Asia/Shanghai`.)
- 有网络时再用公网 IP 复核（`ipapi.co` → `ip-api.com`，各 1.2 秒超时）
  (When the network is available, the public IP is used to double-check via `ipapi.co` → `ip-api.com`, each with a 1.2 s timeout.)
- 中国区（CN/HK/MO/TW）→ 中文，其它 → English
  (China region (CN/HK/MO/TW) → Chinese; anywhere else → English.)
- 无网络或超时 → 只用时区判定
  (No network or timeout → the time zone alone decides.)
- 可在 `config.yml` 用 `language: auto|zh|en` 手动覆盖
  (You can override it manually in `config.yml` with `language: auto|zh|en`.)

---

## 环境要求 (Requirements)

| 项目 (Item) | 要求 (Requirement) |
|---|---|
| 游戏 (Game) | SCP:SL 专用服务器（Dedicated Server） (SCP:SL Dedicated Server) |
| 框架 (Framework) | LabAPI **或** EXILED（两者都装时优先 EXILED） (LabAPI **or** EXILED; if both are installed, EXILED wins) |
| 依赖 (Dependencies) | 无（不需要额外 DLL） (None — no extra DLLs needed) |

---

## 安装 (Installation)

### LabAPI

1. 下载 [`HuoxiangZhengqi.dll`](HuoxiangZhengqi.dll)
   (Download [`HuoxiangZhengqi.dll`](HuoxiangZhengqi.dll).)
2. 放进：
   (Place it in:)

   ```
   %APPDATA%\SCP Secret Laboratory\LabAPI\plugins\global\HuoxiangZhengqi.dll
   ```

### EXILED

1. 下载 [`HuoxiangZhengqi.Exiled.dll`](HuoxiangZhengqi.Exiled.dll)
   (Download [`HuoxiangZhengqi.Exiled.dll`](HuoxiangZhengqi.Exiled.dll).)
2. 放进：
   (Place it in:)

   ```
   %APPDATA%\EXILED\Plugins\HuoxiangZhengqi.Exiled.dll
   ```

3. 重启服务器
   (Restart the server.)

首次启动会自动生成配置文件：
(On first launch the config file is generated automatically:)

- LabAPI：`configs\<端口>\藿香正气水\config.yml`
  (LabAPI: `configs\<port>\藿香正气水\config.yml`)
- EXILED：`EXILED\Configs\<端口>\藿香正气水\config.yml`
  (EXILED: `EXILED\Configs\<port>\藿香正气水\config.yml`)

启动日志应出现：
(The startup log should contain:)

```
[藿香正气水] 已启用: 914 粉可超精 / 储物柜 20% / UCP! 指令
[藿香正气水] 语言/地区: 中文 (检测依据: ip:CN)
```

- 第一行：插件已启用，并列出 914 粉可超精加工、储物柜 20% 生成与 `UCP!` 指令三项功能
  (Line 1: the plugin is enabled, listing the three features — 914 Very Fine on pink cola, 20% locker spawning, and the `UCP!` command.)
- 第二行：语言/地区判定结果（这里为中文，依据是 IP 归属 CN）；出现该行说明语言模块已正常工作
  (Line 2: the language/region decision — Chinese here, detected from an IP in CN; seeing this line means the language module is working.)

---

## 配置 (Configuration)

| 键 (Key) | 默认 (Default) | 说明 (Description) |
|---|---|---|
| `language` | `auto` | `auto` / `zh` / `en`（自动 / 中文 / 英文） (auto / Chinese / English) |
| `enabled` | `true` | 总开关 (Master switch) |
| `pickup_hint` | `这是什么` | 拾取提示 (Pickup hint) |
| `pickup_hint_seconds` | `3` | 拾取提示时长 (How long the pickup hint stays on screen) |
| `drink_hint` | 见上 (see above) | 喝下提示（支持 `\n`） (Hint shown when drunk; supports `\n`) |
| `drink_hint_seconds` | `4` | 喝下提示时长 (How long the drink hint stays on screen) |
| `effect_seconds` | `30` | 效果时长 (Effect duration) |
| `spicy_intensity` | `1` | Spicy 强度 (Spicy intensity) |
| `metal_intensity` | `1` | Metal 强度 (Metal intensity) |
| `fallback_invigorated_intensity` | `1` | 无 Metal 时的 Invigorated 强度 (Invigorated intensity when Metal is unavailable) |
| `fallback_fog_level` | `12` | 无 Metal 时的 FogControl 等级 (FogControl level when Metal is unavailable) |
| `enable914` | `true` | 914 产出 (Whether 914 produces the item) |
| `enable_locker` | `true` | 储物柜生成 (Whether lockers spawn the item) |
| `locker_chance_percent` | `20` | 每个储物柜的出现概率 (Spawn chance per locker) |

`config.yml` 的注释也会随语言自动切换（中文 ↔ English）。
(The comments inside `config.yml` also switch with the language — Chinese ↔ English.)

---

## 构建 (Build)

需要 .NET SDK 与游戏本体托管程序集（`SCPSL_Data/Managed`）。
(You need the .NET SDK and the game's managed assemblies (`SCPSL_Data/Managed`).)

LabAPI 版：
(LabAPI build:)

```
powershell -ExecutionPolicy Bypass -File build.ps1
```

EXILED 版（需要 `Exiled.API.dll` 与 `Exiled.Events.dll`）：
(EXILED build — requires `Exiled.API.dll` and `Exiled.Events.dll`:)

```
powershell -ExecutionPolicy Bypass -File exiled\build.ps1
```

产物：
(Output artifacts:)

- `HuoxiangZhengqi.dll`
- `HuoxiangZhengqi.Exiled.dll`

---

## 许可 (License)

本仓库原创部分采用 **CC0 1.0 Universal（公共领域贡献）**：可任意使用、修改、再发布，无需署名。详见 [`LICENSE`](LICENSE)。
(The original parts of this repository are released under **CC0 1.0 Universal (Public Domain Dedication)**: use, modify and redistribute freely, no attribution required. See [`LICENSE`](LICENSE) for details.)

本项目为第三方非官方插件，与 Northwood Studios 无关。
(This is a third-party, unofficial plugin and is not affiliated with Northwood Studios.)
