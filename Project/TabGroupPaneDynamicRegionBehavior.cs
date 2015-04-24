using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project
{
    /// <summary>
    /// In this particular scenario, we want to have each newly created flaoting pane to act as it's own dynamic region.
    /// To support this scenaio, we must first create a scoped region manager and region for each newly created TabGroupPane.
    /// The TabGroupPane is created when 2 panes have been dragged into the same floating window.
    /// </summary>
    public class TabGroupPaneDynamicRegionBehavior : RegionBehavior, IHostAwareRegionBehavior
    {
        public const string BehaviorKey = "TabGroupPaneDynamicRegionBehavior";
        public static readonly DependencyProperty IsScopedProperty =
            DependencyProperty.RegisterAttached("IsScoped", typeof(bool), typeof(TabGroupPaneDynamicRegionBehavior), new PropertyMetadata(false));

        TabGroupPane _hostControl;
        XamDockManager _parentDockManager;

        public DependencyObject HostControl
        {
            get { return _hostControl; }
            set { _hostControl = value as TabGroupPane; }
        }

        public static bool GetIsScoped(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsScopedProperty);
        }

        public static void SetIsScoped(DependencyObject obj, bool value)
        {
            obj.SetValue(IsScopedProperty, value);
        }

        protected override void OnAttach()
        {
            //if we are dealing with a scoped region, then don;t hook to the xamDockManager events
            var isScoped = GetIsScoped(HostControl);
            if (!isScoped)
            {
                _parentDockManager = XamDockManager.GetDockManager(HostControl);
                _parentDockManager.PaneDragEnded += DockManager_PaneDragEnded;
            }
        }

        void DockManager_PaneDragEnded(object sender, PaneDragEndedEventArgs e)
        {
            var contentPane = e.Panes[0];
            var tgp = contentPane.Parent as TabGroupPane; //make sure we are dealing with a TabgroupPane
            if (tgp != null)
            {
                var regionName = RegionManager.GetRegionName(tgp); //if we already have a region, then we don't want to create another.
                if (regionName == null)
                {
                    var scopedRegionManager = Region.RegionManager.CreateRegionManager(); //create a new region manager
                    RegionManager.SetRegionManager(tgp, scopedRegionManager); //set the region manager to the newly created TabGroupPane
                    RegionManager.SetRegionName(tgp, RegionNames.ToolSplitPanesDockableFloating); //now make the TabGrouPane a region    
                    SetIsScoped(tgp, true); //mark this region as scoped
                }
            }
        }
    }
}
