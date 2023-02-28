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
    [Authorize(Policy = nameof(PermissionEnum.UserAzureADMapped)), Route("api/[controller]")]
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
            UserDAL dalUser,
            ConfigUtil config, ILogger<LookupController> logger)
            : base(config, logger, dalUser)
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
            //profile types, attr types in here
            var data = _dal.GetAll(base.DalUserToken);

            //get entire list and then split out into sub-lists
            var dataTypes = _dalDataType.Where(x => !x.Code.ToLower().Equals("composition"), base.DalUserToken).Data.ToList();

            var result = new AppLookupModel()
            {
                ProfileTypes = data.Where(x => x.LookupType == LookupTypeEnum.ProfileType).ToList(),
                EngUnits = _dalEngineeringUnit.Where(x => x.IsActive, base.DalUserToken).Data.ToList(),
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
            var filters = new List<LookupGroupByModel>
            {
                // separate section for my type - follow the same structure for flexibility but only including one hardcoded type
                new LookupGroupByModel()
                {
                    Name = SearchCriteriaCategoryEnum.Author.ToString(),
                    ID = (int)SearchCriteriaCategoryEnum.Author,
                    Items = new List<LookupItemFilterModel>() { new LookupItemFilterModel() {
                    ID = LocalUser.ID,
                    Name = "My Types"
                }}
                },
                // separate section for popular - follow the same structure for flexibility but only including one hardcoded type
                new LookupGroupByModel()
                {
                    Name = SearchCriteriaCategoryEnum.Popular.ToString(),
                    ID = (int)SearchCriteriaCategoryEnum.Popular,
                    Items = new List<LookupItemFilterModel>() { new LookupItemFilterModel() {
                    ID = -1,
                    Name = "Popular",
                    Visible = true //until we implement a popular calculator, leave this off the display
                }}
                }
            };

            //group the result by lookup type
            var excludeList = new List<int?> { /*(int)ProfileItemTypeEnum.Class*/ };
            excludeList = excludeList.Union(ProfileMapperUtil.ExcludedProfileTypes).ToList();
            var allItems = _dal.GetAll(base.DalUserToken).Where(x => !excludeList.Contains(x.ID) &&
                x.LookupType == LookupTypeEnum.ProfileType);
            var grpItems = allItems.GroupBy(x => new { EnumValue = x.LookupType, Name = x.LookupType.ToString() });
            foreach (var item in grpItems)
            {
                filters.Add(new LookupGroupByModel
                {
                    Name = SearchCriteriaCategoryEnum.TypeDefinitionType.ToString(),
                    ID = (int)SearchCriteriaCategoryEnum.TypeDefinitionType,
                    Items = item.Select(itm => new LookupItemFilterModel
                    {
                        ID = itm.ID,
                        Name = itm.Name,
                        IsActive = itm.IsActive,
                        DisplayOrder = itm.DisplayOrder
                    }).ToList()
                });
            }

            // separate section for profile - follow the same structure for flexibility
            var profiles = _dalProfile.GetAll(base.DalUserToken);
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

        [HttpGet, Route("searchcriteria/profile")]
        [ProducesResponseType(200, Type = typeof(ProfileFilterModel))]
        [ProducesResponseType(400)]
        public IActionResult GetProfileSearchCriteria() //[FromBody] LookupGroupByModel model)
        {
            //populate specific types
            var filters = new List<LookupGroupByModel>
            {
                // separate section for my profiles - follow the same structure for flexibility but only including one hardcoded type
                new LookupGroupByModel()
                {
                    Name = ProfileSearchCriteriaCategoryEnum.Source.ToString(),
                    ID = (int)ProfileSearchCriteriaCategoryEnum.Source,
                    Items = new List<LookupItemFilterModel>() 
                    { 
                        new LookupItemFilterModel() 
                        {
                            ID = (int)ProfileSearchCriteriaSourceEnum.Mine,
                            Name = "My Profiles",
                            Selected = false,
                            Visible = true
                        },
                        new LookupItemFilterModel()
                        {
                            ID = (int)ProfileSearchCriteriaSourceEnum.BaseProfile,
                            Name = "Cloud Profiles", //could be published or pending and some may not be considered base //"Base Profiles",
                            Selected = false,
                            Visible = true
                        },
                        new LookupItemFilterModel()
                        {
                            ID = (int)ProfileSearchCriteriaSourceEnum.CloudLib,
                            Name = "Cloud Library",
                            Selected = false,
                            Visible = false
                        }                  
                    }
                }
            };

            //leave query null, skip, take defaults
            var result = new ProfileFilterModel()
            {
                Filters = filters,
            };

            return Ok(result);
        }

        [HttpGet, Route("searchcriteria/cloublibimporter")]
        [ProducesResponseType(200, Type = typeof(ProfileFilterModel))]
        [ProducesResponseType(400)]
        public IActionResult GetCloudLibSearchCriteria() //[FromBody] LookupGroupByModel model)
        {
            //populate specific types
            var filters = new List<LookupGroupByModel>
            {
                // separate section for my profiles - follow the same structure for flexibility but only including one hardcoded type
                new LookupGroupByModel()
                {
                    Name = ProfileSearchCriteriaCategoryEnum.Source.ToString(),
                    ID = (int)ProfileSearchCriteriaCategoryEnum.Source,
                    Items = new List<LookupItemFilterModel>()
                    {
                        new LookupItemFilterModel()
                        {
                            ID = (int)ProfileSearchCriteriaSourceEnum.BaseProfile,
                            Name = "Show Imported Profiles",
                            Selected = false,
                            Visible = true
                        }
                    }
                }
            };

            //leave query null, skip, take defaults
            var result = new ProfileFilterModel()
            {
                Filters = filters,
            };

            return Ok(result);
        }
    }
}
