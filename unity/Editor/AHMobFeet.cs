// Ashen Hollow (editor test): "Test: Monster Feet Check" (shift+alt+F). Every monster of every area and dungeon, built as
// the game builds it, posed in its idle and its walk; the lowest point of its body in each pose is measured against
// the ground it stands on (its own feet: the ground is where the monster's root is). HeroShots/mob_feet.txt lists every
// monster (gap in metres, + is above the ground); those floating or sunk are photographed from the side standing on a
// grey floor: HeroShots/mob_feet_<n>.png. Monsters meant to hover (wraiths, the risen dead: "float") are noted as such.
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class AHMobFeet
{
    [MenuItem("Ashen Hollow/Test: Monster Feet Check #&f")]
    static void Run()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        string dir = Path.Combine(Application.dataPath, "../HeroShots"); Directory.CreateDirectory(dir);
        var types = new List<AHMobType>(); var seen = new HashSet<string>();
        foreach (var ta in Resources.LoadAll<TextAsset>("AH/Areas"))
        {
            object d; try { d = AHJson.Parse(ta.text); } catch { continue; }
            var mts = AHJson.A(d, "mobTypes"); if (mts == null) continue;
            foreach (var o in mts)
            {
                string id = AHJson.S(o, "id"); if (id == null || !seen.Add(id)) continue;
                types.Add(new AHMobType { id = id, name = AHJson.S(o, "name", id), model = AHJson.S(o, "model"), hp = 10, lvl = 1, speed = 0, radius = 0.5f });
            }
        }
        // the ones that only come as a boss's adds or from events
        var all = AHDB.Mobs as Dictionary<string, object>;
        if (all != null) foreach (var kv in all) if (seen.Add(kv.Key)) types.Add(new AHMobType { id = kv.Key, name = AHJson.S(kv.Value, "name", kv.Key), model = AHJson.S(kv.Value, "model"), hp = 10, lvl = 1, speed = 0, radius = 0.5f });
        types.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
        g.StartCoroutine(Check(g, types, dir));
    }

    // in batches: built, played in their idle (then their walk) for real frames, measured every frame
    static System.Collections.IEnumerator Check(AHGame g, List<AHMobType> types, string dir)
    {
        Vector3 sky = g.player.transform.position + Vector3.up * 600f;
        var sb = new StringBuilder(); var bad = new List<KeyValuePair<GameObject, string>>(); int nBad = 0, sheet = 0;
        var bake = new Mesh();
        var fix = new SortedDictionary<string, float>();
        for (int b0 = 0; b0 < types.Count; b0 += 20)
        {
            var batch = new List<KeyValuePair<AHMobType, AHMob>>();
            for (int i = b0; i < Mathf.Min(types.Count, b0 + 20); i++)
            {
                var t = types[i]; AHMob m = null;
                try { m = AHMob.Create(g, t, sky + new Vector3((i % 20) * 30f, 0f, 0f)); }
                catch (System.Exception e) { sb.AppendLine("ERROR\t" + t.id + "\t" + e.Message); continue; }
                m.enabled = false; m.transform.rotation = Quaternion.identity;
                foreach (var smr in m.GetComponentsInChildren<SkinnedMeshRenderer>(true)) smr.updateWhenOffscreen = true;
                batch.Add(new KeyValuePair<AHMobType, AHMob>(t, m));
            }
            var mins = new Dictionary<AHMob, float[]>();
            foreach (var kv in batch) mins[kv.Value] = new[] { float.MaxValue, float.MaxValue };
            for (int c = 0; c < 2; c++)
            {
                string clip = c == 0 ? "Idle" : "Walk";
                foreach (var kv in batch) { var a = kv.Value.anim; if (a != null && a.HasClips && !a.Play(clip, true, 1f, true) && c == 1) a.Play("Idle", true, 1f, true); }
                float t0 = Time.time;
                while (Time.time - t0 < 1.1f)
                {
                    foreach (var kv in batch) if (kv.Value != null && kv.Value.anim != null) kv.Value.anim.Tick(Time.deltaTime);
                    yield return null;
                    // after the blend in (a quarter second), every frame counts
                    if (Time.time - t0 < 0.3f) continue;
                    foreach (var kv in batch) { if (kv.Value == null) continue; float lo = Lowest(kv.Value.gameObject, bake) - kv.Value.transform.position.y; var mm = mins[kv.Value]; mm[c] = Mathf.Min(mm[c], lo); }
                }
            }
            foreach (var kv in batch)
            {
                var t = kv.Key; var m = kv.Value; if (m == null) continue;
                float h = Mathf.Max(0.3f, m.height); var mm = mins[m];
                var sk = AHJson.O(AHDB.Table("monsters", "BEASTS"), t.id) ?? AHJson.O(AHDB.Table("monsters", "MOB_SKINS"), t.id);
                float fl = sk != null ? (float)AHJson.N(sk, "float", 0) : 0f;
                // standing still its feet should touch; walking, the lowest foot touches at each step (a toad's walk is a
                // hop, squashed on landing: its idle counts)
                bool hop = sk != null && AHJson.B(sk, "hop");
                float gap = hop ? mm[0] : Mathf.Min(mm[0], mm[1]);
                // the feet fix (a share of its height), added to what it already had; not for those meant to hover or
                // the sea beasts that swim
                float old = AHMob.FeetFix(t.id), nw = old;
                if (fl <= 0f && t.id != "seaserpent" && t.id != "thalassa" && Mathf.Abs(gap) > Mathf.Max(0.02f, h * 0.01f)) nw = old + gap / h;
                if (Mathf.Abs(nw) > 1e-4f) fix[t.id] = nw;
                string verdict = gap > Mathf.Max(0.05f, h * 0.04f) ? "FLOAT" : gap < -Mathf.Max(0.08f, h * 0.06f) ? "SUNK" : "ok";
                if (fl > 0f && verdict == "FLOAT") verdict = "hover";
                sb.AppendLine(verdict + "\t" + t.id + "\t" + t.name + "\tidle " + mm[0].ToString("F2") + " walk " + mm[1].ToString("F2") + "\theight " + h.ToString("F2") + "\t" + (m.transform.childCount > 0 ? m.transform.GetChild(0).name : "?") + (fl > 0f ? " (meant to hover " + fl.ToString("F2") + ")" : "") + (m.anim != null && m.anim.HasClips ? "" : " (no clips)"));
                if (verdict == "FLOAT" || verdict == "SUNK" || old != 0f)
                {
                    if (verdict == "ok") verdict = "fixed";
                    if (verdict != "fixed") nBad++;
                    var pl = GameObject.CreatePrimitive(PrimitiveType.Plane); Object.DestroyImmediate(pl.GetComponent<Collider>());
                    pl.name = "Plane"; pl.transform.SetParent(m.transform, false); pl.transform.localScale = Vector3.one * (h * 0.4f + 0.3f);
                    m.transform.position += Vector3.forward * (3000f + b0 * 40f);   // out of the way of the next batch
                    foreach (var smr in m.GetComponentsInChildren<SkinnedMeshRenderer>(true)) smr.updateWhenOffscreen = true;
                    bad.Add(new KeyValuePair<GameObject, string>(m.gameObject, t.id + " " + verdict + " " + gap.ToString("F2")));
                }
                else Object.Destroy(m.gameObject);
            }
            if (bad.Count >= 20 || b0 + 20 >= types.Count) { while (bad.Count > 0) { var part = bad.GetRange(0, Mathf.Min(20, bad.Count)); bad.RemoveRange(0, part.Count); Shots(dir, "mob_feet_" + sheet++, part); } }
        }
        // anything a failed build left behind up there
        foreach (var m in Object.FindObjectsByType<AHMob>(FindObjectsSortMode.None)) if (m != null && m.transform.position.y > sky.y - 50f) Object.Destroy(m.gameObject);
        Object.Destroy(bake);
        File.WriteAllText(Path.Combine(dir, "mob_feet.txt"), sb.ToString());
        // the fixes, for the game (AHMob.Create)
        var js = new StringBuilder("{"); bool first = true;
        foreach (var kv in fix) { if (!first) js.Append(","); first = false; js.Append("\"" + kv.Key + "\":" + kv.Value.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)); }
        js.Append("}");
        File.WriteAllText(Path.Combine(Application.dataPath, "AshenHollow/Resources/AH/Data/mob_feet.json"), js.ToString());
        AHMob.FeetReload();
        Debug.Log("Ashen Hollow: monster feet check: " + types.Count + " types, " + nBad + " floating or sunk (" + sheet + " sheets, HeroShots/mob_feet.txt)");
    }

    // the lowest part of everything drawn on the monster in its current pose: each skinned body's points worked out from
    // its bones (up to ~6000 of them; the height that only 0.2% of them are below, so a stray point does not count),
    // anything else from its mesh
    class Skin { public Vector3[] v; public BoneWeight[] w; public Matrix4x4[] bind; public int step; }
    static readonly Dictionary<Mesh, Skin> skins = new Dictionary<Mesh, Skin>();
    static readonly List<float> ys = new List<float>();
    static float Lowest(GameObject go, Mesh bake)
    {
        ys.Clear();
        foreach (var r in go.GetComponentsInChildren<Renderer>(false))
        {
            if (!r.enabled || r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer || r.name == "Plane") continue;
            var smr = r as SkinnedMeshRenderer;
            if (smr != null)
            {
                var mesh = smr.sharedMesh; if (mesh == null) continue;
                if (!mesh.isReadable)
                {
                    // (a mesh the game cannot read back: baked as drawn instead)
                    smr.BakeMesh(bake, true); var mb = Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, Vector3.one);
                    var bv = bake.vertices; int bs = Mathf.Max(1, bv.Length / 6000); for (int k = 0; k < bv.Length; k += bs) ys.Add(mb.MultiplyPoint3x4(bv[k]).y);
                    continue;
                }
                Skin sk; if (!skins.TryGetValue(mesh, out sk))
                {
                    sk = new Skin { v = mesh.vertices, w = mesh.boneWeights, bind = mesh.bindposes };
                    sk.step = Mathf.Max(1, sk.v.Length / 6000); skins[mesh] = sk;
                }
                var bones = smr.bones;
                if (sk.w == null || sk.w.Length != sk.v.Length || bones == null || bones.Length == 0)
                { var m0 = smr.transform.localToWorldMatrix; for (int k = 0; k < sk.v.Length; k += sk.step) ys.Add(m0.MultiplyPoint3x4(sk.v[k]).y); continue; }
                var mats = new Matrix4x4[bones.Length];
                for (int b = 0; b < bones.Length; b++) mats[b] = bones[b] != null && b < sk.bind.Length ? bones[b].localToWorldMatrix * sk.bind[b] : smr.transform.localToWorldMatrix;
                for (int k = 0; k < sk.v.Length; k += sk.step)
                {
                    var bw = sk.w[k]; var p = sk.v[k]; float y = 0f, tw = 0f;
                    if (bw.weight0 > 0f && bw.boneIndex0 < mats.Length) { y += mats[bw.boneIndex0].MultiplyPoint3x4(p).y * bw.weight0; tw += bw.weight0; }
                    if (bw.weight1 > 0f && bw.boneIndex1 < mats.Length) { y += mats[bw.boneIndex1].MultiplyPoint3x4(p).y * bw.weight1; tw += bw.weight1; }
                    if (bw.weight2 > 0f && bw.boneIndex2 < mats.Length) { y += mats[bw.boneIndex2].MultiplyPoint3x4(p).y * bw.weight2; tw += bw.weight2; }
                    if (bw.weight3 > 0f && bw.boneIndex3 < mats.Length) { y += mats[bw.boneIndex3].MultiplyPoint3x4(p).y * bw.weight3; tw += bw.weight3; }
                    if (tw > 0.01f) ys.Add(y / tw);
                }
            }
            else
            {
                var mf = r.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
                if (!mf.sharedMesh.isReadable) { ys.Add(r.bounds.min.y); continue; }
                var mtx = r.transform.localToWorldMatrix; var vs = mf.sharedMesh.vertices; int st = Mathf.Max(1, vs.Length / 6000);
                for (int k = 0; k < vs.Length; k += st) ys.Add(mtx.MultiplyPoint3x4(vs[k]).y);
            }
        }
        if (ys.Count == 0) return go.transform.position.y;
        ys.Sort(); return ys[Mathf.Min(ys.Count - 1, (int)(ys.Count * 0.002f))];
    }

    // from the side, low, so the gap under the feet shows; the name and gap written into the log next to its cell
    static void Shots(string dir, string name, List<KeyValuePair<GameObject, string>> objs)
    {
        const int S = 320; int cols = Mathf.Min(5, objs.Count), rows = (objs.Count + cols - 1) / cols;
        var sheet = new Texture2D(S * cols, S * rows, TextureFormat.RGB24, false);
        var go = new GameObject("FeetCam"); var cam = go.AddComponent<Camera>(); cam.fieldOfView = 30f; cam.nearClipPlane = 0.05f;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.62f, 0.7f, 0.76f);
        var rt = new RenderTexture(S, S, 24); cam.targetTexture = rt; var tex = new Texture2D(S, S, TextureFormat.RGB24, false);
        var lg = new GameObject("FeetLight"); var li = lg.AddComponent<Light>(); li.type = LightType.Directional; li.intensity = 1.1f; lg.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
        var log = new StringBuilder();
        for (int i = 0; i < objs.Count; i++)
        {
            var o = objs[i].Key; var rs = o.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue;
            var mob = o.GetComponent<AHMob>(); float hh = mob != null ? Mathf.Max(0.4f, mob.height) : 2f;
            Vector3 at = o.transform.position + Vector3.up * hh * 0.4f;
            cam.transform.position = at + (Vector3.right * 0.95f + Vector3.up * 0.1f).normalized * (hh * 2.6f + 1f); cam.transform.LookAt(at);
            cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, S, S), 0, 0); tex.Apply(); RenderTexture.active = null;
            sheet.SetPixels((i % cols) * S, (rows - 1 - i / cols) * S, S, S, tex.GetPixels());
            log.AppendLine((i + 1) + ". " + objs[i].Value);
        }
        sheet.Apply();
        File.WriteAllBytes(Path.Combine(dir, name + ".png"), sheet.EncodeToPNG());
        File.WriteAllText(Path.Combine(dir, name + ".txt"), log.ToString());
        cam.targetTexture = null; Object.DestroyImmediate(go); Object.DestroyImmediate(lg); Object.Destroy(rt); Object.Destroy(tex); Object.Destroy(sheet);
        foreach (var o in objs) Object.Destroy(o.Key);
    }
}
