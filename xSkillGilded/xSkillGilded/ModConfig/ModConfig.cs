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
    }
}