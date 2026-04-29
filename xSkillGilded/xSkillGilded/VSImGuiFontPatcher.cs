using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using System.IO;
using System.IO.Compression;

namespace xSkillGilded
{
    // ПАТЧЕР №1: Запускается ДО VSImGui (Добавляет ключи и регистрирует шрифт)
    public class VSImGuiFontPatcher : ModSystem
    {
        public override double ExecuteOrder() { return -0.1; }
        public override bool ShouldLoad(EnumAppSide forSide) { return forSide == EnumAppSide.Client; }

        private static ushort[] _latinExtendedRanges;
        private static GCHandle _latinExtendedHandle;

        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);
            try
            {
                var vsImGui = api.ModLoader.GetModSystem<VSImGui.ImGuiModSystem>();
                if (vsImGui == null) return;

                var loaderType = typeof(VSImGui.ImGuiModSystem).Assembly.GetType("VSImGui.NativesLoader");
                var loadMethod = loaderType.GetMethod("Load", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                loadMethod?.Invoke(null, new object[] { api.Logger, vsImGui });

                IntPtr dummyContext = ImGui.CreateContext();
                ImGui.SetCurrentContext(dummyContext);

                _latinExtendedRanges = new ushort[] {
                    0x0020, 0x00FF, 0x0100, 0x017F, 0x0400, 0x04FF, 0
                };
                _latinExtendedHandle = GCHandle.Alloc(_latinExtendedRanges, GCHandleType.Pinned);
                nint customRangePtr = _latinExtendedHandle.AddrOfPinnedObject();

                var fontManagerType = typeof(VSImGui.API.FontManager);
                var glyphField = fontManagerType.GetField("GlyphRanges", BindingFlags.Static | BindingFlags.NonPublic);

                if (glyphField != null)
                {
                    var dict = (Dictionary<string, nint>)glyphField.GetValue(null);
                    if (dict != null)
                    {
                        // 1. Возвращаем польский, чешский и т.д.
                        dict["pl"] = customRangePtr;
                        dict["cs"] = customRangePtr;
                        dict["sk"] = customRangePtr;

                        // 2. ИСПРАВЛЕНИЕ ВЫЛЕТА: Добавляем забытые ключи для азиатских языков!
                        nint chineseRange = ImGui.GetIO().Fonts.GetGlyphRangesChineseFull();
                        dict["zh-cn"] = chineseRange;
                        dict["zh-tw"] = chineseRange; 
                        dict["ja"] = ImGui.GetIO().Fonts.GetGlyphRangesJapanese();
                        dict["ko"] = ImGui.GetIO().Fonts.GetGlyphRangesKorean();
                    }
                }

                // 3.  CJK шрифт БЕЗ использования api.Assets (так как они еще спят в фазе StartPre)
                string locale = Lang.CurrentLocale;
                if (locale == "zh-cn" || locale == "zh-tw" || locale == "ja" || locale == "ko")
                {
                    var fontsProp = fontManagerType.GetProperty("Fonts", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                    if (fontsProp != null)
                    {
                        var fontsSet = (HashSet<string>)fontsProp.GetValue(null);

                        // СОЗДАЕМ ИЗОЛИРОВАННУЮ ПАПКУ В КЭШЕ, ЧТОБЫ НЕ МУСОРИТЬ В КОРНЕ
                        string tempDir = Path.Combine(GamePaths.DataPath, "Cache", "xSkillGilded");
                        Directory.CreateDirectory(tempDir); 

                        // Сохраняем физический файл для C++ библиотеки ImGui
                        string tempPath = Path.Combine(tempDir, "cjk_font_temp.ttf");
                        bool fontFound = false;

                        string modsDir = Path.Combine(GamePaths.DataPath, "Mods");

                        if (Directory.Exists(modsDir))
                        {
                            foreach (string dir in Directory.GetDirectories(modsDir))
                            {
                                string testPath = Path.Combine(dir, "assets", "xskillgilded", "fonts", "cjk_font.ttf");
                                if (File.Exists(testPath))
                                {
                                    File.Copy(testPath, tempPath, true);
                                    fontFound = true;
                                    break;
                                }
                            }

                            if (!fontFound)
                            {
                                foreach (string zipFile in Directory.GetFiles(modsDir, "*.zip"))
                                {
                                    try
                                    {
                                        using (var archive = ZipFile.OpenRead(zipFile))
                                        {
                                            var entry = archive.GetEntry("assets/xskillgilded/fonts/cjk_font.ttf");
                                            if (entry != null)
                                            {
                                                entry.ExtractToFile(tempPath, true);
                                                fontFound = true;
                                                break;
                                            }
                                        }
                                    }
                                    catch { /* Игнорируем сломанные или чужие архивы */ }
                                    if (fontFound) break;
                                }
                            }
                        }

                        if (fontFound)
                        {
                            fontsSet.Add(tempPath);
                        }
                       
                    }
                }
            }
            catch (Exception ex)
            {
                api.Logger.Error("[xSkillGilded] Ошибка при StartPre: " + ex);
            }
        }

        public override void Dispose()
        {
            if (_latinExtendedHandle.IsAllocated) _latinExtendedHandle.Free();
            base.Dispose();
        }
    }

    // ПАТЧЕР 2: Запускается ПОСЛЕ VSImGui (Меняет стиль окон на новый шрифт)
    public class VSImGuiFontPostPatcher : ModSystem
    {
        // Выполняется после VSImGui (у которого ExecuteOrder = 0)
        public override double ExecuteOrder() { return 0.1; }
        public override bool ShouldLoad(EnumAppSide forSide) { return forSide == EnumAppSide.Client; }

        public override void AssetsLoaded(ICoreAPI api)
        {
            base.AssetsLoaded(api);
            string locale = Lang.CurrentLocale;

            // Если язык азиатский, принудительно переключаем стиль ImGui на загруженный шрифт
            if (locale == "zh-cn" || locale == "zh-tw" || locale == "ja" || locale == "ko")
            {
                try
                {
                    var vsImGui = api.ModLoader.GetModSystem<VSImGui.ImGuiModSystem>();
                    if (vsImGui != null && vsImGui.DefaultStyle != null)
                    {
                        var styleType = vsImGui.DefaultStyle.GetType();
                        var fontNameProp = styleType.GetProperty("FontName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                        if (fontNameProp != null)
                        {
                            fontNameProp.SetValue(vsImGui.DefaultStyle, "cjk_font_temp");
                            api.Logger.Event($"[xSkillGilded] Успешно переключен стиль ImGui на шрифт 'cjk_font_temp'");
                        }
                    }
                }
                catch (Exception ex)
                {
                    api.Logger.Error("[xSkillGilded] Ошибка при переключении FontName в AssetsLoaded: " + ex);
                }
            }
        }
    }
}