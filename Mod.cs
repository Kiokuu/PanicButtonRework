using System;
using System.Linq;
using System.Reflection;
using MelonLoader;
using UnhollowerRuntimeLib.XrefScans;
using HarmonyLib;
using VRC;

namespace PanicButtonRework
{
    public class Mod : MelonMod
    {
        public static bool recoverSafetySettings = true;

        public static Il2CppSystem.Collections.Generic.Dictionary<VRC.UserSocialClass, VRC.FeaturePermissionSet> prePanicPreset;

        private static MethodBase applySafety;
        private static MethodBase queueHudMessage;
        private static MethodBase panicModeOn;
        private static MethodBase restoreCustomTrustLevelSettings;
        private static MethodBase saveCustomTrustLevelSettings;
        private static MethodBase getCustomTrustLevelKey;
        private static MethodBase onCustomTrustLevelSettingsChanged; // need to get deob name of this

        public static MelonPreferences_Entry<bool> PanicModeButtonFunctionality;
        public static MelonPreferences_Entry<bool> PanicModeTextDisplay;
        public static MelonPreferences_Entry<bool> ModifiedPanicModeTextDisplay;

        public override void OnApplicationStart()
        {
            MelonPreferences.CreateCategory("PanicButtonRework", "PanicButtonRework Settings");
            PanicModeButtonFunctionality = MelonPreferences.CreateEntry("PanicButtonRework", nameof(PanicModeButtonFunctionality), true, "Toggle the functionality of the Panic Button");
            PanicModeTextDisplay = MelonPreferences.CreateEntry("PanicButtonRework", nameof(PanicModeTextDisplay), true, "Toggle the visibility of the HUD Text in Panic Mode");
            ModifiedPanicModeTextDisplay = MelonPreferences.CreateEntry("PanicButtonRework", nameof(ModifiedPanicModeTextDisplay), true, "Toggle PanicButtonRework's modification of the HUD text in Panic Mode");

            
            applySafety = typeof(FeaturePermissionManager).GetMethods().Where(
                methodBase => methodBase.Name.StartsWith("Method_Public_Void_")
                && !methodBase.Name.Contains("PDM")
                && CheckMethod(methodBase, "Safety Settings Changed to: ")
                ).First();
            MelonLogger.Msg($"OnTrustSettingsChanged determined to be: {applySafety.DeclaringType}.{applySafety.Name}");

            getCustomTrustLevelKey = typeof(FeaturePermissionSetDefaults).GetMethods().Where(
                methodBase => methodBase.Name.StartsWith("Method_Private_Static_String_UserSocialClass")
                && !methodBase.Name.Contains("PDM")
                && CheckMethod(methodBase, "CustomTrustLevel_")
                && !CheckMethod(methodBase, "CustomTrustLevel_VeryNegative")
                ).First();
            MelonLogger.Msg($"GetCustomTrustLevelKey determined to be: {getCustomTrustLevelKey.DeclaringType}.{getCustomTrustLevelKey.Name}");

            //VRC.FeaturePermissionSetDefaults::RestoreCustomTrustLevelSettings(System.Collections.Generic.Dictionary`2<VRC.UserSocialClass,VRC.FeaturePermissionSet>)
            restoreCustomTrustLevelSettings = typeof(FeaturePermissionSetDefaults).GetMethods().Where(
                methodBase => methodBase.Name.StartsWith("Method_Private_Static_Void_Dictionary_2_UserSocialClass_FeaturePermissionSet_")
                && !methodBase.Name.Contains("PDM")
                && methodBase.GetParameters().Length == 1
                && methodBase.GetParameters()[0].ParameterType == typeof(Il2CppSystem.Collections.Generic.Dictionary<VRC.UserSocialClass, VRC.FeaturePermissionSet>)
                ).First();
            MelonLogger.Msg($"RestoreCustomTrustLevelSettings determined to be {restoreCustomTrustLevelSettings.DeclaringType}.{restoreCustomTrustLevelSettings.Name}");

            saveCustomTrustLevelSettings = typeof(FeaturePermissionSetDefaults).GetMethods().Where(
                methodBase => methodBase.Name.StartsWith("Method_Private_Static_Void_")
                && !methodBase.Name.Contains("PDM")
                && methodBase.GetParameters().Length == 0
                && CheckUsing(methodBase,getCustomTrustLevelKey.Name,getCustomTrustLevelKey.DeclaringType)
                && !CheckMethod(methodBase,"LoadCustomTrustLevelSettings: CustomTrustLevel doesn't contain entry for class")
                ).First();
            MelonLogger.Msg($"SaveCustomTrustLevelSettings determined to be: {saveCustomTrustLevelSettings.DeclaringType}.{saveCustomTrustLevelSettings.Name}");

            onCustomTrustLevelSettingsChanged = typeof(FeaturePermissionSetDefaults).GetMethods().Where(
                methodBase => methodBase.Name.StartsWith("Method_Private_Static_Void_")
                && !methodBase.Name.Contains("PDM")
                && methodBase.GetParameters().Length == 0
                && !CheckReflectedType(methodBase, typeof(FeaturePermissionSetDefaults))
                ).First();
            MelonLogger.Msg($"OnCustomTrustLevelSettingsChanged determined to be {onCustomTrustLevelSettingsChanged.DeclaringType}.{onCustomTrustLevelSettingsChanged.Name}");

            queueHudMessage = typeof(VRCUiManager).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name.StartsWith("Method_Public_Void_String_") && m.Name.Length <= 28 && m.GetParameters().Length == 1)
                .Where(m => m.GetParameters()[0].Name != "screen")
                .Single();
            MelonLogger.Msg($"QueueHudMessage determined to be: {queueHudMessage.DeclaringType}.{queueHudMessage.Name}");


            panicModeOn = typeof(FeaturePermissionManager).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name.StartsWith("Method_Public_Void_") && m.Name.Length <= 20)
                .Where(m => CheckReflectedType(m, typeof(VRCInputManager)))
                .Where(m => CheckReflectedType(m, typeof(VRC.FeaturePermissionSetDefaults)))
                .First();
            MelonLogger.Msg($"Panic Mode On determined to be : {panicModeOn.DeclaringType}.{panicModeOn.Name}");

            HarmonyInstance.Patch(panicModeOn,
                prefix: new HarmonyMethod(typeof(Mod).GetMethod(nameof(PanicMode), BindingFlags.NonPublic | BindingFlags.Static))
            );

            HarmonyInstance.Patch(queueHudMessage,
                prefix: new HarmonyMethod(typeof(Mod).GetMethod(nameof(QueueHudMessagePrefix),BindingFlags.NonPublic | BindingFlags.Static))
            );
        }

        public static void ApplySafetySettings()
        {
            restoreCustomTrustLevelSettings.Invoke(null, new object[] { prePanicPreset });
            onCustomTrustLevelSettingsChanged.Invoke(null, null);
            saveCustomTrustLevelSettings.Invoke(null, null);
            applySafety.Invoke(FeaturePermissionManager.prop_FeaturePermissionManager_0, new object[] { });
        }

        private static bool PanicMode()
        {
            if (PanicModeButtonFunctionality.Value == false)
            {
                MelonLogger.Msg("Blocked the call to reset the safety settings from panic mode");
                return false; // disable the modification of safety settings
            }

            FeaturePermissionManager fManager = FeaturePermissionManager.prop_FeaturePermissionManager_0;
            recoverSafetySettings = !recoverSafetySettings;
            
            if (recoverSafetySettings)
            {
                MelonLogger.Msg("Safety Setting Restoring to before Panic Mode");
                ApplySafetySettings();

                return false;
            }

            MelonLogger.Msg("Panic Mode enabled! Pre Panic Mode safety setting saved for this session!");

            prePanicPreset = new Il2CppSystem.Collections.Generic.Dictionary<VRC.UserSocialClass, VRC.FeaturePermissionSet>(
                dictionary:
                    fManager.field_Private_Dictionary_2_UserSocialClass_FeaturePermissionSet_0.TryCast<Il2CppSystem.Collections.Generic.IDictionary<VRC.UserSocialClass, VRC.FeaturePermissionSet>>()
            );
            return true;
        }

        private static bool QueueHudMessagePrefix(ref string __0)
        {
            if (__0.StartsWith("You have activated SAFE MODE"))
            {
                if (!PanicModeTextDisplay.Value) return false;
                if (ModifiedPanicModeTextDisplay.Value) __0 = recoverSafetySettings ? "SAFE MODE Active, Tap keybind again in 10 seconds to disable/revert." : "SAFE MODE Disabled. Recovering prior custom settings";
            }
            return true;
        }
        
        /*
        Helper functions for Xref scanning

        Yoinked with love from https://github.com/BenjaminZehowlt/DynamicBonesSafety/blob/master/DynamicBonesSafetyMod.cs
        or well, the current alternative https://github.com/loukylor/VRC-Mods/blob/main/PlayerList/Utilities/Xref.cs
        */
        public static bool CheckMethod(MethodInfo method, string match)
        {
            try
            {
                return XrefScanner.XrefScan(method)
                    .Where(instance => instance.Type == XrefType.Global && instance.ReadAsObject().ToString().Contains(match)).Any();
            }
            catch { }
            return false;
        }

        public static bool CheckUsing(MethodInfo method, string match, Type type)
        {
            foreach (XrefInstance instance in XrefScanner.XrefScan(method))
            {
                if (instance.Type == XrefType.Method)
                    try
                    {
                        if (instance.TryResolve().DeclaringType == type && instance.TryResolve().Name.Contains(match))
                            return true;
                    }
                    catch
                    {

                    }
            }
            return false;
        }

        private bool CheckReflectedType(MethodInfo method, Type reflectedTypeMatch)
        {
            try
            {
                return XrefScanner.XrefScan(method)
                    .Where(x => x.Type == XrefType.Method)
                    .Where(x =>
                    {
                        MethodBase resolvedMethod = x.TryResolve();
                        if (resolvedMethod != null)
                            return resolvedMethod.ReflectedType == reflectedTypeMatch;
                        return false;
                    }).Any();
            }
            catch { }
            return false;
        }
    }
}
