using System;
using Cameca.CustomAnalysis.Utilities;
using Cameca.CustomAnalysis.Utilities.Legacy;

namespace Cameca.CustomAnalysis.GibbExcess.Core;

internal class GibbExcessViewModel
    : BasicCustomAnalysisViewModel<GibbExcessNode, GibbExcessAnalysis, GibbExcessProperties>
{
    public const string UniqueId = "Cameca.CustomAnalysis.GibbExcess.GibbExcessViewModel";

    public GibbExcessViewModel(IAnalysisViewModelBaseServices services)
        : base(services)
    {
    }
}