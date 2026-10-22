// Ashen Hollow: another player, as you see them. Built like your own hero (class, looks, the gear or costume they
// show, their mount), their name and title above them; it glides to where they are and plays what they are playing.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AHNetAvatar : MonoBehaviour
{
    AHNet.Peer peer; AHAnim anim, mountAnim; Transform hero, mount; float seat; Text nameT, sayT; RectTransform sayBg; float sayUntil; string playing;
    Vector3 vel; float yawVel;

    public static AHNetAvatar Make(AHGame g, AHNet.Peer p)
    {
        var go = new GameObject("Player " + p.name);
        go.transform.position = p.pos; go.transform.rotation = Quaternion.Euler(0f, p.yaw, 0f);
        var av = go.AddComponent<AHNetAvatar>(); av.peer = p;
        try { av.Build(g); } catch (System.Exception e) { Debug.LogWarning("Ashen Hollow: could not draw " + p.name + " (" + e.Message + ")"); }
        return av;
    }

    void Build(AHGame g)
    {
        var cls = AHClasses.Get(peer.cls) ?? AHClasses.Get("warrior");
        AHLook look = null; try { look = string.IsNullOrEmpty(peer.look) ? new AHLook() : JsonUtility.FromJson<AHLook>(peer.look); } catch { } if (look == null) look = new AHLook();
        var gear = (peer.gear ?? "").Split(','); var slots = AHItems.GearSlots;
        System.Func<string, string> shown = s => { int i = System.Array.IndexOf(slots, s); return i >= 0 && i < gear.Length && gear[i].Length > 0 && AHItems.Get(gear[i]) != null ? gear[i] : null; };
        hero = new GameObject("Hero").transform; hero.SetParent(transform, false);
        var rig = AHPeople.BuildHero(hero, cls, look, g, out anim);
        if (rig != null)
        {
            if (!string.IsNullOrEmpty(peer.costume) && AHCostumes.Apply(rig, peer.costume, look)) AHWardrobe.DressWith(rig, hero, s => null, () => false);
            else { if (shown("chest") != null) AHCostumes.Apply(rig, AHCostumes.Under, look); AHWardrobe.DressWith(rig, hero, shown, () => peer.clip != null && peer.clip.StartsWith("Run"), look.sex == "f" ? "f" : "m", cls.id); }
        }
        foreach (var c in GetComponentsInChildren<Collider>(true)) Destroy(c);
        // the mount they ride (built when they are on it)
        if (!string.IsNullOrEmpty(peer.mount) && peer.mount != AHPhoenix.Id)
        {
            mount = new GameObject("Mount").transform; mount.SetParent(transform, false);
            AHModel.Spawn(mount, "Comp/mount_" + peer.mount, 0f, false, 0f, out mountAnim);
            float msc = (float)AHJson.N(AHJson.O(AHComp.Mounts, peer.mount), "scale", 1); if (msc != 1f) mount.localScale = Vector3.one * msc;
            foreach (var c in mount.GetComponentsInChildren<Collider>(true)) Destroy(c);
            AHModel.SetShadows(mount.gameObject);
            seat = AHComp.Seat(peer.mount);
            mount.gameObject.SetActive(false);
        }
        // the name plate: name, level and title (party members in green)
        var cv = new GameObject("Plate", typeof(RectTransform)); cv.transform.SetParent(transform, false); cv.transform.localPosition = Vector3.up * 2.35f;
        var canvas = cv.AddComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; ((RectTransform)cv.transform).sizeDelta = new Vector2(400, 120); cv.transform.localScale = Vector3.one * 0.006f;
        nameT = MakeText(cv.transform, new Vector2(0, 20), 46, Color.white); nameT.fontStyle = FontStyle.Bold;
        sayBg = new GameObject("Say", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>(); sayBg.SetParent(cv.transform, false); sayBg.anchoredPosition = new Vector2(0, 110); sayBg.sizeDelta = new Vector2(380, 70);
        sayBg.GetComponent<Image>().color = new Color(0.06f, 0.05f, 0.05f, 0.8f); sayT = MakeText(sayBg, Vector2.zero, 34, new Color(1f, 0.95f, 0.85f)); sayBg.gameObject.SetActive(false);
        Plate();
    }
    static Text MakeText(Transform par, Vector2 at, int size, Color c)
    {
        var go = new GameObject("T", typeof(RectTransform)); go.transform.SetParent(par, false); var rt = (RectTransform)go.transform; rt.anchoredPosition = at; rt.sizeDelta = new Vector2(400, 70);
        var t = go.AddComponent<Text>(); t.font = AHUI.MainFont; t.fontSize = size; t.alignment = TextAnchor.MiddleCenter; t.color = c; t.horizontalOverflow = HorizontalWrapMode.Overflow;
        var sh = go.AddComponent<Outline>(); sh.effectColor = new Color(0f, 0f, 0f, 0.8f); sh.effectDistance = new Vector2(2, -2);
        return t;
    }
    void Plate()
    {
        if (nameT == null) return;
        bool party = AHNet.PartyHas(peer.actor);
        nameT.text = peer.name + "  <size=34>lv " + peer.level + "</size>" + (string.IsNullOrEmpty(peer.title) ? "" : "\n<size=30><color=#ffd27a>“" + peer.title + "”</color></size>");
        nameT.color = party ? new Color(0.55f, 1f, 0.55f) : new Color(0.7f, 0.88f, 1f);
    }
    public void Say(string t) { if (sayT == null) return; sayT.text = AHLoc.Lines(t, 26); sayBg.gameObject.SetActive(true); sayUntil = Time.time + Mathf.Clamp(t.Length * 0.08f + 2f, 3f, 7f); }

    float plateT;
    void Update()
    {
        if (peer == null) { Destroy(gameObject); return; }
        float dt = Time.deltaTime;
        // glide to where they are (a long way off: jump there)
        Vector3 to = peer.pos; if ((to - transform.position).sqrMagnitude > 400f) transform.position = to;
        transform.position = Vector3.SmoothDamp(transform.position, to, ref vel, 0.12f);
        float y = Mathf.SmoothDampAngle(transform.eulerAngles.y, peer.yaw, ref yawVel, 0.08f); transform.rotation = Quaternion.Euler(0f, y, 0f);
        // riding
        bool ride = peer.mounted && mount != null;
        if (mount != null && mount.gameObject.activeSelf != ride) mount.gameObject.SetActive(ride);
        if (hero != null) hero.localPosition = new Vector3(0f, ride ? seat : 0f, 0f);
        if (ride && mountAnim != null) { float sp = vel.magnitude; string mc = sp > 4f ? "Gallop" : sp > 0.6f ? "Walk" : "Idle"; if (!mountAnim.Play(mc, true)) { if (!mountAnim.Play(sp > 0.6f ? "Run" : "Idle", true)) mountAnim.Play("Idle", true); } mountAnim.Tick(dt); }
        // what they are doing
        if (anim != null)
        {
            string c = string.IsNullOrEmpty(peer.clip) ? "Idle" : peer.clip;
            if (c != playing || peer.loop) { if (c != playing) { if (!anim.Play(c, peer.loop, peer.speed, true)) anim.Play("Idle", true); playing = c; } }
            anim.Tick(dt);
        }
        // the name plate faces the camera
        var cam = Camera.main;
        if (nameT != null && cam != null) nameT.transform.parent.rotation = Quaternion.LookRotation(nameT.transform.parent.position - cam.transform.position);
        if (sayBg != null && sayBg.gameObject.activeSelf && Time.time > sayUntil) sayBg.gameObject.SetActive(false);
        if ((plateT -= dt) <= 0f) { plateT = 1f; Plate(); }
        // gone quiet for a while (their phone slept): hidden until they move again
        bool alive = Time.time - peer.seen < 20f; if (hero != null && hero.gameObject.activeSelf != alive) hero.gameObject.SetActive(alive);
    }
    void OnDestroy() { if (anim != null) anim.Dispose(); if (mountAnim != null) mountAnim.Dispose(); }
}
