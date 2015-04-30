using System.Linq;

namespace Project
{
    [Export(typeof(XamRibbonRegionAdapter))]
    public class XamRibbonRegionAdapter : RegionAdapterBase<XamRibbon>
    {
        private XamRibbon _xamRibbonRegionTarget;

        [ImportingConstructor]
        public XamRibbonRegionAdapter(IRegionBehaviorFactory regionBehaviorFactory)
            : base(regionBehaviorFactory)
        { }

        #region Overrides

        protected override void Adapt(IRegion region, XamRibbon regionTarget)
        {
            _xamRibbonRegionTarget = regionTarget;

            if (_xamRibbonRegionTarget.Tabs.Count > 0)
            {
                foreach (var tab in _xamRibbonRegionTarget.Tabs)
                {
                    region.Add(tab);
                }

                _xamRibbonRegionTarget.Tabs.Clear();
            }

            if (_xamRibbonRegionTarget.ContextualTabGroups.Count > 0)
            {
                foreach (var tabGroup in _xamRibbonRegionTarget.ContextualTabGroups)
                {
                    region.Add(tabGroup);
                }

                _xamRibbonRegionTarget.ContextualTabGroups.Clear();
            }

            foreach (var view in region.Views)
            {
                AddViewToRegion(view, _xamRibbonRegionTarget);
            }

            region.ActiveViews.CollectionChanged += (s, args) =>
            {
                switch (args.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        {
                            foreach (var view in args.NewItems)
                            {
                                AddViewToRegion(view, _xamRibbonRegionTarget);
                            }
                            break;
                        }
                    case NotifyCollectionChangedAction.Remove:
                        {
                            foreach (var view in args.OldItems)
                            {
                                RemoveViewFromRegion(view, _xamRibbonRegionTarget);
                            }
                            break;
                        }
                    default:
                        {
                            // Do nothing.
                            break;
                        }
                }
            };
        }

        protected override IRegion CreateRegion()
        {
            return new AllActiveRegion();
        }

        #endregion Overrides

        #region Private

        private static void AddViewToRegion(object view, XamRibbon xamRibbon)
        {
            var ribbonTabItem = view as RibbonTabItem;
            if (ribbonTabItem != null)
            {
                xamRibbon.Tabs.Add(ribbonTabItem);
            }
            else
            {
                var contextualTabGroup = view as ContextualTabGroup;
                if (contextualTabGroup != null)
                {
                    xamRibbon.ContextualTabGroups.Add(contextualTabGroup);
                }
            }
        }

        private static void RemoveViewFromRegion(object view, XamRibbon xamRibbon)
        {
            var ribbonTabItem = view as RibbonTabItem;
            if (ribbonTabItem != null)
            {
                xamRibbon.Tabs.Remove(ribbonTabItem);
            }
            else
            {
                var contextualTabGroup = view as ContextualTabGroup;
                if (contextualTabGroup != null)
                {
                    xamRibbon.ContextualTabGroups.Remove(contextualTabGroup);
                }
            }
        }

        #endregion Private
    }
}
