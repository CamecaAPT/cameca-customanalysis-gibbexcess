using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Prism.Mvvm;
using static Cameca.CustomAnalysis.GibbExcess.MachineModelDetails;

namespace Cameca.CustomAnalysis.GibbExcess;

public class GibbExcessProperties : BindableBase
{
    private int rangeOfInterest;
    [Display(Name = "Range #:", Description = "Starting from 1, corresponds to the list of ion types in the analysis tree")]
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

    private MachineType machineModel = MachineType.other;
    [Display(Name = "Machine Model")]
    public MachineType MachineModel
    {
        get => machineModel;
        set
        {
            SetProperty(ref machineModel, value);
            if (manuallySetting)
                manuallySetting = false;
            else if (value == MachineType.other)
                DetectorEfficiency = 0.0;
            else if (value != MachineType.other)
                DetectorEfficiency = MachineTypeEfficiency[value];
        }
    }

    private double? detectorEfficiency = 0;
    [Display(Name = "Detector Efficiency (%)", Description = "Corresponds to the physical machine used to take this measurement")]
    public double? DetectorEfficiency
    {
        get => detectorEfficiency;
        set
        {
            SetProperty(ref detectorEfficiency, value);
            if (value != null && value != 0.0)
            {
                manuallySetting = true;
                MachineModel = MachineType.other;
            }
        }
    }

    private bool manuallySetting = false;
}

public static class MachineModelDetails
{
    /*
     * Machine Model Efficiencies
     * 
     * 5000S:   MCP = 80    Mesh = 100  => 80
     * INVIZIO: MCP = 80    Mesh = 78   => 62.4
     * 5000R:   MCP = 80    Mesh = 65   => 52
     * 6000R:   MCP = 80    Mesh = 65   => 52
     * EIKOS:   MCP = 55    Mesh = 65   => 35.75
     */
    public enum MachineType { _5000S, INVIZIO, _5000R, _6000R, EIKOS, other }
    public static readonly Dictionary<MachineType, double> MachineTypeEfficiency = new()
    {
        { MachineType._5000S, 80 },
        { MachineType.INVIZIO, 62.4 },
        { MachineType._5000R, 52 },
        { MachineType._6000R, 52 },
        { MachineType.EIKOS, 35.75 }
    };
}