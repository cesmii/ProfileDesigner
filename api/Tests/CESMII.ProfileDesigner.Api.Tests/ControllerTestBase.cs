using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;
using Newtonsoft.Json;

using MyNamespace;
using CESMII.ProfileDesigner.Common.Enums;

namespace CESMII.ProfileDesigner.Api.Tests
{
    public class ControllerTestBase : IClassFixture<CustomWebApplicationFactory<CESMII.ProfileDesigner.Api.Startup>>
    {
        protected readonly CustomWebApplicationFactory<CESMII.ProfileDesigner.Api.Startup> _factory;
        protected readonly ITestOutputHelper output;
        private Client _apiClient;

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

    }
}
