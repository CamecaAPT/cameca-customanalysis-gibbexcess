using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Cameca.CustomAnalysis.Utilities.Legacy;
using Prism.Ioc;
using Prism.Modularity;

namespace Cameca.CustomAnalysis.GibbExcess.Core;

/// <summary>
/// Public <see cref="IModule"/> implementation is the entry point for AP Suite to discover and configure the custom analysis
/// </summary>
public class GibbExcessModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        containerRegistry.AddCustomAnalysisUtilities(options => options.UseLegacy = true);
#pragma warning restore CS0618 // Type or member is obsolete

        containerRegistry.Register<GibbExcessAnalysis>();
        containerRegistry.Register<object, GibbExcessNode>(GibbExcessNode.UniqueId);
        containerRegistry.RegisterInstance(GibbExcessNode.DisplayInfo, GibbExcessNode.UniqueId);
        containerRegistry.Register<IAnalysisMenuFactory, GibbExcessNodeMenuFactory>(nameof(GibbExcessNodeMenuFactory));
        containerRegistry.Register<object, GibbExcessViewModel>(GibbExcessViewModel.UniqueId);
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        var extensionRegistry = containerProvider.Resolve<IExtensionRegistry>();
        extensionRegistry.RegisterAnalysisView<LegacyCustomAnalysisView, GibbExcessViewModel>(AnalysisViewLocation.Top);
    }
}
