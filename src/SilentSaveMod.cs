using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace SilentSave;

public class SilentSaveMod : ModSystem {
    private static SilentSaveMod instance = null!;

    private readonly object @lock = new();

    private bool doingAutoSave;
    private bool doingOffThreadTickSave;

    private Harmony? harmony;

    public SilentSaveMod() {
        instance = this;
    }

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public bool SaveInProgress() {
        lock (@lock) return doingAutoSave || doingOffThreadTickSave;
    }

    public override bool ShouldLoad(EnumAppSide side) {
        return side.IsServer();
    }

    public override void StartServerSide(ICoreServerAPI api) {
        harmony = new Harmony(Mod.Info.ModID);
        harmony.Patch(typeof(ServerSystemAutoSaveGame).GetMethod("doAutoSave", BindingFlags.Instance | BindingFlags.NonPublic),
            prefix: typeof(SilentSaveMod).GetMethod("PreDoAutoSave"),
            postfix: typeof(SilentSaveMod).GetMethod("PostDoAutoSave"));
        harmony.Patch(AccessTools.TypeByName("Vintagestory.Server.ServerSystemLoadAndSaveGame").GetMethod("OnSeparateThreadTick", BindingFlags.Instance | BindingFlags.Public),
            prefix: typeof(SilentSaveMod).GetMethod("PreOnSeparateThreadTick"),
            postfix: typeof(SilentSaveMod).GetMethod("PostOnSeparateThreadTick"));
        harmony.Patch(typeof(ServerLogger).GetMethod("LogImpl", BindingFlags.Instance | BindingFlags.NonPublic, new[] { typeof(EnumLogType), typeof(string), typeof(object[]) }),
            prefix: typeof(SilentSaveMod).GetMethod("PreLogImpl"));
    }

    public override void Dispose() {
        harmony?.UnpatchAll(Mod.Info.ModID);
    }

    public static void PreDoAutoSave() {
        lock (instance.@lock) instance.doingAutoSave = true;
    }

    public static void PostDoAutoSave() {
        lock (instance.@lock) instance.doingAutoSave = false;
    }

    public static void PreOnSeparateThreadTick() {
        lock (instance.@lock) instance.doingOffThreadTickSave = true;
    }

    public static void PostOnSeparateThreadTick() {
        lock (instance.@lock) instance.doingOffThreadTickSave = false;
    }

    public static bool PreLogImpl() {
        return !instance.SaveInProgress();
    }
}
