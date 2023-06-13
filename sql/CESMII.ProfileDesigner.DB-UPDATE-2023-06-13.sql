---------------------------------------------------------------------
--  Profile Designer DB - Update
--	Date: 2023-06-13
--	Who: SeanC
--	Details:
--	Minor enhancement to get dependencies function to check if item is parent of other items
---------------------------------------------------------------------
drop function if exists fn_profile_type_definition_get_dependencies; 
create function fn_profile_type_definition_get_dependencies (
   IN _id int, _ownerId int, _limitByType boolean, _excludeAbstract boolean
) 
/*
	Function: fn_profile_type_definition_get_dependencies
	Who: scoxen
	When: 2023-01-31
	Description: 
	Get a list of type defs which are descendants OR dependencies of this 
	type def. Call the descendants function to get those items. 
	Then call union queries to find the dependencies based on compositions or data type usage
*/
returns table ( 
	id integer, 
	browse_name character varying(256), 
	description character varying, 
	is_abstract boolean, 
	name character varying(512), 
	opc_node_id character varying(100), 
	parent_id integer, 
	type_id integer, 
	type_name character varying(256), 
    variable_data_type_id integer,
	profile_id integer, 
	profile_author_id integer, 
	profile_author_objectid_aad character varying(100), 
	profile_namespace character varying(400), 
	profile_owner_id integer, 
	profile_publish_date timestamp with time zone, 
	profile_title text, 
	profile_version character varying(25), 
	level integer
) 
language plpgsql
as $$
declare 
-- variable declaration
begin
	-- body
	return query

	WITH dependants AS (
		--union with type defs that use this type def as a parent
		SELECT  t.id,
				CAST(1 as integer) AS level
		FROM public.profile_type_definition t 
		WHERE t.parent_id = _id
		UNION
		--union with type defs that use this profile as a composition
		SELECT  t.id,
				CAST(1 as integer) AS level
		FROM public.profile_type_definition t 
		JOIN public.profile_composition c on c.profile_type_definition_id = t.id AND c.composition_id = _id
		JOIN public.profile p ON p.id = t.profile_id
		WHERE 
			(p.owner_id IS NULL) --root nodesets
			OR (p.owner_id = _ownerId)  --my nodesets or nodesets I imported
		
		UNION
		--union with type defs that use this profile as an interface
		SELECT  t.id,
				CAST(1 as integer) AS level
		FROM public.profile_type_definition t 
		JOIN public.profile_interface i on i.profile_type_definition_id = t.id AND i.interface_id = _id
		JOIN public.profile p ON p.id = t.profile_id
		WHERE 
			(p.owner_id IS NULL) --root nodesets
			OR (p.owner_id = _ownerId)  --my nodesets or nodesets I imported
		
		UNION
		--union with type defs that have attributes that use a data type which points to a profile type def (2nd level)
		SELECT  t.id,
				CAST(2 as integer) AS level
		FROM public.profile_type_definition t 
		JOIN public.profile p ON p.id = t.profile_id
		WHERE 
			((p.owner_id IS NULL) --root nodesets
			OR (p.owner_id = _ownerId)) AND  --my nodesets or nodesets I imported
			t.id IN (
			SELECT distinct(t.id) -- , t.name, a.name, d.* 
			FROM public.profile_attribute a
			JOIN public.data_type d on d.id = a.data_type_id
			JOIN public.profile_type_definition t on t.id = a.profile_type_definition_id
			WHERE d.custom_type_id = _id
		)

	)

	--adding extra wrapping sql statement to get the distinct values
	--a type def could be a dependency in multiple scenarios
	SELECT
			dFinal.id,
			dFinal.browse_name,
			dFinal.description,
			dFinal.is_abstract,
			dFinal.name,
			dFinal.opc_node_id,
			dFinal.parent_id,
			dFinal.type_id,
			dFinal.type_name,
			dFinal.variable_data_type_id,
			dFinal.profile_id,
			dFinal.profile_author_id,
			dFinal.profile_author_objectid_aad,
			dFinal.profile_namespace,
			dFinal.profile_owner_id,
			dFinal.profile_publish_date,
			dFinal.profile_title,
			dFinal.profile_version,
			min(dFinal.level) as level
	FROM (SELECT  d.id,
			t.browse_name,
			t.description,
			t.is_abstract,
			t.name,
			t.opc_node_id,
			t.parent_id,
			t.type_id,
			l.name as type_name,
			t.variable_data_type_id,
			p.id as profile_id,
			u.id as profile_author_id,
			u.objectid_aad as profile_author_objectid_aad,
			p.namespace as profile_namespace,
			p.owner_id as profile_owner_id,
			p.publish_date as profile_publish_date,
			p.title as profile_title,
			p.version as profile_version,
			d.level
	FROM dependants d
	JOIN public.profile_type_definition t ON d.id = t.id AND t.id <> _id
	JOIN public.profile p ON p.id = t.profile_id
	JOIN public.lookup l ON l.id = t.type_id
	--determine ownership
	LEFT OUTER JOIN public.fn_profile_get_owner(p.id, _ownerId) u on  u.id = p.author_id
	WHERE 
	--rule: always exclude object and method
	l.id NOT IN (11, 20)   
	--optional parameters
	AND 1 = (CASE WHEN _limitByType = false THEN 1
		 WHEN _limitByType = true AND t.type_id IN (SELECT t1.type_id FROM public.profile_type_definition t1 WHERE t1.id = _id) THEN 1
		 ELSE 0 END)
	--optional parameters
	AND 1 = (CASE WHEN _excludeAbstract = false THEN 1
		 WHEN _excludeAbstract = true AND t.is_abstract = false THEN 1
		 ELSE 0 END)
	UNION 
	SELECT * FROM public.fn_profile_type_definition_get_descendants(_id, _ownerId, _limitByType, _excludeAbstract)
	) as dFinal
	group by 
			dFinal.id,
			dFinal.browse_name,
			dFinal.description,
			dFinal.is_abstract,
			dFinal.name,
			dFinal.opc_node_id,
			dFinal.parent_id,
			dFinal.type_id,
			dFinal.type_name,
			dFinal.variable_data_type_id,
			dFinal.profile_id,
			dFinal.profile_author_id,
			dFinal.profile_author_objectid_aad,
			dFinal.profile_namespace,
			dFinal.profile_owner_id,
			dFinal.profile_publish_date,
			dFinal.profile_title,
			dFinal.profile_version	
	;
end; $$ 
;
ALTER FUNCTION public.fn_profile_type_definition_get_dependencies(integer, integer, boolean, boolean)
    OWNER TO postgres;
