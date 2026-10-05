// Ashen Hollow: item icons. Each item was a coloured circle with three letters on it ("GTR", "LOG"). Now the circle
// carries a white picture of the thing: a log, ore nuggets, a bar, a fish, a helm, a hood, a robe, boots, gloves, a ring,
// an amulet, a sword, a bow, a staff, a potion, a pie, a card... (75 pictures, chosen by the item's form, or its kind
// when it has no form). The circle keeps the item's own colour, so bronze, iron and obsidian gear still look different.
// Pictures: game-icons.net (CC BY 3.0, see Icons/LICENSE_game-icons.txt), in Resources/AH/Icons/item_icons.png.
using System.Collections.Generic;
using UnityEngine;

public static class AHItemIcons
{
    static Texture2D atlas; static object index; static bool tried;
    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    public static Sprite Get(ItemDef d)
    {
        if (d == null) return null;
        string key = !string.IsNullOrEmpty(d.form) ? d.form : d.icon;
        if (string.IsNullOrEmpty(key)) return null;
        Sprite s; if (cache.TryGetValue(key, out s) && s != null) return s;
        if (atlas == null && !tried)
        {
            tried = true;
            atlas = Resources.Load<Texture2D>("AH/Icons/item_icons");
            var ta = Resources.Load<TextAsset>("AH/Icons/item_icons_index");
            if (atlas == null || ta == null) { Debug.LogWarning("Ashen Hollow: item icon atlas missing"); atlas = null; }
            else { atlas.wrapMode = TextureWrapMode.Clamp; index = AHJson.Parse(ta.text); }
        }
        if (atlas != null)
        {
            var ids = AHJson.O(index, "index") as Dictionary<string, object>;
            object cell = null;
            if (ids != null && !ids.TryGetValue(key, out cell) && !string.IsNullOrEmpty(d.icon)) ids.TryGetValue(d.icon, out cell);
            if (cell != null)
            {
                int n = (int)System.Convert.ToDouble(cell), cols = (int)AHJson.N(index, "cols", 12);
                float c = (float)AHJson.N(index, "cell", 128);
                float k = atlas.width / (cols * c); c *= k;   // the importer may have scaled the atlas down
                var r = new Rect((n % cols) * c, atlas.height - (n / cols + 1) * c, c, c);
                s = Sprite.Create(atlas, r, new Vector2(0.5f, 0.5f), 100f); s.name = "item_" + key;
            }
        }
        cache[key] = s; return s;
    }
}
