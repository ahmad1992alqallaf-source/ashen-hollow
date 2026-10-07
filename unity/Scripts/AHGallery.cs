// Ashen Hollow: the costume gallery at your homestead. Every costume you own stands on a plinth in a row by your
// house, worn by a likeness of your hero (in its dye), with its name on a little sign. Up to six at a time.
using System.Collections.Generic;
using UnityEngine;

public static class AHGallery
{
    public static void Setup(AHGame g)
    {
        var p = g.player; if (AHGame.AreaId != AHHome.Area || p == null || g.data == null || g.data.spawn == null) return;
        var own = new List<string>(); foreach (var d in AHCostumes.All) if (AHCostumes.Owns(p, d.id) && AHCostumes.Exists(p.look, d.id)) own.Add(d.id);
        if (own.Count == 0) return;
        int n = Mathf.Min(6, own.Count);
        var stone = new Material(Shader.Find("Universal Render Pipeline/Lit")); stone.SetColor("_BaseColor", new Color(0.5f, 0.46f, 0.42f));
        Vector3 c = g.W(g.data.spawn.x - 7f, g.data.spawn.z + 7f), face = g.W(g.data.spawn.x, g.data.spawn.z);
        Vector3 fw = face - c; fw.y = 0; fw = fw.sqrMagnitude > 0.01f ? fw.normalized : Vector3.forward; Vector3 side = Vector3.Cross(Vector3.up, fw);
        for (int i = 0; i < n; i++)
        {
            string id = own[i];
            Vector3 at = g.Resolve(c + side * (i - (n - 1) / 2f) * 1.8f, 0.8f);
            var plinth = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.Destroy(plinth.GetComponent<Collider>());
            plinth.name = "Gallery plinth"; plinth.transform.position = at + Vector3.up * 0.15f; plinth.transform.localScale = new Vector3(1.1f, 0.15f, 1.1f);
            plinth.GetComponent<Renderer>().sharedMaterial = stone; g.AddBlocker(at, 0.6f);
            var holder = new GameObject("Gallery " + id).transform; holder.position = at + Vector3.up * 0.3f; holder.rotation = Quaternion.LookRotation(fw);
            AHAnim a; GameObject rig = null; var look = JsonUtility.FromJson<AHLook>(JsonUtility.ToJson(p.look ?? new AHLook()));
            try { rig = AHPeople.BuildHero(holder, p.cls, look, g, out a); } catch { a = null; }
            if (rig == null) { Object.Destroy(holder.gameObject); continue; }
            AHCostumes.Apply(rig, id, look, AHFashion.DyeOf(p, id));
            if (a != null) { a.Play("Idle", true); holder.gameObject.AddComponent<AHStudioPose>().anim = a; }
            AHModel.SetShadows(holder.gameObject);
            // the name, on a sign in front
            var sign = new GameObject("Sign"); sign.transform.SetParent(holder, false); sign.transform.localPosition = new Vector3(0f, 0.15f, 0.75f);
            var tm = sign.AddComponent<TextMesh>(); tm.text = AHCostumes.Get(id).name; tm.characterSize = 0.06f; tm.fontSize = 48; tm.anchor = TextAnchor.MiddleCenter; tm.color = new Color(1f, 0.85f, 0.5f);
            sign.transform.localRotation = Quaternion.Euler(20f, 180f, 0f);
        }
        AHSpotLabel(g, c);
    }
    static void AHSpotLabel(AHGame g, Vector3 at)
    {
        AHGather.Spots.Add(new AHSpot { kind = "use", type = "gallery", name = "Costume gallery", pos = at, r = 0.5f, reach = 3f, use = () => g.ui.OpenWardrobe() });
    }
}
