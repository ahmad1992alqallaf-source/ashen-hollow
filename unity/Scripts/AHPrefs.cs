// Ashen Hollow: the save store with room for several heroes. Every key the game keeps for a hero (its save, its
// dungeon timers, the lands it has seen, its tutorial tips...) lives under that hero's slot; the settings (sound,
// graphics, controls) and the list of heroes are shared by everyone who plays on this device.
// Slot 0 is the hero from before there were slots and keeps its old keys; new heroes get "c<n>_" in front.
// A slot number is never handed out twice, so a deleted hero's leftovers can never leak into a new one.
using System;
using System.Collections.Generic;
using UnityEngine;

public static class AHPrefs
{
    public const int MaxHeroes = 6;

    // keys shared by every hero on this device
    static bool Global(string k)
    {
        return k.StartsWith("ah_set_") || k == "ah_muted" || k == "ah_music" || k == "ah_sfx" || k == "ah_vroid" || k == "ah_dreamscape"
            || k == SlotKey || k == SlotsKey || k == NextKey || k == KeysKey || k == DeviceKey;
    }
    const string SlotKey = "ah_slot", SlotsKey = "ah_slots", NextKey = "ah_slot_next", KeysKey = "ah_keys", DeviceKey = "ah_device";

    public static int Slot { get { return PlayerPrefs.GetInt(SlotKey, 0); } }
    static string Pre(int slot) { return slot == 0 ? "" : "c" + slot + "_"; }
    static string K(string k) { return Global(k) ? k : Pre(Slot) + k; }

    // every hero key ever written, so a hero can be deleted completely
    static HashSet<string> keys;
    static void Track(string k)
    {
        if (Global(k)) return;
        if (keys == null) { keys = new HashSet<string>(); foreach (var s in PlayerPrefs.GetString(KeysKey, "").Split(',')) if (s != "") keys.Add(s); }
        if (keys.Add(k)) PlayerPrefs.SetString(KeysKey, string.Join(",", new List<string>(keys).ToArray()));
    }

    public static string GetString(string k, string d = "") { return PlayerPrefs.GetString(K(k), d); }
    public static int GetInt(string k, int d = 0) { return PlayerPrefs.GetInt(K(k), d); }
    public static float GetFloat(string k, float d = 0f) { return PlayerPrefs.GetFloat(K(k), d); }
    public static bool HasKey(string k) { return PlayerPrefs.HasKey(K(k)); }
    public static void SetString(string k, string v) { Track(k); PlayerPrefs.SetString(K(k), v); }
    public static void SetInt(string k, int v) { Track(k); PlayerPrefs.SetInt(K(k), v); }
    public static void SetFloat(string k, float v) { Track(k); PlayerPrefs.SetFloat(K(k), v); }
    public static void DeleteKey(string k) { PlayerPrefs.DeleteKey(K(k)); }
    public static void Save() { PlayerPrefs.Save(); }

    // ---------- the heroes on this device ----------
    public class Hero { public int slot; public string name = "", cls = "", area = ""; public int level = 1; public long t; public string look = ""; public string gear = ""; }

    public static List<int> Slots()
    {
        var l = new List<int>();
        if (!PlayerPrefs.HasKey(SlotsKey))
        {
            // the first time: the hero from before slots (if there is one) is slot 0
            if (PlayerPrefs.HasKey("ah_save") || PlayerPrefs.GetString("ah_class", "") != "") l.Add(0);
            PlayerPrefs.SetString(SlotsKey, string.Join(",", l.ConvertAll(i => i.ToString()).ToArray()));
            if (!PlayerPrefs.HasKey(NextKey)) PlayerPrefs.SetInt(NextKey, 1);
            return l;
        }
        foreach (var s in PlayerPrefs.GetString(SlotsKey, "").Split(',')) { int i; if (int.TryParse(s, out i) && !l.Contains(i)) l.Add(i); }
        return l;
    }
    static void SetSlots(List<int> l) { PlayerPrefs.SetString(SlotsKey, string.Join(",", l.ConvertAll(i => i.ToString()).ToArray())); PlayerPrefs.Save(); }

    // the hero being played is on the list once it has a class (the creator has finished)
    public static void Register()
    {
        var l = Slots();
        if (!l.Contains(Slot) && GetString("ah_class", "") != "") { l.Add(Slot); SetSlots(l); }
    }

    public static Hero Info(int slot)
    {
        var h = new Hero { slot = slot };
        string m = PlayerPrefs.GetString(Pre(slot) + "ah_meta", "");
        var s = m.Split('|');
        if (s.Length >= 5) { h.name = s[0]; h.cls = s[1]; int.TryParse(s[2], out h.level); h.area = s[3]; long.TryParse(s[4], out h.t); }
        else h.cls = PlayerPrefs.GetString(Pre(slot) + "ah_class", "");
        if (s.Length >= 7) { h.look = s[5]; h.gear = s[6]; }
        return h;
    }
    public static void WriteMeta(AHPlayer p)
    {
        if (p == null || p.cls == null) return;
        string gear = "";
        foreach (var sl in AHItems.GearSlots) gear += (gear == "" ? "" : ",") + sl + "=" + (AHWardrobe.Shown(p, sl) ?? "");
        SetString("ah_meta", (p.heroName ?? "").Replace("|", "") + "|" + p.cls.id + "|" + p.level + "|" + AHGame.AreaId + "|" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            + "|" + JsonUtility.ToJson(p.look ?? new AHLook()).Replace("|", "") + "|" + gear);
    }

    // play another hero (or a new one: slot -1); the game reloads into it
    public static int Prev = -1;
    public static bool Locked;   // between choosing another hero and the land reloading: nothing is saved
    public static void Use(int slot)
    {
        Prev = Slot; Locked = true;
        if (slot < 0) { slot = Math.Max(1, PlayerPrefs.GetInt(NextKey, 1)); PlayerPrefs.SetInt(NextKey, slot + 1); }
        PlayerPrefs.SetInt(SlotKey, slot);
        PlayerPrefs.Save();
        // what the last hero left in memory
        AHFinder.Pick = ""; AHFinder.Run = ""; AHFinder.Role = "";
        AHAuto.On = false;
        AHTutorial.Restart();
    }

    public static void Delete(int slot)
    {
        var l = Slots(); l.Remove(slot); SetSlots(l);
        if (keys == null) Track("ah_class");
        foreach (var k in new List<string>(keys)) PlayerPrefs.DeleteKey(Pre(slot) + k);
        foreach (var k in new[] { "ah_save", "ah_class", "ah_meta", "ah_area", "ah_seen" }) PlayerPrefs.DeleteKey(Pre(slot) + k);
        PlayerPrefs.Save();
    }

    // ---------- this device ----------
    // A random id made once per install. Today it only tags saves; when the game has a server, the server will use it
    // (with the account and the network address) to limit how many heroes one device can make and to spot bot farms.
    public static string DeviceId
    {
        get
        {
            string id = PlayerPrefs.GetString(DeviceKey, "");
            if (id == "") { id = Guid.NewGuid().ToString("N"); PlayerPrefs.SetString(DeviceKey, id); PlayerPrefs.Save(); }
            return id;
        }
    }
}
