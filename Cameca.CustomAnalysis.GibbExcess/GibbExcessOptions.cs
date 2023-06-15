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
}