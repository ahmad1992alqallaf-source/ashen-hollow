// Ashen Hollow: a torch for the dark. It sits on the tool belt (its own slot); when night falls, or underground, the
// hero lifts it in the left hand: a short wooden haft with a living flame and a warm light that reaches about ten
// metres round you and flickers. Day comes and it is put away. Every hero is given one the first night; more are sold
// at the general stores (a spare to give a friend).
using UnityEngine;

public static class AHTorch
{
    public const string Id = "torch";
    static GameObject torch; static Light light; static Transform flame; static GameObject on;   // 'on': the model it hangs on
    static float t, sparkT;

    // the torch into the item table (from AHItems.Get's first call)
    public static void Inject()
    {
        var items = AHDB.Items; if (items == null || items.ContainsKey(Id)) return;
        items[Id] = new System.Collections.Generic.Dictionary<string, object>
        {
            { "name", "Torch" }, { "icon", "log" }, { "c", "#ffb04a" }, { "price", 6.0 },
            { "tool", new System.Collections.Generic.Dictionary<string, object> { { "kind", "torch" }, { "k", 0.0 } } },
            { "note", "Put it on your tool belt: at night and underground you carry it lit, and it lights the way round you." },
        };
    }

    static bool Has(AHPlayer p) { return AHBags.Tool(p, "torch") != null; }

    public static void Tick(AHGame g, float dt)
    {
        var p = g.player; if (p == null || p.prog == null) return;
        bool dark = g.IsNight || g.Dark || AHDungeon.IsDungeon(AHGame.AreaId);
        // the first dark night: a torch on the belt
        if (dark && !p.prog.torchGiven)
        {
            p.prog.torchGiven = true;
            if (!Has(p)) { AHBags.SetToolPublic(p, "torch", Id); if (g.ui != null) g.ui.Toast("It's dark: you light the torch from your tool belt.", 3f); g.MarkDirty(); }
        }
        bool want = dark && Has(p) && !p.dead && !AHInterior.Inside && p.Model != null;
        if (!want) { if (torch != null) Object.Destroy(torch); torch = null; return; }
        if (torch == null || on != p.Model) Make(p);
        if (torch == null) return;
        // the flame breathes and the light flickers with it
        t += dt;
        float f = 0.85f + 0.1f * Mathf.Sin(t * 13f) + 0.06f * Mathf.Sin(t * 23.7f + 1.3f) + 0.04f * Mathf.PerlinNoise(t * 4f, 0.3f);
        if (light != null) light.intensity = 2.6f * f;
        if (flame != null) flame.localScale = new Vector3(0.09f, 0.16f * f, 0.09f);
        sparkT -= dt;
        if (sparkT <= 0f && flame != null) { sparkT = 0.35f; AHSpark.Burst(flame.position + Vector3.up * 0.06f, new Color(1f, 0.6f, 0.2f, 0.9f), 2, 0.5f, 0.6f, 0.04f, 1.8f); }
    }

    static void Make(AHPlayer p)
    {
        if (torch != null) Object.Destroy(torch);
        on = p.Model;
        Transform hand = null;
        var an = on.GetComponentInChildren<Animator>();
        if (an != null && an.isHuman) hand = an.GetBoneTransform(HumanBodyBones.LeftHand);
        torch = new GameObject("Torch");
        if (hand != null) { torch.transform.SetParent(hand, false); torch.transform.localPosition = new Vector3(0f, 0.05f, 0.04f); torch.transform.localRotation = Quaternion.identity; }
        else { torch.transform.SetParent(p.transform, false); torch.transform.localPosition = new Vector3(-0.35f, 1.05f, 0.25f); }
        // the haft stands upright in the world whatever the hand does (kept so in LateUpdate-free style: world rotation each make)
        var sh = Shader.Find("Universal Render Pipeline/Lit");
        var haft = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.Destroy(haft.GetComponent<Collider>());
        haft.transform.SetParent(torch.transform, false); haft.transform.localScale = new Vector3(0.045f, 0.22f, 0.045f); haft.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        haft.GetComponent<Renderer>().sharedMaterial = new Material(sh) { color = new Color(0.36f, 0.22f, 0.12f) };
        var head = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.Destroy(head.GetComponent<Collider>());
        head.transform.SetParent(torch.transform, false); head.transform.localScale = new Vector3(0.07f, 0.05f, 0.07f); head.transform.localPosition = new Vector3(0f, 0.31f, 0f);
        head.GetComponent<Renderer>().sharedMaterial = new Material(sh) { color = new Color(0.2f, 0.14f, 0.1f) };
        var fl = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(fl.GetComponent<Collider>());
        fl.transform.SetParent(torch.transform, false); fl.transform.localPosition = new Vector3(0f, 0.42f, 0f);
        var fm = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? sh); fm.color = new Color(1f, 0.62f, 0.2f);
        fl.GetComponent<Renderer>().sharedMaterial = fm; fl.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        flame = fl.transform;
        var lg = new GameObject("Light"); lg.transform.SetParent(torch.transform, false); lg.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        light = lg.AddComponent<Light>(); light.type = LightType.Point; light.color = new Color(1f, 0.72f, 0.42f); light.range = 10f; light.intensity = 2.6f; light.shadows = LightShadows.None;
        light.renderMode = LightRenderMode.ForcePixel;
        // keep it upright: the haft points up in the world
        torch.transform.rotation = Quaternion.identity;
        torch.AddComponent<AHTorchUpright>();
    }
}

// the torch stays upright while the hand swings
public class AHTorchUpright : MonoBehaviour { void LateUpdate() { transform.rotation = Quaternion.identity; } }
