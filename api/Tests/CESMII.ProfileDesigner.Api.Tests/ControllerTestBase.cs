using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

using Xunit;
using Xunit.Abstractions;
using Newtonsoft.Json;

using MyNamespace;
using CESMII.ProfileDesigner.Common.Enums;
using System.Diagnostics;

namespace CESMII.ProfileDesigner.Api.Tests
{
    public class ControllerTestBase : IClassFixture<CustomWebApplicationFactory<CESMII.ProfileDesigner.Api.Startup>>, IDisposable
    {
        protected readonly CustomWebApplicationFactory<CESMII.ProfileDesigner.Api.Startup> _factory;
        protected readonly ITestOutputHelper output;
        private Client _apiClient;

        private const string URL_IMPORT_GETBYID = "/api/importlog/getbyid";

        private const string _filterPayload = @"{'filters':[{'items':" +
            "[{'selected':false,'visible':true,'name':'My Profiles','code':null,'lookupType':0,'typeId':null,'displayOrder':0,'isActive':false,'id':1}," +
            " {'selected':false,'visible':true,'name':'Cloud Profiles','code':null,'lookupType':0,'typeId':null,'displayOrder':0,'isActive':false,'id':2}," +
            " {'selected':false,'visible':false,'name':'Cloud Library','code':null,'lookupType':0,'typeId':null,'displayOrder':0,'isActive':false,'id':3}],'name':'Source','id':1}" +
            "]" +
            ",'sortByEnum':3,'query':null,'take':25,'skip':0}";


        public ControllerTestBase(CustomWebApplicationFactory<CESMII.ProfileDesigner.Api.Startup> factory, ITestOutputHelper output)
        {
            _factory = factory;
            this.output = output;
        }

        protected Client ApiClient
        {
            get
            {
                if (_apiClient == null)
                    _apiClient = _factory.GetApiClientAuthenticated();
                return _apiClient;
            }
        }

        protected Shared.Models.ProfileTypeDefFilterModel ProfileFilter
        {
            get
            {
                //get stock filter
                return JsonConvert.DeserializeObject<Shared.Models.ProfileTypeDefFilterModel>(_filterPayload);
            }
        }

        protected Shared.Models.CloudLibFilterModel CloudLibFilter
        {
            get
            {
                //get stock cloud lib filter
                var result = JsonConvert.DeserializeObject<Shared.Models.CloudLibFilterModel>(_filterPayload);
                result.Cursor = null;
                result.PageBackwards = false;
                return result;
            }
        }

        ///shared filter used to represent an exclusion of local profiles 
        protected List<Shared.Models.LookupGroupByModel> FilterExcludeLocalItems
        {
            get
            {
                return new List<Shared.Models.LookupGroupByModel>{
                    new Shared.Models.LookupGroupByModel() {
                        Name = "Source", ID = (int)ProfileSearchCriteriaCategoryEnum.Source,
                        Items = new List<Shared.Models.LookupItemFilterModel>() {
                            new Shared.Models.LookupItemFilterModel() { ID = (int)ProfileSearchCriteriaSourceEnum.BaseProfile, Selected = false }
                        }
                    }
                };
            }
        }

        ///shared filter used to represent an exclusion of local profiles 
        protected List<Shared.Models.LookupGroupByModel> FilterIncludeLocalItems
        {
            get
            {
                return new List<Shared.Models.LookupGroupByModel>{
                    new Shared.Models.LookupGroupByModel() {
                        Name = "Source", ID = (int)ProfileSearchCriteriaCategoryEnum.Source,
                        Items = new List<Shared.Models.LookupItemFilterModel>() {
                            new Shared.Models.LookupItemFilterModel() { ID = (int)ProfileSearchCriteriaSourceEnum.BaseProfile, Selected = false }
                        }
                    }
                };
            }
        }

        protected async Task PollImportStatus(int id)
        {
            //TODO - add loop to wait for import to complete
            DAL.Models.ImportLogModel status;
            var model = new Shared.Models.IdIntModel { ID = id };
            var sw = Stopwatch.StartNew();
            do
            {
                System.Threading.Thread.Sleep(2000);
                status = await ApiClient.ApiGetItemAsync<DAL.Models.ImportLogModel>(URL_IMPORT_GETBYID, model);
            } 
            while (sw.Elapsed < TimeSpan.FromMinutes(15) &&
                     ((int)status.Status == (int)Common.Enums.TaskStatusEnum.InProgress
                     || (int)status.Status == (int)Common.Enums.TaskStatusEnum.NotStarted));
            if ((int?)(status?.Status) != (int)Common.Enums.TaskStatusEnum.Completed)
            {
                var errorText = $"Error importing nodeset with id '{id}': {status.Messages.FirstOrDefault().Message}";
                output.WriteLine(errorText);
                Assert.True(false, errorText);
            }
        }

        public virtual void Dispose()
        {
            //do clean up here
        }
    }
}
