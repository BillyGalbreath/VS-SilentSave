using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace SilentSave;

public class SilentSaveMod : ModSystem {
    private static readonly object LOCK = new();

    private static bool doingAutoSave;
    private static bool doingOffThreadTickSave;

    private Harmony? harmony;

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public static bool SaveInProgress() {
        lock (LOCK) return doingAutoSave || doingOffThreadTickSave;
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
        lock (LOCK) doingAutoSave = true;
    }

    public static void PostDoAutoSave() {
        lock (LOCK) doingAutoSave = false;
    }

    public static void PreOnSeparateThreadTick() {
        lock (LOCK) doingOffThreadTickSave = true;
    }

    public static void PostOnSeparateThreadTick() {
        lock (LOCK) doingOffThreadTickSave = false;
    }

    public static bool PreLogImpl() {
        return !SaveInProgress();
    }
}
