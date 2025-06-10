using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace PadelMatcherNet.Services;

public class RazorComponentRenderer
{
    private readonly IServiceScopeFactory _scopeFactory;

    public RazorComponentRenderer(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<string> RenderAsync<TComponent>(Dictionary<string, object?>? parameters = null)
        where TComponent : IComponent
    {
        using var scope = _scopeFactory.CreateScope();

        var renderer = scope.ServiceProvider.GetRequiredService<HtmlRenderer>();

        var result = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var view = ParameterView.FromDictionary(parameters ?? new());
            var rootComponent = await renderer.RenderComponentAsync<TComponent>(view);
            return rootComponent.ToHtmlString();
        });

        return result;
    }
}
