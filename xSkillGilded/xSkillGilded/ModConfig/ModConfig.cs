using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xSkillGilded {
    public class ModConfig {
        public bool lvPopupEnabled { get; set; }      = true;

        public bool effectBoxEnabled { get; set; }    = true;
        public float effectBoxOriginX { get; set; }   = 8f;
        public float effectBoxOriginY { get; set; }   = 8f;
        public float effectBoxSize { get; set; }      = 40f;
        public int effectBoxOrientation { get; set; } = 0;
    }
}
