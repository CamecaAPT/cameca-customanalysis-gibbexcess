using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Cameca.CustomAnalysis.Utilities.Legacy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;

namespace Cameca.CustomAnalysis.GibbExcess;

internal class GibbExcessAnalysis : ICustomAnalysis<GibbExcessOptions>
{
    /* Services defined by the Cameca.CustomAnalysis.Interface APS can be injected into the constructor for later use.
    private readonly IIsosurfaceAnalysis _isosurfaceAnalysis;

    public GibbExcessAnalysis(IIsosurfaceAnalysis isosurfaceAnalysis)
    {
        _isosurfaceAnalysis = isosurfaceAnalysis;
    }
    //*/

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

        var rawLines = ReadFile(options.CsvFilePath); //2 is nickel
        var gibbs = GibbsCalculation(rawLines, options.RangeOfInterest, options.SelectionStart, options.SelectionEnd, options.DetectorEfficiency, ionData);

        viewBuilder.AddText("Gibbs Value", $"{gibbs}");
    }

    static bool IsRangeValid(IIonData ionData, int rangeOfInterest, out int validRangeEnd)
    {
        validRangeEnd = ionData.GetIonTypeCounts().Count;
        return (rangeOfInterest >= 1 && rangeOfInterest <= validRangeEnd);
    }

    enum Coordinate { X, Y, Z };

    static double CrossSectionCalculation(IIonData ionData, Coordinate coord)
    {
        var diff = ionData.Extents.Max - ionData.Extents.Min;
        if (coord == Coordinate.X)
            return diff.Y * diff.Z;
        if (coord == Coordinate.Y)
            return diff.X * diff.Z;
        return diff.X * diff.Y;
    }

    static double GibbsCalculation(List<string[]> rawLines, int ionTypeIndex, float selectionStart, float selectionEnd, float detectorEfficiency, IIonData ionData)
    {
        float deltaDistance = float.Parse(rawLines[1][0]) - float.Parse(rawLines[0][0]);
        int numIn = (int)((selectionEnd - selectionStart) / deltaDistance) + 1;

        int[] inCounts = new int[numIn];
        int[] outCounts = new int[rawLines.Count - numIn];

        int inIndex = 0;
        int outIndex = 0;
        int totalMatrixCount = 0;
        foreach (string[] line in rawLines)
        {
            float rowThisDistance = float.Parse(line[0]);
            int rowTotalIonCount = int.Parse(line[1]);
            int rowThisIonCount = (int)Math.Round((rowTotalIonCount * float.Parse(line[1 + ionTypeIndex]) * .01));
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


        return theoreticalIons / CrossSectionCalculation(ionData, Coordinate.Z);
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
                var values = line.Split(",");
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