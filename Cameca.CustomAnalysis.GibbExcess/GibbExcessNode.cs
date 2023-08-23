using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using System;
using System.Collections.Generic;

namespace Cameca.CustomAnalysis.GibbExcess;

[DefaultView(GibbExcessViewModel.UniqueId, typeof(GibbExcessViewModel))]
internal class GibbExcessNode : StandardAnalysisNodeBase
{
    public const string UniqueId = "Cameca.CustomAnalysis.GibbExcess.GibbExcessNode";
    
    public static INodeDisplayInfo DisplayInfo { get; } = new NodeDisplayInfo("Gibbsian Excess");

    private readonly ResourceFactory _resourceFactory;


    public IRenderDataFactory RenderDataFactory { get; private set; }
    public INodeDataProvider NodeDataProvider { get; private set; }

    public IResources? Resources { get; private set; }

    public Guid? ID { get; private set; } = null;

    public List<string> ValidIonNames { get; } = new();

    public GibbExcessNode(IStandardAnalysisNodeBaseServices services,
        IRenderDataFactory renderDataFactory,
        INodeDataProvider nodeDataProvider,
        ResourceFactory resourceFactory)
        : base(services)
    {
        RenderDataFactory = renderDataFactory;
        NodeDataProvider = nodeDataProvider;
        _resourceFactory = resourceFactory;
    }

    protected override void OnAdded(NodeAddedEventArgs eventArgs)
    {
        base.OnAdded(eventArgs);

        ID = eventArgs.NodeId;

        Resources = _resourceFactory.CreateResource((Guid)ID);

        var massSpectrumRangeManager = Resources.RangeManager;
        if(massSpectrumRangeManager != null)
        {
            var ranges = massSpectrumRangeManager.GetRanges();
            foreach(var range in ranges)
            {
                if (range.Value.Name != null)
                    ValidIonNames.Add(range.Value.Name);
            }
        }
    }
}