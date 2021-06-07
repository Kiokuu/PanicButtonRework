using System;
using System.Linq;
using System.Reflection;
using MelonLoader;
using UnhollowerRuntimeLib.XrefScans;
using Harmony;
using VRC;

namespace PanicButtonRework
{
    public class Mod : MelonMod
    {
        public static bool recoverSafetySettings = true;

        public static Il2CppSystem.Collections.Generic.Dictionary<VRC.UserSocialClass, VRC.FeaturePermissionSet> prePanicPreset;

        private static MethodBase applySafety;
        private static MethodBase bestUsingExample;
        private static MethodBase panicModeOn;

        public static MelonPreferences_Entry<bool> PanicModeButtonFunctionality;

        public override void OnApplicationStart()
        {
            MelonPreferences.CreateCategory("PanicButtonRework", "PanicButtonRework Settings");
            PanicModeButtonFunctionality = (MelonPreferences_Entry<bool>)MelonPreferences.CreateEntry("PanicButtonRework", nameof(PanicModeButtonFunctionality), true, "Toggle the functionality of the Panic Button");

            applySafety = typeof(FeaturePermissionManager).GetMethods().Where(
                methodBase => methodBase.Name.StartsWith("Method_Public_Void_")
                && !methodBase.Name.Contains("PDM")
                && CheckMethod(methodBase, "Safety Settings Changed to: "))
                .First();

            bestUsingExample = typeof(VRCInputManager).GetMethods().Where(
                methodBase => methodBase.Name.StartsWith("Method_Public_Static_set_Void_Int32_")
                && !methodBase.Name.Contains("PDM")
                && CheckMethod(methodBase, "VRC_SAFETY_LEVEL"))
                .First();

            panicModeOn = typeof(FeaturePermissionManager).GetMethods().Where(
                methodBase => methodBase.Name.StartsWith("Method_Public_Void_")
                && !methodBase.Name.Contains("PDM")
                && CheckUsing(methodBase, bestUsingExample.Name, typeof(VRCInputManager)))
                .First();

            MelonLogger.Msg($"OnTrustSettingsChanged determined to be: {applySafety.DeclaringType}.{applySafety.Name}");
            MelonLogger.Msg($"VRC_SAFETY_LEVEL string found in {bestUsingExample.DeclaringType}.{bestUsingExample.Name}");
            MelonLogger.Msg($"Panic Mode On determined to be : {panicModeOn.DeclaringType}.{panicModeOn.Name}");

            Harmony.Patch(panicModeOn, 
                prefix: new HarmonyMethod(typeof(Mod).GetMethod(nameof(PanicMode), BindingFlags.NonPublic | BindingFlags.Static))
            );

        }
        public static void ApplySafetySettings() => applySafety.Invoke(FeaturePermissionManager.prop_FeaturePermissionManager_0, null);

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

                //If anyone knows a better way of doing this, please submit a PR ^^
                foreach (Il2CppSystem.Collections.Generic.KeyValuePair<UserSocialClass, FeaturePermissionSet> rankPerm in prePanicPreset)
                {
                    fManager.field_Private_Dictionary_2_UserSocialClass_FeaturePermissionSet_0[rankPerm.Key].field_Public_Boolean_0 = rankPerm.value.field_Public_Boolean_0;
                    fManager.field_Private_Dictionary_2_UserSocialClass_FeaturePermissionSet_0[rankPerm.Key].field_Public_Boolean_1 = rankPerm.value.field_Public_Boolean_1;
                    fManager.field_Private_Dictionary_2_UserSocialClass_FeaturePermissionSet_0[rankPerm.Key].field_Public_Boolean_2 = rankPerm.value.field_Public_Boolean_2;
                    fManager.field_Private_Dictionary_2_UserSocialClass_FeaturePermissionSet_0[rankPerm.Key].field_Public_Boolean_3 = rankPerm.value.field_Public_Boolean_3;
                    fManager.field_Private_Dictionary_2_UserSocialClass_FeaturePermissionSet_0[rankPerm.Key].field_Public_Boolean_4 = rankPerm.value.field_Public_Boolean_4;
                    fManager.field_Private_Dictionary_2_UserSocialClass_FeaturePermissionSet_0[rankPerm.Key].field_Public_Boolean_5 = rankPerm.value.field_Public_Boolean_5;
                    fManager.field_Private_Dictionary_2_UserSocialClass_FeaturePermissionSet_0[rankPerm.Key].field_Public_Boolean_6 = rankPerm.value.field_Public_Boolean_6;
                }

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
    }
}
