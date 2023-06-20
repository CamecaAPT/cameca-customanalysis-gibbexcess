using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Cameca.CustomAnalysis.Utilities.Legacy;

namespace Cameca.CustomAnalysis.GibbExcess.Core;

[DefaultView(GibbExcessViewModel.UniqueId, typeof(GibbExcessViewModel))]
internal class GibbExcessNode : LegacyCustomAnalysisNodeBase<GibbExcessAnalysis, GibbExcessOptions>
{
    public const string UniqueId = "Cameca.CustomAnalysis.GibbExcess.GibbExcessNode";
    
    public static INodeDisplayInfo DisplayInfo { get; } = new NodeDisplayInfo("Gibbsian Excess");

    public GibbExcessNode(IStandardAnalysisNodeBaseServices services, GibbExcessAnalysis analysis)
        : base(services, analysis)
    {
    }

    protected override void OnCreated(NodeCreatedEventArgs eventArgs)
    {
        base.OnCreated(eventArgs);
        Analysis.ID = eventArgs.NodeId;
    }
}