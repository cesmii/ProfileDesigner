using System;
using System.IO;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace CESMII.ProfileDesigner.Api.Shared.Utils
{

    public interface ICustomRazorViewEngine
    {
        Task<string> RazorViewToHtmlAsync<TModel>(string viewName, TModel model);
    }

    /*
    //
    // Summary:
    //     Defines methods for objects that are managed by the host.
    public interface ICustomViewRenderer
    {
        //Task<string> RenderToStringAsync(string viewName, object model);
        //Task StartAsync(CancellationToken cancellationToken);
        //Task StopAsync(CancellationToken cancellationToken);
    }
    */

    public class CustomRazorViewEngine : ICustomRazorViewEngine //: IHostedService
    {
        private readonly IRazorViewEngine _razorViewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        //private readonly IServiceProvider _serviceProvider;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public CustomRazorViewEngine(IRazorViewEngine razorViewEngine,
            ITempDataProvider tempDataProvider,
            //IServiceProvider serviceProvider,
            IServiceScopeFactory serviceScopeFactory)
        {
            _razorViewEngine = razorViewEngine;
            _tempDataProvider = tempDataProvider;
            //_serviceProvider = serviceProvider;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task<string> RazorViewToHtmlAsync<TModel>(string viewName, TModel model)
        {
            //using (var requestServices = _serviceProvider.CreateScope())
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                //var hostingEnv = scope.ServiceProvider.GetService<IWebHostEnvironment>();

                var httpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
                //var routeData = new RouteData();
                //routeData.Values.Add("controller", "Home");
                var routeData = httpContext.GetRouteData();
                var actionContext = new ActionContext(httpContext, routeData, new ActionDescriptor());

                using (var sw = new StringWriter())
                {
                    //var viewResult = _razorViewEngine.GetView(hostingEnv.WebRootPath, viewName, true);
                    var viewResult = _razorViewEngine.GetView("", viewName, true);

                    if (viewResult.View == null)
                    {
                        throw new ArgumentNullException($"{viewName} does not match any available view");
                    }

                    var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                    {
                        Model = model
                    };

                    var viewContext = new ViewContext(
                        actionContext,
                        viewResult.View,
                        viewDictionary,
                        new TempDataDictionary(actionContext.HttpContext, _tempDataProvider),
                        sw,
                        new HtmlHelperOptions()
                    );

                    await viewResult.View.RenderAsync(viewContext);
                    return sw.ToString();
                }
            }
        }

        /*
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            //var html = await RenderToStringAsync("About", null);
            return;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
        }
        */
    }

    public class CustomRazorViewEngineOld: ICustomRazorViewEngine
    {
        private readonly IRazorViewEngine _razorViewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IServiceProvider _serviceProvider;
        //private readonly IHosted _serviceProvider;

        public CustomRazorViewEngineOld(
            IRazorViewEngine razorViewEngine,
            ITempDataProvider tempDataProvider,
            IServiceProvider serviceProvider
            )
        {
            _razorViewEngine = razorViewEngine;
            _tempDataProvider = tempDataProvider;
            _serviceProvider = serviceProvider;
        }

        public async Task<string> RazorViewToHtmlAsync<TModel>(string viewName, TModel model)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = _serviceProvider;
            var actionContext = new ActionContext(httpContext, httpContext.GetRouteData(), new ActionDescriptor());

            //var actionContext = GetContext();
            var view = FindView(viewName);

            using (var output = new StringWriter())
            {
                var viewContext = new ViewContext(
                    actionContext: actionContext,
                    view: view,
                    viewData: new ViewDataDictionary<TModel>(
                        metadataProvider: new EmptyModelMetadataProvider(),
                        modelState: new ModelStateDictionary()
                        )
                    {
                        Model = model
                    },
                    tempData: new TempDataDictionary(actionContext.HttpContext, _tempDataProvider),
                    writer: output,
                    htmlHelperOptions: new HtmlHelperOptions()
                    );
                await view.RenderAsync(viewContext);
                return output.ToString();
            }
        }

        private IView FindView(string ViewName)
        {
            ViewEngineResult viewResult = _razorViewEngine.GetView(executingFilePath: null, viewPath: ViewName, isMainPage: true);
            if (viewResult.Success)
            {
                return viewResult.View;
            }
            throw new Exception("Invalid View Path");
        }

        private ActionContext GetContext()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = _serviceProvider;
            //return new ActionContext(httpContext, null, new ActionDescriptor());
            return new ActionContext(httpContext, httpContext.GetRouteData(), new ActionDescriptor());
        }
    }
}