using System.Collections.Generic;
using System.Linq;

namespace RebalancedCrests.Crests
{
    /// <summary>
    /// A crest as players know it, mapped to the game's internal crest IDs. The display name doubles as the
    /// config section name.
    /// </summary>
    internal sealed class Crest
    {
        internal static readonly Crest Hunter = new Crest("Hunter", "Hunter", "Hunter_v2", "Hunter_v3");
        internal static readonly Crest Reaper = new Crest("Reaper", "Reaper");
        internal static readonly Crest Wanderer = new Crest("Wanderer", "Wanderer");
        internal static readonly Crest Beast = new Crest("Beast", "Warrior");
        internal static readonly Crest Architect = new Crest("Architect", "Toolmaster");
        internal static readonly Crest Witch = new Crest("Witch", "Witch");
        internal static readonly Crest Shaman = new Crest("Shaman", "Spell");

        internal static readonly IReadOnlyList<Crest> All = new[] { Hunter, Reaper, Wanderer, Beast, Architect, Witch, Shaman };

        private readonly string[] _ids;

        private Crest(string name, params string[] ids)
        {
            Name = name;
            _ids = ids;
        }

        internal string Name { get; }

        internal bool IsEquipped => PlayerData.instance != null && _ids.Contains(PlayerData.instance.CurrentCrestID);
    }
}
