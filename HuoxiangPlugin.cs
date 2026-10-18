// ============================================================================
//  藿香正气水 —— SCP:SL LabAPI 整活插件
//
//  获取方式:
//    ① SCP-914 用「超精加工(VeryFine)」加工 反 SCP-207(粉可) → 藿香正气水
//    ② 每回合开始，设施储存柜 20% 概率出现
//    ③ 管理指令 UCP! 发放:
//         UCP! 2.3.4        给 2、3、4 号玩家各 1 瓶
//         UCP! 2 3          给 2 号玩家 3 瓶
//         UCP! 2.3.4 3      给 2、3、4 号玩家各 3 瓶
//
//  行为:
//    · 拾取时 Hint「这是什么」
//    · 喝下后 Hint「你饮用了 藿香正气水! (拉嗓子!)」
//      并给予 Metal 30 秒 + Spicy 30 秒
//    · 若当前版本没有 Metal 效果 → 回退 Invigorated 30 秒 + FogControl 12 级 30 秒
//
//  语言:
//    · 按系统时区 + 公网 IP 自动检测: 中国区 → 中文，其它 → English
//    · config.yml 的 language: auto|zh|en 可手动覆盖
//
//  载体: SCP-207(可乐)。插件用物品 Serial 表区分「藿香正气水」与普通可乐。
// ============================================================================
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using CommandSystem;
using CustomPlayerEffects;
using InventorySystem.Items;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.Scp914Events;
using LabApi.Events.Handlers;
using LabApi.Features;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using LabApi.Loader.Features.Plugins;
using MapGeneration.Distributors;
using Scp914;
using UnityEngine;
using Logger = LabApi.Features.Console.Logger;
using Locker = MapGeneration.Distributors.Locker;
using LockerChamber = MapGeneration.Distributors.LockerChamber;

namespace HuoxiangZhengqi
{
    public class HuoxiangPlugin : Plugin<HuoxiangPlugin.PluginConfig>
    {
        public static HuoxiangPlugin Instance { get; private set; }

        private static readonly HashSet<ushort> Bottles = new HashSet<ushort>();
        private static readonly object Sync = new object();

        private UcpCommand _command;

        public override string Name => "藿香正气水";
        public override string Description => Lang.T(
            "SCP-914 粉可超精 / 储物柜 20% / UCP! 指令获取；拾取提示「这是什么」，喝下 Metal+Spicy",
            "Obtained via SCP-914 Anti-SCP-207 Very Fine / 20% lockers / UCP! command; pickup hint, then Metal+Spicy");
        public override string Author => "匿名作者";
        public override Version Version => new Version(1, 1, 0);
        public override Version RequiredApiVersion => LabApiProperties.CurrentVersion;

        public class PluginConfig
        {
            [System.ComponentModel.Description("界面语言: auto=按地区自动检测(时区+公网IP) / zh=中文 / en=English (默认 auto)")]
            public string Language { get; set; } = "auto";

            [System.ComponentModel.Description("总开关")]
            public bool Enabled { get; set; } = true;

            [System.ComponentModel.Description("拾取时的提示")]
            public string PickupHint { get; set; } = "这是什么";

            [System.ComponentModel.Description("拾取提示显示秒数")]
            public float PickupHintSeconds { get; set; } = 3f;

            [System.ComponentModel.Description("喝下时的提示(可用 \\n 换行)")]
            public string DrinkHint { get; set; } = "你饮用了\n藿香正气水!\n(拉嗓子!)";

            [System.ComponentModel.Description("喝下提示显示秒数")]
            public float DrinkHintSeconds { get; set; } = 4f;

            [System.ComponentModel.Description("效果持续秒数")]
            public float EffectSeconds { get; set; } = 30f;

            [System.ComponentModel.Description("Spicy 强度(1-255, 默认 1)")]
            public int SpicyIntensity { get; set; } = 1;

            [System.ComponentModel.Description("Metal 强度(1-255, 默认 1)")]
            public int MetalIntensity { get; set; } = 1;

            [System.ComponentModel.Description("没有 Metal 效果时的回退: Invigorated 强度(默认 1)")]
            public int FallbackInvigoratedIntensity { get; set; } = 1;

            [System.ComponentModel.Description("没有 Metal 效果时的回退: FogControl 等级(默认 12)")]
            public int FallbackFogLevel { get; set; } = 12;

            [System.ComponentModel.Description("SCP-914 超精加工 反 SCP-207 产出藿香正气水")]
            public bool Enable914 { get; set; } = true;

            [System.ComponentModel.Description("每回合在储物柜生成(每个柜子独立判定)")]
            public bool EnableLocker { get; set; } = true;

            [System.ComponentModel.Description("储物柜出现概率(百分比, 默认 20)")]
            public int LockerChancePercent { get; set; } = 20;
        }

        public override void Enable()
        {
            // 双框架优先 EXILED: 如果 EXILED 版已加载，LabAPI 版自动让位
            if (ExiledBuildLoaded())
            {
                Logger.Info(Lang.T(
                    "[藿香正气水] 检测到 EXILED 版，LabAPI 版自动让位",
                    "[HuoxiangZhengqi] EXILED build detected, LabAPI build stands down"));
                return;
            }

            Instance = this;

            // 语言/地区检测先跑: 后面的默认文案与日志都按结果取词
            Lang.Initialize(Config.Language);
            if (Lang.ApplyTextDefaults(Config)) SaveConfig();
            Lang.LocalizeConfigFile(this);

            PlayerEvents.PickedUpItem += OnPickedUpItem;
            PlayerEvents.UsedItem += OnUsedItem;
            Scp914Events.ProcessedInventoryItem += On914Inventory;
            Scp914Events.ProcessedPickup += On914Pickup;
            ServerEvents.RoundStarted += OnRoundStarted;

            _command = new UcpCommand();
            Server.RemoteAdminCommandHandler.RegisterCommand(_command);
            Server.GameConsoleCommandHandler.RegisterCommand(_command);

            Logger.Info(Lang.T(
                "[藿香正气水] 已启用: 914 粉可超精 / 储物柜 20% / UCP! 指令",
                "[HuoxiangZhengqi] Enabled: SCP-914 Anti-SCP-207 Very Fine / 20% lockers / UCP! command"));
            Logger.Info(Lang.T(
                "[藿香正气水] 语言/地区: 中文 (检测依据: " + Lang.DetectedBy + ")",
                "[HuoxiangZhengqi] Language/region: English (detected by: " + Lang.DetectedBy + ")"));
        }

        public override void Disable()
        {
            PlayerEvents.PickedUpItem -= OnPickedUpItem;
            PlayerEvents.UsedItem -= OnUsedItem;
            Scp914Events.ProcessedInventoryItem -= On914Inventory;
            Scp914Events.ProcessedPickup -= On914Pickup;
            ServerEvents.RoundStarted -= OnRoundStarted;

            if (_command != null)
            {
                try { Server.RemoteAdminCommandHandler.UnregisterCommand(_command); } catch { }
                try { Server.GameConsoleCommandHandler.UnregisterCommand(_command); } catch { }
                _command = null;
            }

            lock (Sync) Bottles.Clear();
            Instance = null;
        }

        // ==================== 物品登记 ====================

        internal static void Register(ushort serial)
        {
            lock (Sync) Bottles.Add(serial);
        }

        internal static bool IsBottle(ushort serial)
        {
            lock (Sync) return Bottles.Contains(serial);
        }

        internal static void Unregister(ushort serial)
        {
            lock (Sync) Bottles.Remove(serial);
        }

        /// <summary>给玩家发若干瓶；背包放不下就掉在脚下。返回实际发放数量。</summary>
        internal static int Give(Player player, int count, ItemAddReason reason)
        {
            if (player == null || count <= 0) return 0;
            int ok = 0;
            for (int i = 0; i < count; i++)
            {
                Item item = null;
                try { item = player.AddItem(ItemType.SCP207, reason); } catch { }
                if (item != null)
                {
                    Register(item.Serial);
                    ok++;
                    continue;
                }

                try
                {
                    Pickup pickup = Pickup.Create(ItemType.SCP207, player.Position + Vector3.up * 0.2f, player.Rotation);
                    if (pickup != null)
                    {
                        Register(pickup.Serial);
                        ok++;
                    }
                }
                catch { }
            }
            return ok;
        }

        // ==================== 事件 ====================

        private void OnRoundStarted()
        {
            try
            {
                lock (Sync) Bottles.Clear();
                if (!Config.Enabled || !Config.EnableLocker) return;

                Locker[] lockers = UnityEngine.Object.FindObjectsOfType<Locker>();
                int spawned = 0;
                foreach (Locker locker in lockers)
                {
                    if (locker == null || locker.Chambers == null || locker.Chambers.Length == 0) continue;
                    if (UnityEngine.Random.Range(0f, 100f) >= Config.LockerChancePercent) continue;

                    LockerChamber chamber = locker.Chambers[UnityEngine.Random.Range(0, locker.Chambers.Length)];
                    Transform point = chamber != null ? chamber.Spawnpoint : null;
                    Vector3 pos = point != null ? point.position : locker.transform.position + locker.transform.forward * 0.5f;
                    Quaternion rot = point != null ? point.rotation : locker.transform.rotation;

                    Pickup pickup = Pickup.Create(ItemType.SCP207, pos, rot);
                    if (pickup != null)
                    {
                        Register(pickup.Serial);
                        spawned++;
                    }
                }

                if (spawned > 0)
                    Logger.Info(Lang.T(
                        "[藿香正气水] 本回合储物柜生成 " + spawned + " 瓶",
                        "[HuoxiangZhengqi] Spawned " + spawned + " bottle(s) in lockers this round"));
            }
            catch (Exception e)
            {
                Logger.Error(Lang.T("[藿香正气水] 储物柜生成失败: ", "[HuoxiangZhengqi] Locker spawn failed: ") + e);
            }
        }

        private void OnPickedUpItem(PlayerPickedUpItemEventArgs ev)
        {
            try
            {
                if (ev == null || !Config.Enabled || ev.Item == null) return;
                if (!IsBottle(ev.Item.Serial)) return;
                if (ev.Player != null && !string.IsNullOrEmpty(Config.PickupHint))
                    ev.Player.SendHint(Config.PickupHint, Clamp(Config.PickupHintSeconds, 0.5f, 30f));
            }
            catch (Exception e)
            {
                Logger.Error(Lang.T("[藿香正气水] 拾取提示异常: ", "[HuoxiangZhengqi] Pickup hint failed: ") + e);
            }
        }

        private void OnUsedItem(PlayerUsedItemEventArgs ev)
        {
            try
            {
                if (ev == null || !Config.Enabled || ev.UsableItem == null) return;
                if (ev.UsableItem.Type != ItemType.SCP207) return;

                ushort serial = ev.UsableItem.Serial;
                if (!IsBottle(serial)) return;
                Unregister(serial);

                Player player = ev.Player;
                if (player == null) return;

                float duration = Clamp(Config.EffectSeconds, 1f, 3600f);

                // 首选 Metal + Spicy；Metal 不存在时回退 Invigorated + FogControl
                bool metalApplied = TryApplyMetal(player, ToByte(Config.MetalIntensity), duration);
                player.EnableEffect<Spicy>(ToByte(Config.SpicyIntensity), duration);

                if (!metalApplied)
                {
                    player.EnableEffect<Invigorated>(ToByte(Config.FallbackInvigoratedIntensity), duration);
                    player.EnableEffect<FogControl>(ToByte(Config.FallbackFogLevel), duration);
                }

                if (!string.IsNullOrEmpty(Config.DrinkHint))
                    player.SendHint(Config.DrinkHint, Clamp(Config.DrinkHintSeconds, 0.5f, 30f));
            }
            catch (Exception e)
            {
                Logger.Error(Lang.T("[藿香正气水] 饮用处理异常: ", "[HuoxiangZhengqi] Drink handling failed: ") + e);
            }
        }

        private void On914Inventory(Scp914ProcessedInventoryItemEventArgs ev)
        {
            try
            {
                if (ev == null || !Config.Enabled || !Config.Enable914) return;
                if (ev.KnobSetting != Scp914KnobSetting.VeryFine) return;
                if (ev.OldItemType != ItemType.AntiSCP207) return;

                Player player = ev.Player;
                if (player == null) return;

                // 先把原版加工出来的东西拿走（可能是空的，也可能是另一瓶可乐），再换成藿香正气水
                if (ev.Item != null)
                {
                    try { player.RemoveItem(ev.Item); } catch { }
                }

                int given = Give(player, 1, ItemAddReason.Scp914Upgrade);
                if (given > 0)
                    Logger.Info(Lang.T(
                        "[藿香正气水] 914 超精加工: " + player.Nickname + " 获得 1 瓶",
                        "[HuoxiangZhengqi] SCP-914 Very Fine: " + player.Nickname + " got 1 bottle"));
            }
            catch (Exception e)
            {
                Logger.Error(Lang.T("[藿香正气水] 914 背包加工异常: ", "[HuoxiangZhengqi] SCP-914 inventory processing failed: ") + e);
            }
        }

        private void On914Pickup(Scp914ProcessedPickupEventArgs ev)
        {
            try
            {
                if (ev == null || !Config.Enabled || !Config.Enable914) return;
                if (ev.KnobSetting != Scp914KnobSetting.VeryFine) return;
                if (ev.OldItemType != ItemType.AntiSCP207) return;

                if (ev.Pickup != null)
                {
                    try { ev.Pickup.Destroy(); } catch { }
                }

                Pickup pickup = Pickup.Create(ItemType.SCP207, ev.NewPosition, Quaternion.identity);
                if (pickup != null) Register(pickup.Serial);
            }
            catch (Exception e)
            {
                Logger.Error(Lang.T("[藿香正气水] 914 地面加工异常: ", "[HuoxiangZhengqi] SCP-914 pickup processing failed: ") + e);
            }
        }

        // ==================== 工具 ====================

        /// <summary>EXILED 版是否已加载(双框架时让位给对方)。</summary>
        private static bool ExiledBuildLoaded()
        {
            try
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (string.Equals(assembly.GetName().Name, "HuoxiangZhengqi.Exiled", StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            catch { }
            return false;
        }

        // 单独方法 + NoInlining: 若当前游戏版本没有 Metal 效果，
        // 让 TypeLoad 发生在调用点而不是整个 OnUsedItem 的 JIT 阶段，便于回退。
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool TryApplyMetal(Player player, byte intensity, float duration)
        {
            try
            {
                player.EnableEffect<Metal>(intensity, duration);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static byte ToByte(int value)
        {
            if (value < 1) return 1;
            if (value > 255) return 255;
            return (byte)value;
        }

        private static float Clamp(float value, float min, float max)
        {
            if (float.IsNaN(value) || value < min) return min;
            if (value > max) return max;
            return value;
        }
    }

    /// <summary>UCP! —— 发放藿香正气水。</summary>
    public class UcpCommand : ICommand
    {
        public string Command => "UCP!";
        public string[] Aliases => new[] { "UCP", "hxzqs" };
        public string Description => Lang.T(
            "发放藿香正气水: UCP! <玩家ID>[.<玩家ID>…] [数量]",
            "Give Huoxiang Zhengqi Water: UCP! <PlayerID>[.<PlayerID>...] [amount]");

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count == 0)
            {
                response = Lang.T(
                    "用法: UCP! <玩家ID>[.<玩家ID>…] [数量]\n" +
                    "例: UCP! 2.3.4   (给 2、3、4 号各 1 瓶)\n" +
                    "    UCP! 2 3     (给 2 号 3 瓶)",
                    "Usage: UCP! <PlayerID>[.<PlayerID>...] [amount]\n" +
                    "e.g. UCP! 2.3.4   (1 bottle each to players 2, 3, 4)\n" +
                    "     UCP! 2 3     (3 bottles to player 2)");
                return false;
            }

            string targets = arguments.Array[arguments.Offset];
            int count = 1;
            if (arguments.Count >= 2)
                int.TryParse(arguments.Array[arguments.Offset + 1], out count);
            if (count < 1) count = 1;
            if (count > 64) count = 64;

            var done = new List<string>();
            var missing = new List<string>();

            foreach (string token in targets.Split('.'))
            {
                string idText = token.Trim();
                if (idText.Length == 0) continue;

                int id;
                if (!int.TryParse(idText, out id)) { missing.Add(idText); continue; }

                Player player = null;
                try { player = Player.Get(id); } catch { }
                if (player == null) { missing.Add(idText); continue; }

                int given = HuoxiangPlugin.Give(player, count, ItemAddReason.AdminCommand);
                done.Add(player.Nickname + " x" + given);
            }

            if (done.Count == 0)
            {
                response = Lang.T("没有找到玩家: ", "Player(s) not found: ") + string.Join(", ", missing.ToArray());
                return false;
            }

            response = Lang.T("已发放: ", "Given: ") + string.Join(", ", done.ToArray());
            if (missing.Count > 0)
                response += Lang.T("  (未找到: ", "  (not found: ") + string.Join(", ", missing.ToArray()) + ")";
            return true;
        }
    }

    /// <summary>语言与地区自动检测(时区 + 公网 IP)，以及配置文案切换。</summary>
    internal static class Lang
    {
        public static bool Chinese { get; private set; } = true;
        public static string DetectedBy { get; private set; } = "default";

        public static string T(string zh, string en)
        {
            return Chinese ? zh : en;
        }

        public static void Initialize(string mode)
        {
            string m = (mode ?? "auto").Trim().ToLowerInvariant();

            if (m == "zh" || m == "cn" || m == "chinese" || m == "中文")
            {
                Chinese = true;
                DetectedBy = "config:zh";
                return;
            }

            if (m == "en" || m == "english" || m == "英文")
            {
                Chinese = false;
                DetectedBy = "config:en";
                return;
            }

            // auto(以及无法识别的值): 时区优先给出离线结果，有网络再用公网 IP 复核
            bool tzChina = IsChinaZone();
            string country = TryGetCountryCode();
            if (country != null)
            {
                Chinese = IsChineseRegion(country);
                DetectedBy = "ip:" + country + (tzChina == Chinese ? "" : " (tz overridden)");
            }
            else
            {
                Chinese = tzChina;
                DetectedBy = "tz:" + SafeZoneId();
            }
        }

        private static string SafeZoneId()
        {
            try { return TimeZoneInfo.Local.Id; }
            catch { return "unknown"; }
        }

        private static bool IsChinaZone()
        {
            try
            {
                string id = TimeZoneInfo.Local.Id.ToLowerInvariant();
                return id == "china standard time"
                    || id == "asia/shanghai" || id == "asia/urumqi" || id == "asia/chongqing"
                    || id == "asia/harbin" || id == "asia/kashgar"
                    || id == "prc" || id == "roc";
            }
            catch { return false; }
        }

        private static bool IsChineseRegion(string countryCode)
        {
            string c = (countryCode ?? string.Empty).ToUpperInvariant();
            return c == "CN" || c == "HK" || c == "MO" || c == "TW";
        }

        private static string TryGetCountryCode()
        {
            string[] urls =
            {
                "https://ipapi.co/country/",
                "http://ip-api.com/json/?fields=countryCode"
            };
            foreach (string url in urls)
            {
                try
                {
                    try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; } catch { }
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                    req.Method = "GET";
                    req.Timeout = 1200;
                    req.ReadWriteTimeout = 1200;
                    req.UserAgent = "HuoxiangZhengqi/1.0";
                    using (WebResponse resp = req.GetResponse())
                    using (Stream stream = resp.GetResponseStream())
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        string body = reader.ReadToEnd().Trim();
                        if (body.Length == 2) return body.ToUpperInvariant();
                        Match match = Regex.Match(body, "\"countryCode\"\\s*:\\s*\"([A-Za-z]{2})\"");
                        if (match.Success) return match.Groups[1].Value.ToUpperInvariant();
                    }
                }
                catch
                {
                    // 无网络/超时 → 试下一个，最终回退时区判定
                }
            }
            return null;
        }

        /// <summary>把仍是内置默认值的提示文案换成当前语言；返回是否有改动。</summary>
        public static bool ApplyTextDefaults(HuoxiangPlugin.PluginConfig c)
        {
            if (c == null) return false;
            bool changed = false;

            c.PickupHint = Swap(c.PickupHint, "这是什么", "What is this?", ref changed);
            c.DrinkHint = Swap(c.DrinkHint,
                "你饮用了\n藿香正气水!\n(拉嗓子!)",
                "You drank\nHuoxiang Zhengqi Water!\n(Throat burn!)", ref changed);
            return changed;
        }

        private static string Swap(string value, string zh, string en, ref bool changed)
        {
            string from = Chinese ? en : zh;
            string to = Chinese ? zh : en;
            if (string.Equals(value, from, StringComparison.Ordinal))
            {
                changed = true;
                return to;
            }
            return value;
        }

        /// <summary>config.yml 注释中英对照（LabAPI 生成的是 [Description] 原文）。</summary>
        private static readonly Dictionary<string, string> Comments = new Dictionary<string, string>
        {
            { "界面语言: auto=按地区自动检测(时区+公网IP) / zh=中文 / en=English (默认 auto)",
              "Interface language: auto = detect by region (timezone + public IP) / zh = Chinese / en = English (default auto)" },
            { "总开关", "Master switch" },
            { "拾取时的提示", "Hint shown when picked up" },
            { "拾取提示显示秒数", "Pickup hint duration (seconds)" },
            { "喝下时的提示(可用 \\n 换行)", "Hint shown when drunk (\\n for a new line)" },
            { "喝下提示显示秒数", "Drink hint duration (seconds)" },
            { "效果持续秒数", "Effect duration (seconds)" },
            { "Spicy 强度(1-255, 默认 1)", "Spicy intensity (1-255, default 1)" },
            { "Metal 强度(1-255, 默认 1)", "Metal intensity (1-255, default 1)" },
            { "没有 Metal 效果时的回退: Invigorated 强度(默认 1)", "Fallback when Metal is unavailable: Invigorated intensity (default 1)" },
            { "没有 Metal 效果时的回退: FogControl 等级(默认 12)", "Fallback when Metal is unavailable: FogControl level (default 12)" },
            { "SCP-914 超精加工 反 SCP-207 产出藿香正气水", "SCP-914 Very Fine: Anti-SCP-207 produces Huoxiang Zhengqi Water" },
            { "每回合在储物柜生成(每个柜子独立判定)", "Spawn in lockers each round (per-locker roll)" },
            { "储物柜出现概率(百分比, 默认 20)", "Locker spawn chance (percent, default 20)" }
        };

        public static void LocalizeConfigFile(HuoxiangPlugin plugin)
        {
            try
            {
                string dir = null;
                try { dir = LabApi.Loader.ConfigurationLoader.GetConfigDirectory(plugin).FullName; } catch { }
                if (string.IsNullOrEmpty(dir)) return;

                string path = Path.Combine(dir, "config.yml");
                if (!File.Exists(path)) return;

                byte[] raw = File.ReadAllBytes(path);
                bool hasBom = raw.Length >= 3 && raw[0] == 0xEF && raw[1] == 0xBB && raw[2] == 0xBF;
                string text = new UTF8Encoding(false).GetString(raw, hasBom ? 3 : 0, raw.Length - (hasBom ? 3 : 0));
                string[] lines = text.Split(new char[] { '\n' });
                bool changed = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    Match m = Regex.Match(lines[i], "^(\\s*)#\\s?(.*?)\\s*$");
                    if (!m.Success) continue;

                    string body = m.Groups[2].Value;
                    string target = null;
                    if (Chinese)
                    {
                        foreach (KeyValuePair<string, string> pair in Comments)
                        {
                            if (string.Equals(pair.Value, body, StringComparison.Ordinal)) { target = pair.Key; break; }
                        }
                    }
                    else
                    {
                        string en;
                        if (Comments.TryGetValue(body, out en)) target = en;
                    }

                    if (target == null || target == body) continue;
                    lines[i] = m.Groups[1].Value + "# " + target;
                    changed = true;
                }

                if (changed)
                    File.WriteAllText(path, string.Join("\n", lines), new UTF8Encoding(hasBom));
            }
            catch
            {
                // 注释翻译失败不影响插件运行
            }
        }
    }
}