// Ashen Hollow: when scripts change while the game is playing in the editor, wait for Play to stop before
// recompiling (recompiling mid-play wipes the running game's state and floods the console with errors).
using UnityEditor;

[InitializeOnLoad]
public static class AHEditorPrefs
{
    static AHEditorPrefs() { if (EditorPrefs.GetInt("ScriptCompilationDuringPlay", 0) != 1) EditorPrefs.SetInt("ScriptCompilationDuringPlay", 1); }
}
