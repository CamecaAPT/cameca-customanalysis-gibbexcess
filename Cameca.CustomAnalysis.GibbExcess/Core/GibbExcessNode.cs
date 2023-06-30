using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Cameca.CustomAnalysis.Utilities.Legacy;
using System;

namespace Cameca.CustomAnalysis.GibbExcess.Core;

[DefaultView(GibbExcessViewModel.UniqueId, typeof(GibbExcessViewModel))]
[DelegatedNodeType(typeof(GibbExcessAnalysis))]
internal class GibbExcessNode : BasicCustomAnalysisNodeBase<GibbExcessAnalysis, GibbExcessProperties>
{
    public const string UniqueId = "Cameca.CustomAnalysis.GibbExcess.GibbExcessNode";
    
    public static INodeDisplayInfo DisplayInfo { get; } = new NodeDisplayInfo("Gibbsian Excess");

    public GibbExcessNode(IStandardAnalysisFilterNodeBaseServices services, GibbExcessAnalysis analysis, Func<IResources> resourceFactory)
        : base(services, analysis, resourceFactory)
    {
    }
}