﻿/* Author:      Chris Muench, C-Labs
 * Last Update: 4/8/2022
 * License:     MIT
 * 
 * Some contributions thanks to CESMII – the Smart Manufacturing Institute, 2021
 */
using CESMII.ProfileDesigner.DAL.Models;
using System.Collections.Generic;
using System.Linq;

namespace CESMII.OpcUa.NodeSetImporter
{
    public static class UANodeSetValidator
    {
        /// <summary>
        /// Looks for a standard NodeSet in the NodeSet Lookup Table and sets the UAStandardNodeSetID
        /// </summary>
        /// <param name="importResult"></param>
        /// <param name="standardNodeSets"></param>
        /// <returns></returns>
        public static UANodeSetImportResult VerifyNodeSetStandard(UANodeSetImportResult importResult, List<StandardNodeSetModel> standardNodeSets)
        {
            if (importResult?.Models?.Any() != true || standardNodeSets?.Any() != true)
            {
                if (importResult != null)
                    importResult.ErrorMessage = $"Cannot validate models. Either no models in ResultSet or no Standard Models supplied.";
                return importResult;
            }
            importResult.ErrorMessage = "";
            foreach (var tMod in importResult.Models)
            {
                var t = standardNodeSets.Where(s => s.Namespace == tMod.NameVersion.ModelUri).OrderByDescending(s => s.PublishDate).FirstOrDefault();
                tMod.NameVersion.UAStandardModelID = t?.ID;
            }
            return importResult;
        }
    }
}