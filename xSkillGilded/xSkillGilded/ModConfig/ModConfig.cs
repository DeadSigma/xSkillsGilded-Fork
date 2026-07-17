using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xSkillGilded
{
    public class ModConfig
    {
        public bool lvPopupEnabled { get; set; } = true;

        public bool effectBoxEnabled { get; set; } = true;
        public float effectBoxOriginX { get; set; } = 8f;
        public float effectBoxOriginY { get; set; } = 8f;
        public float effectBoxSize { get; set; } = 40f;
        public int effectBoxOrientation { get; set; } = 0;

        public bool EnableCustomFont { get; set; } = true;
        public string _comment_CustomFontPath { get; set; } = "Optional: absolute path to a .ttf font file. Example: C:\\Windows\\Fonts\\simhei.ttf (Use double slashes!)";
        public string CustomFontPath { get; set; } = "";

        /// <summary>Игрок хоть раз двигал окно. Пока false - окно центрируется, как раньше</summary>
        public bool windowPosSet = false;

        /// <summary>Позиция окна относительно viewport.Pos, а не абсолютная</summary>
        public int windowX = 0;
        public int windowY = 0;

        /// <summary>Множитель размера поверх ClientSettings.GUIScale</summary>
        public float windowScale = 1f;

        /// <summary>Игрок хоть раз двигал попап. Пока false - центр сверху, как раньше</summary>
        public bool levelPopupPosSet = false;

        /// <summary>Позиция попапа относительно viewport.Pos</summary>
        public int levelPopupX = 0;
        public int levelPopupY = 0;

        /// <summary>Множитель размера попапа поверх ClientSettings.GUIScale</summary>
        public float levelPopupScale = 1f;
    }
}