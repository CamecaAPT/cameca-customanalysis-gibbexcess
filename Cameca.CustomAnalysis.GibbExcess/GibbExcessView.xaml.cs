using System.Threading.Tasks;

namespace Cameca.CustomAnalysis.GibbExcess;

/// <summary>
/// Interaction logic for NewGibbsView.xaml
/// </summary>
internal partial class GibbExcessView
{
    public GibbExcessView()
    {
        InitializeComponent();

        Loaded += NewGibbsView_Loaded;
    }

    private void NewGibbsView_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        var task = new Task(async () => await ((GibbExcessViewModel)DataContext).GetIonData());

        task.RunSynchronously();

        Loaded -= NewGibbsView_Loaded;
    }
}