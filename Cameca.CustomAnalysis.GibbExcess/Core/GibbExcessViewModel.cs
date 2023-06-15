using System;
using Cameca.CustomAnalysis.Utilities;
using Cameca.CustomAnalysis.Utilities.Legacy;

namespace Cameca.CustomAnalysis.GibbExcess.Core;

internal class GibbExcessViewModel
    : LegacyCustomAnalysisViewModelBase<GibbExcessNode, GibbExcessAnalysis, GibbExcessOptions>
{
    public const string UniqueId = "Cameca.CustomAnalysis.GibbExcess.GibbExcessViewModel";

    public GibbExcessViewModel(IAnalysisViewModelBaseServices services, Func<IViewBuilder> viewBuilderFactory)
        : base(services, viewBuilderFactory)
    {
    }
}