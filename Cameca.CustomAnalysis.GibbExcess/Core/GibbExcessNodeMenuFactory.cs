using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Cameca.CustomAnalysis.Utilities.Legacy;
using Prism.Events;
using Prism.Services.Dialogs;

namespace Cameca.CustomAnalysis.GibbExcess.Core;

internal class GibbExcessNodeMenuFactory : LegacyAnalysisMenuFactoryBase
{
    public GibbExcessNodeMenuFactory(IEventAggregator eventAggregator, IDialogService dialogService)
        : base(eventAggregator, dialogService)
    {
    }

    protected override INodeDisplayInfo DisplayInfo => GibbExcessNode.DisplayInfo;
    protected override string NodeUniqueId => GibbExcessNode.UniqueId;
    public override AnalysisMenuLocation Location { get; } = AnalysisMenuLocation.Analysis;
}