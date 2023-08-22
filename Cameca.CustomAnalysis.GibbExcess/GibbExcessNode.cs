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

    private readonly IIonDataProvider _ionDataProvider;
    private readonly IMassSpectrumRangeManagerProvider _massSpectrumRangeManagerProvider;
    private readonly ResourceFactory _resourceFactory;

    public IIonDataResolver? IonDataResolver { get; private set; }

    public IRenderDataFactory RenderDataFactory { get; private set; }
    public INodeDataProvider NodeDataProvider { get; private set; }
    public INodeInfoProvider NodeInfoProvider { get; private set; }

    public IResources? Resources { get; private set; }

    public Guid? ID { get; private set; } = null;

    public List<string> ValidIonNames { get; } = new();

    public GibbExcessNode(IStandardAnalysisNodeBaseServices services,
        IIonDataProvider ionDataProvider,
        IMassSpectrumRangeManagerProvider massSpectrumRangeManagerProvider,
        IRenderDataFactory renderDataFactory,
        INodeDataProvider nodeDataProvider,
        INodeInfoProvider nodeInfoProvider,
        ResourceFactory resourceFactory)
        : base(services)
    {
        _ionDataProvider = ionDataProvider;
        _massSpectrumRangeManagerProvider = massSpectrumRangeManagerProvider;
        RenderDataFactory = renderDataFactory;
        NodeDataProvider = nodeDataProvider;
        NodeInfoProvider = nodeInfoProvider;
        _resourceFactory = resourceFactory;
    }

    protected override void OnAdded(NodeAddedEventArgs eventArgs)
    {
        base.OnAdded(eventArgs);

        ID = eventArgs.NodeId;

        Resources = _resourceFactory.CreateResource((Guid)ID);

        IonDataResolver = _ionDataProvider.Resolve(eventArgs.NodeId);

        var massSpectrumRangeManager = _massSpectrumRangeManagerProvider.Resolve(eventArgs.NodeId);
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