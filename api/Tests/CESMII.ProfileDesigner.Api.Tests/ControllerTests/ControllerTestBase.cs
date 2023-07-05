using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Xunit;
using Xunit.Abstractions;
using Newtonsoft.Json;

using CESMII.ProfileDesigner.Common.Enums;
using CESMII.ProfileDesigner.Data.Contexts;
using CESMII.ProfileDesigner.DAL.Models;
using CESMII.ProfileDesigner.Api.Shared.Models;
using CESMII.ProfileDesigner.Data.Entities;

namespace CESMII.ProfileDesigner.Api.Tests.Int
{
    public class ControllerTestBase : IClassFixture<CustomWebApplicationFactory<Api.Startup>>, IDisposable
    {
        protected readonly CustomWebApplicationFactory<Api.Startup> _factory;
        protected readonly ITestOutputHelper output;
        private MyNamespace.Client _apiClient;
        private MyNamespace.Client _apiClientAdmin;

        protected const string TEST_USER_OBJECTID_AAD = "1234";

        #region api endpoints
        //protected const string URL_IMPORT_GETBYID = "/api/importlog/getbyid";
        protected const string URL_USER_SEARCH = "/api/user/search";
        #endregion

        #region filter json
        private const string _filterPayloadProfile = @"{'filters':[{'items':" +
            "[{'selected':false,'visible':true,'name':'My Profiles','code':null,'lookupType':0,'typeId':null,'displayOrder':0,'isActive':false,'id':1}," +
            " {'selected':false,'visible':true,'name':'Cloud Profiles','code':null,'lookupType':0,'typeId':null,'displayOrder':0,'isActive':false,'id':2}," +
            " {'selected':false,'visible':false,'name':'Cloud Library','code':null,'lookupType':0,'typeId':null,'displayOrder':0,'isActive':false,'id':3}],'name':'Source','id':1}" +
            "]" +
            ",'sortByEnum':3,'query':null,'take':25,'skip':0}";

        private const string _filterPayloadSimple = @"{'query':null,'take':25,'skip':0}";
        #endregion

        public ControllerTestBase(CustomWebApplicationFactory<CESMII.ProfileDesigner.Api.Startup> factory, ITestOutputHelper output)
        {
            _factory = factory;
            this.output = output;
        }

        #region Properties
        protected MyNamespace.Client ApiClient
        {
            get
            {
                if (_apiClient == null)
                    _apiClient = _factory.GetApiClientAuthenticated(false);
                return _apiClient;
            }
        }

        /// <summary>
        /// Inits api client and adds user as an admin
        /// </summary>
        protected MyNamespace.Client ApiClientAdmin
        {
            get
            {
                if (_apiClientAdmin == null)
                    _apiClientAdmin = _factory.GetApiClientAuthenticated(true);
                return _apiClientAdmin;
            }
        }

        protected ProfileTypeDefFilterModel ProfileFilter
        {
            get
            {
                //get stock filter
                return JsonConvert.DeserializeObject<ProfileTypeDefFilterModel>(_filterPayloadProfile);
            }
        }

        protected CloudLibFilterModel CloudLibFilter
        {
            get
            {
                //get stock cloud lib filter
                var result = JsonConvert.DeserializeObject<CloudLibFilterModel>(_filterPayloadProfile);
                result.Cursor = null;
                result.PageBackwards = false;
                return result;
            }
        }

        protected PagerFilterSimpleModel SimpleFilter
        {
            get
            {
                //get stock filter
                return JsonConvert.DeserializeObject<PagerFilterSimpleModel>(_filterPayloadSimple);
            }
        }

        ///shared filter used to represent an exclusion of local profiles 
        protected List<LookupGroupByModel> FilterExcludeLocalItems
        {
            get
            {
                return new List<LookupGroupByModel>{
                    new LookupGroupByModel() {
                        Name = "Source", ID = (int)ProfileSearchCriteriaCategoryEnum.Source,
                        Items = new List<LookupItemFilterModel>() {
                            new LookupItemFilterModel() { ID = (int)ProfileSearchCriteriaSourceEnum.BaseProfile, Selected = false }
                        }
                    }
                };
            }
        }

        ///shared filter used to represent an exclusion of local profiles 
        protected List<LookupGroupByModel> FilterIncludeLocalItems
        {
            get
            {
                return new List<LookupGroupByModel>{
                    new LookupGroupByModel() {
                        Name = "Source", ID = (int)ProfileSearchCriteriaCategoryEnum.Source,
                        Items = new List<LookupItemFilterModel>() {
                            new LookupItemFilterModel() { ID = (int)ProfileSearchCriteriaSourceEnum.BaseProfile, Selected = false }
                        }
                    }
                };
            }
        }
        #endregion

        /// <summary>
        /// if a test needs a db context, init it here. 
        /// This will be used whenever a test needs a <IRepository<TEntity>>.
        /// </summary>
        /// <param name="services"></param>
        protected void InitDBContext(ServiceCollection services)
        {
            var connectionStringProfileDesigner = _factory.Config.GetConnectionString("ProfileDesignerDB");
            services.AddDbContext<ProfileDesignerPgContext>(options =>
                    options.UseNpgsql(connectionStringProfileDesigner));
        }

        protected User GetTestUser(Data.Repositories.IRepository<User> repo)
        {
            var result = repo.FindByCondition(x => x.ObjectIdAAD.ToLower().Equals(TEST_USER_OBJECTID_AAD.ToLower())).ToList();

            //get test user - check for unique match
            if (result == null || result.Count == 0) throw new InvalidOperationException("GetUser - no data found.");
            else if (result.Count > 1)
            {
                throw new InvalidOperationException($"GetUser - Multiple matches found for username = '{TEST_USER_OBJECTID_AAD}'.");
            }
            else
            {
                return result[0];
            }
        }

        public static IEnumerable<object[]> ControllerTestCounterData()
        {
            var result = new List<object[]>();
            for (int i = 1; i <= 10; i++)
            {
                result.Add(new object[] { i });
            }
            return result;
        }


        public virtual void Dispose()
        {
            //do clean up here
        }
    }
}
