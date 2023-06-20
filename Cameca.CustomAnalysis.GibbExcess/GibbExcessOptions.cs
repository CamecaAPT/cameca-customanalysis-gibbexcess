﻿using System.ComponentModel.DataAnnotations;
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

    private double selectionStart;
    [Display(Name = "Start of Selection (nm)")]
    public double SelectionStart
    {
        get => selectionStart;
        set => SetProperty(ref selectionStart, value);
    }

    private double selectionEnd;
    [Display(Name = "End of selection (nm)")]
    public double SelectionEnd
    {
        get => selectionEnd;
        set => SetProperty(ref selectionEnd, value);
    }

    private double detectorEfficiency;
    [Display(Name = "Detector Efficiency", Description = "Corresponds to the physical machine used to take this measurement")]
    public double DetectorEfficiency
    {
        get => detectorEfficiency;
        set => SetProperty(ref detectorEfficiency, value);
    }
}