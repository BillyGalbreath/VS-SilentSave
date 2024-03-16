using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace SilentSave;

public class SilentSaveMod : ModSystem {
    private static SilentSaveMod _instance = null!;

    private readonly object _lock = new();

    private bool _doingAutoSave;
    private bool _doingOffThreadTickSave;

    private Harmony? _harmony;

    public SilentSaveMod() {
        _instance = this;
    }

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public bool SaveInProgress() {
        lock (_lock) return _doingAutoSave || _doingOffThreadTickSave;
    }

    public override bool ShouldLoad(EnumAppSide side) {
        return side.IsServer();
    }

    public override void StartServerSide(ICoreServerAPI api) {
        _harmony = new Harmony(Mod.Info.ModID);
        _harmony.Patch(typeof(ServerSystemAutoSaveGame).GetMethod("doAutoSave", BindingFlags.Instance | BindingFlags.NonPublic),
            prefix: typeof(SilentSaveMod).GetMethod("PreDoAutoSave"),
            postfix: typeof(SilentSaveMod).GetMethod("PostDoAutoSave"));
        _harmony.Patch(AccessTools.TypeByName("Vintagestory.Server.ServerSystemLoadAndSaveGame").GetMethod("OnSeparateThreadTick", BindingFlags.Instance | BindingFlags.Public),
            prefix: typeof(SilentSaveMod).GetMethod("PreOnSeparateThreadTick"),
            postfix: typeof(SilentSaveMod).GetMethod("PostOnSeparateThreadTick"));
        _harmony.Patch(typeof(ServerLogger).GetMethod("LogImpl", BindingFlags.Instance | BindingFlags.NonPublic, new[] { typeof(EnumLogType), typeof(string), typeof(object[]) }),
            prefix: typeof(SilentSaveMod).GetMethod("PreLogImpl"));
    }

    public override void Dispose() {
        _harmony?.UnpatchAll(Mod.Info.ModID);
    }

    public static void PreDoAutoSave() {
        lock (_instance._lock) _instance._doingAutoSave = true;
    }

    public static void PostDoAutoSave() {
        lock (_instance._lock) _instance._doingAutoSave = false;
    }

    public static void PreOnSeparateThreadTick() {
        lock (_instance._lock) _instance._doingOffThreadTickSave = true;
    }

    public static void PostOnSeparateThreadTick() {
        lock (_instance._lock) _instance._doingOffThreadTickSave = false;
    }

    public static bool PreLogImpl() {
        return !_instance.SaveInProgress();
    }
}
