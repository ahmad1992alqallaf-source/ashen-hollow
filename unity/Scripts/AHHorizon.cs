// Ashen Hollow: the open sea. Where a land runs down to the sea, ships sail far out on it: a few tall sailing ships
// beating slowly along the coast between forty and a hundred and fifty metres out, half lost in the haze, coming about
// when they reach the end of their run. With the border mountains sunk where the sea is (AHRange) the water runs on
// to the horizon.
using System.Collections.Generic;
using UnityEngine;

public static class AHHorizon
{
    public static void Build(AHGame g)
    {
        if (!AHSea.On || AHDungeon.IsDungeon(AHGame.AreaId)) return;
        var S = AHSea.Sides; if (S == Vector4.zero) return;
        var L = AHSea.Land; float y = AHSea.SeaY;
        var root = new GameObject("Horizon ships").transform;
        var rnd = new System.Random(AHGame.AreaId.GetHashCode());
        int n = 0;
        // each open side: its line along the coast, the way out to sea
        var sides = new List<KeyValuePair<Vector3, Vector3>>();   // (a point on the side's middle, outward)
        if (S.x > 0.5f) sides.Add(new KeyValuePair<Vector3, Vector3>(new Vector3(L.xMin, y, L.center.y), Vector3.left));
        if (S.y > 0.5f) sides.Add(new KeyValuePair<Vector3, Vector3>(new Vector3(L.xMax, y, L.center.y), Vector3.right));
        if (S.z > 0.5f) sides.Add(new KeyValuePair<Vector3, Vector3>(new Vector3(L.center.x, y, L.yMin), Vector3.back));
        if (S.w > 0.5f) sides.Add(new KeyValuePair<Vector3, Vector3>(new Vector3(L.center.x, y, L.yMax), Vector3.forward));
        Color[] sails = { new Color(0.93f, 0.89f, 0.79f), new Color(0.86f, 0.8f, 0.68f), new Color(0.78f, 0.36f, 0.28f), new Color(0.9f, 0.9f, 0.86f) };
        Color[] flags = { new Color(0.7f, 0.12f, 0.14f), new Color(0.14f, 0.3f, 0.62f), new Color(0.85f, 0.65f, 0.2f), new Color(0.2f, 0.45f, 0.25f) };
        foreach (var sd in sides)
        {
            Vector3 mid = sd.Key, outw = sd.Value, along = Vector3.Cross(Vector3.up, outw);
            float half = Mathf.Abs(Vector3.Dot(along, new Vector3(L.width, 0f, L.height))) * 0.5f;
            int count = Mathf.Clamp(Mathf.RoundToInt(half / 45f), 2, 4);
            for (int i = 0; i < count; i++)
            {
                float out1 = 25f + (float)rnd.NextDouble() * 80f;
                float at = ((i + 0.5f) / count * 2f - 1f) * half * 0.9f + ((float)rnd.NextDouble() - 0.5f) * 20f;
                float len = 9f + (float)rnd.NextDouble() * 9f;
                bool two = len > 13f;
                Vector3 heading = (rnd.Next(2) == 0 ? along : -along);
                var ship = AHShips.Make(root, mid + outw * out1 + along * at - Vector3.up * len * 0.06f, heading, len, two, sails[rnd.Next(sails.Length)], flags[rnd.Next(flags.Length)], rnd.Next());
                ship.name = "Horizon ship";
                foreach (var r in ship.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var sail = ship.gameObject.AddComponent<AHHorizonShip>();
                sail.mid = mid + outw * out1; sail.along = along; sail.half = half + 60f; sail.speed = 1.1f + (float)rnd.NextDouble() * 1.4f;
                n++;
            }
        }
        if (n == 0) Object.Destroy(root.gameObject);
        else Debug.Log("Ashen Hollow: " + n + " ships out on the open sea");
    }
}

// sails along the coast and comes about at the end of its run, riding the swell
public class AHHorizonShip : MonoBehaviour
{
    public Vector3 mid, along; public float half, speed;
    float ph, turnLeft; Vector3 heading; float baseY;
    void Start() { heading = transform.forward; heading.y = 0f; heading.Normalize(); baseY = transform.position.y; ph = Random.value * 6.28f; }
    void Update()
    {
        float dt = Time.deltaTime, t = Time.time + ph;
        Vector3 p = transform.position;
        float u = Vector3.Dot(p - mid, along);
        // past the end of the run, heading on out: come about, slowly
        if (Mathf.Abs(u) > half && Vector3.Dot(heading, along) * Mathf.Sign(u) > 0f && turnLeft <= 0f) turnLeft = 180f;
        if (turnLeft > 0f) { float a = Mathf.Min(turnLeft, 9f * dt); turnLeft -= a; heading = Quaternion.Euler(0f, a, 0f) * heading; }
        p += heading * speed * dt; p.y = baseY + Mathf.Sin(t * 0.8f) * 0.12f;
        transform.position = p;
        transform.rotation = Quaternion.LookRotation(heading, Vector3.up) * Quaternion.Euler(Mathf.Sin(t * 0.6f + 1f) * 1.4f, 0f, Mathf.Sin(t * 0.5f) * 3f + (turnLeft > 0f ? -6f : 0f));
    }
}
