using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Cameca.CustomAnalysis.Utilities.Legacy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;

namespace Cameca.CustomAnalysis.GibbExcess;

internal class GibbExcessAnalysis : ICustomAnalysis<GibbExcessOptions>
{
    const int ROUNDING_LENGTH = 3;

    public Guid ID;

    private IExportToCsvProvider _exportToCsvProvider;
    private INodeInfoProvider _nodeInfoProvider;
    private IIonDataProvider _provider;

    public GibbExcessAnalysis(IExportToCsvProvider exportToCsvProvider, INodeInfoProvider nodeInfoProvider, IIonDataProvider provider)
    {
        _exportToCsvProvider = exportToCsvProvider;
        _nodeInfoProvider = nodeInfoProvider;
        _provider = provider;
    }

    /// <summary>
    /// Main custom analysis execution method.
    /// </summary>
    /// <remarks>
    /// Use <paramref name="ionData"/> as the data source for your calculation.
    /// Configurability in AP Suite can be implemented by creating editable properties in the options object. Access here with <paramref name="options"/>.
    /// Render your results with a variety of charts or tables by passing your final data to <see cref="IViewBuilder"/> methods.
    /// e.g. Create a histogram by calling <see cref="IViewBuilder.AddHistogram2D"/> on <paramref name="viewBuilder"/>
    /// </remarks>
    /// <param name="ionData">Provides access to mass, position, and other ion data.</param>
    /// <param name="options">Configurable options displayed in the property editor.</param>
    /// <param name="viewBuilder">Defines how the result will be represented in AP Suite</param>
    public void Run(IIonData ionData, GibbExcessOptions options, IViewBuilder viewBuilder)
    {
        //var nodeInfo = _nodeInfoProvider.Resolve(ID);
        //var util = _nodeInfoProvider.IterateNodeContainers(_nodeInfoProvider.GetRootNodeContainer(ID).NodeId);

        //var thing = _provider.Resolve(ID).OwnerNodeId;

        //foreach (var node in util)
        //{
        //    if(node.NodeInfo.TypeId == "ConcentrationProfile1D")
        //    {
        //        var csvExport = _exportToCsvProvider.Resolve(node.NodeId);
        //        var tempName = Path.GetTempFileName();
        //        var result = Task.Run(() => csvExport!.ExportToCsv(tempName)).GetAwaiter().GetResult();
        //        if(result.Success)
        //        {
        //            MessageBox.Show(tempName);
        //        }
        //    }

        //    if (node.NodeInfo is IGeometricRoiNodeInfo geometricNode)
        //    {
        //        var reg = geometricNode.Region;
        //        Matrix4x4.Decompose(reg.SrtTransformation, out var scale, out var _, out var _);
        //        var area = scale.X * scale.Y;
        //    }
        //}


        //for now going to have to assume that the 1d ion comp is along the Z axis

        if (!IsRangeValid(ionData, options.RangeOfInterest, out var validRangeEnd))
        {
            MessageBox.Show($"Invalid Range Specified. Enter value from 1 to {validRangeEnd} inclusive.");
            return;
        }

        if(!File.Exists(options.CsvFilePath))
        {
            MessageBox.Show($"Bad filepath for CSV Input File.");
            return;
        }

        var rawLines = ReadFile(options.CsvFilePath); 
        GibbsCalculation(rawLines, options.RangeOfInterest, options.SelectionStart, options.SelectionEnd, options.DetectorEfficiency, ionData, viewBuilder);
    }

    static bool IsRangeValid(IIonData ionData, int rangeOfInterest, out int validRangeEnd)
    {
        validRangeEnd = ionData.GetIonTypeCounts().Count;
        return (rangeOfInterest >= 1 && rangeOfInterest <= validRangeEnd);
    }

    enum Coordinate { X, Y, Z };

    bool TryCrossSectionCalculation(out double area, IIonData ionData, Coordinate coord)
    {
        //right now parent must be an ROI

        var parentNodeInfo = _nodeInfoProvider.Resolve((Guid)_nodeInfoProvider.Resolve(ID)!.Parent!); //may need to check on this
        if(parentNodeInfo is IGeometricRoiNodeInfo roiNode)
        {
            var reg = roiNode.Region;
            Matrix4x4.Decompose(reg.SrtTransformation, out var scale, out var _, out var _);

            //Cube
            if (reg.Shape == Shape.Cube)
            {
                if(coord == Coordinate.X)
                {
                    area = scale.Y * scale.Z;
                    return true;
                }
                else if(coord == Coordinate.Y)
                {
                    area = scale.X * scale.Z;
                    return true;
                }
                else //Z direction
                {
                    area = scale.X * scale.Y;
                    return true;
                }
            }
            //Cylinder
            else if(reg.Shape == Shape.Cylinder)
            {
                //doesn't make sense to take the area of any other direction BUT z. Throw error / display message if ask for x or y?
                area = Math.PI * (scale.X / 2) * (scale.Y / 2);
                return true;
            }
            else
            {
                MessageBox.Show("Error. Only Cube and Cyliner ROI currently supported");
                area = 0;
                return false;
            }
        }
        else
        {
            MessageBox.Show("Error. Gibbsian Excess must be ran on a geometric ROI");
            area = 0;
            return false;
        }
    }

    void GibbsCalculation(List<string[]> rawLines, int ionTypeIndex, double selectionStart, double selectionEnd, double detectorEfficiency, IIonData ionData, IViewBuilder viewBuilder)
    {
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

        double averageMatrix = (double)totalMatrixCount / outCounts.Length;

        double peakIons = 0;

        foreach(int inCount in inCounts)
            peakIons += (inCount - averageMatrix);
        foreach (int outCount in outCounts)
            peakIons += (outCount - averageMatrix);

        double theoreticalIons = peakIons / detectorEfficiency;

        if (!TryCrossSectionCalculation(out var crossSectionArea, ionData, Coordinate.Z))
            return;

        double gibbsExcess = theoreticalIons / crossSectionArea!;

        string averageMatrixStr = averageMatrix.ToString($"f{ROUNDING_LENGTH}");
        string peakIonsStr = peakIons.ToString($"f{ROUNDING_LENGTH}");
        string theoreticalIonsStr = theoreticalIons.ToString($"f{ROUNDING_LENGTH}");
        string gibbsExcessStr = gibbsExcess.ToString($"f{ROUNDING_LENGTH}");

        viewBuilder.AddTable("Gibbsian Excess and Intermediates", new Object[] { new GibbsRow(averageMatrixStr, peakIonsStr, theoreticalIonsStr, gibbsExcessStr)});
    }

    static List<string[]> ReadFile(string filePath)
    {
        List<string[]> csvLines = new();

        using (var reader = new StreamReader(filePath))
        {
            //get rid of header information
            reader.ReadLine();
            reader.ReadLine();

            
            while(!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line!.Split(",");
                if(values.Length <= 1)
                    continue;
                int thisBoxIonCount = int.Parse(values[1]);
                if (thisBoxIonCount == 0)
                    continue;

                csvLines.Add(values);
            }
        }

        return csvLines;
    }
}

public class GibbsRow
{
    public string AverageMatrix { get; }
    public string PeakIons { get; }
    public string TheoreticalIons { get; }
    public string GibbsianExcess { get; }

    public GibbsRow(string averageMatrix, string peakIons, string theoreticalIons, string gibbsianExcess)
    {
        AverageMatrix = averageMatrix;
        PeakIons = peakIons;
        TheoreticalIons = theoreticalIons;
        GibbsianExcess = gibbsianExcess;
    }
}