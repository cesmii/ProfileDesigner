namespace CESMII.ProfileDesigner.Api.Shared.Extensions
{
    using System;
    using Microsoft.AspNetCore.Mvc;

    public static class UrlExtensions
    {
        /// <summary>
        /// Gets the absolute path to the content, useful for PDF generation and the like.
        /// </summary>
        /// <param name="url">Extension of the IHelper url.</param>
        /// <param name="contentPath">The content path.</param>
        /// <returns>The absolute content path.</returns>
        public static string AbsoluteContent(this IUrlHelper url, string contentPath)
        {
            var request = url.ActionContext.HttpContext.Request;
            return new Uri(new Uri(request.Scheme + "://" + request.Host.Value), url.Content(contentPath)).ToString();
        }
    }
}