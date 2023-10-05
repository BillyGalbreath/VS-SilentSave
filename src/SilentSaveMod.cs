using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace SilentSave;

public class SilentSaveMod : ModSystem {
    private static bool allowLogger = true;

    private Harmony? harmony;

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
        allowLogger = false;
    }

    public static void PostDoAutoSave() {
        allowLogger = true;
    }

    public static void PreOnSeparateThreadTick() {
        allowLogger = false;
    }

    public static void PostOnSeparateThreadTick() {
        allowLogger = true;
    }

    public static bool PreLogImpl() {
        return allowLogger;
    }
}
