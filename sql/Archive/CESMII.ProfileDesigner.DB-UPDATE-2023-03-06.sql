﻿---------------------------------------------------------------------
--  Profile Designer DB - Update
--	Date: 2023-02-06
--	Who: MarkusH
--	Details:
--	Remove standard nodeset table
--  functions - Add object id aad to returned result to determine ownership
---------------------------------------------------------------------

ALTER TABLE public.profile_attribute
	ADD COLUMN instrument_range_nodeid character varying(256) NULL,
	ADD COLUMN instrument_range_modeling_rule character varying(256) NULL,
	ADD COLUMN instrument_range_access_level integer NULL;

--ALTER TABLE public.profile_composition
--  ADD COLUMN opc_node_id character varying(256) NULL,
--  ADD COLUMN symbolic_name character varying(256) COLLATE pg_catalog."default" NULL,
--  ADD COLUMN metatags varchar NULL,
--  ADD COLUMN document_url character varying(512) COLLATE pg_catalog."default" NULL;


DROP VIEW IF EXISTS public.v_data_type_rank;

CREATE VIEW public.v_data_type_rank
AS
select 
	--lu.name, 
	--ptd.*, 
	dt.*, 
    baseDt.id as base_data_type_id,
	COALESCE(a.usage_count, 0) + COALESCE(dtr.manual_rank, 0) as popularity_index,
	--create a tiered system to distinguish between very popular and mildly popular and the others
	CASE 
		WHEN COALESCE(a.usage_count, 0) + COALESCE(dtr.manual_rank, 0) > 40 THEN 3
		WHEN COALESCE(a.usage_count, 0) + COALESCE(dtr.manual_rank, 0) > 20 THEN 2
		WHEN COALESCE(a.usage_count, 0) + COALESCE(dtr.manual_rank, 0) > 10 THEN 1
		ELSE 0 END as popularity_level,
	COALESCE(a.usage_count, 0) as usage_count, 
	COALESCE(dtr.manual_rank, 0) as manual_rank
from public.data_type dt
left outer join public.data_type_rank dtr on dtr.data_type_id = dt.id
left outer join public.profile_type_definition ptd on ptd.id = dt.custom_type_id
left outer join public.data_type baseDt on ptd.parent_id = baseDt.custom_type_id
--left outer join public.lookup lu on lu.id = ptd.type_id
left outer join (
	SELECT data_type_id, count(*) as usage_count 
	from public.profile_attribute 
	group by data_type_id
) a on a.data_type_id = dt.id
--order by 
-- 	 COALESCE(a.usage_count, 0) + COALESCE(dtr.manual_rank, 0) desc
-- 	,COALESCE(a.usage_count, 0) desc 
--	,dt.display_order
--	,dt.name
;

ALTER VIEW public.v_data_type_rank
    OWNER to profiledesigner;

/*---------------------------------------------------------
	Stored Procedures / Functions: 
		determine profile ownership - do as a function to ensure same rules in all uses
		get all ancestors for a type definition
		get all descendants for a type definition
		get all dependents for a type definition (uses get all descendants)
---------------------------------------------------------*/
/*---------------------------------------------------------
	Function: fn_profile_get_owner
---------------------------------------------------------*/
drop function if exists fn_profile_get_owner; 
create function fn_profile_get_owner (
   IN _id int, _ownerId int) 
/*
	Function: fn_profile_get_owner
	Who: scoxen
	When: 2023-03-27
	Description: 
	Determine if this profile is owned by this ownerId. 
	Return user
*/
returns table ( 
	id integer, 
	objectid_aad character varying(100) 
) 
language plpgsql
as $$
declare 
-- variable declaration
begin
	-- body
	return query
		SELECT u.id, u.objectid_aad  
		FROM public.profile p
		--determine ownership
		--has no cloudlibraryid and author id = passed in user id - owned by user
		--has cloudlibraryid and cloud library pending approval is true - owned by user
		--note Cloud library fields are managed and maintained by the API updating its values based on current
		--Cloud Lib (separate system). If there was any mismatch in values between this and CloudLib, it would be uncommon and updated through 
		--the normal course of visiting pages within the application.
		LEFT OUTER JOIN public.user u ON u.id = p.author_id AND
			(p.cloud_library_id IS NULL OR
			(p.cloud_library_id IS NOT NULL AND 
				COALESCE(p.cloud_library_pending_approval, FALSE) IS TRUE))
		WHERE 
			 p.id = _id AND
			(p.owner_id IS NULL --root nodesets
			 OR p.owner_id = _ownerId)  --my nodesets or nodesets I imported
	;
end; $$ 
;

/*---------------------------------------------------------
	Function: fn_profile_type_definition_get_descendants
---------------------------------------------------------*/
drop function if exists fn_profile_type_definition_get_descendants; 
create function fn_profile_type_definition_get_descendants (
   IN _id int, _ownerId int, _limitByType boolean, _excludeAbstract boolean
) 
/*
	Function: fn_profile_type_definition_get_descendants
	Who: scoxen
	When: 2023-01-31
	Description: 
	Get a list of type defs which are descendants of this 
	type def. This is a recursive query.
	This also considers ownership. It will consider type defs associated with 
	standard nodeset profiles or profiles this owner has created.
	Notes:
		allow for ordered, paged results
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
	WITH RECURSIVE descendant AS (
		SELECT  t.id,
				CAST(0 as integer) AS level
		FROM public.profile_type_definition t
		JOIN public.profile p on p.id = t.profile_id
		WHERE t.id = _id

		UNION ALL

		SELECT  t.id,
				CAST(d.level + 1 as integer) as level
		FROM public.profile_type_definition t
		JOIN public.profile p on p.id = t.profile_id
		JOIN descendant d ON t.parent_id = d.id
		WHERE
			(p.owner_id IS NULL) --root nodesets
			OR (p.owner_id = _ownerId)  --my nodesets or nodesets I imported
	)

	SELECT  d.id,
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
	FROM descendant d
	JOIN public.profile_type_definition t ON d.id = t.id 
		AND t.id <> _id
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
	;
end; $$ 
;
/*
--test - Execute the function
SELECT * FROM public.fn_profile_type_definition_get_descendants(13603, 40, true) d
WHERE 
	1 = (CASE WHEN is_abstract = false THEN 1
		 ELSE 0 END)

WHERE d.level < 2
ORDER BY d.level, d.name
LIMIT 10 OFFSET 10;
SELECT * FROM public.fn_profile_type_definition_get_descendants(16107, 40)  ORDER BY level,profile_title,profile_namespace,profile_version,profile_publish_date,name LIMIT 4

SELECT Count(*) FROM public.fn_profile_type_definition_get_descendants(16107, 40) d
WHERE d.level < 2
ORDER BY d.level, d.name
LIMIT 10 OFFSET 10;

*/

/*---------------------------------------------------------
	Function: fn_profile_type_definition_get_dependencies
---------------------------------------------------------*/
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
/*
--test - Execute the function
SELECT * FROM public.fn_profile_type_definition_get_dependencies(13603, 40, false, false) d
order by profile_title
--WHERE d.level < 2
--ORDER BY d.level, d.name
--LIMIT 10 OFFSET 10;
--SELECT * FROM public.fn_profile_type_definition_get_dependencies(16107, 40)  ORDER BY level,profile_title,profile_namespace,profile_version,profile_publish_date,name LIMIT 4

SELECT Count(*) FROM public.fn_profile_type_definition_get_dependencies(16107, 40) d
WHERE d.level < 2
ORDER BY d.level, d.name
LIMIT 10 OFFSET 10;

*/

/*---------------------------------------------------------
	Function: fn_profile_type_definition_get_ancestors
---------------------------------------------------------*/
drop function if exists fn_profile_type_definition_get_ancestors; 
create function fn_profile_type_definition_get_ancestors (
   IN _id int, _ownerId int
) 
/*
	Function: fn_profile_type_definition_get_ancestors
	Who: scoxen
	When: 2023-01-31
	Description: 
	Get a list of type defs which are parents, grandparents of this 
	type def. 
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
	WITH RECURSIVE ancestor AS (
		SELECT  t.id,
				t.parent_id,
				CAST(0 as integer) AS level
		FROM public.profile_type_definition t
		JOIN public.profile p on p.id = t.profile_id
		WHERE t.id = _id

		UNION ALL

		SELECT  t.id,
				t.parent_id,
				CAST(d.level - 1 as integer) as level
		FROM public.profile_type_definition t
		JOIN public.profile p on p.id = t.profile_id
		JOIN ancestor d ON d.parent_id = t.id
		--WHERE p.cloud_library_id IS NOT NULL OR p.owner_id = _ownerId
	)

	SELECT  d.id,
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
	FROM ancestor d
	JOIN public.profile_type_definition t ON d.id = t.id --AND t.id <> _id --include item itself
	JOIN public.profile p ON p.id = t.profile_id
	JOIN public.lookup l ON l.id = t.type_id
	--determine ownership
	LEFT OUTER JOIN public.fn_profile_get_owner(p.id, _ownerId) u on  u.id = p.author_id
	order by d.level, t.name
	;
end; $$ 

/*
--test - Execute the function
SELECT * FROM public.profile_type_definition where id = 13603

SELECT * FROM public.fn_profile_type_definition_get_ancestors(6149, 40) d
WHERE d.level < 2
ORDER BY d.level, d.name
LIMIT 10 OFFSET 10;
SELECT * FROM public.fn_profile_type_definition_get_dependencies(16107, 40)  ORDER BY level,profile_title,profile_namespace,profile_version,profile_publish_date,name LIMIT 4

SELECT Count(*) FROM public.fn_profile_type_definition_get_dependencies(16107, 40) d
WHERE d.level < 2
ORDER BY d.level, d.name
LIMIT 10 OFFSET 10;

*/
;
