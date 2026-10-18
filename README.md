# 藿香正气水 · Huoxiang Zhengqi Water

> SCP:SL（SCP Secret Laboratory）LabAPI 整活插件 —— 一瓶会拉嗓子的“药”

[![状态](https://img.shields.io/badge/status-Beta-orange)](#版本状态)
[![平台](https://img.shields.io/badge/platform-SCP%3ASL%20LabAPI-blue)](#环境要求)
[![授权](https://img.shields.io/badge/license-CC0--1.0-lightgrey)](LICENSE)

---

## 版本状态：Beta

> **非官方第三方插件，仍在测试中。** 欢迎反馈，但不建议直接用于正式服。

---

## 这是什么

藿香正气水是挂在 **SCP-207（可乐）** 载体上的自定义物品，插件用物品 Serial 表把它和普通可乐区分开。

- 拾取时屏幕 Hint：**这是什么**
- 喝下时屏幕 Hint：
  ```
  你饮用了
  藿香正气水!
  (拉嗓子!)
  ```
- 喝下后给予 **Metal 30 秒 + Spicy 30 秒**
- 若当前游戏版本没有 `Metal` 效果，自动回退为 **Invigorated 30 秒 + FogControl 12 级 30 秒**

---

## 获取方式

| 方式 | 说明 |
|---|---|
| SCP-914 | 把 **反 SCP-207（粉可）** 用 **超精加工 / Very Fine** 加工 → 藿香正气水 |
| 储物柜 | 每回合开始，每个储物柜独立 **20%** 概率在柜内生成一瓶 |
| 管理指令 | `UCP!` 直接发放到背包 |

### UCP! 指令

```
UCP! 2.3.4      给 2、3、4 号玩家各 1 瓶
UCP! 2 3        给 2 号玩家 3 瓶
UCP! 2.3.4 3    给 2、3、4 号玩家各 3 瓶
```

同时注册了别名 `UCP` 与 `hxzqs`；RA 面板与服务器控制台都可用。

---

## 语言

内置中英文，按服务器地区自动切换：

- 优先看系统时区（`China Standard Time` / `Asia/Shanghai` 等）
- 有网络时再用公网 IP 复核（`ipapi.co` → `ip-api.com`，各 1.2 秒超时）
- 中国区（CN/HK/MO/TW）→ 中文，其它 → English
- 无网络或超时 → 只用时区判定
- 可在 `config.yml` 用 `language: auto|zh|en` 手动覆盖

---

## 环境要求

| 项目 | 要求 |
|---|---|
| 游戏 | SCP:SL 专用服务器（Dedicated Server） |
| 框架 | LabAPI |
| 依赖 | 无（不需要 Harmony 等额外 DLL） |

---

## 安装

1. 下载 [`HuoxiangZhengqi.dll`](HuoxiangZhengqi.dll)
2. 放进：

   ```
   %APPDATA%\SCP Secret Laboratory\LabAPI\plugins\global\HuoxiangZhengqi.dll
   ```

3. 重启服务器

首次启动会自动生成 `configs\<端口>\藿香正气水\config.yml`。

启动日志应出现：

```
[藿香正气水] 已启用: 914 粉可超精 / 储物柜 20% / UCP! 指令
[藿香正气水] 语言/地区: 中文 (检测依据: ip:CN)
```

---

## 配置

| 键 | 默认 | 说明 |
|---|---|---|
| `language` | `auto` | `auto` / `zh` / `en` |
| `enabled` | `true` | 总开关 |
| `pickup_hint` | `这是什么` | 拾取提示 |
| `pickup_hint_seconds` | `3` | 拾取提示时长 |
| `drink_hint` | 见上 | 喝下提示（支持 `\n`） |
| `drink_hint_seconds` | `4` | 喝下提示时长 |
| `effect_seconds` | `30` | 效果时长 |
| `spicy_intensity` | `1` | Spicy 强度 |
| `metal_intensity` | `1` | Metal 强度 |
| `fallback_invigorated_intensity` | `1` | 无 Metal 时的 Invigorated 强度 |
| `fallback_fog_level` | `12` | 无 Metal 时的 FogControl 等级 |
| `enable914` | `true` | 914 产出 |
| `enable_locker` | `true` | 储物柜生成 |
| `locker_chance_percent` | `20` | 每个储物柜的出现概率 |

`config.yml` 的注释也会随语言自动切换（中文 ↔ English）。

---

## 构建

需要 .NET SDK 与游戏本体托管程序集（`SCPSL_Data/Managed`）：

```
powershell -ExecutionPolicy Bypass -File build.ps1
```

产物：`HuoxiangZhengqi.dll`

---

## 许可

本仓库原创部分采用 **CC0 1.0 Universal（公共领域贡献）**：可任意使用、修改、再发布，无需署名。详见 [`LICENSE`](LICENSE)。

本项目为第三方非官方插件，与 Northwood Studios 无关。
