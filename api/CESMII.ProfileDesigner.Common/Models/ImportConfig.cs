namespace CESMII.ProfileDesigner.Common.Models
{
    using CESMII.ProfileDesigner.Common.Enums;
    using System.Collections.Generic;

    public class ImportConfig
    {
        /// <summary>
        /// Base url for Azure functions for Profile Designer
        /// This will be different for different environments
        /// </summary>
        public string AzureFunctionBaseUrl { get; set; }
        /// <summary>
        /// Relative url to the Azure function - no leading slash 
        /// </summary>
        public string ImportCloudFunctionUrl { get; set; }
        /// <summary>
        /// Relative url to the Azure function - no leading slash 
        /// </summary>
        public string ImportFileFunctionUrl { get; set; }
        /// <summary>
        /// Toggle to control whether we use Azure WebJob, Azure function or 
        /// or direct code method (the existing approach).
        /// </summary>
        /// <remarks>This applies only to file imports. This toggle is intended to simplify the automated testing of the 
        /// import code in scenarios where an Azure function is not present.</remarks>
        public ImportModeEnum FileImportMode { get; set; }

        /// <summary>
        /// Toggle to control whether we use Azure WebJob, Azure function or 
        /// or direct code method (the existing approach).
        /// </summary>
        /// <remarks>This applies only to Cloud imports. This toggle is intended to simplify the automated testing of the 
        /// import code in scenarios where an Azure function is not present.</remarks>
        public ImportModeEnum CloudImportMode { get; set; }

        public string AzureFunctionClientKey { get; set; }
        public string AzureWebJobsStorage { get; set; }
        public string AzureWebJobsStorageQueueName { get; set; }
    }
}
