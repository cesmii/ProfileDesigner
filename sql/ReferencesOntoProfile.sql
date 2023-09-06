-- WIP: Queries to find the profiles that reference a specific profile
-- parent, instance parent or variable data type
SELECT p.id, p0.id, p.parent_id, p0.namespace, p2.Namespace, p1.Name, p.author_id, p.browse_name, p.created, p.created_by_id, p.description, p.document_url, p.author_name, p.instance_parent_id, p.is_abstract, p.is_option_set, p.metatags, p.name, p.opc_node_id, p.owner_id, p.parent_id, p.profile_id, p.type_id, p.symbolic_name, p.updated, p.updated_by_id, p.variable_array_dimensions, p.variable_data_type_id, p.variable_value, p.variable_value_rank
FROM public.profile_type_definition AS p
LEFT JOIN public.profile AS p0 ON p.profile_id = p0.id
LEFT JOIN public.profile_type_definition AS p1 ON p.parent_id = p1.id or p.instance_parent_id = p1.id or p.variable_data_type_id = p1.id
LEFT JOIN public.profile AS p2 ON p1.profile_id = p2.id
WHERE ((p0.namespace <> p2.namespace) OR ((p0.namespace IS NULL))) AND (p2.id = 49)

-- attribute data types
SELECT p1.id, p1.access_level, p1.access_restrictions, p1.addl_data_json, p1.array_dimensions, p1.attribute_type_id, p1.opc_browse_name, p1.created, p1.created_by_id, p1.data_type_id, p1.data_variable_nodeids, p1.description, p1.display_name, p1.eu_range_access_level, p1.eu_range_modeling_rule, p1.eu_range_nodeid, p1.eng_unit_access_level, p1.eng_unit_id, p1.eng_unit_modeling_rule, p1.eng_unit_nodeid, p1.enum_value, p1.instrument_max_value, p1.instrument_min_value, p1.is_array, p1.is_required, p1.max_string_length, p1.max_value, p1.min_value, p1.minimum_sampling_interval, p1.modeling_rule, p1.name, p1.namespace, p1.opc_node_id, p1.profile_type_definition_id, p1.symbolic_name, p1.updated, p1.updated_by_id, p1.user_write_mask, p1.value_rank, p1.variable_type_definition_id, p1.write_mask
FROM public.profile_type_definition AS p
LEFT JOIN public.profile AS p0 ON p.profile_id = p0.id
INNER JOIN public.profile_attribute AS p1 ON p.id = p1.profile_type_definition_id
LEFT JOIN public.data_type AS d ON p1.data_type_id = d.id
LEFT JOIN public.profile_type_definition AS p2 ON d.custom_type_id = p2.id
LEFT JOIN public.profile AS p3 ON p2.profile_id = p3.id
WHERE ((p0.namespace <> p3.namespace) OR ((p0.namespace IS NULL))) AND (p3.id = 49)

SELECT t.id as type_id, t.name as type_name, d.* 
FROM public.data_type d
join profile_type_definition t on t.id = d.custom_type_id
WHERE d.custom_type_id in (
    SELECT ptd.id 
    FROM public.profile_type_definition ptd 
    JOIN public.profile p on p.id = ptd.profile_id
        AND p.id IN (49) 
    )

-- attribute variable types
SELECT p1.id, p1.access_level, p1.access_restrictions, p1.addl_data_json, p1.array_dimensions, p1.attribute_type_id, p1.opc_browse_name, p1.created, p1.created_by_id, p1.data_type_id, p1.data_variable_nodeids, p1.description, p1.display_name, p1.eu_range_access_level, p1.eu_range_modeling_rule, p1.eu_range_nodeid, p1.eng_unit_access_level, p1.eng_unit_id, p1.eng_unit_modeling_rule, p1.eng_unit_nodeid, p1.enum_value, p1.instrument_max_value, p1.instrument_min_value, p1.is_array, p1.is_required, p1.max_string_length, p1.max_value, p1.min_value, p1.minimum_sampling_interval, p1.modeling_rule, p1.name, p1.namespace, p1.opc_node_id, p1.profile_type_definition_id, p1.symbolic_name, p1.updated, p1.updated_by_id, p1.user_write_mask, p1.value_rank, p1.variable_type_definition_id, p1.write_mask
FROM public.profile_type_definition AS p
LEFT JOIN public.profile AS p0 ON p.profile_id = p0.id
INNER JOIN public.profile_attribute AS p1 ON p.id = p1.profile_type_definition_id
LEFT JOIN public.profile_type_definition AS p2 ON p1.variable_type_definition_id = p2.id
LEFT JOIN public.profile AS p3 ON p2.profile_id = p3.id
WHERE ((p0.namespace <> p3.namespace) OR ((p0.namespace IS NULL))) AND (p3.id = 49)

-- compositions
SELECT p1.id, p0.namespace, p1.opc_browse_name, p1.composition_id, p1.description, p1.is_event, p1.is_required, p1.modeling_rule, p1.name, p1.profile_type_definition_id, p1.reference_id, p1.reference_is_inverse
FROM public.profile_type_definition AS p
LEFT JOIN public.profile AS p0 ON p.profile_id = p0.id
INNER JOIN public.profile_composition AS p1 ON p.id = p1.profile_type_definition_id
LEFT JOIN public.profile_type_definition AS p2 ON p1.composition_id = p2.id
LEFT JOIN public.profile AS p3 ON p2.profile_id = p3.id
WHERE ((p0.namespace <> p3.namespace) OR ((p0.namespace IS NULL))) AND (p3.id = 49)

-- interfaces
SELECT p1.id, p.name, p0.id, p0.namespace, p2.name, p3.id, p3.namespace, p1.interface_id, p1.profile_type_definition_id
FROM public.profile_type_definition AS p
LEFT JOIN public.profile AS p0 ON p.profile_id = p0.id
INNER JOIN public.profile_interface AS p1 ON p.id = p1.profile_type_definition_id
LEFT JOIN public.profile_type_definition AS p2 ON p1.interface_id = p2.id
LEFT JOIN public.profile AS p3 ON p2.profile_id = p3.id
WHERE ((p0.namespace <> p3.namespace) OR ((p0.namespace IS NULL))) AND (p3.id = 49)

-- user analytics
SELECT p.id, p.extend_count, p.manual_rank, p.page_visit_count, p.profile_type_definition_id
FROM public.profile_type_definition_user_analytics AS p
INNER JOIN public.profile_type_definition AS p0 ON p.profile_type_definition_id = p0.id
LEFT JOIN public.profile AS p1 ON p0.profile_id = p1.id
WHERE p1.id = 49

-- user favorites
SELECT p.id, p.is_favorite, p.owner_id, p.profile_type_definition_id
FROM public.profile_type_definition_user_favorite AS p
INNER JOIN public.profile_type_definition AS p0 ON p.profile_type_definition_id = p0.id
LEFT JOIN public.profile AS p1 ON p0.profile_id = p1.id
WHERE p1.id = 49

-- data_type_rank
SELECT dtr.id, dt.id, p.id, p0.id, p.parent_id, p0.namespace, p.author_id, p.browse_name, p.created, p.created_by_id, p.description, p.document_url, p.author_name, p.instance_parent_id, p.is_abstract, p.is_option_set, p.metatags, p.name, p.opc_node_id, p.owner_id, p.parent_id, p.profile_id, p.type_id, p.symbolic_name, p.updated, p.updated_by_id, p.variable_array_dimensions, p.variable_data_type_id, p.variable_value, p.variable_value_rank
FROM public.profile_type_definition AS p
LEFT JOIN public.profile AS p0 ON p.profile_id = p0.id
JOIN public.data_type AS dt ON dt.custom_type_id = p.id
JOIN public.data_type_rank AS dtr ON dtr.data_type_id = dt.id
WHERE p0.id = 49
