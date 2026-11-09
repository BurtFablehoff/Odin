#if ODIN_DEV
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace OdinV2
{
    // V12 1d: the headless menu listing in the --test-dev-art report. The window is constructed but never shown.
    // 3.2.0: it checks the testing menu's layout (Hatt 2026-11-08) and lists every item in every state. Before 3.2.0 it listed (top level, Latest artwork stills, the held-state items, and Transitions
    // and Actions for every state) and fails if a retired (5, 9, 10) or parked (11, plus the pack's parkedStatesV12 ids: 6/7/8 since
    // V12 3d) pose shows up in any of them.
    internal sealed partial class OdinWindow
    {
        private static readonly int[] RetiredStillIds={5,9,10};
        private static readonly int[] ParkedStillIds={11};
        private static readonly string[] RetiredMenuWords={"tucked","curled","side sleep","side-sleep","belly"};
        private static void ListMenuTexts(ToolStripItemCollection items,string prefix,List<string> into)
        {
            foreach(ToolStripItem entry in items)
            {
                ToolStripMenuItem item=entry as ToolStripMenuItem;if(item==null)continue;
                into.Add(prefix+item.Text);ListMenuTexts(item.DropDownItems,prefix+item.Text+" > ",into);
            }
        }
        private static int StillIdOfLabel(string label)
        {
            int dot=label.IndexOf(". ",StringComparison.Ordinal),id;
            return dot>0 && Int32.TryParse(label.Substring(0,dot),out id)?id:-1;
        }
        private static bool IsListedId(int[] ids,int id){return Array.IndexOf(ids,id)>=0;}
        internal object CheckDevMenuListing()
        {
            List<string> failures=new List<string>(),every=new List<string>();
            List<string> topLevel=TopLevelTexts(menu.Items);string shape=String.Join(" / ",topLevel.ToArray());
            if(shape!=String.Join(" / ",DevTopLevel))failures.Add("Testing menu is not Hatt's layout: "+shape);
            foreach(string text in topLevel)if(text.StartsWith("Latest artwork stills") || text.StartsWith("Transitions"))failures.Add("Removed menu is back: "+text);
            Dictionary<string,object> byState=new Dictionary<string,object>();
            foreach(string state in DevStates)
            {
                SelectDevState(state);RefreshDevMenu();List<string> texts=new List<string>(),enabled=new List<string>();
                ListMenuTexts(menu.Items,"",texts);every.AddRange(texts);
                foreach(ToolStripItem item in devActionItems)if(item.Enabled)enabled.Add(item.Text);
                byState.Add(state,new{enabledActions=enabled,clockGazes=devClockGazes.DropDownItems.Count});
            }
            SelectDevState("sit");
            foreach(string text in every)
            {
                string lower=text.ToLowerInvariant();
                foreach(string word in RetiredMenuWords)if(lower.Contains(word))failures.Add("Menu item names a retired or parked pose ('"+word+"'): "+text);
            }
            return new{passed=failures.Count==0,failures=failures,layout=topLevel,retiredStillIds=RetiredStillIds,parkedStillIds=ParkedStillIds,parkedForThisVersion=latestArt.ParkedStateIds,
                forbiddenWords=RetiredMenuWords,byState=byState,menuItemsChecked=every.Count};
        }
    }
}
#endif
