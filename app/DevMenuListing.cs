#if ODIN_DEV
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace OdinV2
{
    // V12 1d: the headless menu listing in the --test-dev-art report. The window is constructed but never shown.
    // It lists what the real DEV menus offer (top level, Latest artwork stills, the held-state items, and Transitions
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
            List<string> failures=new List<string>();
            List<string> topLevel=new List<string>(),stills=new List<string>(),stateItems=new List<string>(),every=new List<string>();
            foreach(ToolStripItem entry in menu.Items){ToolStripMenuItem item=entry as ToolStripMenuItem;if(item!=null)topLevel.Add(item.Text);}
            ListMenuTexts(menu.Items,"",every);
            ToolStripMenuItem stillsMenu=null;
            foreach(ToolStripItem entry in menu.Items){ToolStripMenuItem item=entry as ToolStripMenuItem;if(item!=null && item.Text.StartsWith("Latest artwork stills"))stillsMenu=item;}
            if(stillsMenu==null)failures.Add("The DEV menu has no Latest artwork stills item.");
            else foreach(ToolStripItem entry in stillsMenu.DropDownItems)
            {
                stills.Add(entry.Text);int id=StillIdOfLabel(entry.Text);
                if(IsListedId(RetiredStillIds,id))failures.Add("Retired pose "+id+" is in Latest artwork stills: "+entry.Text);
                if(IsListedId(ParkedStillIds,id)||latestArt.IsParked(id))failures.Add("Parked pose "+id+" is in Latest artwork stills: "+entry.Text);
            }
            if(stillsMenu!=null && stills.Count!=latestArt.Poses.Length)failures.Add("Latest artwork stills lists "+stills.Count+" items for "+latestArt.Poses.Length+" packed stills.");
            int expectedStills=13-latestArt.ParkedStateIds.Length;
            if(stills.Count!=expectedStills)failures.Add("Expected "+expectedStills+" active stills in the menu (13 less "+latestArt.ParkedStateIds.Length+" parked for this version), found "+stills.Count+".");
            foreach(KeyValuePair<string,ToolStripMenuItem> held in devStateMenus)stateItems.Add(held.Value.Text);
            Dictionary<string,object> transitions=new Dictionary<string,object>(),actions=new Dictionary<string,object>();
            foreach(string state in DevStates)
            {
                SelectDevState(state);
                PopulateDevTransitions();PopulateDevActions();
                List<string> fromTransitions=new List<string>(),fromActions=new List<string>();
                ListMenuTexts(devTransitions.DropDownItems,"",fromTransitions);ListMenuTexts(devActions.DropDownItems,"",fromActions);
                transitions.Add(state,fromTransitions);actions.Add(state,fromActions);every.AddRange(fromTransitions);every.AddRange(fromActions);
            }
            SelectDevState("sit");
            foreach(string text in every)
            {
                string lower=text.ToLowerInvariant();
                foreach(string word in RetiredMenuWords)if(lower.Contains(word))failures.Add("Menu item names a retired or parked pose ('"+word+"'): "+text);
            }
            return new{passed=failures.Count==0,failures=failures,activeStillCount=stills.Count,retiredStillIds=RetiredStillIds,parkedStillIds=ParkedStillIds,parkedForThisVersion=latestArt.ParkedStateIds,
                forbiddenWords=RetiredMenuWords,topLevel=topLevel,latestArtworkStills=stills,heldStateItems=stateItems,transitionsByState=transitions,actionsByState=actions,menuItemsChecked=every.Count};
        }
    }
}
#endif
