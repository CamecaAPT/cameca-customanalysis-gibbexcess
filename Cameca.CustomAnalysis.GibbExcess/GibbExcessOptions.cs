using System.ComponentModel.DataAnnotations;
using Prism.Mvvm;

namespace Cameca.CustomAnalysis.GibbExcess;

public class GibbExcessOptions : BindableBase
{
    private string csvInputFilePath = "";
    [Display(Name = "1D Comp. File:", Description = "Path of CSV file that has the 1D composition from an ROI in it")]
    public string CsvFilePath
    {
        get => csvInputFilePath;
        set => SetProperty(ref csvInputFilePath, value);
    }

    private int rangeOfInterest;
    [Display(Name = "Range #:", Description = "Number corresponding to the range of interest")]
    public int RangeOfInterest
    {
        get => rangeOfInterest;
        set => SetProperty(ref rangeOfInterest, value);
    }

    private float selectionStart;
    [Display(Name = "Start of Selection (nm)")]
    public float SelectionStart
    {
        get => selectionStart;
        set => SetProperty(ref selectionStart, value);
    }

    private float selectionEnd;
    [Display(Name = "End of selection (nm)")]
    public float SelectionEnd
    {
        get => selectionEnd;
        set => SetProperty(ref selectionEnd, value);
    }

    private float detectorEfficiency;
    [Display(Name = "Detector Efficiency", Description = "Corresponds to the physical machine used to take this measurement")]
    public float DetectorEfficiency
    {
        get => detectorEfficiency;
        set => SetProperty(ref detectorEfficiency, value);
    }
}