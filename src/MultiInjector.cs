using System;
using System.IO;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

class MultiInjector
{
    static int Main(string[] args)
    {
        try
        {
            string managedDir = @"C:\Program Files (x86)\Steam\steamapps\common\Travelling at Night Demo\travelling_Data\Managed";
            string scratchDir = @"C:\Users\czion\.gemini\antigravity\brain\3d906a87-bd8a-42d5-9be5-78a6d517da72\scratch";

            string scriptsBak = Path.Combine(managedDir, "travelling.scripts.dll.bak");
            if (!File.Exists(scriptsBak)) scriptsBak = Path.Combine(managedDir, "travelling.scripts.dll");

            string tmproBak = Path.Combine(managedDir, "Unity.TextMeshPro.dll.bak");
            if (!File.Exists(tmproBak)) tmproBak = Path.Combine(managedDir, "Unity.TextMeshPro.dll");

            string dsBak = Path.Combine(managedDir, "DialogueSystem.dll.bak");
            if (!File.Exists(dsBak)) dsBak = Path.Combine(managedDir, "DialogueSystem.dll");

            string patchDllPath = Path.Combine(scratchDir, "TravellingKoreanPatch.dll");
            string outScriptsPath = Path.Combine(scratchDir, "travelling.scripts.dll");
            string outTmproPath = Path.Combine(scratchDir, "Unity.TextMeshPro.dll");
            string outDsPath = Path.Combine(scratchDir, "DialogueSystem.dll");

            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(managedDir);
            resolver.AddSearchDirectory(scratchDir);
            var readerParams = new ReaderParameters { AssemblyResolver = resolver, ReadWrite = false };

            var patchAsm = AssemblyDefinition.ReadAssembly(patchDllPath, readerParams);
            var patchType = patchAsm.MainModule.GetType("Travelling.Korean.KoreanPatch");

            MethodDefinition translateDef = null;
            MethodDefinition translateDialogueDef = null;
            MethodDefinition translateSpeakerDef = null;
            MethodDefinition ensureFallbackDef = null;
            MethodDefinition ensureTmpTextDef = null;
            MethodDefinition processTmpTextDef = null;
            MethodDefinition translateLocKeyDef = null;
            MethodDefinition translateLocArgsDef = null;
            MethodDefinition translateTokenValDef = null;
            MethodDefinition hookQuoteSceneDef = null;

            foreach (var m in patchType.Methods)
            {
                if (m.Name == "Translate" && m.Parameters.Count == 1) translateDef = m;
                if (m.Name == "TranslateDialogue" && m.Parameters.Count == 1) translateDialogueDef = m;
                if (m.Name == "TranslateSpeakerName" && m.Parameters.Count == 1) translateSpeakerDef = m;
                if (m.Name == "EnsureFallbackFont" && m.Parameters.Count == 1) ensureFallbackDef = m;
                if (m.Name == "EnsureTMPText" && m.Parameters.Count == 1) ensureTmpTextDef = m;
                if (m.Name == "ProcessTMPText" && m.Parameters.Count == 1) processTmpTextDef = m;
                if (m.Name == "TranslateLocKey" && m.Parameters.Count == 1) translateLocKeyDef = m;
                if (m.Name == "TranslateLocArgs" && m.Parameters.Count == 2) translateLocArgsDef = m;
                if (m.Name == "TranslateTokenValue" && m.Parameters.Count == 1) translateTokenValDef = m;
                if (m.Name == "HookQuoteScene" && m.Parameters.Count == 2) hookQuoteSceneDef = m;
            }

            Console.WriteLine("Resolved KoreanPatch methods: Translate=" + (translateDef != null) +
                ", TranslateDialogue=" + (translateDialogueDef != null) +
                ", TranslateSpeakerName=" + (translateSpeakerDef != null) +
                ", EnsureFallbackFont=" + (ensureFallbackDef != null) +
                ", EnsureTMPText=" + (ensureTmpTextDef != null) +
                ", ProcessTMPText=" + (processTmpTextDef != null) +
                ", TranslateLocKey=" + (translateLocKeyDef != null) +
                ", TranslateLocArgs=" + (translateLocArgsDef != null));

            // ==========================================
            // 1. PATCH travelling.scripts.dll
            // ==========================================
            Console.WriteLine("\n--- Patching travelling.scripts.dll ---");
            var scriptsAsm = AssemblyDefinition.ReadAssembly(scriptsBak, readerParams);

            var translateRefScripts = scriptsAsm.MainModule.ImportReference(translateDef);
            var translateDialogueRefScripts = scriptsAsm.MainModule.ImportReference(translateDialogueDef);
            var translateSpeakerRefScripts = scriptsAsm.MainModule.ImportReference(translateSpeakerDef);
            var translateLocKeyRef = scriptsAsm.MainModule.ImportReference(translateLocKeyDef);
            var translateLocArgsRef = scriptsAsm.MainModule.ImportReference(translateLocArgsDef);
            var translateTokenValRef = scriptsAsm.MainModule.ImportReference(translateTokenValDef);
            var hookQuoteRef = scriptsAsm.MainModule.ImportReference(hookQuoteSceneDef);

            // 1-A. Travelling.Loc hooks
            var locType = scriptsAsm.MainModule.GetType("Travelling.Loc");
            if (locType != null)
            {
                foreach (var m in locType.Methods)
                {
                    if (m.Name == "ForCulture" && m.Parameters.Count == 2)
                    {
                        Console.WriteLine("Hooking Travelling.Loc::ForCulture(key, cultureId)...");
                        HookAllStringReturns(m, translateRefScripts);

                        var il = m.Body.GetILProcessor();
                        var first = m.Body.Instructions[0];
                        var popInst = il.Create(OpCodes.Pop);
                        il.InsertBefore(first, il.Create(OpCodes.Ldarg_0));
                        il.InsertBefore(first, il.Create(OpCodes.Call, translateLocKeyRef));
                        il.InsertBefore(first, il.Create(OpCodes.Dup));
                        il.InsertBefore(first, il.Create(OpCodes.Brfalse_S, popInst));
                        il.InsertBefore(first, il.Create(OpCodes.Call, translateRefScripts));
                        il.InsertBefore(first, il.Create(OpCodes.Ret));
                        il.InsertBefore(first, popInst);
                    }
                    else if (m.Name == "FormatWithArgs" && m.Parameters.Count == 2)
                    {
                        Console.WriteLine("Hooking Travelling.Loc::FormatWithArgs(template, args)...");
                        HookAllStringReturns(m, translateRefScripts);

                        var il = m.Body.GetILProcessor();
                        var first = m.Body.Instructions[0];
                        il.InsertBefore(first, il.Create(OpCodes.Ldarga_S, m.Parameters[0]));
                        il.InsertBefore(first, il.Create(OpCodes.Ldarg_1));
                        il.InsertBefore(first, il.Create(OpCodes.Call, translateLocArgsRef));
                    }
                }
            }

            // 1-B. Travelling.Utility.TravellingUtility hooks
            var utilType = scriptsAsm.MainModule.GetType("Travelling.Utility.TravellingUtility");
            if (utilType != null)
            {
                foreach (var m in utilType.Methods)
                {
                    if ((m.Name == "BracketsToColourizedLinks" || m.Name == "ResolveQualityTokensAndColourizeLinks") &&
                        m.Parameters.Count > 0 && m.Parameters[0].ParameterType.FullName == "System.String")
                    {
                        Console.WriteLine("Hooking TravellingUtility::" + m.Name + " (" + m.Parameters.Count + " params)...");
                        var il = m.Body.GetILProcessor();
                        var first = m.Body.Instructions[0];
                        il.InsertBefore(first, il.Create(OpCodes.Ldarg_0));
                        il.InsertBefore(first, il.Create(OpCodes.Call, translateRefScripts));
                        il.InsertBefore(first, il.Create(OpCodes.Starg_S, m.Parameters[0]));
                    }
                }
            }

            // 1-C. TravellingResponseButton::DecomposeResponseText hook
            var respBtnType = scriptsAsm.MainModule.GetType("Travelling.UI.Dialogue.TravellingResponseButton");
            if (respBtnType != null)
            {
                foreach (var m in respBtnType.Methods)
                {
                    if (m.Name == "DecomposeResponseText" && m.Parameters.Count == 2)
                    {
                        Console.WriteLine("Hooking TravellingResponseButton::DecomposeResponseText(rawText, scm)...");
                        var il = m.Body.GetILProcessor();
                        var first = m.Body.Instructions[0];
                        il.InsertBefore(first, il.Create(OpCodes.Ldarg_1));
                        il.InsertBefore(first, il.Create(OpCodes.Call, translateDialogueRefScripts));
                        il.InsertBefore(first, il.Create(OpCodes.Starg_S, m.Parameters[0]));
                    }
                }
            }

            // 1-D. TravellingTypewriter hooks
            var typewriterType = scriptsAsm.MainModule.GetType("Travelling.UI.Dialogue.TravellingTypewriter");
            if (typewriterType != null)
            {
                foreach (var m in typewriterType.Methods)
                {
                    if ((m.Name == "StartTyping" || m.Name == "PlayText") && m.Parameters.Count == 2)
                    {
                        Console.WriteLine("Hooking TravellingTypewriter::" + m.Name + "(text, fromIndex)...");
                        var il = m.Body.GetILProcessor();
                        var first = m.Body.Instructions[0];
                        il.InsertBefore(first, il.Create(OpCodes.Ldarg_1));
                        il.InsertBefore(first, il.Create(OpCodes.Call, translateDialogueRefScripts));
                        il.InsertBefore(first, il.Create(OpCodes.Starg_S, m.Parameters[0]));
                    }
                }
            }

            // 1-E. TravellingSubtitlePanel Speaker hooks
            var subPanelType = scriptsAsm.MainModule.GetType("Travelling.UI.Dialogue.TravellingSubtitlePanel");
            if (subPanelType != null)
            {
                foreach (var m in subPanelType.Methods)
                {
                    if (m.Name == "SetPortraitName" && m.Parameters.Count >= 1 && m.Parameters[0].ParameterType.FullName == "System.String")
                    {
                        Console.WriteLine("Hooking TravellingSubtitlePanel::SetPortraitName...");
                        var il = m.Body.GetILProcessor();
                        var first = m.Body.Instructions[0];
                        il.InsertBefore(first, il.Create(OpCodes.Ldarg_1));
                        il.InsertBefore(first, il.Create(OpCodes.Call, translateSpeakerRefScripts));
                        il.InsertBefore(first, il.Create(OpCodes.Starg_S, m.Parameters[0]));
                    }
                    else if (m.Name == "GetMarkedSpeakerName" && m.ReturnType.FullName == "System.String" && m.HasBody)
                    {
                        Console.WriteLine("Hooking TravellingSubtitlePanel::GetMarkedSpeakerName...");
                        HookAllStringReturns(m, translateSpeakerRefScripts);
                    }
                }
            }

            // 1-F. QualitySpecHandler::ComposeRequirementPrefix (handles [Craft], [Not Ready For Crafting?], etc.)
            var qshType = scriptsAsm.MainModule.GetType("Travelling.UI.Dialogue.QualitySpecHandler");
            if (qshType != null)
            {
                foreach (var m in qshType.Methods)
                {
                    if (m.Name == "ComposeRequirementPrefix" && m.Parameters.Count == 4 && m.HasBody)
                    {
                        Console.WriteLine("Hooking QualitySpecHandler::ComposeRequirementPrefix...");
                        HookAllStringReturns(m, translateRefScripts);

                        var il = m.Body.GetILProcessor();
                        var first = m.Body.Instructions[0];
                        // arg 1: whenLockedOverride (static method, so ldarg.1 is parameter 1)
                        il.InsertBefore(first, il.Create(OpCodes.Ldarg_1));
                        il.InsertBefore(first, il.Create(OpCodes.Call, translateRefScripts));
                        il.InsertBefore(first, il.Create(OpCodes.Starg_S, m.Parameters[1]));
                        // arg 2: whenUnlockedOverride
                        il.InsertBefore(first, il.Create(OpCodes.Ldarg_2));
                        il.InsertBefore(first, il.Create(OpCodes.Call, translateRefScripts));
                        il.InsertBefore(first, il.Create(OpCodes.Starg_S, m.Parameters[2]));
                    }
                }
            }

            // 1-G. QualitySpec summary methods
            var qSpecType = scriptsAsm.MainModule.GetType("QualitySpec");
            if (qSpecType != null)
            {
                foreach (var m in qSpecType.Methods)
                {
                    if ((m.Name == "RequirementSummary" || m.Name == "FriendlySummary" || m.Name == "Summary") &&
                        m.ReturnType.FullName == "System.String" && m.HasBody)
                    {
                        Console.WriteLine("Hooking QualitySpec::" + m.Name + "()...");
                        HookAllStringReturns(m, translateRefScripts);
                    }
                }
            }

            // 1-H. QuoteSceneController
            var quoteCtrlType = scriptsAsm.MainModule.GetType("Travelling.UI.Quotes.QuoteSceneController");
            if (quoteCtrlType != null)
            {
                FieldDefinition quoteField = null;
                FieldDefinition sourceField = null;
                foreach (var f in quoteCtrlType.Fields)
                {
                    if (f.Name == "_quoteText") quoteField = f;
                    if (f.Name == "_sourceText") sourceField = f;
                }
                if (quoteField != null && sourceField != null)
                {
                    foreach (var m in quoteCtrlType.Methods)
                    {
                        if (m.Name == "UpdateText" && m.HasBody)
                        {
                            Console.WriteLine("Hooking QuoteSceneController::UpdateText...");
                            var il = m.Body.GetILProcessor();
                            var lastRet = m.Body.Instructions[m.Body.Instructions.Count - 1];
                            il.InsertBefore(lastRet, il.Create(OpCodes.Ldarg_0));
                            il.InsertBefore(lastRet, il.Create(OpCodes.Ldfld, quoteField));
                            il.InsertBefore(lastRet, il.Create(OpCodes.Ldarg_0));
                            il.InsertBefore(lastRet, il.Create(OpCodes.Ldfld, sourceField));
                            il.InsertBefore(lastRet, il.Create(OpCodes.Call, hookQuoteRef));
                        }
                    }
                }
            }

            // 1-I. Journal & Quality Display Classes (preserving ScriptablesCurator label keys!)
            string[] journalClasses = new string[] {
                "AxisQuality",
                "VariableQuality",
                "Quality",
                "QualityLevelDescription",
                "Travelling.PCQualities.RelationshipQuality",
                "Travelling.PCQualities.StateSO",
                "Travelling.PCQualities.Given",
                "Travelling.PCQualities.ConditionQuality",
                "Travelling.PCQualities.CompanionQuality",
                "Travelling.PCQualities.SkillInstance",
                "Travelling.Infrastructure.Footnotes.Footnote",
                "Travelling.Infrastructure.Footnotes.FootnoteChoice",
                "Travelling.Aliases.Alias"
            };

            string[] journalMethods = new string[] {
                "get_Label",
                "get_PlayerFacingLabel",
                "get_DisplayName",
                "get_Description",
                "get_AuthoredSummary",
                "get_ChangeMessage",
                "GetQualityDescriptionForJournal",
                "GetStatusLabelForJournal",
                "GetStatusDescriptionForJournal",
                "GetLongDescriptionForQLD",
                "GetAppropriateStateChangeMessage",
                "GetAppropriateQualityChangeMessage",
                "GetCurrentRelationshipDescriptorLocalised",
                "BuildRelationshipAlertMessage",
                "HumanisedLabel",
                "FriendlySummary",
                "LabelWithCriterionQuantity",
                "RenderEntryMessage",
                "RenderExitMessage",
                "RenderTransitionMessageTo",
                "GetModifiersDescription",
                "GetSummary",
                "ModifierDescription",
                "GetMessageForAlert",
                "get_FirstName",
                "get_LastName",
                "get_FullName",
                "get_Formal",
                "get_Familiar",
                "get_Title",
                "get_Nationality",
                "get_Rank",
                "GetTitleFor",
                "GetFormalFor"
            };

            var jMethodSet = new HashSet<string>(journalMethods);

            foreach (var cName in journalClasses)
            {
                var jType = scriptsAsm.MainModule.GetType(cName);
                if (jType == null) continue;

                foreach (var m in jType.Methods)
                {
                    if (!m.HasBody || m.ReturnType.FullName != "System.String") continue;

                    // get_Label of Footnote/FootnoteChoice/Alias is the ScriptablesCurator lookup KEY
                    // (link id -> GetDetailedScriptableByLabel). Translating it breaks every footnote tooltip.
                    // Display text is still translated later by the TMP_Text::set_text hook.
                    if (m.Name == "get_Label" && (cName == "Travelling.Infrastructure.Footnotes.Footnote" ||
                        cName == "Travelling.Infrastructure.Footnotes.FootnoteChoice" || cName == "Travelling.Aliases.Alias"))
                    {
                        Console.WriteLine("SKIP (lookup key) " + jType.Name + "::get_Label");
                        continue;
                    }

                    if (jMethodSet.Contains(m.Name))
                    {
                        Console.WriteLine("Hooking Journal/Quality method: " + jType.Name + "::" + m.Name + "()...");
                        HookAllStringReturns(m, translateRefScripts);
                    }
                    else if (m.Name == "GetActorDisplayName")
                    {
                        Console.WriteLine("Hooking " + jType.Name + "::GetActorDisplayName()...");
                        HookAllStringReturns(m, translateSpeakerRefScripts);
                    }
                }
            }

            // 1-J. QualityTokenResolver::Resolve (translates [q=...] templates BEFORE token substitution and resolves particles AFTER)
            var qtrType = scriptsAsm.MainModule.GetType("Travelling.PCQualities.QualityTokenResolver");
            if (qtrType != null)
            {
                foreach (var m in qtrType.Methods)
                {
                    if (m.Name == "Resolve" && m.Parameters.Count == 3 && m.HasBody)
                    {
                        Console.WriteLine("Hooking QualityTokenResolver::Resolve(input, env, curator)...");
                        HookAllStringReturns(m, translateRefScripts);

                        var il = m.Body.GetILProcessor();
                        var first = m.Body.Instructions[0];
                        il.InsertBefore(first, il.Create(OpCodes.Ldarg_0));
                        il.InsertBefore(first, il.Create(OpCodes.Call, translateRefScripts));
                        il.InsertBefore(first, il.Create(OpCodes.Starg_S, m.Parameters[0]));
                    }
                }
                foreach (var nt in qtrType.NestedTypes)
                {
                    foreach (var nm in nt.Methods)
                    {
                        if (nm.Name.Contains("<Resolve>b__") && nm.ReturnType.FullName == "System.String" && nm.HasBody)
                        {
                            Console.WriteLine("Hooking QualityTokenResolver::<Resolve>b__0 token evaluator...");
                            HookAllStringReturns(nm, translateTokenValRef);
                        }
                    }
                }
            }

            // 1-K. ConditionAlertRenderer::Render & QualityChangeFraming::Frame & AlertRouter
            var carType = scriptsAsm.MainModule.GetType("Travelling.PCQualities.ConditionAlertRenderer");
            if (carType != null)
            {
                foreach (var m in carType.Methods)
                {
                    if (m.Name == "Render" && m.Parameters.Count == 4 && m.HasBody)
                    {
                        Console.WriteLine("Hooking ConditionAlertRenderer::Render...");
                        HookAllStringReturns(m, translateRefScripts);
                        var il = m.Body.GetILProcessor();
                        var first = m.Body.Instructions[0];
                        for (int pi = 1; pi <= 3; pi++)
                        {
                            il.InsertBefore(first, il.Create(OpCodes.Ldarg_S, m.Parameters[pi]));
                            il.InsertBefore(first, il.Create(OpCodes.Call, translateRefScripts));
                            il.InsertBefore(first, il.Create(OpCodes.Starg_S, m.Parameters[pi]));
                        }
                    }
                    else if ((m.Name == "RenderAgain" || m.Name == "RenderExitAgain") && m.HasBody)
                    {
                        HookAllStringReturns(m, translateRefScripts);
                    }
                }
            }

            var qcfType = scriptsAsm.MainModule.GetType("Travelling.PCQualities.QualityChangeFraming");
            if (qcfType != null)
            {
                foreach (var m in qcfType.Methods)
                {
                    if (m.Name == "Frame" && m.Parameters.Count == 3 && m.HasBody)
                    {
                        Console.WriteLine("Hooking QualityChangeFraming::Frame...");
                        HookAllStringReturns(m, translateRefScripts);
                        var il = m.Body.GetILProcessor();
                        var first = m.Body.Instructions[0];
                        for (int pi = 1; pi <= 2; pi++)
                        {
                            il.InsertBefore(first, il.Create(OpCodes.Ldarg_S, m.Parameters[pi]));
                            il.InsertBefore(first, il.Create(OpCodes.Call, translateRefScripts));
                            il.InsertBefore(first, il.Create(OpCodes.Starg_S, m.Parameters[pi]));
                        }
                    }
                }
            }

            var arType = scriptsAsm.MainModule.GetType("Travelling.UI.Popups.AlertRouter");
            if (arType != null)
            {
                foreach (var m in arType.Methods)
                {
                    if ((m.Name == "EnqueueInlineAlert" || m.Name == "Show") && m.Parameters.Count >= 1 &&
                        m.Parameters[0].ParameterType.FullName == "System.String" && m.HasBody)
                    {
                        Console.WriteLine("Hooking AlertRouter::" + m.Name + "...");
                        var il = m.Body.GetILProcessor();
                        var first = m.Body.Instructions[0];
                        il.InsertBefore(first, il.Create(OpCodes.Ldarg_1));
                        il.InsertBefore(first, il.Create(OpCodes.Call, translateRefScripts));
                        il.InsertBefore(first, il.Create(OpCodes.Starg_S, m.Parameters[0]));
                    }
                }
            }

            scriptsAsm.Write(outScriptsPath);
            Console.WriteLine("Successfully wrote patched travelling.scripts.dll to: " + outScriptsPath);

            // ==========================================
            // 1.5. PATCH travelling.hud.dll
            // ==========================================
            string hudPath = Path.Combine(managedDir, "travelling.hud.dll");
            string hudBak = Path.Combine(managedDir, "travelling.hud.dll.bak");
            string outHudPath = Path.Combine(scratchDir, "travelling.hud.dll");
            if (File.Exists(hudPath))
            {
                if (!File.Exists(hudBak)) File.Copy(hudPath, hudBak, false);
                Console.WriteLine("\n--- Patching travelling.hud.dll ---");
                var hudAsm = AssemblyDefinition.ReadAssembly(hudBak, readerParams);
                var translateRefHud = hudAsm.MainModule.ImportReference(translateDef);

                var stockRowType = hudAsm.MainModule.GetType("Travelling.HUD.Merchant.StockRow");
                if (stockRowType != null)
                {
                    foreach (var m in stockRowType.Methods)
                    {
                        if (m.Name == "TruncateAtWord" && m.Parameters.Count == 2 && m.HasBody)
                        {
                            Console.WriteLine("Hooking StockRow::TruncateAtWord(text, maxChars)...");
                            var il = m.Body.GetILProcessor();
                            var first = m.Body.Instructions[0];
                            il.InsertBefore(first, il.Create(OpCodes.Ldarg_0));
                            il.InsertBefore(first, il.Create(OpCodes.Call, translateRefHud));
                            il.InsertBefore(first, il.Create(OpCodes.Starg_S, m.Parameters[0]));
                        }
                    }
                }

                hudAsm.Write(outHudPath);
                Console.WriteLine("Successfully wrote patched travelling.hud.dll to: " + outHudPath);
            }

            // ==========================================
            // 2. PATCH DialogueSystem.dll
            // ==========================================
            Console.WriteLine("\n--- Patching DialogueSystem.dll ---");
            var dsAsm = AssemblyDefinition.ReadAssembly(dsBak, readerParams);

            var translateRefDs = dsAsm.MainModule.ImportReference(translateDef);
            var translateDialogueRefDs = dsAsm.MainModule.ImportReference(translateDialogueDef);
            var translateSpeakerRefDs = dsAsm.MainModule.ImportReference(translateSpeakerDef);

            // 2-A. DialogueEntry hooks
            var entryType = dsAsm.MainModule.GetType("PixelCrushers.DialogueSystem.DialogueEntry");
            if (entryType != null)
            {
                foreach (var m in entryType.Methods)
                {
                    if ((m.Name == "get_currentDialogueText" || m.Name == "get_DialogueText" ||
                         m.Name == "get_currentMenuText" || m.Name == "get_MenuText" ||
                         m.Name == "get_subtitleText" || m.Name == "get_responseButtonText") &&
                        m.ReturnType.FullName == "System.String" && m.HasBody)
                    {
                        Console.WriteLine("Hooking DialogueEntry::" + m.Name + "()...");
                        HookAllStringReturns(m, translateDialogueRefDs);
                    }
                }
            }

            // 2-B. DialogueManager::GetLocalizedText
            var dmType = dsAsm.MainModule.GetType("PixelCrushers.DialogueSystem.DialogueManager");
            if (dmType != null)
            {
                foreach (var m in dmType.Methods)
                {
                    if (m.Name == "GetLocalizedText" && m.Parameters.Count == 1 && m.ReturnType.FullName == "System.String")
                    {
                        Console.WriteLine("Hooking DialogueManager::GetLocalizedText(string)...");
                        HookAllStringReturns(m, translateRefDs);
                    }
                }
            }

            // 2-C. FormattedText::Parse
            var ftType = dsAsm.MainModule.GetType("PixelCrushers.DialogueSystem.FormattedText");
            if (ftType != null)
            {
                foreach (var m in ftType.Methods)
                {
                    if (m.Name == "Parse" && m.Parameters.Count >= 1 && m.Parameters[0].ParameterType.FullName == "System.String")
                    {
                        Console.WriteLine("Hooking FormattedText::Parse(string, ...)...");
                        var il = m.Body.GetILProcessor();
                        var first = m.Body.Instructions[0];
                        il.InsertBefore(first, il.Create(OpCodes.Ldarg_0));
                        il.InsertBefore(first, il.Create(OpCodes.Call, translateDialogueRefDs));
                        il.InsertBefore(first, il.Create(OpCodes.Starg_S, m.Parameters[0]));
                    }
                }
            }

            // 2-D. CharacterInfo::get_Name & GetLocalizedDisplayNameInDatabase
            var ciType = dsAsm.MainModule.GetType("PixelCrushers.DialogueSystem.CharacterInfo");
            if (ciType != null)
            {
                foreach (var m in ciType.Methods)
                {
                    if ((m.Name == "get_Name" || m.Name == "GetLocalizedDisplayNameInDatabase") &&
                        m.ReturnType.FullName == "System.String" && m.HasBody)
                    {
                        Console.WriteLine("Hooking CharacterInfo::" + m.Name + "()...");
                        HookAllStringReturns(m, translateSpeakerRefDs);
                    }
                }
            }

            // 2-E. StandardUISubtitlePanel::SetPortraitName
            var suspType = dsAsm.MainModule.GetType("PixelCrushers.DialogueSystem.StandardUISubtitlePanel");
            if (suspType != null)
            {
                foreach (var m in suspType.Methods)
                {
                    if (m.Name == "SetPortraitName" && m.Parameters.Count >= 1)
                    {
                        Console.WriteLine("Hooking StandardUISubtitlePanel::SetPortraitName...");
                        var il = m.Body.GetILProcessor();
                        var first = m.Body.Instructions[0];
                        il.InsertBefore(first, il.Create(OpCodes.Ldarg_1));
                        il.InsertBefore(first, il.Create(OpCodes.Call, translateSpeakerRefDs));
                        il.InsertBefore(first, il.Create(OpCodes.Starg_S, m.Parameters[0]));
                    }
                }
            }

            dsAsm.Write(outDsPath);
            Console.WriteLine("Successfully wrote patched DialogueSystem.dll to: " + outDsPath);

            // ==========================================
            // 3. PATCH Unity.TextMeshPro.dll
            // ==========================================
            Console.WriteLine("\n--- Patching Unity.TextMeshPro.dll ---");
            var tmproAsm = AssemblyDefinition.ReadAssembly(tmproBak, readerParams);
            var translateRefTmpro = tmproAsm.MainModule.ImportReference(translateDef);
            var ensureFallbackRef = tmproAsm.MainModule.ImportReference(ensureFallbackDef);
            var ensureTmpTextRef = tmproAsm.MainModule.ImportReference(ensureTmpTextDef);
            var processTmpTextRef = tmproAsm.MainModule.ImportReference(processTmpTextDef);

            var tmpText = tmproAsm.MainModule.GetType("TMPro.TMP_Text");
            var tmpUGUI = tmproAsm.MainModule.GetType("TMPro.TextMeshProUGUI");
            var tmp3D = tmproAsm.MainModule.GetType("TMPro.TextMeshPro");

            FieldDefinition mTextField = null;
            foreach (var f in tmpText.Fields)
            {
                if (f.Name == "m_text") { mTextField = f; break; }
            }

            foreach (var m in tmpText.Methods)
            {
                if (m.Name == "set_text")
                {
                    Console.WriteLine("Patching TMP_Text::set_text...");
                    var il = m.Body.GetILProcessor();
                    var first = m.Body.Instructions[0];
                    il.InsertBefore(first, il.Create(OpCodes.Ldarg_0));
                    il.InsertBefore(first, il.Create(OpCodes.Call, ensureTmpTextRef));
                    il.InsertBefore(first, il.Create(OpCodes.Ldarg_1));
                    il.InsertBefore(first, il.Create(OpCodes.Call, translateRefTmpro));
                    il.InsertBefore(first, il.Create(OpCodes.Starg_S, m.Parameters[0]));
                }
                else if (m.Name == "SetText" && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "System.String")
                {
                    Console.WriteLine("Patching TMP_Text::SetText(string)...");
                    var il = m.Body.GetILProcessor();
                    var first = m.Body.Instructions[0];
                    il.InsertBefore(first, il.Create(OpCodes.Ldarg_0));
                    il.InsertBefore(first, il.Create(OpCodes.Call, ensureTmpTextRef));
                    il.InsertBefore(first, il.Create(OpCodes.Ldarg_1));
                    il.InsertBefore(first, il.Create(OpCodes.Call, translateRefTmpro));
                    il.InsertBefore(first, il.Create(OpCodes.Starg_S, m.Parameters[0]));
                }
                else if (m.Name == "set_font")
                {
                    Console.WriteLine("Patching TMP_Text::set_font...");
                    var il = m.Body.GetILProcessor();
                    var first = m.Body.Instructions[0];
                    il.InsertBefore(first, il.Create(OpCodes.Ldarg_1));
                    il.InsertBefore(first, il.Create(OpCodes.Call, ensureFallbackRef));
                }
                else if (m.Name == "ParseInputText")
                {
                    Console.WriteLine("Patching TMP_Text::ParseInputText...");
                    var il = m.Body.GetILProcessor();
                    var first = m.Body.Instructions[0];
                    il.InsertBefore(first, il.Create(OpCodes.Ldarg_0));
                    il.InsertBefore(first, il.Create(OpCodes.Call, ensureTmpTextRef));
                    if (mTextField != null)
                    {
                        il.InsertBefore(first, il.Create(OpCodes.Ldarg_0));
                        il.InsertBefore(first, il.Create(OpCodes.Ldarg_0));
                        il.InsertBefore(first, il.Create(OpCodes.Ldfld, mTextField));
                        il.InsertBefore(first, il.Create(OpCodes.Call, translateRefTmpro));
                        il.InsertBefore(first, il.Create(OpCodes.Stfld, mTextField));
                    }
                }
            }

            if (tmpUGUI != null)
            {
                foreach (var m in tmpUGUI.Methods)
                {
                    if (m.Name == "Awake" || m.Name == "OnEnable" || m.Name == "LoadFontAsset")
                    {
                        Console.WriteLine("Patching TextMeshProUGUI::" + m.Name + "...");
                        var il = m.Body.GetILProcessor();
                        var first = m.Body.Instructions[0];
                        il.InsertBefore(first, il.Create(OpCodes.Ldarg_0));
                        il.InsertBefore(first, il.Create(OpCodes.Call, processTmpTextRef));
                    }
                    else if (m.Name == "GenerateTextMesh")
                    {
                        Console.WriteLine("Patching TextMeshProUGUI::GenerateTextMesh...");
                        var il = m.Body.GetILProcessor();
                        var first = m.Body.Instructions[0];
                        il.InsertBefore(first, il.Create(OpCodes.Ldarg_0));
                        il.InsertBefore(first, il.Create(OpCodes.Call, ensureTmpTextRef));
                    }
                }
            }

            if (tmp3D != null)
            {
                foreach (var m in tmp3D.Methods)
                {
                    if (m.Name == "Awake" || m.Name == "OnEnable" || m.Name == "LoadFontAsset")
                    {
                        Console.WriteLine("Patching TextMeshPro::" + m.Name + "...");
                        var il = m.Body.GetILProcessor();
                        var first = m.Body.Instructions[0];
                        il.InsertBefore(first, il.Create(OpCodes.Ldarg_0));
                        il.InsertBefore(first, il.Create(OpCodes.Call, processTmpTextRef));
                    }
                    else if (m.Name == "GenerateTextMesh")
                    {
                        Console.WriteLine("Patching TextMeshPro::GenerateTextMesh...");
                        var il = m.Body.GetILProcessor();
                        var first = m.Body.Instructions[0];
                        il.InsertBefore(first, il.Create(OpCodes.Ldarg_0));
                        il.InsertBefore(first, il.Create(OpCodes.Call, ensureTmpTextRef));
                    }
                }
            }

            tmproAsm.Write(outTmproPath);
            Console.WriteLine("Successfully wrote patched Unity.TextMeshPro.dll to: " + outTmproPath);

            Console.WriteLine("\nALL ASSEMBLIES SUCCESSFULLY PATCHED AND WRITTEN (v9)!");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR: " + ex);
            return 1;
        }
    }

    private static void HookAllStringReturns(MethodDefinition m, MethodReference transformMethod)
    {
        var il = m.Body.GetILProcessor();
        var rets = new List<Instruction>();
        foreach (var inst in m.Body.Instructions)
        {
            if (inst.OpCode == OpCodes.Ret)
            {
                rets.Add(inst);
            }
        }

        foreach (var ret in rets)
        {
            var callInst = il.Create(OpCodes.Call, transformMethod);
            il.InsertBefore(ret, callInst);

            // Redirect any branches that jumped directly to ret so they jump to callInst instead!
            foreach (var inst in m.Body.Instructions)
            {
                if (inst.Operand == ret)
                {
                    inst.Operand = callInst;
                }
                else if (inst.Operand is Instruction[])
                {
                    Instruction[] targets = (Instruction[])inst.Operand;
                    for (int i = 0; i < targets.Length; i++)
                    {
                        if (targets[i] == ret) targets[i] = callInst;
                    }
                }
            }
            if (m.Body.HasExceptionHandlers)
            {
                foreach (var eh in m.Body.ExceptionHandlers)
                {
                    if (eh.HandlerEnd == ret) eh.HandlerEnd = callInst;
                    if (eh.TryEnd == ret) eh.TryEnd = callInst;
                }
            }
        }
    }
}
