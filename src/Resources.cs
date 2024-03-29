using UnityEngine;

namespace everlaster
{
    static class Colors
    {
        public static Color backgroundGray = new Color(0.85f, 0.85f, 0.85f);
        public static Color inactive = new Color(0.5f, 0.5f, 0.5f);
    }

    static class JSONKeys
    {
        public const string DYNAMIC_ITEM_PARAMS = "DynamicItemParams";
        public const string NAME = "Name";
        public const string TRIGGER = "Trigger";
        public const string TRIGGERS = "Triggers";
        public const string VERSION = "Version";
    }
}
