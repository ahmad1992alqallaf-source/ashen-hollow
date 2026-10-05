// Ashen Hollow: the world data exported from the web game (all distances in metres)
using System;

[Serializable] public class AHBounds { public float x0, z0, x1, z1; }
[Serializable] public class AHPoint { public float x, z; }
[Serializable] public class AHMobType
{
    public string id, name, model; public int lvl, hp, dmg, xp; public float speed, radius, aggro; public bool flee;
    // from the web game's MOBS table (monsters.json), filled in by AHGame
    public int gold, skinReq; public float atkCd = 1.3f, respawn = 18f; public bool night, lunge, elite, rare, noSkin; public string[] skin;
    [NonSerialized] public System.Collections.Generic.List<object> drops;
}
[Serializable] public class AHMobSpawn { public string type; public float x, z; }
[Serializable] public class AHCircle { public float x, z, r; }
[Serializable] public class AHBox { public float x, z, w, h; }
[Serializable] public class AHWaterArea { public float x, z, rx, rz; }
[Serializable] public class AHGroundMap { public float x0, z0, w, h; }
// a way out of the area (web passCheck): a circle (r > 0) at a gap in the border, or a strip at the end of a road
// a city gate (web GATES / useGate): a button by the gate takes you in or out
[Serializable] public class AHGate { public float x, z, r, tx, tz, f; public string to, label, name, dung; }
[Serializable] public class AHExit { public float x0, z0, x1, z1, x, z, r, tx, tz, f; public string to; }

[Serializable]
public class AHWorldData
{
    public int version;
    public string id, levels;
    public AHGroundMap groundMap;
    public string region;
    public AHBounds bounds;
    public AHPoint spawn;
    public float playerSpeed;
    public AHMobType[] mobTypes;
    public AHMobSpawn[] mobs;
    public AHCircle[] circles;
    public AHBox[] boxes;
    public AHWaterArea[] water;
    public AHExit[] exits;
    public AHGate[] gates;
    public AHBox[] dgates;   // a dungeon's iron gates between rooms (open once the room behind is cleared)
    public AHBox[] walk;     // if given, the only floor there is (the Sunken Forge's rooms)
    public float dayLength;
}

// the hero's look, as in the web game (LOOK_DEF): option names and list indexes
[Serializable]
public class AHLook
{
    public string sex = "m", body = "average", eyeShape = "almond", brow = "arched", mouth = "neutral", nose = "none", ears = "round", mark = "none", hair = "pony", beard = "none";
    public int height = 2, skin = 1, eye = -1, markCol = 0, hairCol = 0, hiCol = 9, cloth = 0;
}
