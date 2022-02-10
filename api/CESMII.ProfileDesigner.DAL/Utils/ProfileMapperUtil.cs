using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using CESMII.ProfileDesigner.Common.Enums;
using CESMII.ProfileDesigner.DAL.Models;
using CESMII.ProfileDesigner.Data.Entities;

namespace CESMII.ProfileDesigner.DAL.Utils
{
    /// <summary>
    /// Mapping helper class to allow multiple areas of the code to perform common mapping routines
    /// and generate complex inter-relational lists for profiles
    /// </summary>
    public class ProfileMapperUtil
    {
        private readonly IDal<ProfileTypeDefinition, ProfileTypeDefinitionModel> _dal;
        private readonly IDal<LookupItem, LookupItemModel> _dalLookup;
        private readonly IDal<LookupDataType, LookupDataTypeModel> _dalDataType;
        private readonly Common.ConfigUtil _config;

        public ProfileMapperUtil(IDal<ProfileTypeDefinition, ProfileTypeDefinitionModel> dal,
            IDal<LookupItem, LookupItemModel> dalLookup,
            IDal<LookupDataType, LookupDataTypeModel> dalDataType,
            Common.ConfigUtil config
            )
        {
            _dal = dal;
            _dalLookup = dalLookup;
            _dalDataType = dalDataType;
            _config = config;
        }


        #region Getting, Assembling, Generating Related Profile Data (for explorer, breadcrumb trail, extended attributes)
        /// <summary>
        /// An ordered representation of the profile's lineage. This is a flat collection
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        public List<ProfileTypeDefinitionSimpleModel> GenerateAncestoryLineage(ProfileTypeDefinitionModel profile, UserToken userToken)
        {
            //navigate up the inheritance tree until the root. 
            List<ProfileTypeDefinitionSimpleModel> result = new List<ProfileTypeDefinitionSimpleModel>();
            int counter = 0;
            var ancestor = profile;
            while (ancestor != null)
            {
                result.Add(MapToModelProfileSimple(ancestor, counter));
                ancestor = ancestor.Parent?.ID == null ? null : _dal.GetById(ancestor.Parent.ID.Value, userToken);
                counter--;
            }
            //sort the result grandaprent / parent / profile
            return result.OrderBy(p => p.Level).ThenBy(p => p.Name).ToList();
        }

        public static List<int?> ExcludedProfileTypes = new List<int?> { (int)ProfileItemTypeEnum.Object, (int)ProfileItemTypeEnum.Method, };

        /// <summary>
        /// A nested representation of the profile's place relative to its ancestors and siblings
        /// This is a recursive represenation for ancestors and siblings. 
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="includeSiblings"></param>
        /// <returns></returns>
        public List<ProfileTypeDefinitionAncestoryModel> GenerateAncestoryTree(ProfileTypeDefinitionModel profile, UserToken userToken, bool includeSiblings = false)
        {
            //navigate up the inheritance tree until the root. this is sorted properly. Should be at least one.
            var lineage = this.GenerateAncestoryLineage(profile ,userToken);
            //build a nested loop from root to profile item
            ProfileTypeDefinitionAncestoryModel root = null;
            ProfileTypeDefinitionAncestoryModel curItem = null;
            ProfileTypeDefinitionAncestoryModel finalParent = null;
            foreach (var p in lineage)
            {
                var pConverted = new ProfileTypeDefinitionAncestoryModel()
                { ID = p.ID, Author = p.Author, Description = p.Description, Level = p.Level, Name = p.Name, Type = p.Type
                    , /*Namespace = p.Namespace,*/ IsAbstract = p.IsAbstract, OpcNodeId = p.OpcNodeId
                    , Profile = new ProfileModel() { ID = p.Profile.ID, Namespace = p.Profile.Namespace, Version = p.Profile.Version }
                };
                //1st iteration
                if (root == null)
                {
                    root = pConverted;
                    root.Children = new List<ProfileTypeDefinitionAncestoryModel>();
                    finalParent = root;
                }
                else //all other iterations
                {
                    curItem.Children.Add(pConverted);
                    finalParent = curItem;
                }
                curItem = pConverted;
                curItem.Children = new List<ProfileTypeDefinitionAncestoryModel>();
            }

            var result = new List<ProfileTypeDefinitionAncestoryModel>();
            result.Add(root);

            //build out a list and siblings will go on same level as the profile

            //find siblings - profiles with same parent. 
            //Append to cur item if there is a multi-gen scenario. append as sibling if profile is the root 
            if (includeSiblings)
            {
                var siblings = _dal.Where(p => !ProfileMapperUtil.ExcludedProfileTypes.Contains(p.ProfileTypeId) /*p.ProfileTypeId != (int)ProfileItemTypeEnum.Object*/ && profile.Parent != null &&
                                            p.ParentId.Equals(profile.Parent.ID) &&
                                            p.ID != profile.ID, userToken, null, null, false).Data
                    .Select(s => MapToModelProfileAncestory(s, 1));

                if (lineage.Count == 1) result = result.Concat(siblings).ToList();
                else
                {
                    //append siblings to final parent
                    finalParent.Children = finalParent.Children.Concat(siblings).ToList();
                }
            }
            return result;
        }

        /// <summary>
        /// Get the list of profiles that depend on this profile
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        public List<ProfileTypeDefinitionSimpleModel> GenerateDependencies(ProfileTypeDefinitionModel profile, UserToken userToken)
        {
            //find all descendants that may be related...go n levels deep
            var result = new List<ProfileTypeDefinitionSimpleModel>();
            BuildDescendantsTree(ref result, profile.ID, 1, userToken);
            var count = result.Count();

            //find compositions, variable types which depend on this profile
            var dependencies = _dal.Where(p => !ProfileMapperUtil.ExcludedProfileTypes.Contains(p.ProfileTypeId) /*p.ProfileTypeId != (int)ProfileItemTypeEnum.Object*/ &&
            (p.ParentId.Equals(profile.ID) ||
                            p.Compositions.Any(p => p.CompositionId.Equals(profile.ID)) 
                            || p.Attributes.Any(a => a.DataType.CustomTypeId.HasValue 
                                    && a.DataType.CustomTypeId.Equals(profile.ID)))
                            , userToken, null, null).Data
                .Select(s => MapToModelProfileAncestory(s, count + 1));

            return result.Concat(dependencies).OrderBy(p => p.Level).ThenBy(p => p.Name).ToList();
        }

        /// <summary>
        /// Build a list of descendants that will be used in the dependencies list. 
        /// </summary>
        /// <param name="descendants"></param>
        /// <param name="parentId"></param>
        /// <param name="level"></param>
        private void BuildDescendantsTree(ref List<ProfileTypeDefinitionSimpleModel> descendants, int? parentId, int level, UserToken userToken)
        {
            if (!parentId.HasValue) return;
            //add the current set of children
            var children = _dal.Where(p => p.ParentId.Equals(parentId.Value) && !ProfileMapperUtil.ExcludedProfileTypes.Contains(p.ProfileTypeId) /*p.ProfileTypeId != (int)ProfileItemTypeEnum.Object*/, userToken, null, null, false).Data
                .Select(s => MapToModelProfileAncestory(s, level));
            if (children.Count() == 0) return;
            descendants.Concat(children);

            //add grandchildren and their children recusive...
            level++;
            foreach (var child in children)
            {
                BuildDescendantsTree(ref descendants, child.ID, level, userToken);
            }
        }

        /// <summary>
        /// TBD - combine this with the lineage call so we reduce churn on DB. 
        /// Get a list of extended attributes for this profile. Extended attributes are attributes that belong to the ancestors of this
        /// profile. These are read only in the front end but used to avoid duplication and to show the complete make up of this profile. 
        /// </summary>
        /// <returns></returns>
        public List<ProfileAttributeModel> GetExtendedAttributes(ProfileTypeDefinitionModel profile, UserToken userToken)
        {
            //navigate up the inheritance tree until the root. 
            List<ProfileAttributeModel> result = new List<ProfileAttributeModel>();
            //List<ProfileItemSimpleModel> lineage = new List<ProfileItemSimpleModel>();
            var ancestor = profile;
            while (ancestor != null)
            {
                //lineage.Add(MapToModelProfileSimple(ancestor));
                ancestor = ancestor.Parent?.ID == null ? null : _dal.GetById(ancestor.Parent.ID.Value, userToken);
                //note we don't get the attribs from the current profile. They are not "extended". 
                if (ancestor != null && ancestor.Attributes != null)
                {
                    result = result.Concat(ancestor.Attributes).ToList();
                }
            }

            //A profile can implement interface(s). This is indicated by a collection hanging off the profile object.
            //Get the attributes associated with these interfaces. These are essentially "virtual" (read-only) attributes
            //that will show in the list of attributes along side the other attributes. 
            result = result.Concat(MapInterfaceAttributesToProfileAttributeModels(profile, userToken)).ToList();

            //trim down attributes of type def attributes and ancestors where certain things match up. If the ancestor attribute
            //has same name, type, browse name, we only show one.  
            //for now, this arbitrarily chooses the extended attribute to keep. revisit this if we need
            //to only keep a specific extended attribute (ie only the immediate parent's attrib)
            result = result.GroupBy(x => new { x.Name, x.DataTypeId, x.AttributeType.ID, x.BrowseName })
                                  .Select(d => d.First())
                                  .ToList();

            //sort the result by attribute name
            return result.OrderBy(a => a.Name).ToList();
        }

        public List<int> GetPopularItems(UserToken userToken)
        {
            //build list of where clauses
            List<Expression<Func<ProfileTypeDefinition, bool>>> predicate = new List<Expression<Func<ProfileTypeDefinition, bool>>>();
            var paramExpr = Expression.Parameter(typeof(ProfileTypeDefinition), "x");

            //Part 0 - Always exclude some types that are behind the scenes type
            predicate.Add(x => !ProfileMapperUtil.ExcludedProfileTypes.Contains(x.ProfileTypeId));
            predicate.Add(x => x.Favorite != null || x.Analytics != null);

            //build list of order bys - is favorites true first, then the manual ranking, then extend count, then page visit count
            var obes = new List<OrderByExpression<ProfileTypeDefinition>>();
            obes.Add(new OrderByExpression<ProfileTypeDefinition>() { Expression = x => x.Favorite == null || !x.Favorite.IsFavorite ? 0 : 1, IsDescending = true });
            obes.Add(new OrderByExpression<ProfileTypeDefinition>() { Expression = x => x.Analytics == null ? 0 : x.Analytics.ManualRank, IsDescending = true });
            obes.Add(new OrderByExpression<ProfileTypeDefinition>() { Expression = x => x.Analytics == null ? 0 : x.Analytics.ExtendCount, IsDescending = true });
            obes.Add(new OrderByExpression<ProfileTypeDefinition>() { Expression = x => x.Analytics == null ? 0 : x.Analytics.PageVisitCount, IsDescending = true });

            return _dal.Where(predicate, userToken, null, 30, false, false, obes.ToArray()).Data
                .OrderByDescending(x => x.PopularityIndex).Select(x => x.ID.Value).Take(30).ToList();

            //var matches = _dal.Where(x => 
            //        x.Favorite != null ||
            //        (x.Analytics != null && (x.Analytics.ExtendCount + x.Analytics.PageVisitCount) > 0), userToken, null, null, false, false).Data;
            //var matches = _dal.Where(x => x.Favorite != null && x.Analytics != null, userToken, null, null, false, false).Data;

            //if (matches == null)
            //{
            //    return new List<int>();
            //}

            ////trim down list
            //return matches.OrderByDescending(x => x.PopularityIndex).Select(x => x.ID.Value).Take(30).ToList();
        }

        public List<OrderByExpression<ProfileTypeDefinition>> BuildSearchOrderByExpressions(int userId, SearchCriteriaSortByEnum val = SearchCriteriaSortByEnum.Name)
        {
            //build list of order bys based on user selection of an enum, default is sort by name. 
            var result = new List<OrderByExpression<ProfileTypeDefinition>>();
            switch (val)
            {
                case SearchCriteriaSortByEnum.Author:
                    result.Add(new OrderByExpression<ProfileTypeDefinition>() { Expression = x => !x.Profile.StandardProfileID.HasValue && 
                                                                                    x.AuthorId.HasValue && x.AuthorId.Equals(userId) ? 1 : 0, IsDescending = true });
                    result.Add(new OrderByExpression<ProfileTypeDefinition>() { Expression = x => x.Name });
                    result.Add(new OrderByExpression<ProfileTypeDefinition>() { Expression = x => x.Profile.Namespace });
                    break;
                case SearchCriteriaSortByEnum.Popular:
                    result.Add(new OrderByExpression<ProfileTypeDefinition>() { Expression = x => x.Favorite == null || !x.Favorite.IsFavorite ? 0 : 1, IsDescending = true });
                    result.Add(new OrderByExpression<ProfileTypeDefinition>() { Expression = x => x.Analytics == null ? 0 : x.Analytics.ExtendCount, IsDescending = true });
                    result.Add(new OrderByExpression<ProfileTypeDefinition>() { Expression = x => x.Analytics == null ? 0 : x.Analytics.PageVisitCount, IsDescending = true });
                    result.Add(new OrderByExpression<ProfileTypeDefinition>() { Expression = x => x.Name });
                    result.Add(new OrderByExpression<ProfileTypeDefinition>() { Expression = x => x.Profile.Namespace });
                    break;
                case SearchCriteriaSortByEnum.Name:
                default:
                    result.Add(new OrderByExpression<ProfileTypeDefinition>() { Expression = x => x.Name});
                    result.Add(new OrderByExpression<ProfileTypeDefinition>() { Expression = x => x.Profile.Namespace });
                    break;
            }
            
            return result;
        }

        #endregion

        #region Build lookup tables for given profile
        public List<ProfileTypeDefinitionSimpleModel> BuildCompositionLookup(ProfileTypeDefinitionModel profile, UserToken userToken)
        {
            //TBD - remove variable types from composition list.           
            //navigate up the inheritance tree until the root. 
            List<ProfileTypeDefinitionAncestoryModel> ancestors = profile == null ? new List<ProfileTypeDefinitionAncestoryModel>() :
                this.GenerateAncestoryTree(profile, userToken);
            //find all descendants that may be related...
            List<ProfileTypeDefinitionSimpleModel> dependencies = profile == null ? new List<ProfileTypeDefinitionSimpleModel>() :
                this.GenerateDependencies(profile, userToken);

            //now return the profiles that are NOT an ancestor or a dependency.
            var finalDependencies = ancestors.Concat(dependencies).Select(x => x.ID).ToList();

            //compositions can only derive from BaseObjectType - get BaseObjectType profile's dependencies and trim down the
            //list of the compositions if any of these are in the final dependencies list
            var compRoot = _dal.GetByFunc(
                x => x.Name.ToLower().Equals(_config.ProfilesSettings.ReservedProfileNames.CompositionRootProfileName.ToLower()),
                userToken, false);
            var eligibleComps = this.GenerateDependencies(compRoot, userToken);

            //if no dependencies, just get all. This should only happen in a new scenario.
            if (finalDependencies.Count == 0)
            {
                return eligibleComps;
            }
            else
            {
                return eligibleComps.Where(p => !finalDependencies.Contains(p.ID) &&
                    !p.ID.Equals(profile.ID)).ToList(); 
                    //&& p.ProfileType.ID.Equals(profile.Type.ID), userToken, null, null, false).Data; //only get items of same profile type
            }
        }

        public List<ProfileTypeDefinitionSimpleModel> BuildCompositionLookupExtend(ProfileTypeDefinitionModel parent, UserToken userToken)
        {
            //navigate up the inheritance tree until the root. 
            List<ProfileTypeDefinitionAncestoryModel> ancestors = this.GenerateAncestoryTree(parent, userToken);
            var finalDependencies = ancestors.Select(x => x.ID).ToList();
            //add parent to dependencies list manually
            finalDependencies.Add(parent.ID);

            ////now return the profiles that are NOT an ancestor or a dependency.
            //var items = _dal.Where(p => !finalDependencies.Contains(p.ID) &&
            //        !p.ID.Equals(parent.ID) &&
            //         p.ProfileType.ID.Equals(parent.Type.ID), userToken, null, null, false); //only get items of same profile type

            //return MapToModelProfileSimpleList(items.Data);
            //compositions can only derive from BaseObjectType - get BaseObjectType profile's dependencies and trim down the
            //list of the compositions if any of these are in the final dependencies list
            var compRoot = _dal.GetByFunc(
                x => x.Name.ToLower().Equals(_config.ProfilesSettings.ReservedProfileNames.CompositionRootProfileName.ToLower()),
                userToken, false);
            var eligibleComps = this.GenerateDependencies(compRoot, userToken);

            return eligibleComps.Where(p => !finalDependencies.Contains(p.ID) &&
                !p.ID.Equals(parent.ID)).ToList();
            //&& p.ProfileType.ID.Equals(parent.Type.ID), userToken, null, null, false).Data; //only get items of same profile type
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// TBD - handle scenario where user implemented same interface in ancestor profile. 
        /// Prevent this from happening by not allowing this as an interface selection.
        ///</remarks>
        /// <param name="profile"></param>
        /// <returns></returns>
        public List<ProfileTypeDefinitionModel> BuildInterfaceLookup(ProfileTypeDefinitionModel profile, UserToken userToken)
        {
            //remove items of a different type, remove self
            //remove interfaces already used by profile. 
            //Note: interfaces also must only derive from BaseInterfaceType - by selecting only interface profile types, we satisfy this.
            var items = profile == null ?
                _dal.Where(p => p.ProfileType.Name.ToLower().Equals("interface"), userToken, null, null, false) : 
                _dal.Where(p => p.ProfileType.Name.ToLower().Equals("interface") &&
                                    !p.ID.Equals(profile.ID) &&
                                    !profile.Interfaces.Select(x => x.ID).ToList().Contains(p.ID), userToken, null, null, false);
            var result = new List<ProfileTypeDefinitionModel>();
            int counter = 0;
            foreach (var p in items.Data)
            {
                //pull extended attributes from ancestory AND interface attributes
                p.ExtendedProfileAttributes = this.GetExtendedAttributes(p, userToken);
                //merge profile attributes, compositions, variable types
                p.ProfileAttributes = this.MergeProfileAttributes(p);

                //add some special indicators these attrs are interface attr
                foreach (var a in p.ProfileAttributes)
                {
                    a.Interface = new ProfileTypeDefinitionRelatedModel() { ID = p.ID, Name = p.Name, BrowseName = p.BrowseName, OpcNodeId = p.OpcNodeId }; ;
                    a.InterfaceGroupId = counter;
                }

                //add some special indicators the ancestor attrs are interface attr associated with this interface
                foreach (var a in p.ExtendedProfileAttributes)
                {
                    a.Interface = new ProfileTypeDefinitionRelatedModel() { ID = p.ID, Name = p.Name, BrowseName = p.BrowseName, OpcNodeId = p.OpcNodeId }; ;
                    a.InterfaceGroupId = counter;
                }

                result.Add(p);

                counter++;
            }

            return result;
        }

        #endregion

        #region Profiles - Mapping MODEL(s) to ViewModel Methods
        protected ProfileTypeDefinitionAncestoryModel MapToModelProfileAncestory(ProfileTypeDefinitionModel item, int level)
        {
            if (item != null)
            {
                return new ProfileTypeDefinitionAncestoryModel
                {
                    ID = item.ID,
                    Name = item.Name,
                    BrowseName = item.BrowseName,
                    /*Namespace = item.Profile.Namespace,*/
                    Profile = new ProfileModel() { ID = item.Profile.ID, Namespace = item.Profile.Namespace, Version = item.Profile.Version },
                    Description = item.Description,
                    Type = item.Type,
                    Author = item.Author,
                    OpcNodeId = item.OpcNodeId,
                    IsAbstract = item.IsAbstract,
                    Level = level
                };
            }
            else
            {
                return null;
            }
        }

        public List<ProfileTypeDefinitionSimpleModel> MapToModelProfileSimpleList(List<ProfileTypeDefinitionModel> items)
        {
            var result = new List<ProfileTypeDefinitionSimpleModel>();
            foreach (var p in items)
            {
                result.Add(MapToModelProfileSimple(p));
            }
            return result.OrderBy(a => a.Name).ToList(); ;
        }

        public ProfileTypeDefinitionSimpleModel MapToModelProfileSimple(ProfileTypeDefinitionModel item, int level = 0)
        {
            if (item != null)
            {
                return new ProfileTypeDefinitionSimpleModel
                {
                    ID = item.ID,
                    Name = item.Name,
                    BrowseName = item.BrowseName,
                    /*Namespace = item.Profile.Namespace,*/
                    Profile = item.Profile,
                    ProfileTypeDefinition = item,
                    Description = item.Description,
                    Type = item.Type != null ? item.Type : new LookupItemModel { ID = item.TypeId, LookupType = LookupTypeEnum.ProfileType },
                    Author = item.Author,
                    OpcNodeId = item.OpcNodeId,
                    IsAbstract = item.IsAbstract,
                    Level = level
                };
            }
            else
            {
                return null;
            }

        }

        public ProfileTypeDefinitionSimpleWithAttrsModel MapToModelProfileSimpleWithAttrs(ProfileTypeDefinitionModel item, int level = 0)
        {
            if (item != null)
            {
                return new ProfileTypeDefinitionSimpleWithAttrsModel
                {
                    ID = item.ID,
                    Name = item.Name,
                    BrowseName = item.BrowseName,
                    /*Namespace = item.Profile.Namespace,*/
                    Profile = new ProfileModel() { ID = item.Profile.ID, Namespace = item.Profile.Namespace, Version = item.Profile.Version },
                    Description = item.Description,
                    Type = item.Type,
                    Author = item.Author,
                    OpcNodeId = item.OpcNodeId,
                    IsAbstract = item.IsAbstract,
                    Level = level,
                    ProfileAttributes = item.Attributes
                };
            }
            else
            {
                return null;
            }

        }
        #endregion

        #region Attributes - Mapping MODEL(s) to ViewModel Methods
        /// <summary>
        /// Take the profile item attributes, compositions, variable types and merge into a single 
        /// collection that the front end can use. 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public List<ProfileAttributeModel> MergeProfileAttributes(ProfileTypeDefinitionModel item)
        {
            var result = item.Attributes == null ? new List<ProfileAttributeModel>() : item.Attributes;
            result = result.Concat(MapCompositionsToProfileAttributeModels(item)).ToList();
            //result = result.Concat(MapCustomDataTypesToProfileAttributeModels(item)).ToList();

            //trim down where comparable item is in the parent (match on name, data type id, attribute type id, browse name)
            //for now, we trim out the profile attribute and leave extended so we get the read-only ui
            if (item.ExtendedProfileAttributes?.Count > 0)
            {
                result = result.Where(x => !item.ExtendedProfileAttributes.Any(y =>
                        y.Name == x.Name &&
                        y.DataTypeId == x.DataTypeId &&
                        y.AttributeType.ID == x.AttributeType.ID &&
                        y.BrowseName == x.BrowseName)).ToList();
            }

            //sort by enum value if present, then by name
            return result
                .OrderByDescending(a => a.EnumValue.HasValue)
                .ThenBy(a => a.EnumValue)
                .ThenBy(a => a.Name)
                .ToList();
        }

        /// <summary>
        /// For a given profile, map the composition attributes to a unified model
        /// structure (ProfileAttributeModel) that can be used downstream when merging 
        /// all sttributes into a single collection. This will add some data to distinguish
        /// these as composition type attributes. 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        protected List<ProfileAttributeModel> MapCompositionsToProfileAttributeModels(ProfileTypeDefinitionModel item)
        {
            var result = new List<ProfileAttributeModel>();
            if (item == null || item.Compositions == null) return result;

            //get lookup data type for composition
            var dataTypeComp = _dalDataType.Where(x => x.Code.ToLower().Equals("composition"), new UserToken(), null, null).Data.FirstOrDefault();
            //get lookup data type for attr type composition
            var attrTypeComp = _dalLookup.Where(x => x.Code.ToLower().Equals("composition"), new UserToken(), null, null).Data.FirstOrDefault();

            foreach (var comp in item.Compositions)
            {
                result.Add(new ProfileAttributeModel
                {
                    Composition = comp,
                    CompositionId = comp.RelatedProfileTypeDefinitionId,
                    ID = comp.ID,
                    Name = comp.Name,
                    BrowseName = comp.BrowseName,
                    SymbolicName = comp.SymbolicName,
                    Description = comp.Description,
                    TypeDefinitionId = item.ID,
                    //TypeDefinition = item, // Results in cycle during serialization
                    // No VariableTypeDefinition for compositions
                    DataTypeId = dataTypeComp.ID,
                    DataType = dataTypeComp, 
                    AttributeType = attrTypeComp
                });
            }
            return result;
        }

        /// <summary>
        /// For a given profile, map the custom data type attributes to a unified model
        /// structure (ProfileAttributeModel) that can be used downstream when merging 
        /// all sttributes into a single collection. This will add some data to distinguish
        /// these as custom data type attributes. 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        //[System.Obsolete("This is going away. TBD - Remove once we cut over custom data types.")]
        //protected List<ProfileAttributeModel> MapCustomDataTypesToProfileAttributeModels(ProfileItemModel item)
        //{
        //    var result = new List<ProfileAttributeModel>();
        //    if (item == null || item.CustomDataTypes == null) return result;

        //    //get lookup data type for custom data type
        //    var dataTypeCustom = _dalDataType.Where(x => x.Code.ToLower().Equals("customdatatype"), null, null).Data.FirstOrDefault();

        //    foreach (var cdt in item.CustomDataTypes)
        //    {
        //        result.Add(new ProfileAttributeModel
        //        {
        //            CustomDataType = cdt,
        //            CustomDataTypeId = cdt.RelatedId,
        //            ID = cdt.ID,
        //            Name = cdt.Name,
        //            Description = cdt.Description,
        //            ProfileId = item.ID,
        //            TypeId = dataTypeCustom.ID,
        //            DataType = dataTypeCustom
        //        });
        //    }
        //    return result;
        //}

        /// <summary>
        /// For each interface associated with the profile, append the interface attribute into the 
        /// profile attributes list. These would be read only and show how the profile is virtually implementing
        /// the attributes for each interface. 
        /// </summary>
        /// <remarks>
        /// TBD - need to get the ancestor attributes of each interface as well. 
        /// </remarks>
        /// <param name="item"></param>
        /// <returns></returns>
        protected List<ProfileAttributeModel> MapInterfaceAttributesToProfileAttributeModels(ProfileTypeDefinitionModel item, UserToken userToken)
        {
            var result = new List<ProfileAttributeModel>();
            if (item == null || item.Interfaces == null) return result;

            var counter = 0;
            foreach (var p in item.Interfaces)
            {
                var attrs = this.MergeProfileAttributes(p);

                //get the ancestor attribs which are also now a part of this interface implementation
                attrs = attrs.Concat(this.GetExtendedAttributes(p, userToken)).ToList();

                //add some special indicators these attrs are interface attr all associated with this interface
                //even acncestor attribs should be associated with this interface. 
                foreach (var a in attrs)
                {
                    a.Interface = new ProfileTypeDefinitionRelatedModel() { ID = p.ID, Name = p.Name, BrowseName = p.BrowseName, OpcNodeId = p.OpcNodeId }; ;
                    a.InterfaceGroupId = counter;
                }

                result = result.Concat(attrs).ToList();
                counter++;
            }
            return result;
        }


        #endregion

        #region Mapping ViewModel(s) to MODEL(s) to ENTITY(s)
        /// <summary>
        /// Take a list of attributes returned by front end and find the ones which compositions. Split those out 
        /// into a collection that the dal and DB is expecting.
        /// </summary>
        /// <param name="comps"></param>
        /// <returns></returns>
        public List<ProfileTypeDefinitionRelatedModel> MapProfileAttributeToCompositionModels(List<ProfileAttributeModel> comps)
        {
            var result = new List<ProfileTypeDefinitionRelatedModel>();
            if (comps == null || comps.Count == 0) return result;

            foreach (var a in comps)
            {
                result.Add(new ProfileTypeDefinitionRelatedModel
                {
                    ID = a.ID,
                    RelatedProfileTypeDefinitionId = a.CompositionId.Value,
                    Name = a.Name,
                    BrowseName = a.BrowseName,
                    SymbolicName = a.SymbolicName,
                    Description = a.Description,
                    RelatedName = a.Composition.Name,
                    RelatedDescription = a.Composition.Description,
                    RelatedIsRequired = a.Composition.RelatedIsRequired,
                    RelatedModelingRule = a.Composition.RelatedModelingRule,
                    Author = a.Composition.Author
                });
            }
            return result;
        }


        ///// <summary>
        ///// Take a list of attributes returned by front end and find the ones which are custom data types. Split those out 
        ///// into a collection that the dal and DB is expecting.
        ///// </summary>
        ///// <param name="customDataTypes"></param>
        ///// <returns></returns>
        //public List<ProfileItemRelatedModel> MapProfileAttributeToCustomDataTypeModels(List<ProfileAttributeModel> customDataTypes)
        //{
        //    var result = new List<ProfileItemRelatedModel>();
        //    if (customDataTypes == null || customDataTypes.Count == 0) return result;

        //    foreach (var a in customDataTypes)
        //    {
        //        result.Add(new ProfileItemRelatedModel
        //        {
        //            ID = a.ID,
        //            RelatedId = a.CustomDataTypeId.Value,
        //            Name = a.Name,
        //            Description = a.Description,
        //            RelatedName = a.CustomDataType.Name,
        //            RelatedDescription = a.CustomDataType.Description,
        //            Author = a.CustomDataType.Author
        //        });
        //    }
        //    return result;
        //}
        #endregion

        #region Mapping MODEL(s) to ENTITY(s) Methods
        #endregion


    }
}
