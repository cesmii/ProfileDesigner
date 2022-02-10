using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;


using CESMII.ProfileDesigner.Common;
using CESMII.ProfileDesigner.Common.Enums;
using CESMII.ProfileDesigner.Data.Entities;
using CESMII.ProfileDesigner.DAL.Models;
using CESMII.ProfileDesigner.DAL;
using CESMII.ProfileDesigner.Api.Shared.Controllers;
using CESMII.ProfileDesigner.Api.Shared.Models;
using CESMII.ProfileDesigner.Api.Shared.Extensions;
using CESMII.ProfileDesigner.DAL.Utils;

namespace CESMII.ProfileDesigner.Api.Controllers
{
    [Authorize, Route("api/[controller]")]
    public class LookupController : BaseController<LookupController>
    {
        private readonly IDal<LookupItem, LookupItemModel> _dal;
        private readonly IDal<LookupDataTypeRanked, LookupDataTypeRankedModel> _dalDataType;
        private readonly IDal<EngineeringUnitRanked, EngineeringUnitRankedModel> _dalEngineeringUnit;
        private readonly IDal<Profile, ProfileModel> _dalProfile;

        public LookupController(IDal<LookupItem, LookupItemModel> dal,
            IDal<LookupDataTypeRanked, LookupDataTypeRankedModel> dalDataType,
            IDal<EngineeringUnitRanked, EngineeringUnitRankedModel> dalEngineeringUnit,
            IDal<Profile, ProfileModel> dalProfile,
            ConfigUtil config, ILogger<LookupController> logger)
            : base(config, logger)
        {
            _dal = dal;
            _dalDataType = dalDataType;
            _dalEngineeringUnit = dalEngineeringUnit;
            _dalProfile = dalProfile;
        }


        [HttpGet, Route("All")]
        [ProducesResponseType(200, Type = typeof(AppLookupModel))]
        [ProducesResponseType(400)]
        public IActionResult GetAll()
        {
            var userToken = UserExtension.DalUserToken(User);

            //profile types, attr types in here
            var data = _dal.GetAll(userToken);
            //var lookupCustomDataType = data.Find(x => x.LookupType == LookupTypeEnum.ProfileType && x.Code.ToLower().Equals("customdatatype"));

            //get entire list and then split out into sub-lists
            var dataTypes = _dalDataType.Where(x => !x.Code.ToLower().Equals("composition"), userToken).Data.ToList();

            var result = new AppLookupModel()
            {
                ProfileTypes = data.Where(x => x.LookupType == LookupTypeEnum.ProfileType).ToList(),
                EngUnits = _dalEngineeringUnit.Where(x => x.IsActive, userToken).Data.ToList(),
                AttributeTypes = data.Where(x => x.LookupType == LookupTypeEnum.AttributeType).ToList(),
                //DataTypes = dataTypes.Where(x => x.CustomType == null || !x.CustomType.TypeId.Equals((int)ProfileItemTypeEnum.Structure)).ToList(),
                //Structures = dataTypes.Where(x => x.CustomType != null && x.CustomType.TypeId.Equals((int)ProfileItemTypeEnum.Structure)).ToList()
                DataTypes = dataTypes,
                Structures = null
            };

            return Ok(result);
        }



        [HttpGet, Route("searchcriteria")]
        [ProducesResponseType(200, Type = typeof(ProfileTypeDefFilterModel))]
        [ProducesResponseType(400)]
        public IActionResult GetSearchCriteria() //[FromBody] LookupGroupByModel model)
        {
            //populate specific types
            var userToken = UserExtension.DalUserToken(User);
            var filters = new List<LookupGroupByModel>();

            // separate section for my type - follow the same structure for flexibility but only including one hardcoded type
            filters.Add(new LookupGroupByModel()
            {
                Name = SearchCriteriaCategoryEnum.Author.ToString(),
                ID = (int)SearchCriteriaCategoryEnum.Author,
                Items = new List<LookupItemFilterModel>() { new LookupItemFilterModel() { 
                    ID = User.GetUserID(),
                    Name = "My Types"
                }}
            });

            // separate section for popular - follow the same structure for flexibility but only including one hardcoded type
            filters.Add(new LookupGroupByModel()
            {
                Name = SearchCriteriaCategoryEnum.Popular.ToString(),
                ID = (int)SearchCriteriaCategoryEnum.Popular,
                Items = new List<LookupItemFilterModel>() { new LookupItemFilterModel() {
                    ID = -1,
                    Name = "Popular",
                    Visible = true //until we implement a popular calculator, leave this off the display
                }}
            });

            //group the result by lookup type
            List<int?> excludeList = new List<int?> { (int)ProfileItemTypeEnum.Class };
            excludeList = excludeList.Union(ProfileMapperUtil.ExcludedProfileTypes).ToList();
            var allItems = _dal.GetAll(userToken).Where(x => !excludeList.Contains(x.ID) &&
                x.LookupType == LookupTypeEnum.ProfileType);
            var grpItems = allItems.GroupBy(x => new { EnumValue = x.LookupType, Name = x.LookupType.ToString() });
            foreach (var item in grpItems)
            {
                filters.Add(new LookupGroupByModel
                {
                    Name = SearchCriteriaCategoryEnum.TypeDefinitionType.ToString(),
                    ID = (int)SearchCriteriaCategoryEnum.TypeDefinitionType,
                    Items = item.ToList().Select(itm => new LookupItemFilterModel
                    {
                        ID = itm.ID,
                        Name = itm.Name,
                        IsActive = itm.IsActive,
                        DisplayOrder = itm.DisplayOrder
                    }).ToList()
                });
            }

            // separate section for profile - follow the same structure for flexibility
            var profiles = _dalProfile.GetAll(userToken);
            filters.Add(new LookupGroupByModel()
            {
                Name = SearchCriteriaCategoryEnum.Profile.ToString(),
                ID = (int)SearchCriteriaCategoryEnum.Profile,
                Items = profiles.OrderBy(p => p.Namespace).Select(p => new LookupItemFilterModel
                {
                    ID = p.ID,
                    Name = p.Namespace + (string.IsNullOrEmpty(p.Version) ? "" : $" ({p.Version})"),
                    IsActive = true,
                    Visible = false,  //don't show in UI but can be set selected on View Type Definitions scenario
                    DisplayOrder = 999
                }).ToList()
            });

            //leave query null, skip, take defaults
            var result = new ProfileTypeDefFilterModel()
            {
                Filters = filters,
            };

            //return result
            return Ok(result);
        }

    }
}
