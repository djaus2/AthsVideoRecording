using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AthsVideoRecording.Data
{
    public enum TrackType { run, walk, steeple, hurdles, relay, none = 100 }
    public enum Gender { male, female, mixed, none = 100 }
    public enum AgeGrouping { junior, open, masters, none = 100 }
    public enum MastersAgeGroup { M30, M35, M40, M45, M50, M55, M60, M65, M70, M75, M80, M85, M90, M95, W30, W35, W40, W45, W50, W55, W60, W65, W70, W75, W80, W85, W90, W95, other = 100 }

    public enum MaleMastersAgeGroup { M30, M35, M40, M45, M50, M55, M60, M65, M70, M75, M80, M85, M90, M95, other = 100 }

    public enum FemaleMastersAgeGroup { W30, W35, W40, W45, W50, W55, W60, W65, W70, W75, W80, W85, W90, W95, other = 100 }

    public enum UnderAgeGroup { U13, U14, U15, U16, U17, U18, U19, U20, other = 100 }
    public enum LittleAthleticsAgeGroup { U6, U7, U8, U9, U10, U11, U12, other = 100 }

}
