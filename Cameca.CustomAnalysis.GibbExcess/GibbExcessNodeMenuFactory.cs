using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Prism.Events;

namespace Cameca.CustomAnalysis.GibbExcess;

internal class GibbExcessNodeMenuFactory : AnalysisMenuFactoryBase
{
    public GibbExcessNodeMenuFactory(IEventAggregator eventAggregator)
        : base(eventAggregator)
    {
    }

    protected override INodeDisplayInfo DisplayInfo => GibbExcessNode.DisplayInfo;
    protected override string NodeUniqueId => GibbExcessNode.UniqueId;
    public override AnalysisMenuLocation Location { get; } = AnalysisMenuLocation.Analysis;
}