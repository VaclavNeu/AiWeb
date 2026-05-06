using Microsoft.JSInterop;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace AiWeb3
{
    public class ThemePlugin(IJSRuntime js)
    {
        [KernelFunction]
        [Description("Nastaví barvu \"pozadí\" použitím CSS")]

        public async Task SetThemeColors(string color1,string color2)
        {
            await js.InvokeVoidAsync("document.documentElement.style.setProperty", "--color1", color1);
            await js.InvokeVoidAsync("document.documentElement.style.setProperty", "--color2", color2);
        }
    }
}
