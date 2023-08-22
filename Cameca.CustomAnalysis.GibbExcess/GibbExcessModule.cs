using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Prism.Ioc;
using Prism.Modularity;

namespace Cameca.CustomAnalysis.GibbExcess;

/// <summary>
/// Public <see cref="IModule"/> implementation is the entry point for AP Suite to discover and configure the custom analysis
/// </summary>
public class GibbExcessModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.AddCustomAnalysisUtilities(options => options.UseLegacy = true);

        containerRegistry.Register<object, GibbExcessNode>(GibbExcessNode.UniqueId);
        containerRegistry.RegisterInstance(GibbExcessNode.DisplayInfo, GibbExcessNode.UniqueId);
        containerRegistry.Register<IAnalysisMenuFactory, GibbExcessNodeMenuFactory>(nameof(GibbExcessNodeMenuFactory));
        containerRegistry.Register<object, GibbExcessViewModel>(GibbExcessViewModel.UniqueId);
        containerRegistry.RegisterBasicAnalysis();
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        var extensionRegistry = containerProvider.Resolve<IExtensionRegistry>();

        extensionRegistry.RegisterAnalysisView<GibbExcessView, GibbExcessViewModel>(AnalysisViewLocation.Default);
    }
}
