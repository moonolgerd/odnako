using System;
using Microsoft.Practices.ServiceLocation;
using Prism.Commands;
using Prism.Modularity;
using Prism.Logging;

namespace Project
{
    class MyBootStrapper : MefBootstrapper
    {
        #region Fields

        private readonly IObservable<EventEntry> _listener;
        private readonly string _region;
        private readonly string _environment;

        #endregion

        public MyBootStrapper(IObservable<EventEntry> listener, string region = null, string environment = null)
        {
            _listener = listener;
            _region = region;
            _environment = environment;

            GlobalCommands.ShellLoadedCommand.RegisterCommand(new DelegateCommand(() =>
            {
                var shell = (Views.Shell)Shell;
                Application.Current.MainWindow = shell;

                Application.Current.MainWindow.Show();
                Application.Current.MainWindow.Activate();
            }));
        }

        #region Overrides

        protected override void ConfigureAggregateCatalog()
        {
            base.ConfigureAggregateCatalog();

            AggregateCatalog.Catalogs.Add(new AssemblyCatalog(GetType().Assembly));
            AggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(IDockAware).Assembly));
        }

        protected override DependencyObject CreateShell()
        {
            return Container.GetExportedValue<Views.Shell>();
        }

        protected override RegionAdapterMappings ConfigureRegionAdapterMappings()
        {
            var mappings = base.ConfigureRegionAdapterMappings();
            mappings.RegisterMapping(typeof(TabGroupPane), Container.GetExportedValue<TabGroupPaneRegionAdapter>());
            mappings.RegisterMapping(typeof(XamRibbon), Container.GetExportedValue<XamRibbonRegionAdapter>());
            return mappings;
        }

        protected override IModuleCatalog CreateModuleCatalog()
        {
            return new ModuleCatalog();
        }

        protected override void ConfigureContainer()
        {
            base.ConfigureContainer();
            Container.ComposeExportedValue(Container);

            var locator = Container.GetExportedValue<IServiceLocator>();

            if (_region != null)
                Container.ComposeExportedValue("Domain", _region);
            if (_environment != null)
                Container.ComposeExportedValue("Env", _environment);

            var sink = locator.GetInstance<RedisSink>();
            var subscription = new SinkSubscription<RedisSink>(_listener.Subscribe(sink), sink);

            Container.ComposeParts(this);
        }

        protected override ILoggerFacade CreateLogger()
        {
            return new MyLogger();
        }

        #endregion
    }
}
