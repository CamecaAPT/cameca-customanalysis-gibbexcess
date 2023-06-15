using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Cameca.CustomAnalysis.Utilities.Legacy;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        var gibbs = GibbsCalculation(rawLines, options.RangeOfInterest);
    }

    static bool IsRangeValid(IIonData ionData, int rangeOfInterest, out int validRangeEnd)
    {
        validRangeEnd = ionData.GetIonTypeCounts().Count;
        return (rangeOfInterest >= 1 && rangeOfInterest <= validRangeEnd);
    }

    static float GibbsCalculation(List<string[]> rawLines, int ionTypeIndex)
    {

        foreach (string[] line in rawLines)
        {
            int rowTotalIonCount = int.Parse(line[1]);
            int rowThisIonCount = (int)(rowTotalIonCount * float.Parse(line[1 + ionTypeIndex]) * .01);

        }

        return 0f;
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