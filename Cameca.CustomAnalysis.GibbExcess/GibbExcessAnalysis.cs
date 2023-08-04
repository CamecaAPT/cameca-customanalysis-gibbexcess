using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Cameca.CustomAnalysis.Utilities.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using static Cameca.CustomAnalysis.GibbExcess.MachineModelDetails;
using System.Linq;

namespace Cameca.CustomAnalysis.GibbExcess;

[NodeType(NodeType.Analysis)]
internal class GibbExcessAnalysis : IAnalysis<GibbExcessProperties>
{
    private const int ROUNDING_LENGTH = 3;

    //private bool isInputValid = false;
    public bool WasJustOpened { get; set; }

    INodeDataProvider _nodeDataProvider;

    public GibbExcessAnalysis(INodeDataProvider nodeDataProvider)
    {
        _nodeDataProvider = nodeDataProvider;
    }


    public IEnumerable<ReadOnlyMemory<ulong>>? Filter(
        IIonData ionData,
        GibbExcessProperties properties,
        IResources resources,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        // Does not need to be implemented unless changing to a DataFilter type analysis.
        // To return filtered ions from this analysis, change the NodeTypeAttribute to [NodeType(NodeType.DataFilter)]
        // and return ion sequence numbers from this method to filter
        // Recommended pattern is using 'yield return' while iterating through IIonData
        throw new NotImplementedException();
    }

    public async Task<object?> Update(
        IIonData ionData,
        GibbExcessProperties properties,
        IResources resources,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        //Vector3 bounds = resources.IonDataOwnerNode.Region?.GetDimensions() ?? ionData.Extents.GetDimensions();

        var ionTypesDict = GetIonTypes(ionData);
        properties.IonNames = ionTypesDict.Keys.ToList();

        (var isValid, var rawLines, var compositionType) = await ValidateInput(ionData, properties, resources);

        if (isValid)
        {
            if (rawLines == null)
                throw new Exception("Error: data valid but rawlines null");

            GibbsCalculation(rawLines, properties, resources, (CompositionType)compositionType!, ionTypesDict);

            resources.DataState.IsValid = true;
        }

        // Return object to be placed as Content in the user interface.
        // As a special case return the IResources.ViewBuilder to use that builder to create a basic UI
        return resources.ViewBuilder;
    }

    static Dictionary<string, byte> GetIonTypes(IIonData ionData)
    {
        Dictionary<string, byte> ionTypes = new();

        byte count = 1;
        foreach(var ionType in ionData.Ions)
            ionTypes.Add(ionType.Name, count++);

        return ionTypes;
    }

    void GibbsCalculation(List<string[]> rawLines, GibbExcessProperties properties, IResources resources, CompositionType compositionType, Dictionary<string, byte> ionTypesDict)
    {
        var ionTypeIndex = ionTypesDict[properties.IonOfInterest];
        var selectionStart = properties.SelectionStart;
        var selectionEnd = properties.SelectionEnd;
        var viewBuilder = resources.ViewBuilder;
        double detectorEfficiency;
        if (properties.DetectorEfficiency == null)
            detectorEfficiency = MachineTypeEfficiency[properties.MachineModel] / 100;
        else
            detectorEfficiency = (double)properties.DetectorEfficiency / 100;


        double deltaDistance = double.Parse(rawLines[1][0]) - double.Parse(rawLines[0][0]);
        int numIn = (int)((selectionEnd - selectionStart) / deltaDistance) + 1;

        int[] inCounts = new int[numIn];
        int[] outCounts = new int[rawLines.Count - numIn];

        int inIndex = 0;
        int outIndex = 0;
        int totalMatrixCount = 0;
        foreach (string[] line in rawLines)
        {
            double rowThisDistance = double.Parse(line[0]);
            int rowTotalIonCount = int.Parse(line[1]);
            int rowThisIonCount = (int)Math.Round((rowTotalIonCount * double.Parse(line[1 + ionTypeIndex]) * .01));
            if (rowThisDistance >= selectionStart && rowThisDistance <= selectionEnd)
            {
                inCounts[inIndex++] = rowThisIonCount;
            }
            else
            {
                outCounts[outIndex++] = rowThisIonCount;
                totalMatrixCount += rowThisIonCount;
            }
        }

        double averageMatrix;
        if (outCounts.Length == 0)
            averageMatrix = 0.0;
        else
            averageMatrix = (double)totalMatrixCount / outCounts.Length;

        double peakIons = 0;

        //TODO: this may be a bit inefficient now
        foreach (int inCount in inCounts)
            peakIons += (inCount - averageMatrix);
        foreach (int outCount in outCounts)
            peakIons += (outCount - averageMatrix);

        double theoreticalIons = peakIons / detectorEfficiency;

        //if (!TryCrossSectionCalculation(out var crossSectionArea, ionData, Coordinate.Z))
        //    return;
        var crossSectionArea = CrossSectionCalculation(resources, Coordinate.Z, compositionType);

        double gibbsExcess = theoreticalIons / crossSectionArea!;

        string averageMatrixStr = averageMatrix.ToString($"f{ROUNDING_LENGTH}");
        string peakIonsStr = peakIons.ToString($"f{ROUNDING_LENGTH}");
        string theoreticalIonsStr = theoreticalIons.ToString($"f{ROUNDING_LENGTH}");
        string gibbsExcessStr = gibbsExcess.ToString($"f{ROUNDING_LENGTH}");

        viewBuilder.AddTable("Gibbsian Excess and Intermediates", new Object[] { new GibbsRow(averageMatrixStr, peakIonsStr, theoreticalIonsStr, gibbsExcessStr) });
    }

    enum Coordinate { X, Y, Z };

    double CrossSectionCalculation(IResources resources, Coordinate coord, CompositionType compositionType)
    {
        if (compositionType == CompositionType.Comp1D)
        {
            var ROIregion = resources.IonDataOwnerNode.Region!;
            var scale = ROIregion.GetDimensions();

            if (ROIregion.Shape == Shape.Cube)
            {
                if (coord == Coordinate.X)
                    return scale.Y * scale.Z;
                else if (coord == Coordinate.Y)
                    return scale.X * scale.Z;
                else //Z direction
                    return scale.X * scale.Y;
            }
            else if (ROIregion.Shape == Shape.Cylinder)
                return Math.PI * (scale.X / 2) * (scale.Y / 2);
            else
                throw new Exception("should only have cube or cylinder");
        }
        else if (compositionType == CompositionType.Proxigram)
        {
            var dataOwnerNode = resources.IonDataOwnerNode;
            var nodeData = _nodeDataProvider.Resolve(dataOwnerNode.Id);
            if (nodeData == null)
                throw new Exception("No data on IonDataOwnerNode");
            if (nodeData.GetData(typeof(IInterfaceData)).Result is not IInterfaceData interfaceData)
                throw new Exception("No data on IonDataOwnerNode");

            var metrics = interfaceData.InterfaceMetrics;

            double area = 0;
            foreach (var metric in metrics)
                area += metric.SurfaceArea;
            area /= metrics.Count();

            return area;
        }
        else
            throw new Exception("Should only be Comp1D or Proxigram");
    }

    static List<string[]> ReadFile(string filePath)
    {
        List<string[]> csvLines = new();

        using (var reader = new StreamReader(filePath))
        {
            //get rid of header information
            reader.ReadLine();
            reader.ReadLine();


            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line!.Split(",");
                if (values.Length <= 1)
                    continue;
                int thisBoxIonCount = int.Parse(values[1]);
                if (thisBoxIonCount == 0)
                    continue;

                csvLines.Add(values);
            }
        }

        return csvLines;
    }

    static async Task<string> Get1DCompFile(IResources resources)
    {
        //assuming already found that there is a 1D Comp
        var ownerNode = resources.IonDataOwnerNode;
        INodeResource? concentrationNode = null;
        foreach (var sibling in ownerNode.Children)
        {
            if (sibling.TypeId == "ConcentrationProfile1D" || sibling.TypeId == "ProxigramNode")
            {
                concentrationNode = sibling;
                break;
            }
        }

        if (concentrationNode == null)
        {
            throw new Exception("should've found a concentration node");
        }

        string tempPath = Path.GetTempFileName();

        await concentrationNode.ExportToCsv!.ExportToCsv(tempPath);

        return tempPath;
    }

    /*
     * Data Validation
     */
    static async Task<(bool, List<string[]>?, CompositionType?)> ValidateInput(IIonData ionData, GibbExcessProperties properties, IResources resources)
    {
        StringBuilder outBuilder = new();
        bool isValid = true;
        List<string[]>? rawLines = null;

        (var hasGoodSibling, var siblingType) = Has1DCompOrProxigramSibling(resources, outBuilder);

        if (!hasGoodSibling)
            isValid = false;

        if (isValid)
        {
            var tempFile = await Get1DCompFile(resources);

            rawLines = ReadFile(tempFile);

            if (!IsSelectionAreaValid(rawLines, properties, outBuilder))
                isValid = false;
        }

        if(siblingType != null && siblingType == CompositionType.Comp1D)
        {
            if (!IsChildOfROI(resources, outBuilder))
                isValid = false;
        }

        if (properties.IonOfInterest == "")
        {
            isValid = false;
            outBuilder.AppendLine("Enter a valid ion to run the gibbsian excess calculation on.");
        }

        if (!isValid)
            MessageBox.Show(outBuilder.ToString());

        return (isValid, rawLines, siblingType);
    }

    static bool IsChildOfROI(IResources resources, StringBuilder outputBuilder)
    {
        var ownerNode = resources.IonDataOwnerNode;
        if (ownerNode.Region != null)
        {
            var region = ownerNode.Region;
            if (region.Shape == Shape.Cylinder || region.Shape == Shape.Cube)
                return true;
            else
            {
                outputBuilder.AppendLine("Error. Only Cube and Cyliner ROI currently supported");
                return false;
            }
        }
        else
        {
            outputBuilder.AppendLine("Error. Gibbsian Excess must be ran on a geometric ROI");
            return false;
        } 
    }

    enum CompositionType { Comp1D, Proxigram };

    static (bool, CompositionType?) Has1DCompOrProxigramSibling(IResources resources, StringBuilder outputBuilder)
    {
        var ownerNode = resources.IonDataOwnerNode;
        foreach (var sibling in ownerNode.Children)
        {
            if (sibling.TypeId == "ConcentrationProfile1D")
                return (true, CompositionType.Comp1D);
            if (sibling.TypeId == "ProxigramNode")
                return (true, CompositionType.Proxigram);
        }
        outputBuilder.AppendLine("Error. Gibbsian Excess requires a 1D Composition or Proxigram analysis as a sibling");
        return (false, null);
    }

    static bool IsSelectionAreaValid(List<string[]> rawLines, GibbExcessProperties properties, StringBuilder outBuilder)
    {
        var selectionStart = properties.SelectionStart;
        var selectionEnd = properties.SelectionEnd;

        double actualStart = double.Parse(rawLines[0][0]);
        double actualEnd = double.Parse(rawLines[rawLines.Count - 1][0]);

        bool isValid = true;

        if (selectionStart > selectionEnd)
        {
            outBuilder.AppendLine("Selection end cannot be before the beginning");
            isValid = false;
        }

        bool startTooSmall = selectionStart < actualStart;
        bool startTooBig = selectionStart > actualEnd;
        bool endTooSmall = selectionEnd < actualStart;
        bool endTooBig = selectionEnd > actualEnd;
        if (startTooSmall || startTooBig || endTooSmall || endTooBig)
        {
            outBuilder.AppendLine($"Start and end selection must be between {actualStart} and {actualEnd} inclusive");
            isValid = false;
        }
        return isValid;
    }

}

public class GibbsRow
{
    public string AverageMatrixIons { get; }
    public string PeakIons { get; }
    public string TheoreticalIons { get; }
    public string GibbsianExcess { get; }

    public GibbsRow(string averageMatrixIons, string peakIons, string theoreticalIons, string gibbsianExcess)
    {
        AverageMatrixIons = averageMatrixIons;
        PeakIons = peakIons;
        TheoreticalIons = theoreticalIons;
        GibbsianExcess = gibbsianExcess;
    }
}