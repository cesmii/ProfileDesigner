/*
	Stored Procedures / Functions: 
		get all ancestors for a type definition
		get all descendants for a type definition
		get all dependents for a type definition (uses get all descendants)
*/
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
		WHERE p.ua_standard_profile_id IS NOT NULL OR p.owner_id = _ownerId
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
	JOIN public.profile_type_definition t ON d.id = t.id
	JOIN public.profile p ON p.id = t.profile_id
	JOIN public.lookup l ON l.id = t.type_id
	;
	--ORDER BY d.level, parent_id;
end; $$ 

/*
--Execute the function
SELECT * FROM public.fn_profile_type_definition_get_descendants(16107, 40) d
WHERE d.level < 2
ORDER BY d.level, d.name
LIMIT 10 OFFSET 10;

*/

--
----
----
----
---
---
--
CREATE OR REPLACE FUNCTION public.sp_type_definitions_descendants(
	IN _id int, _ownerId int)     
RETURNS return_datatype   
LANGUAGE plpgsql  
AS $BODY$    
DECLARE    
declaration;    
[...] -- variable declaration   
 BEGIN    
< function_body >    
[...]  -- logic  
RETURN { variable_name | value }    
END;   
$BODY$  


/*
	Stored Procedure: sp_type_definitions_descendants
	Who: scoxen
	When: 2023-01-31
	Description: 
	Get a list of type defs which are descendants of this 
	type def. This is a recursive query.
	This also considers ownership. It will consider type defs associated with 
	standard nodeset profiles or profiles this owner has created.
*/
CREATE OR REPLACE PROCEDURE public.sp_type_definitions_descendants(
	IN _id int, _ownerId int)
LANGUAGE 'plpgsql'
AS $BODY$
begin

WITH RECURSIVE descendant AS (
    SELECT  t.id,
            t.name,
            t.parent_id,
			p.author_id as profile_author_id,
			p.ua_standard_profile_id,
			p.owner_id as profile_owner_id,
            0 AS level
    FROM public.profile_type_definition t
	JOIN public.profile p on p.id = t.profile_id
    WHERE t.id = 16107
 
    UNION ALL
 
    SELECT  t.id,
            t.name,
            t.parent_id,
			p.author_id as profile_author_id,
			p.ua_standard_profile_id,
			p.owner_id as profile_owner_id,
            level + 1
    FROM public.profile_type_definition t
	JOIN public.profile p on p.id = t.profile_id
	JOIN descendant d ON t.parent_id = d.id
	WHERE p.ua_standard_profile_id IS NOT NULL OR p.owner_id = 40
)
 
SELECT  d.id,
        d.name,
		d.profile_author_id,
		d.ua_standard_profile_id,
		d.profile_owner_id,
        a.id AS ancestor_id,
        a.name AS ancestor_first_name,
        d.level
FROM descendant d
JOIN public.profile_type_definition a
ON d.parent_id = a.id
ORDER BY level, ancestor_id;

end;
$BODY$;





--collect descendant profiles for the profile passed in, multiple generations
WITH RECURSIVE descendant AS (
    SELECT  t.id,
            t.name,
            t.parent_id,
			t.type_id, 
			p.author_id as profile_author_id,
			p.ua_standard_profile_id,
			p.owner_id as profile_owner_id,
            0 AS level
    FROM public.profile_type_definition t
	JOIN public.profile p on p.id = t.profile_id
    WHERE t.id = 13603
 
    UNION ALL
 
    SELECT  t.id,
            t.name,
            t.parent_id,
            t.type_id,
			p.author_id as profile_author_id,
			p.ua_standard_profile_id,
			p.owner_id as profile_owner_id,
            level + 1
    FROM public.profile_type_definition t
	JOIN public.profile p on p.id = t.profile_id
	JOIN descendant d ON t.parent_id = d.id
	WHERE 
		--ua and ua/di nodesets - shared
		(p.author_id IS NULL AND p.owner_id IS NULL AND p.ua_standard_profile_id IS NOT NULL)
		--could be custom profiles OR could be standard nodesets imported
		OR p.owner_id = 40
)
 
SELECT  d.id,
        d.name,
		d.profile_author_id,
		d.ua_standard_profile_id,
		d.profile_owner_id,
		d.parent_id,
        --a.id AS ancestor_id,
        --a.name AS ancestor_name,
        d.level
FROM descendant d
JOIN public.profile_type_definition a ON d.parent_id = a.id

--union with type defs that use this profile as a composition or interface
UNION

SELECT  t.id,
        t.name,
		p.author_id as profile_author_id,
		p.ua_standard_profile_id,
		p.owner_id as profile_owner_id,
		t.parent_id,
        --a.id AS ancestor_id,
        --a.name AS ancestor_name,
        1 as level
FROM public.profile_type_definition t 
JOIN public.profile p on p.id = t.profile_id
WHERE t.id IN (
	SELECT profile_type_definition_id FROM public.profile_composition WHERE composition_id = 13603
	UNION
	SELECT profile_type_definition_id FROM public.profile_interface WHERE interface_id = 13603
)

--union with type defs that have attributes that use a data type which points to a profile type def (2nd level)
UNION

SELECT  t.id,
        t.name,
		p.author_id as profile_author_id,
		p.ua_standard_profile_id,
		p.owner_id as profile_owner_id,
		t.parent_id,
        --a.id AS ancestor_id,
        --a.name AS ancestor_name,
        2 as level
FROM public.profile_type_definition t 
JOIN public.profile p on p.id = t.profile_id
WHERE t.id IN (
	SELECT distinct(t.id) -- , t.name, a.name, d.* 
	FROM public.profile_attribute a
	JOIN public.data_type d on d.id = a.data_type_id
	JOIN public.profile_type_definition t on t.id = a.profile_type_definition_id
	WHERE d.custom_type_id = 13603
)

