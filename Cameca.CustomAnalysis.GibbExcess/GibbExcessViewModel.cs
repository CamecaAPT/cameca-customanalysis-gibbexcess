using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Cameca.CustomAnalysis.Utilities.ExtensionMethods;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static Cameca.CustomAnalysis.GibbExcess.MachineModelDetails;

namespace Cameca.CustomAnalysis.GibbExcess;

internal class GibbExcessViewModel : AnalysisViewModelBase<GibbExcessNode>
{
    public const string UniqueId = "Cameca.CustomAnalysis.GibbExcess.GibbExcessViewModel";

    private const int OUTPUT_ARR_LENGTH = 4;
    private const int DECIMAL_PLACES = 4;

    /*
     * Data
     */
    private IIonData? ionData = null;

    public ObservableCollection<IRenderData> ChartRenderData { get; set; } = new();

    private string? currentIon = null;
    private List<string[]>? currentCsvData = null;

    private Dictionary<string, byte> ionToIonIndexDict = new();

    /*
     * Commands
     */

    private readonly AsyncRelayCommand updateCommand;
    public ICommand UpdateCommand => updateCommand;

    /*
     * Options Panel Stuff
     */
    private string ionOfInterest = "";
    public string IonOfInterest
    {
        get => ionOfInterest;
        set
        {
            if (value == "")
            {
                AreSelectionTextBoxesVisible = false;
                SetProperty(ref ionOfInterest, "");
            }
            else if (Node.ValidIonNames.Contains(value))
            {
                SetProperty(ref ionOfInterest, value);
                AreSelectionTextBoxesVisible = true;
            }
            else
            {
                MessageBox.Show("Pick a valid ion in this analysis");
                AreSelectionTextBoxesVisible = false;
                SetProperty(ref ionOfInterest, "");
            }
        }
    }

    private float selectionStart;
    public float SelectionStart
    {
        get => selectionStart;
        set => SetProperty(ref selectionStart, value);
    }

    private float selectionEnd;
    public float SelectionEnd
    {
        get => selectionEnd;
        set => SetProperty(ref selectionEnd, value);
    }

    public ObservableCollection<string> MachineTypes { get; } = new()
    {
        "Leap 5000 S",
        "Invizio 6000",
        "Leap 5000 R",
        "Leap 6000 R",
        "Eikos",
        "other"
    };

    private string machineModel = "other";
    public string MachineModel
    {
        get => machineModel;
        set
        {
            SetProperty(ref machineModel, value);

            if (value == "other")
                detectorEfficiency = 0.0;
            else
                detectorEfficiency = MachineTypeEfficiency[value];

            RaisePropertyChanged(nameof(DetectorEfficiency));
        }
    }

    private double detectorEfficiency = 0;
    public double DetectorEfficiency
    {
        get => detectorEfficiency;
        set
        {
            SetProperty(ref detectorEfficiency, value);

            machineModel = "other";
            RaisePropertyChanged(nameof(MachineModel));
        }
    }

    private bool pureMatrixAverageSelected = false;
    public bool PureMatrixAverageSelected
    {
        get => pureMatrixAverageSelected;
        set => SetProperty(ref pureMatrixAverageSelected, value);
    }

    private bool bestFitLineSelected = true;
    public bool BestFitLineSelected
    {
        get => bestFitLineSelected;
        set => SetProperty(ref bestFitLineSelected, value);
    }

    private bool endpointLineSelected = false;
    public bool EndpointLineSelected
    {
        get => endpointLineSelected;
        set => SetProperty(ref endpointLineSelected, value);
    }

    /*
     * Options Helpers
     */
    private bool areSelectionTextBoxesVisible = false;
    public bool AreSelectionTextBoxesVisible
    {
        get => areSelectionTextBoxesVisible;
        set => SetProperty(ref areSelectionTextBoxesVisible, value);
    }

    /*
     * Calculation and Intermediates Outputs
     */
    private DataTable outputTable = new();
    public DataTable OutputTable
    {
        get => outputTable;
        set => SetProperty(ref outputTable, value);
    }
    

    /*
     * Constructor
     */
    public GibbExcessViewModel(IAnalysisViewModelBaseServices services)
        : base(services)
    {
        updateCommand = new(Update);
        OutputTable.Columns.Add("Average Matrix Ions (Ions / slice)");
        OutputTable.Columns.Add("Peak Ions (Ions)");
        OutputTable.Columns.Add("Theoretical Ions (Ions)");
        OutputTable.Columns.Add("Gibbsian Interfacial Excess (Ions / square nm)");
        outputTable.Rows.Add(new string[OUTPUT_ARR_LENGTH]);
    }



    private bool ParentsAndSiblingsValid(out INodeResource? siblingNodeResource, out CompositionType? compositionType)
    {
        siblingNodeResource = null;
        compositionType = null;
        if (Node.Resources == null)
            return false;

        var ionDataOwnerNode = Node.Resources.IonDataOwnerNode;

        if (ionDataOwnerNode.TypeId == "RoiNode")
        {
            
        }
        else if (ionDataOwnerNode.TypeId == "IsosurfaceNode" || ionDataOwnerNode.TypeId == "InterfaceSubgroupNode")
        {
            //isosurface stuff
        }
        else
            throw new Exception("Ion Data Owner Node should be either ROI or isosurface/grid");


        foreach(var sibling in ionDataOwnerNode.Children)
        {
            if(sibling.TypeId == "ConcentrationProfile1D")
            {
                siblingNodeResource = sibling;
                compositionType = CompositionType.Comp1D;
                break;
            }
            if(sibling.TypeId == "ProxigramNode")
            {
                siblingNodeResource = sibling;
                compositionType = CompositionType.Proxigram;
                break;
            }
        }

        if(siblingNodeResource == null)
            return false;

        return true;
    }

    public async Task Update()
    {
        //clear chart
        ChartRenderData.Clear();

        OutputTable.Rows.Clear();
        string[] output = new string[OUTPUT_ARR_LENGTH];

        if (Node == null)
        {
            OutputTable.Rows.Add(output);
            return;
        }

        if(Node.Resources == null)
        {
            OutputTable.Rows.Add(output);
            return;
        }

        if (IonOfInterest == "")
        {
            OutputTable.Rows.Add(output);
            return;
        }

        if(!ParentsAndSiblingsValid(out var siblingNodeData, out var compositionType))
        {
            MessageBox.Show("You need a 1D Concentration Profile Analysis as a sibling to this extension.");
            OutputTable.Rows.Add(output);
            return;
        }

        //if(currentIon == null || currentCsvData == null || currentIon != IonOfInterest)
        //{
        //    currentIon = IonOfInterest;
        //    string fileName = await GetFile(siblingNodeData!);
        //    currentCsvData = ReadFile(fileName);
        //}

        currentIon = IonOfInterest;
        string fileName = await GetFile(siblingNodeData!);
        currentCsvData = ReadFile(fileName);

        var index = ionToIonIndexDict[currentIon];

        List<Vector3> mainChartLine = new();

        float max = 0;

        Dictionary<float, int> distanceToIonCountDict = new();
        Dictionary<float, int> distanceToThisIonCountDict = new();
        foreach (var line in currentCsvData)
        {
            var val = float.Parse(line[index + 2]);
            if(val > max)
                max = val;
            mainChartLine.Add(new(float.Parse(line[0]), 0, val));
            distanceToIonCountDict.Add(float.Parse(line[0]), int.Parse(line[1]));
            distanceToThisIonCountDict.Add(float.Parse(line[0]), (int)Math.Round(val * int.Parse(line[1]) * .01f));
        }

        //main line
        ChartRenderData.Add(Node.RenderDataFactory.CreateLine(mainChartLine.ToArray(), Colors.Black, 3f, currentIon));

        //left line
        List<Vector3> leftLinePoints = new() { new(SelectionStart, 0, 0), new(SelectionStart, 0, max) };
        ChartRenderData.Add(Node.RenderDataFactory.CreateLine(leftLinePoints.ToArray(), Color.FromRgb(0,255,0), 3f, "Selection Lines"));

        //right line
        List<Vector3> rightLinePoints = new() { new(SelectionEnd, 0, 0), new(SelectionEnd, 0, max) };
        ChartRenderData.Add(Node.RenderDataFactory.CreateLine(rightLinePoints.ToArray(), Colors.Red, 3f, "Selection Lines"));

        //Average / BestFit Line
        //check if left and right lines are valid and not nonsense
        if(SelectionStart < SelectionEnd && SelectionStart >= mainChartLine[0].X && SelectionEnd <= mainChartLine[mainChartLine.Count-1].X)
        {
            List<Vector3> points;
            float averageMatrixLevel = 0;

            //pure matrix average
            if(PureMatrixAverageSelected)
            {
                points = GetPureMatrixAveragePoints(mainChartLine);
                averageMatrixLevel = GetPureMatrixAverage(distanceToIonCountDict, mainChartLine);
            }
            //best fit line for matrix
            else if(BestFitLineSelected)
            {
                points = GetBestFitLinePoints(mainChartLine, out var slope, out var yInt);
                averageMatrixLevel = GetBestFitMatrixAverage(distanceToIonCountDict, slope, yInt);
            }
            //endpoint line
            else
            {
                points = GetEndpointLinePoints(mainChartLine, out var slope, out var yInt);
                averageMatrixLevel = GetEndpointLineMatrixAverage(distanceToIonCountDict, slope, yInt);
            }

            ChartRenderData.Add(Node.RenderDataFactory.CreateLine(points.ToArray(), Colors.Orange, 3f, "Average Matrix Line"));


            /*
             * Update Table
             */

            //Average Matrix Level (Ions / Slice)
            output[0] = averageMatrixLevel.ToString($"f{DECIMAL_PLACES}");

            //Peak Ions (Ions)
            var peakIons = CalculatePeakIons(averageMatrixLevel, distanceToThisIonCountDict);
            output[1] = peakIons.ToString($"f{DECIMAL_PLACES}");

            //Theoretical Ions (Ions)
            var theoreticalIons = (peakIons / DetectorEfficiency) * 100;
            output[2] = theoreticalIons.ToString($"f{DECIMAL_PLACES}");

            //Gibbsian Interfacial Excess (Ions / square nm)
            var surfaceArea = await CalculateSurfaceArea((CompositionType)compositionType!, Node.Resources);
            var gibbExcess = theoreticalIons / surfaceArea;
            output[3] = gibbExcess.ToString($"f{DECIMAL_PLACES}");
        }

        OutputTable.Rows.Add(output);
    }

    public async Task<double> CalculateSurfaceArea(CompositionType compositionType, IResources resources)
    {
        if (compositionType == CompositionType.Comp1D)
        {
            var ROIregion = resources.IonDataOwnerNode.Region!;
            var scale = ROIregion.GetDimensions();

            if (ROIregion.Shape == Shape.Cube)
                return scale.X * scale.Y;
            else if (ROIregion.Shape == Shape.Cylinder)
                return Math.PI * (scale.X / 2) * (scale.Y / 2);
            else
                throw new Exception("should only have cube or cylinder");
        }
        else if (compositionType == CompositionType.Proxigram)
        {
            var dataOwnerNode = resources.IonDataOwnerNode;
            var nodeData = Node.NodeDataProvider.Resolve(dataOwnerNode.Id);
            if (nodeData == null)
                throw new Exception("No data on IonDataOwnerNode");
            if (await nodeData.GetData(typeof(IInterfaceData)) is not IInterfaceData interfaceData)
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

    public float CalculatePeakIons(float averageMatrixLevel, Dictionary<float, int> distanceToThisIonCountDict)
    {
        float peakIons = 0;
        foreach(var distanceThisIonCountPair in distanceToThisIonCountDict)
        {
            peakIons += distanceThisIonCountPair.Value - averageMatrixLevel;
        }
        return peakIons;
    }

    public Dictionary<string, byte> GetIonToIndexDict()
    {
        Dictionary<string, byte> ionToIndexDict = new();

        byte index = 0;
        foreach(var ionName in Node.ValidIonNames)
        {
            ionToIndexDict.Add(ionName, index++);
        }

        return ionToIndexDict;
    }

    public async Task GetIonData()
    {
        if (Node == null || Node.IonDataResolver == null)
            return;

        if (await Node.IonDataResolver.GetIonData() is not { } ionData)
            return;

        this.ionData = ionData;

        this.ionToIonIndexDict = GetIonToIndexDict();
    }

    List<Vector3> GetEndpointLinePoints(List<Vector3> mainChartLine, out float slope, out float yInt)
    {
        Vector3 trueStart = mainChartLine[0];
        Vector3 trueEnd = mainChartLine[^1];
        for(int i=1; i<mainChartLine.Count; i++)
        {
            var prevPoint = mainChartLine[i - 1];
            var currPoint = mainChartLine[i];
            
            if(currPoint.X >= SelectionStart && prevPoint.X < SelectionStart)
                trueStart = currPoint;

            if (currPoint.X > SelectionEnd && prevPoint.X <= SelectionEnd)
                trueEnd = prevPoint;
        }

        if (trueStart == trueEnd)
            throw new Exception("Selection Lines must differ");

        slope = (trueEnd.Z - trueStart.Z) / (trueEnd.X - trueStart.X);
        yInt = trueStart.Z - (slope * trueStart.X);

        return new() { trueStart, trueEnd };
    }

    List<Vector3> GetPureMatrixAveragePoints(List<Vector3> mainChartLine)
    {
        float matrixAverage = 0;
        int inCount = 0;
        foreach(var point in mainChartLine)
        {
            if (point.X < SelectionStart || point.X > SelectionEnd)
            {
                matrixAverage += point.Z;
                inCount++;
            }
        }

        matrixAverage /= inCount;

        return new() { new(mainChartLine[0].X, 0, matrixAverage), new(mainChartLine[^1].X, 0, matrixAverage) };
    }

    List<Vector3> GetBestFitLinePoints(List<Vector3> mainChartLine, out float slope, out float yInt)
    {
        float xAverage = 0;
        float yAverage = 0;
        int inCount = 0;
        foreach (var point in mainChartLine)
        {
            if (point.X < SelectionStart || point.X > SelectionEnd)
            {
                xAverage += point.X;
                yAverage += point.Z;
                inCount++;
            }
        }
        xAverage /= inCount;
        yAverage /= inCount;
        float xSquareSum = 0;
        float xySum = 0;
        foreach (var point in mainChartLine)
        {
            if (point.X < SelectionStart || point.X > SelectionEnd)
            {
                xSquareSum += (point.X - xAverage) * (point.X - xAverage);
                xySum += (point.X - xAverage) * (point.Z - yAverage);
            }
        }
        slope = xySum / xSquareSum;
        yInt = yAverage - (slope * xAverage);

        return new() { new(mainChartLine[0].X, 0, (mainChartLine[0].X * slope) + yInt), new(mainChartLine[^1].X, 0, (mainChartLine[^1].X * slope) + yInt) };
    }


    float GetPureMatrixAverage(Dictionary<float, int> distanceToIonCountDict, List<Vector3> mainChartLine)
    {
        float matrixAverage = 0;
        int inCount = 0;
        foreach(var point in mainChartLine)
        {
            var distance = point.X;
            var totalCount = distanceToIonCountDict[distance];
            if(distance < SelectionStart || distance > SelectionEnd)
            {
                matrixAverage += totalCount * point.Z * .01f;
                inCount++;
            }
        }
        return matrixAverage / inCount;
    }

    float GetBestFitMatrixAverage(Dictionary<float, int> distanceToIonCountDict, float slope, float yInt)
    {
        float matrixAverage = 0;
        int inCount = 0;
        foreach(var distanceIonCountPair in distanceToIonCountDict)
        {
            if(distanceIonCountPair.Key >= SelectionStart && distanceIonCountPair.Key <= SelectionEnd)
            {
                float bestFitLinePercentage = (distanceIonCountPair.Key * slope) + yInt;
                float bestFitLineIonCount = bestFitLinePercentage * distanceIonCountPair.Value * .01f;
                matrixAverage += bestFitLineIonCount;
                inCount++;
            }
        }
        return matrixAverage / inCount;
    }

    float GetEndpointLineMatrixAverage(Dictionary<float, int> distanceToIonCountDict, float slope, float yInt)
    {
        float matrixAverage = 0;
        int inCount = 0;
        foreach(var distanceIonCountPair in distanceToIonCountDict)
        {
            if(distanceIonCountPair.Key >= SelectionStart && distanceIonCountPair.Key <= SelectionEnd)
            {
                float endpointLinePercentage = (slope * distanceIonCountPair.Key) + yInt;
                float endpointLineIonCount = endpointLinePercentage * distanceIonCountPair.Value * .01f;
                matrixAverage += endpointLineIonCount;
                inCount++;
            }
        }

        return matrixAverage / inCount;
    }

    /*
     * Static Methods
     */
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

    static async Task<string> GetFile(INodeResource dataNode)
    {
        string tempPath = Path.GetTempFileName();

        if (dataNode.TypeId == "ConcentrationProfile1D")
        {
            if (dataNode.ExportToCsv == null)
                throw new Exception("export to csv should not be null");

            await dataNode.ExportToCsv.ExportToCsv(tempPath);
        }
        else if (dataNode.TypeId == "ProxigramNode")
        {
            if (dataNode.ExportToCsv == null)
                throw new Exception("export to csv should not be null");

            await dataNode.ExportToCsv.ExportToCsv(tempPath);
        }
        else
            throw new Exception("Wrong node type");

        return tempPath;
    }

    public enum CompositionType
    {
        Comp1D, Proxigram
    }
}