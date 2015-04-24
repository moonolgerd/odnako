using System;
using System.Linq;

namespace Project
{
    [Export(typeof(TabGroupPaneRegionAdapter))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class TabGroupPaneRegionAdapter : RegionAdapterBase<TabGroupPane>
    {
        /// <summary>
        /// Used to determine what views were injected and ContentPanes were generated for
        /// </summary>
        private static readonly DependencyProperty IsGeneratedProperty = DependencyProperty.RegisterAttached("IsGenerated", typeof(bool), typeof(TabGroupPaneRegionAdapter), null);

        /// <summary>
        /// Used to track the region that a ContentPane belongs to so that we can access the region from within the ContentPane.Closed event handler
        /// </summary>
        private static readonly DependencyProperty RegionProperty = DependencyProperty.RegisterAttached("Region", typeof(IRegion), typeof(TabGroupPaneRegionAdapter), null);

        [ImportingConstructor]
        public TabGroupPaneRegionAdapter(IRegionBehaviorFactory regionBehaviorFactory)
            : base(regionBehaviorFactory)
        { }

        protected override void Adapt(IRegion region, TabGroupPane regionTarget)
        {
            if (regionTarget.ItemsSource != null)
                throw new InvalidOperationException(
                    "ItemsSource property is not empty. This control is being associated with a region, but the control is already bound to something else. If you did not explicitly set the control's ItemSource property, this exception may be caused by a change in the value of the inherited RegionManager attached property.");

            SynchronizeItems(region, regionTarget);

            region.Views.CollectionChanged += (sender, e) => OnViewsCollectionChanged(e, region, regionTarget);
        }

        protected override void AttachBehaviors(IRegion region, TabGroupPane regionTarget)
        {
            base.AttachBehaviors(region, regionTarget);

            if (!region.Behaviors.ContainsKey(TabGroupPaneRegionActiveAwareBehavior.BehaviorKey))
                region.Behaviors.Add(TabGroupPaneRegionActiveAwareBehavior.BehaviorKey, new TabGroupPaneRegionActiveAwareBehavior { HostControl = regionTarget });
            if (!region.Behaviors.ContainsKey(TabGroupPaneDynamicRegionBehavior.BehaviorKey))
                region.Behaviors.Add(TabGroupPaneDynamicRegionBehavior.BehaviorKey, new TabGroupPaneDynamicRegionBehavior { HostControl = regionTarget });
        }

        protected override IRegion CreateRegion()
        {
            return new SingleActiveRegion();
        }

        private void OnViewsCollectionChanged(NotifyCollectionChangedEventArgs e, IRegion region, ItemsControl regionTarget)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                //we want to add them behind any previous views that may have been manually declare in XAML or injected
                var startIndex = e.NewStartingIndex;
                foreach (var newItem in e.NewItems)
                {
                    var contentPane = PrepareContainerForItem(newItem, region);

                    if (regionTarget.Items.Count != startIndex)
                        startIndex = 0;

                    //we must make sure we bring the TabGroupPane into view.  If we don't a System.StackOverflowException will occur in 
                    //UIAutomationProvider.dll if trying to add a ContentPane to a TabGroupPane that is not in view. 
                    //This is most common when using nested TabGroupPane regions. If you don't need this, you can comment it out.
                    regionTarget.BringIntoView();

                    regionTarget.Items.Insert(startIndex, contentPane);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                var manager = XamDockManager.GetDockManager(regionTarget);
                if (manager == null) return;

                var contentPanes = manager.GetPanes(PaneNavigationOrder.VisibleOrder);
                contentPanes.Where(contentPane => e.OldItems.Contains(contentPane) || e.OldItems.Contains(contentPane.Content)).ToList()
                    .ForEach(contentPane => contentPane.ExecuteCommand(ContentPaneCommands.Close));
            }
        }

        /// <summary>
        /// Takes all the views that were declared in XAML manually and merges them with the region.
        /// </summary>
        private void SynchronizeItems(IRegion region, ItemsControl regionTarget)
        {
            if (regionTarget.Items.Count > 0)
            {
                foreach (var item in regionTarget.Items)
                {
                    PrepareContainerForItem(item, region);
                    region.Add(item);
                }
            }
        }

        /// <summary>
        /// Prepares a view being injected as a ContentPane
        /// </summary>
        /// <param name="item">the view</param>
        /// <param name="region"></param>
        /// <returns>The injected view as a ContentPane</returns>
        protected virtual ContentPane PrepareContainerForItem(object item, IRegion region)
        {
            var container = item as ContentPane;

            if (container == null)
            {
                container = new ContentPane
                {
                    Name = "Z" + Guid.NewGuid().ToString("N"),
                    Content = item,
                    DataContext = ResolveDataContext(item)
                };
                container.SetValue(IsGeneratedProperty, true); //we generated this one
                container.CreateDockAwareBindings();
                container.CreateRenameMenuItem();
            }

            container.SetValue(RegionProperty, region); //let's keep track of which region the container belongs to

            container.CloseAction = PaneCloseAction.RemovePane;
            container.Closed += Container_Closed;

            return container;
        }

        /// <summary>
        /// Executes when a ContentPane is closed.
        /// </summary>
        /// <remarks>Responsible for removing the ContentPane from the region, any event handlers, and clears the content as well as any bindings from the ContentPane to prevent memory leaks.</remarks>
        private void Container_Closed(object sender, PaneClosedEventArgs e)
        {
            var contentPane = sender as ContentPane;
            if (contentPane != null)
            {
                contentPane.Closed -= Container_Closed; //no memory leaks

                var region = contentPane.GetValue(RegionProperty) as IRegion;
                //get the region associated with the ContentPane so that we can remove it.
                if (region != null)
                {
                    if (region.Views.Contains(contentPane)) //we are dealing with a ContentPane directly
                        region.Remove(contentPane);

                    var item = contentPane.Content;
                    //this view was injected and set as the content of our ContentPane
                    if (item != null && region.Views.Contains(item))
                        region.Remove(item);
                }

                ClearContainerForItem(contentPane); //reduce memory leaks
            }
        }

        /// <summary>
        /// Sets the Content property of a generated ContentPane to null.
        /// </summary>
        /// <param name="contentPane">The ContentPane</param>
        protected virtual void ClearContainerForItem(ContentPane contentPane)
        {
            if ((bool)contentPane.GetValue(IsGeneratedProperty))
            {
                contentPane.ClearValue(HeaderedContentControl.HeaderProperty); //remove any bindings

                var frameworkElement = contentPane.Content as FrameworkElement;
                if (frameworkElement != null)
                {
                    var disposable = frameworkElement.DataContext as IDisposable;
                    if (disposable != null) disposable.Dispose();
                }
                var view = frameworkElement as IDisposable;
                if (view != null)
                    view.Dispose();

                contentPane.SaveInLayout = false;
            }
        }

        /// <summary>
        /// Finds the DataContext of the view.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private static object ResolveDataContext(object item)
        {
            var frameworkElement = item as FrameworkElement;
            return frameworkElement == null ? item : frameworkElement.DataContext;
        }
    }
}
