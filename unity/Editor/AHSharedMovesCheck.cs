// Ashen Hollow > Test: Shared Moves Check. For each model that plays another model's clips (AHModel.ClipPath), checks
// that every bone each clip moves is found in it, and that a moment of each clip puts its bones where it puts the
// lead model's. Writes Builds/shared_moves.txt.
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class AHSharedMovesCheck
{
    static readonly string[] Followers = {
        "Comp/mount_goldsteed_hd", "Comp/mount_nightmare_hd", "Comp/mount_voidwing_hd",
        "Comp/mount_souleater_blue_hd", "Comp/mount_souleater_green_hd", "Comp/mount_souleater_red_hd",
        "Comp/mount_glacierwolf_hd", "Comp/pet_snow_fox_hd",
        "Beasts/b_boar_hd", "Beasts/b_boarlord_hd", "Beasts/b_dm_behemoth", "Beasts/b_dm_hellhound", "Beasts/b_goldhorn_hd",
        "Beasts/b_icewolf_hd", "Beasts/b_mistfang_hd", "Beasts/b_rimeclaw_hd", "Beasts/b_shadowwolf_hd", "Beasts/b_wolf_hd",
        "Beasts/b_tjackal_hd", "Beasts/b_stag_hd" };

    static string Tops(Transform t)
    {
        var s = t.name + "[" + string.Join(",", System.Array.ConvertAll(t.GetComponents<Component>(), c => c.GetType().Name)) + "]";
        for (int i = 0; i < t.childCount && i < 3; i++) { var c = t.GetChild(i); s += " > " + c.name; for (int k = 0; k < c.childCount && k < 3; k++) s += " >> " + c.GetChild(k).name; }
        return s;
    }

    [MenuItem("Ashen Hollow/Test: Shared Moves Check %#&c")]
    static void Run()
    {
        var sb = new StringBuilder(); int good = 0, bad = 0;
        foreach (var f in Followers)
        {
            string path = "AH/Models/" + f, lead = AHModel.ClipPath(path);
            var fp = Resources.Load<GameObject>(path); var lp = Resources.Load<GameObject>(lead);
            var clips = Resources.LoadAll<AnimationClip>(lead);
            var own = Resources.LoadAll<AnimationClip>(path);
            if (fp == null || lp == null || clips.Length == 0) { sb.AppendLine("BAD  " + f + ": model or clips missing (" + (fp != null) + ", " + (lp != null) + ", " + clips.Length + ")"); bad++; continue; }
            var fi = Object.Instantiate(fp); var li = Object.Instantiate(lp);
            int bindings = 0, missing = 0, leadMissing = 0; float worst = 0f; string worstAt = "", sample = "";
            foreach (var c in clips)
            {
                foreach (var bnd in AnimationUtility.GetCurveBindings(c))
                {
                    bindings++;
                    if (sample == "") sample = "'" + bnd.path + "' " + bnd.propertyName + " " + bnd.type.Name;
                    if (AnimationUtility.GetAnimatedObject(fi, bnd) == null) missing++;
                    if (AnimationUtility.GetAnimatedObject(li, bnd) == null) leadMissing++;
                }
                float t = c.length * 0.37f;
                c.SampleAnimation(fi, t); c.SampleAnimation(li, t);
                var lt = new Dictionary<string, Transform>();
                foreach (var tr in li.GetComponentsInChildren<Transform>(true)) lt[AnimationUtility.CalculateTransformPath(tr, li.transform)] = tr;
                foreach (var tr in fi.GetComponentsInChildren<Transform>(true))
                {
                    Transform o; if (!lt.TryGetValue(AnimationUtility.CalculateTransformPath(tr, fi.transform), out o)) continue;
                    float d = Quaternion.Angle(tr.localRotation, o.localRotation);
                    if (d > worst) { worst = d; worstAt = c.name + " " + tr.name; }
                }
            }
            string top = " | follower: " + Tops(fi.transform) + " | lead: " + Tops(li.transform);
            Object.DestroyImmediate(fi); Object.DestroyImmediate(li);
            bool ok = missing == leadMissing && worst < 0.5f;
            if (ok) good++; else bad++;
            sb.AppendLine((ok ? "OK   " : "BAD  ") + f + " plays " + lead.Substring(10) + ": " + clips.Length + " clips, " + bindings + " curves, " + missing + " not found (lead itself " + leadMissing + "), e.g. " + sample + ", legacy " + clips[0].legacy + ", own clips " + own.Length + ", largest pose difference " + worst.ToString("0.00") + " deg" + (worst > 0f ? " (" + worstAt + ")" : "") + top);
        }
        Directory.CreateDirectory("Builds"); File.WriteAllText("Builds/shared_moves.txt", sb.ToString());
        Debug.Log("Ashen Hollow: shared moves check: " + good + " fine, " + bad + " with problems (Builds/shared_moves.txt)");
    }
}
