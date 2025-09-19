using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HamamatsuCamera
{
    public static class StaticData
    {
        public const double SubArr_OFF = 1.0;
        public const double SubArr_ON = 2.0;

        /// <summary>
        /// Common string lookups.
        /// </summary>
        public static class Strings
        {
            // Camera Setting Strings
            public const string SETTINGS = "SETTINGS";
            public const string SUBARR = "SUBARRAY";
            public const string SUBARR_HPOS = "SUBARRAY HPOS";
            public const string SUBARR_VPOS = "SUBARRAY VPOS";
            public const string SUBARR_HSIZE = "SUBARRAY HSIZE";
            public const string SUBARR_VSIZE = "SUBARRAY VSIZE";
            public const string SUBARR_MODE = "SUBARRAY MODE";
            public const string NUM_PIXELS_HORZ = "IMAGE DETECTOR PIXEL NUM HORZ";
            public const string NUM_PIXELS_VERT = "IMAGE DETECTOR PIXEL NUM VERT";
            public const string BINNING = "BINNING";
            public const string IMG_WIDTH = "IMAGE WIDTH";
            public const string IMG_HEIGHT = "IMAGE HEIGHT";
            public const string IMG_ROWBYTES = "IMAGE ROWBYTES";
            public const string IMG_FRAMEBYTES = "IMAGE FRAMEBYTES";
            public const string IMG_TOP_OFFSET_BYTES = "IMAGE TOP OFFSET BYTES";

            // Misc. Setting Strings
            public const string CROP_MODE = "Crop Mode";
            public const string AUTO = "Auto";
            public const string MANUAL = "Manual";
            public const string LUT = "LUT";
            public const string LUT_MIN = "LUT Min";
            public const string LUT_MAX = "LUT Max";

            // Other Strings
            public const string ERROR = "Error:";
        }

        public static Dictionary<int, string> GroupNames = new Dictionary<int, string>()
        {
            { 0         , "Miscellaneous"           },
            { 1         , "Sensor Mode and Speed"   },
            { 2         , "Trigger"                 },
            { 4         , "Feature"                 },
            { 8         , "Output Trigger"          },
            { 128       , "Sensor Cooler"           },
            { 1024      , "Binning and ROI"         },
            { 2048      , "Sensor Mode and Speed"   },
            { 4096      , "ALU"                     },
            { 8192      , "System Information 1"    },
            { 65536     , "Synchronous Timing"      },
            { 131072    , "System Information 2"    },
            { 262144    , "System Information 3"    },
            { 4194304   , "System Information 4"    },
            { 8388608   , "Master Pulse"            },
            { 33554432  , "Data Reduction"          }
        };

        public static Dictionary<string, int> MiscSettingGroups = new Dictionary<string, int>()
        {
            { Strings.CROP_MODE  , 1024   }
        };
    }
}
