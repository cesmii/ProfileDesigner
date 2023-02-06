/*---------------------------------------------------------
	Stored Procedures / Functions: 
		get all ancestors for a type definition
		get all descendants for a type definition
		get all dependents for a type definition (uses get all descendants)
---------------------------------------------------------*/

/*---------------------------------------------------------
	Function: fn_profile_type_definition_get_descendants
---------------------------------------------------------*/
drop function if exists fn_profile_type_definition_get_descendants; 
create function fn_profile_type_definition_get_descendants (
   IN _id int, _ownerId int
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
	profile_id integer, 
	profile_author_id integer, 
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
			(p.owner_id IS NULL AND p.ua_standard_profile_id IS NOT NULL) --root nodesets
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
			p.id as profile_id,
			p.author_id as profile_author_id,
			p.namespace as profile_namespace,
			p.owner_id as profile_owner_id,
			p.publish_date as profile_publish_date,
			p.title as profile_title,
			p.version as profile_version,
			d.level
	FROM descendant d
	JOIN public.profile_type_definition t ON d.id = t.id AND t.id <> _id
	JOIN public.profile p ON p.id = t.profile_id
	JOIN public.lookup l ON l.id = t.type_id
	;
end; $$ 

/*
--test - Execute the function
SELECT * FROM public.fn_profile_type_definition_get_descendants(16107, 40) d
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
   IN _id int, _ownerId int
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
	profile_id integer, 
	profile_author_id integer, 
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
			(p.owner_id IS NULL AND p.ua_standard_profile_id IS NOT NULL) --root nodesets
			OR (p.owner_id = _ownerId)  --my nodesets or nodesets I imported
		
		UNION
		--union with type defs that use this profile as an interface
		SELECT  t.id,
				CAST(1 as integer) AS level
		FROM public.profile_type_definition t 
		JOIN public.profile_interface i on i.profile_type_definition_id = t.id AND i.interface_id = _id
		JOIN public.profile p ON p.id = t.profile_id
		WHERE 
			(p.owner_id IS NULL AND p.ua_standard_profile_id IS NOT NULL) --root nodesets
			OR (p.owner_id = _ownerId)  --my nodesets or nodesets I imported
		
		UNION
		--union with type defs that have attributes that use a data type which points to a profile type def (2nd level)
		SELECT  t.id,
				CAST(2 as integer) AS level
		FROM public.profile_type_definition t 
		JOIN public.profile p ON p.id = t.profile_id
		WHERE 
			((p.owner_id IS NULL AND p.ua_standard_profile_id IS NOT NULL) --root nodesets
			OR (p.owner_id = _ownerId)) AND  --my nodesets or nodesets I imported
			t.id IN (
			SELECT distinct(t.id) -- , t.name, a.name, d.* 
			FROM public.profile_attribute a
			JOIN public.data_type d on d.id = a.data_type_id
			JOIN public.profile_type_definition t on t.id = a.profile_type_definition_id
			WHERE d.custom_type_id = _id
		)

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
			p.id as profile_id,
			p.author_id as profile_author_id,
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
	UNION 
	SELECT * FROM public.fn_profile_type_definition_get_descendants(_id, _ownerId)
	;
end; $$ 

/*
--test - Execute the function
SELECT * FROM public.fn_profile_type_definition_get_dependencies(13603, 40) d
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
	profile_id integer, 
	profile_author_id integer, 
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
		--WHERE p.ua_standard_profile_id IS NOT NULL OR p.owner_id = _ownerId
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
			p.id as profile_id,
			p.author_id as profile_author_id,
			p.namespace as profile_namespace,
			p.owner_id as profile_owner_id,
			p.publish_date as profile_publish_date,
			p.title as profile_title,
			p.version as profile_version,
			d.level
	FROM ancestor d
	JOIN public.profile_type_definition t ON d.id = t.id AND t.id <> _id
	JOIN public.profile p ON p.id = t.profile_id
	JOIN public.lookup l ON l.id = t.type_id
	order by d.level, t.name
	;
end; $$ 

/*
--test - Execute the function
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
