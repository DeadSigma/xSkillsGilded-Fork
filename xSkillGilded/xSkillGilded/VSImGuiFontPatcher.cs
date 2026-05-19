using ImGuiNET;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace xSkillGilded
{
    // ПАТЧЕР Запускается ДО VSImGui (Добавляет ключи и регистрирует шрифт)
    public class VSImGuiFontPatcher : ModSystem
    {
        public override double ExecuteOrder() { return -0.1; }
        public override bool ShouldLoad(EnumAppSide forSide) { return forSide == EnumAppSide.Client; }

        private static ushort[] _latinExtendedRanges;
        private static GCHandle _latinExtendedHandle;

        // Переменная для передачи имени загруженного шрифта во второй патчер
        public static string SuccessfullyLoadedFontName = null;

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
                        dict["pl"] = customRangePtr;
                        dict["cs"] = customRangePtr;
                        dict["sk"] = customRangePtr;

                        // Назначаем полные/общие наборы символов
                        nint chineseRange = ImGui.GetIO().Fonts.GetGlyphRangesChineseFull();
                        dict["zh-cn"] = chineseRange;
                        dict["zh-tw"] = chineseRange;
                        dict["ja"] = ImGui.GetIO().Fonts.GetGlyphRangesJapanese();
                        dict["ko"] = ImGui.GetIO().Fonts.GetGlyphRangesKorean();
                    }
                }

                // 1. ЧИТАЕМ КОНФИГ НАПРЯМУЮ ИЗ ФАЙЛА
                string configPath = Path.Combine(GamePaths.DataPath, "ModConfig", "xskillsgilded.json");
                bool enableCustomFont = false;
                string customFontPath = "";

                if (File.Exists(configPath))
                {
                    try
                    {
                        string json = File.ReadAllText(configPath);
                        var parsedConfig = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                        if (parsedConfig != null)
                        {
                            if (parsedConfig.ContainsKey("EnableCustomFont")) enableCustomFont = Convert.ToBoolean(parsedConfig["EnableCustomFont"]);
                            if (parsedConfig.ContainsKey("CustomFontPath")) customFontPath = parsedConfig["CustomFontPath"].ToString();
                        }
                    }
                    catch (Exception ex)
                    {
                        api.Logger.Error("[xSkillGilded] Ошибка чтения конфига в StartPre: " + ex);
                    }
                }

                // 2. ЗАГРУЖАЕМ ШРИФТ
                var fontsProp = fontManagerType.GetProperty("Fonts", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                if (fontsProp != null)
                {
                    var fontsSet = (HashSet<string>)fontsProp.GetValue(null);

                    // ПРОВЕРКА: Если шрифты включены в конфиге
                    if (enableCustomFont)
                    {
                        // Сценарий А: Пользователь указал свой шрифт
                        if (!string.IsNullOrEmpty(customFontPath) && File.Exists(customFontPath))
                        {
                            fontsSet.Add(customFontPath);
                            SuccessfullyLoadedFontName = Path.GetFileNameWithoutExtension(customFontPath);
                            api.Logger.Event($"[xSkillGilded] Успешно загружен кастомный шрифт пользователя: {customFontPath}");
                        }
                        // Сценарий Б: Азиатский язык, используем встроенный 
                        else
                        {
                            string locale = Lang.CurrentLocale;
                            if (locale == "zh-cn" || locale == "zh-tw" || locale == "ja" || locale == "ko")
                            {
                                string tempDir = Path.Combine(GamePaths.DataPath, "Cache", "xSkillGilded");
                                Directory.CreateDirectory(tempDir);

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
                                            catch { }
                                            if (fontFound) break;
                                        }
                                    }
                                }

                                if (fontFound)
                                {
                                    fontsSet.Add(tempPath);
                                    SuccessfullyLoadedFontName = "cjk_font_temp";
                                    api.Logger.Event("[xSkillGilded] Успешно распакован и загружен встроенный CJK шрифт.");
                                }
                            }
                        }
                    }
                    else
                    {
                        // Если enableCustomFont == false, ничего не делаем
                        api.Logger.Event("[xSkillGilded] Custom fonts are disabled in the config. ImGui will use the default font instead.");
                    }
                }
            }
            catch (Exception ex)
            {
                api.Logger.Error("[xSkillGilded] Error with StartPre: " + ex);
            }
        }

        public override void Dispose()
        {
            if (_latinExtendedHandle.IsAllocated) _latinExtendedHandle.Free();
            base.Dispose();
        }
    }

    // Запускается ПОСЛЕ VSImGui (Меняет стиль окон на новый шрифт)
    public class VSImGuiFontPostPatcher : ModSystem
    {
        public override double ExecuteOrder() { return 0.1; }
        public override bool ShouldLoad(EnumAppSide forSide) { return forSide == EnumAppSide.Client; }

        public override void AssetsLoaded(ICoreAPI api)
        {
            base.AssetsLoaded(api);

            // Если шрифт (пользовательский или встроенный) был успешно загружен в первом патчере
            if (!string.IsNullOrEmpty(VSImGuiFontPatcher.SuccessfullyLoadedFontName))
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
                            fontNameProp.SetValue(vsImGui.DefaultStyle, VSImGuiFontPatcher.SuccessfullyLoadedFontName);
                            api.Logger.Event($"[xSkillGilded] Successfully switched the xSkillsGilded style to the font. '{VSImGuiFontPatcher.SuccessfullyLoadedFontName}'");
                        }
                    }
                }
                catch (Exception ex)
                {
                    api.Logger.Error("[xSkillGilded]Error while switching FontName in AssetsLoaded: " + ex);
                }
            }
        }
    }
}