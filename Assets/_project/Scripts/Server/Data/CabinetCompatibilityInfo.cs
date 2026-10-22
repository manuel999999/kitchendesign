using System.Collections.Generic;

namespace KitchenDesign.Server
{
    public class CabinetCompatibilityInfo
    {
        public int id;
        public string name;
        public HashSet<int> productTagIdDenylist = new ();
        public HashSet<int> productTagIdAllowlist = new ();
    }
}
