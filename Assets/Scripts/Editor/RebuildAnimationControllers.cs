using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Reconstruye Curro1 y Pijo1 con la API de Unity en vez de YAML a mano: los
// .controller escritos fuera del editor importaban vacíos. Se edita el asset
// in situ (mismo GUID) para no romper las referencias ya asignadas.
public static class RebuildAnimationControllers
{
    [MenuItem("Tools/GuirisTrap/Rebuild Curro1 + Pijo1 Controllers")]
    public static void RebuildBoth()
    {
        RebuildCurro();
        RebuildPijo();
        AssetDatabase.SaveAssets();
        Debug.Log("[RebuildControllers] Listo. Abre los controllers en el Animator y verifica estados y parámetros.");
    }

    static void RebuildCurro()
    {
        const string path = "Assets/Animations/Curro/Curro1.controller";
        var clips = LoadClips("Assets/Animations/Curro",
            "01_IDLE", "02_ANDAR", "03_CORRER", "04_RECOGER", "05_BANDEJA", "06_SERVIR",
            "07_BARRER", "08_LIMPIAR_PLATOS", "09_CORTAR", "10_COCINAR", "11_BANDEJA_IDLE");

        AnimatorController c = GetController(path);
        Clear(c);

        c.AddParameter("Speed", AnimatorControllerParameterType.Float);
        c.AddParameter("Carrying", AnimatorControllerParameterType.Bool);
        var triggerNames = new[] { "Recoger", "Servir", "Barrer", "LimpiarPlatos", "Cortar", "Cocinar" };
        foreach (var t in triggerNames) c.AddParameter(t, AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = c.layers[0].stateMachine;
        AnimatorState loco = sm.AddState("Locomotion");
        sm.defaultState = loco;

        var tree = new BlendTree
        {
            name = "Locomotion",
            blendType = BlendTreeType.Simple1D,
            blendParameter = "Speed",
            useAutomaticThresholds = false,
        };
        tree.children = new[]
        {
            Motion(clips["01_IDLE"], 0f),
            Motion(clips["02_ANDAR"], 1f),
            Motion(clips["03_CORRER"], 2f),
        };
        AssetDatabase.AddObjectToAsset(tree, c);
        loco.motion = tree;

        // Carry: árbol Speed — idle con bandeja (0) y andar con bandeja (1),
        // así parado y caminando portando comida tienen animación propia.
        AnimatorState carry = sm.AddState("Carry");
        var carryTree = new BlendTree
        {
            name = "Carry",
            blendType = BlendTreeType.Simple1D,
            blendParameter = "Speed",
            useAutomaticThresholds = false,
        };
        carryTree.children = new[]
        {
            Motion(clips["11_BANDEJA_IDLE"], 0f),
            Motion(clips["05_BANDEJA"], 1f),
        };
        AssetDatabase.AddObjectToAsset(carryTree, c);
        carry.motion = carryTree;

        var actions = new (string state, string clip, string trigger)[]
        {
            ("Recoger",      "04_RECOGER",        "Recoger"),
            ("Servir",       "06_SERVIR",         "Servir"),
            ("Barrer",       "07_BARRER",         "Barrer"),
            ("LimpiarPlatos","08_LIMPIAR_PLATOS", "LimpiarPlatos"),
            ("Cortar",       "09_CORTAR",         "Cortar"),
            ("Cocinar",      "10_COCINAR",        "Cocinar"),
        };
        foreach (var (stateName, clipName, trigger) in actions)
        {
            AnimatorState st = sm.AddState(stateName);
            st.motion = clips[clipName];
            TriggerTransition(loco, st, trigger);

            AnimatorStateTransition back = st.AddTransition(loco);
            back.hasExitTime = true;
            back.exitTime = 1f;
            back.duration = 0.15f;
        }

        // el bool va al final: en el mismo frame gana el trigger del gesto
        BoolTransition(loco, carry, true);
        AnimatorState servirState = sm.states.First(s => s.state.name == "Servir").state;
        TriggerTransition(carry, servirState, "Servir");
        BoolTransition(carry, loco, false);

        Debug.Log($"[RebuildControllers] {path}: Locomotion (Speed 0/1/2) + Carry (bool) + {actions.Length} acciones por trigger.");
    }

    static void RebuildPijo()
    {
        const string path = "Assets/Animations/Pijo/Pijo1.controller";
        var clips = LoadClips("Assets/Animations/Pijo",
            "01_IDLE", "02_ANDAR", "03_SENTARSE", "04_LEVANTARSE", "05_COMER", "06_ENFADADO", "07_FELIZ");

        AnimatorController c = GetController(path);
        Clear(c);

        c.AddParameter("Speed", AnimatorControllerParameterType.Float);
        c.AddParameter("State", AnimatorControllerParameterType.Int);

        AnimatorStateMachine sm = c.layers[0].stateMachine;
        AnimatorState loco   = sm.AddState("Locomotion");
        AnimatorState sentar = sm.AddState("Sentarse");
        AnimatorState comer  = sm.AddState("Comida");
        AnimatorState feliz  = sm.AddState("Feliz");
        AnimatorState levantar = sm.AddState("Levantarse");
        AnimatorState enfadado = sm.AddState("Enfadado");

        sm.defaultState = loco;
        sentar.motion = clips["03_SENTARSE"];
        comer.motion  = clips["05_COMER"];
        feliz.motion  = clips["07_FELIZ"];
        levantar.motion = clips["04_LEVANTARSE"];
        enfadado.motion = clips["06_ENFADADO"];

        var tree = new BlendTree
        {
            name = "Locomotion",
            blendType = BlendTreeType.Simple1D,
            blendParameter = "Speed",
            useAutomaticThresholds = false,
        };
        tree.children = new[]
        {
            Motion(clips["01_IDLE"], 0f),
            Motion(clips["02_ANDAR"], 1f),
        };
        AssetDatabase.AddObjectToAsset(tree, c);
        loco.motion = tree;

        // locomoción → acciones (también cubre clientes que se saltan el sentarse)
        IntTransition(loco, sentar, 3);
        IntTransition(loco, comer, 4);
        IntTransition(loco, enfadado, 7);
        // sentado → siguiente fase
        IntTransition(sentar, comer, 4);
        IntTransition(sentar, feliz, 5);
        IntTransition(sentar, levantar, 6);
        IntTransition(sentar, enfadado, 7);
        // comiendo → siguiente fase
        IntTransition(comer, feliz, 5);
        IntTransition(comer, levantar, 6);
        IntTransition(comer, enfadado, 7);
        // feliz → levantarse al acabar el clip; enfadado también se levanta para irse
        ExitTransition(feliz, levantar);
        IntTransition(feliz, enfadado, 7);
        IntTransition(enfadado, levantar, 6);
        // levantarse → andar
        ExitTransition(levantar, loco);

        Debug.Log("[RebuildControllers] Pijo1: Locomotion + Sentarse/Comida/Feliz/Levantarse/Enfadado por State int.");
    }

    // ── helpers ───────────────────────────────────────────────────────────

    static AnimatorController GetController(string path)
    {
        AnimatorController c = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (c == null)
        {
            c = AnimatorController.CreateAnimatorControllerAtPath(path);
            Debug.LogWarning($"[RebuildControllers] {path} no existía; creado nuevo.");
        }
        return c;
    }

    static void Clear(AnimatorController c)
    {
        c.parameters = new AnimatorControllerParameter[0];

        foreach (var layer in c.layers)
        {
            AnimatorStateMachine sm = layer.stateMachine;
            foreach (var child in sm.states.ToArray()) sm.RemoveState(child.state);
            foreach (var t in sm.anyStateTransitions.ToArray()) sm.RemoveAnyStateTransition(t);
            foreach (var t in sm.entryTransitions.ToArray()) sm.RemoveEntryTransition(t);
        }

        // barrido de sub-assets huérfanos (estados/transiciones/trees del intento YAML)
        string assetPath = AssetDatabase.GetAssetPath(c);
        Object rootSm = c.layers[0].stateMachine;
        foreach (Object obj in AssetDatabase.LoadAllAssetsAtPath(assetPath))
        {
            bool internalType = obj is AnimatorState || obj is AnimatorStateTransition
                             || obj is BlendTree || obj is AnimatorStateMachine;
            if (internalType && obj != rootSm)
                Object.DestroyImmediate(obj, true);
        }
    }

    static ChildMotion Motion(AnimationClip clip, float threshold) => new ChildMotion
    {
        motion = clip,
        threshold = threshold,
        timeScale = 1f,
    };

    static void TriggerTransition(AnimatorState from, AnimatorState to, string trigger)
    {
        AnimatorStateTransition t = from.AddTransition(to);
        t.hasExitTime = false;
        t.duration = 0.15f;
        t.AddCondition(AnimatorConditionMode.If, 0f, trigger);
    }

    static void BoolTransition(AnimatorState from, AnimatorState to, bool value)
    {
        AnimatorStateTransition t = from.AddTransition(to);
        t.hasExitTime = false;
        t.duration = 0.15f;
        t.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, "Carrying");
    }

    static void IntTransition(AnimatorState from, AnimatorState to, int value)
    {
        AnimatorStateTransition t = from.AddTransition(to);
        t.hasExitTime = false;
        t.duration = 0.15f;
        t.AddCondition(AnimatorConditionMode.Equals, value, "State");
    }

    static void ExitTransition(AnimatorState from, AnimatorState to)
    {
        AnimatorStateTransition t = from.AddTransition(to);
        t.hasExitTime = true;
        t.exitTime = 1f;
        t.duration = 0.15f;
    }

    static Dictionary<string, AnimationClip> LoadClips(string folder, params string[] names)
    {
        var result = new Dictionary<string, AnimationClip>();
        foreach (string name in names)
        {
            var clip = AssetDatabase.FindAssets($"t:AnimationClip {name}", new[] { folder })
                .Select(g => AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(g)))
                .FirstOrDefault(a => a != null && a.name.StartsWith(name));
            if (clip == null)
                Debug.LogError($"[RebuildControllers] Falta el clip '{name}' en {folder}.");
            else
                result[name] = clip;
        }
        return result;
    }
}
