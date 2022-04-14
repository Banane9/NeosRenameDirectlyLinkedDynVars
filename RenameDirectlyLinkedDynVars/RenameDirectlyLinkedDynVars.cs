using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CodeX;
using FrooxEngine;
using FrooxEngine.LogiX.Input;
using HarmonyLib;
using NeosModLoader;

namespace RenameDirectlyLinkedDynVars
{
    public class RenameDirectlyLinkedDynVars : NeosMod
    {
        public static ModConfiguration Config;

        [AutoRegisterConfigKey]
        private static ModConfigurationKey<bool> ChangeDynVarNamespaces = new ModConfigurationKey<bool>("ChangeDynVarNamespaces", "Enable searching and renaming directly linked variables and drivers when namespace changes.", () => false);

        [AutoRegisterConfigKey]
        private static ModConfigurationKey<bool> ChangeLogixStringInputs = new ModConfigurationKey<bool>("ChangeLogixStringInputs", "Search and rename logix inputs with the old name in the form OldName/.* (Experimental).", () => false);

        public override string Author => "Banane9";
        public override string Link => "https://github.com/Banane9/NeosRenameDirectlyLinkedDynVars";
        public override string Name => "RenameDirectlyLinkedDynVars";
        public override string Version => "1.1.0";

        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony($"{Author}.{Name}");
            Config = GetConfiguration();
            Config.Save(true);
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(DynamicVariableSpace))]
        private static class DynamicVariableSpacePatch
        {
            [HarmonyPrefix]
            [HarmonyPatch("UpdateName")]
            private static void UpdateNamePrefix(DynamicVariableSpace __instance, string ____lastName, bool ____lastNameSet)
            {
                var newName = DynamicVariableHelper.ProcessName(__instance.SpaceName.Value);

                if (!Config.GetValue(ChangeDynVarNamespaces) || (newName == ____lastName && ____lastNameSet))
                    return;

                __instance.Slot.ForeachComponentInChildren<IDynamicVariable>(dynVar =>
                {
                    DynamicVariableHelper.ParsePath(dynVar.VariableName, out string spaceName, out string variableName);
                    if (spaceName == null || Traverse.Create(dynVar).Field("handler").Field("_currentSpace").GetValue() != __instance)
                        return;

                    var nameField = ((Worker)dynVar).TryGetField<string>("VariableName") ?? ((Worker)dynVar).TryGetField<string>("_variableName");
                    nameField.Value = $"{newName}/{variableName}";
                }, true, true);

                if (!Config.GetValue(ChangeLogixStringInputs))
                    return;

                __instance.Slot.ForeachComponentInChildren<StringInput>(stringInput =>
                {
                    DynamicVariableHelper.ParsePath(stringInput.CurrentValue, out string spaceName, out string variableName);
                    if (spaceName == null || spaceName != ____lastName)
                        return;

                    stringInput.CurrentValue = $"{newName}/{variableName}";
                }, true, true);
            }
        }
    }
}