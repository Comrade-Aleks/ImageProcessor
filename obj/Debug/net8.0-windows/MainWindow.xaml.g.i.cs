// <OpenSilver><XamlHash>8EB4FCA814135BF2C4FF9D3727CF196C</XamlHash><CompilationDate>01/27/2025 13:57:15</CompilationDate></OpenSilver>



//------------------------------------------------------------------------------
// <auto-generated>
//     This code was auto-generated by "C#/XAML for HTML5"
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

/// <summary>
/// ǀǀImageprocessorǀǀComponentǀǀMainwindowǀǀXamlǀǀFactory
/// </summary>
[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
#pragma warning disable CS0618
public sealed class ǀǀImageprocessorǀǀComponentǀǀMainwindowǀǀXamlǀǀFactory : global::OpenSilver.Internal.Xaml.IXamlComponentFactory<global::ImageProcessor.MainWindow>, global::OpenSilver.Internal.Xaml.IXamlComponentLoader<global::System.Windows.Window>
#pragma warning restore CS0618
{
    /// <summary>
    /// Instantiate
    /// </summary>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    public static object Instantiate()
    {
        return CreateComponentImpl();
    }

    global::ImageProcessor.MainWindow global::OpenSilver.Internal.Xaml.IXamlComponentFactory<global::ImageProcessor.MainWindow>.CreateComponent()
    {
        return CreateComponentImpl();
    }

    object global::OpenSilver.Internal.Xaml.IXamlComponentFactory.CreateComponent()
    {
        return CreateComponentImpl();
    }

    void global::OpenSilver.Internal.Xaml.IXamlComponentLoader<global::System.Windows.Window>.LoadComponent(global::System.Windows.Window component)
    {
        LoadComponentImpl(component);
    }

    void global::OpenSilver.Internal.Xaml.IXamlComponentLoader.LoadComponent(object component)
    {
        LoadComponentImpl((global::System.Windows.Window)component);
    }

    private static void LoadComponentImpl(global::System.Windows.Window window_12aa9b641a52437e8ac57a4157847824)
    {
        if ((object)window_12aa9b641a52437e8ac57a4157847824 is global::System.Windows.UIElement)
        {
            ((global::System.Windows.UIElement)(object)window_12aa9b641a52437e8ac57a4157847824).XamlSourcePath = @"ImageProcessor\MainWindow.xaml";
        }

        throw new global::System.NotImplementedException();
    }

    private static global::ImageProcessor.MainWindow CreateComponentImpl()
    {
        throw new global::System.NotImplementedException();
    }

    
}


namespace ImageProcessor
{

public partial class MainWindow : global::System.Windows.Window, global::OpenSilver.Internal.Xaml.IComponentConnector
{

#pragma warning disable 169, 649, 0628 // Prevents warning CS0169 ('field ... is never used'), CS0649 ('field ... is never assigned to, and will always have its default value null'), and CS0628 ('member : new protected member declared in sealed class')

#pragma warning restore 169, 649, 0628




        private bool _contentLoaded;

        /// <summary>
        /// InitializeComponent
        /// </summary>
        public void InitializeComponent()
        {
            if (_contentLoaded) 
            {
                return;
            }
            _contentLoaded = true;
            global::System.Windows.Application.LoadComponent(this, new global::ǀǀImageprocessorǀǀComponentǀǀMainwindowǀǀXamlǀǀFactory());
            
        }


        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
        void global::OpenSilver.Internal.Xaml.IComponentConnector.Connect(int componentId, object target)
        {
        }

}

}
