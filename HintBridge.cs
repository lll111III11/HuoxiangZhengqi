// ============================================================================
//  藿香正气水 —— Hint 输出适配层（LabAPI / EXILED 共用） / Huoxiang Zhengqi Water — Hint output adapter layer (shared by LabAPI / EXILED)
//  优先级: / Priority:
//    1. HintChorus（我们自己的 Hint 框架）→ ShowTransient / 1. HintChorus (our own hint framework) -> ShowTransient
//    2. 社区 Hint 框架: HintServiceMeow → RueI / 2. Community hint frameworks: HintServiceMeow -> RueI
//    3. 框架原生: LabAPI Player.SendHint / EXILED Player.ShowHint / 3. Framework-native: LabAPI Player.SendHint / EXILED Player.ShowHint
//  全部通过反射探测，没装就自动跳过，不会因为缺依赖报错。 / Everything is probed via reflection; missing frameworks are skipped automatically, so no errors from missing dependencies.
// ============================================================================
using System;
using System.Reflection;

namespace HuoxiangZhengqi.Shared
{
    internal static class HintBridge
    {
        // ---- HintChorus ----
        private static bool _chorusChecked;
        private static MethodInfo _chorusShow;

        // ---- HintServiceMeow ----
        private static bool _hsmChecked;
        private static MethodInfo _hsmGet;
        private static MethodInfo _hsmShow;
        private static ConstructorInfo _hsmCtor;
        private static PropertyInfo _hsmText;

        // ---- RueI ----
        private static bool _rueChecked;
        private static MethodInfo _rueGet;
        private static MethodInfo _rueShow;
        private static ConstructorInfo _rueCtor;

        /// <summary>
        /// 按优先级显示一条 Hint。 / Show a hint following the priority order.
        /// native 为当前框架的原生显示方法: LabAPI = SendHint, EXILED = ShowHint。 / native is the current framework's native display method: LabAPI = SendHint, EXILED = ShowHint.
        /// </summary>
        public static void Show(object player, ReferenceHub hub, string text, float seconds, Action<string, float> native)
        {
            if (string.IsNullOrEmpty(text)) return;

            try { if (TryHintChorus(hub, text, seconds)) return; } catch { }
            try { if (TryHintServiceMeow(player, text, seconds)) return; } catch { }
            try { if (TryRueI(player, text, seconds)) return; } catch { }

            if (native != null) native(text, seconds);
        }

        // ==================== HintChorus ====================

        private static bool TryHintChorus(ReferenceHub hub, string text, float seconds)
        {
            if (!_chorusChecked)
            {
                _chorusChecked = true;
                Type type = FindType("HintChorus.UiIsolation", "UiIsolation");
                if (type != null)
                {
                    foreach (MethodInfo m in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                    {
                        if (m.Name != "ShowTransient") continue;
                        ParameterInfo[] ps = m.GetParameters();
                        if (ps.Length == 3 && ps[0].ParameterType.Name == "ReferenceHub"
                            && ps[1].ParameterType == typeof(string) && ps[2].ParameterType == typeof(float))
                        {
                            _chorusShow = m;
                            break;
                        }
                    }
                }
            }

            if (_chorusShow == null || hub == null) return false;
            _chorusShow.Invoke(null, new object[] { hub, text, seconds });
            return true;
        }

        // ==================== HintServiceMeow ====================

        private static bool TryHintServiceMeow(object player, string text, float seconds)
        {
            if (!_hsmChecked)
            {
                _hsmChecked = true;
                Type display = FindType("HintServiceMeow.Core.Utilities.PlayerDisplay", "PlayerDisplay");
                Type hint = FindType("HintServiceMeow.Core.Models.Hints.Hint", "Hint");
                if (display != null && hint != null)
                {
                    _hsmGet = FindPlayerMethod(display, "Get", player);
                    foreach (MethodInfo m in display.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (m.Name != "ShowHint") continue;
                        ParameterInfo[] ps = m.GetParameters();
                        if (ps.Length == 2 && ps[0].ParameterType == hint && ps[1].ParameterType == typeof(float))
                        {
                            _hsmShow = m;
                            break;
                        }
                    }
                    _hsmCtor = hint.GetConstructor(Type.EmptyTypes);
                    _hsmText = hint.GetProperty("Text");
                }
            }

            if (_hsmGet == null || _hsmShow == null || _hsmCtor == null || _hsmText == null) return false;

            object displayObj = _hsmGet.Invoke(null, new object[] { player });
            if (displayObj == null) return false;

            object hintObj = _hsmCtor.Invoke(null);
            _hsmText.SetValue(hintObj, text, null);
            _hsmShow.Invoke(displayObj, new object[] { hintObj, seconds });
            return true;
        }

        // ==================== RueI ====================

        private static bool TryRueI(object player, string text, float seconds)
        {
            if (!_rueChecked)
            {
                _rueChecked = true;
                Type display = FindType("RueI.API.Features.RueDisplay", "RueDisplay");
                Type element = FindType("RueI.API.Features.BasicElement", "BasicElement");
                if (display != null && element != null)
                {
                    _rueGet = FindPlayerMethod(display, "Get", player);
                    foreach (ConstructorInfo c in element.GetConstructors())
                    {
                        ParameterInfo[] ps = c.GetParameters();
                        if (ps.Length == 2 && (ps[0].ParameterType == typeof(int) || ps[0].ParameterType == typeof(float))
                            && ps[1].ParameterType == typeof(string))
                        {
                            _rueCtor = c;
                            break;
                        }
                    }
                    foreach (MethodInfo m in display.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (m.Name != "Show") continue;
                        ParameterInfo[] ps = m.GetParameters();
                        if (ps.Length == 2 && ps[0].ParameterType == element && ps[1].ParameterType == typeof(float))
                        {
                            _rueShow = m;
                            break;
                        }
                    }
                }
            }

            if (_rueGet == null || _rueShow == null || _rueCtor == null) return false;

            object displayObj = _rueGet.Invoke(null, new object[] { player });
            if (displayObj == null) return false;

            object size = _rueCtor.GetParameters()[0].ParameterType == typeof(int) ? (object)30 : 30f;
            object elementObj = _rueCtor.Invoke(new object[] { size, text });
            _rueShow.Invoke(displayObj, new object[] { elementObj, seconds });
            return true;
        }

        // ==================== 工具 / Utilities ====================

        private static MethodInfo FindPlayerMethod(Type type, string name, object player)
        {
            foreach (MethodInfo m in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (m.Name != name) continue;
                ParameterInfo[] ps = m.GetParameters();
                if (ps.Length == 1 && ps[0].ParameterType.IsInstanceOfType(player)) return m;
            }
            return null;
        }

        private static Type FindType(string fullName, string shortName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    Type type = assembly.GetType(fullName, false);
                    if (type != null) return type;
                }
                catch { }

                try
                {
                    foreach (Type type in assembly.GetTypes())
                        if (type.Name == shortName) return type;
                }
                catch { }
            }
            return null;
        }
    }
}