// Ashen Hollow: item icons. Each item was a coloured circle with three letters on it ("GTR", "LOG"). Now the circle
// carries a white picture of the thing: a log, ore nuggets, a bar, a fish, a helm, a hood, a robe, boots, gloves, a ring,
// an amulet, a sword, a bow, a staff, a potion, a pie, a card... (75 pictures, chosen by the item's form, or its kind
// when it has no form). The circle keeps the item's own colour, so bronze, iron and obsidian gear still look different.
// Pictures: game-icons.net (CC BY 3.0, see Icons/LICENSE_game-icons.txt), in Resources/AH/Icons/item_icons.png.
// Every item that is not gear also has its very own full-colour picture (Resources/AH/Icons/item_icons_id.png, made by
// tools/icons/build.py): each fish its own fish, each gem its own cut and colour, dishes on a plate, soups in a bowl,
// seed bags showing their crop, reins with their mount, treasure maps with their land's mark, monster cards with the
// monster on them, and silver / gold stars on Fine and Masterwork food. Gear shows its studio photograph (AHItemStudio).
using System.Collections.Generic;
using UnityEngine;

public static class AHItemIcons
{
    static Texture2D atlas; static object index; static bool tried;
    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    static Texture2D idAtlas; static Dictionary<string, object> idIndex; static int idCols = 18; static float idCell = 112f; static bool idTried;
    static readonly Dictionary<string, Sprite> idCache = new Dictionary<string, Sprite>();

    // the item's own full-colour picture, if it has one
    public static Sprite Own(ItemDef d)
    {
        if (d == null) return null;
        Sprite s; if (idCache.TryGetValue(d.id, out s)) return s;
        if (!idTried)
        {
            idTried = true;
            idAtlas = Resources.Load<Texture2D>("AH/Icons/item_icons_id");
            var ta = Resources.Load<TextAsset>("AH/Icons/item_icons_id_index");
            if (idAtlas != null && ta != null)
            {
                var o = AHJson.Parse(ta.text); idIndex = AHJson.O(o, "index") as Dictionary<string, object>;
                idCols = (int)AHJson.N(o, "cols", 18); idCell = (float)AHJson.N(o, "cell", 112); idAtlas.wrapMode = TextureWrapMode.Clamp;
            }
        }
        object cell = null;
        if (idAtlas != null && idIndex != null && idIndex.TryGetValue(d.id, out cell))
        {
            int n = (int)System.Convert.ToDouble(cell);
            float c = idCell * idAtlas.width / (idCols * idCell);
            s = Sprite.Create(idAtlas, new Rect((n % idCols) * c, idAtlas.height - (n / idCols + 1) * c, c, c), new Vector2(0.5f, 0.5f), 100f); s.name = "own_" + d.id;
        }
        idCache[d.id] = s; return s;
    }

    // the best picture of an item anywhere in the game: gear its photograph, everything else its own picture, then the glyph
    public static Sprite Best(ItemDef d) { return d == null ? null : AHItemStudio.Icon(d) ?? Own(d) ?? Get(d); }
    // a full-colour picture (sits on a dark socket) rather than a white glyph (sits on the item's coloured circle)
    public static bool Full(ItemDef d, Sprite s) { return s != null && d != null && !s.name.StartsWith("item_"); }

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
