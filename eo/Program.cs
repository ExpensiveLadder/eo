using System;
using System.Collections.Generic;
using System.Linq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using System.Threading.Tasks;

namespace eo
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .Run(args, new RunPreferences()
                {
                    ActionsForEmptyArgs = new RunDefaultPatcher()
                    {
                        IdentifyingModKey = "YourPatcher.esp",
                        TargetRelease = GameRelease.SkyrimSE,
                    }
                });
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
           
            var LChars = new List<ILeveledNpcGetter>();

            foreach (var LCharGetter in state.LoadOrder.PriorityOrder.LeveledNpc().WinningOverrides())
            {
                if (LCharGetter.EditorID != null && LCharGetter.EditorID.Contains("SubSubCharWarlockBoss02"))
                {
                    LChars.Add(LCharGetter);
                    Console.WriteLine("Adding " + LCharGetter.EditorID + " to list");
                }

            }

            int i = 3;

            do
            {
                var LChars2 = new List<LeveledNpc>();
                foreach (var LChar in LChars)
                {
                    var LCharNew = state.PatchMod.LeveledNpcs.DuplicateInAsNewRecord(LChar);
                    if (LCharNew.EditorID != null)
                    {
                        var newEditorID = LCharNew.EditorID.Replace("SubSubCharWarlockBoss02", "SubSubCharWarlockBoss0" + i);
                        Console.WriteLine("Renaming " + LCharNew.EditorID + " to " + newEditorID);
                        LCharNew.EditorID = newEditorID;
                        state.PatchMod.LeveledNpcs.Set(LCharNew);
                        LChars2.Add(LCharNew);
                    }
                }
                foreach (var LChar in LChars2)
                {
                    if (LChar.Entries != null)
                    {
                        var newReferences = new List<FormKey>();
                        foreach (var LCharEntry in LChar.Entries)
                        {
                            if (LCharEntry.Data?.Reference != null)
                            {
                                var reference = LCharEntry.Data.Reference.Resolve(state.LinkCache);
                                if (reference?.EditorID != null)
                                {
                                    var editorID = reference.EditorID.Replace("WarlockBoss02", "WarlockBoss0" + i);

                                    if (state.LinkCache.TryResolve<ILeveledNpcGetter>(editorID, out var newReference))
                                    {
                                        newReferences.Add(newReference.FormKey);
                                        Console.WriteLine("Changing reference " + reference.EditorID + " to " + editorID);
                                    }
                                    else if (state.LinkCache.TryResolve<INpcGetter>(editorID, out var newReference2))
                                    {
                                        newReferences.Add(newReference2.FormKey);
                                        Console.WriteLine("Changing reference " + reference.EditorID + " to " + editorID);
                                    }
                                    else
                                    {
                                        Console.WriteLine("Could not find " + editorID + " for " + LChar.EditorID);
                                    }
                                }
                            }
                        }
                        LChar.Entries = new Noggog.ExtendedList<LeveledNpcEntry>();
                        foreach (var reference in newReferences)
                        {
                            LChar.Entries.Add(new LeveledNpcEntry()
                            {
                                Data = new LeveledNpcEntryData()
                                {
                                    Level = 1,
                                    Count = 1,
                                    Reference = reference
                                }
                            });
                        }
                        state.PatchMod.LeveledNpcs.Set(LChar);
                    }
                }
                i++;
            } while (i < 8);
        }
    }
}