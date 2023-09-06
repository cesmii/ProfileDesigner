/*
--executing delete stored proc directly in PgSql
--this reveals FK issue update or delete on table "profile_type_definition" violates foreign key
--constraint "profile_type_def_parent_id_4ae5e508_fk_parent_id" on table "profile_type_definition"
--DETAIL:  Key (id)=(2224) is still referenced from table "profile_type_definition"
CALL public.sp_nodeset_delete('31');

--this reveals FK issue in profile_attribute 
--update or delete on table "data_type" violates foreign key constraint 
--"profile_attribute_data_type_id_304b6874_fk_data_type_id" on table "profile_attribute"
--DETAIL:  Key (id)=(304) is still referenced from table "profile_attribute".
CALL public.sp_nodeset_delete('32');

*/

--one row has type MaterialSublotType, owner_id null and code custom. 
--There is a 2nd row which appears duplicative with a valid owner_id and code
SELECT t.id as type_id, t.name as type_name, d.id as d_id, d.* 
FROM public.data_type d
join profile_type_definition t on t.id = d.custom_type_id
WHERE d.custom_type_id in (
	SELECT ptd.id 
	FROM public.profile_type_definition ptd 
	JOIN public.profile p on p.id = ptd.profile_id
		AND p.id IN (32) 
	)
--	AND d.owner_id IS NULL
--	AND d.code = 'custom'
;

--ROOT CAUSE ANALYSIS--
--Can't delete data type 304 because 
--		it has a reference to profile attribute 14584 (problematic), 5192 (good)
--			14584 has a reference to type_def 7151 (problematic)
--			5192 has a reference to type_def 2346 (good)
--				type_def 7151 is part of a different profile 44 and is owned by id 15 (ie someone else)
--				type_def 2346 is part of correct profile 32 and is owned by id 7 (good)
--Can't delete data type 304 because there is a reference to a profile which is not being deleted

/*
	--REMEDIATION - point the offending attribute record to a different profile type definition
	UPDATE public.profile_attribute
	SET profile_type_definition_id = 2346
	--SELECT * FROM public.profile_attribute
	WHERE id = 14584 and data_type_id = 304 and profile_type_definition_id=7151 
*/

--There are two rows id 5192, 14584 which points to the material sublot data type (id 304) which seems to be problematic
SELECT * 
FROM profile_attribute a
WHERE a.data_type_id in (304)
order by data_type_id;


--custom_type_id points to type_definition_id 2346
SELECT * 
FROM data_type d
WHERE d.id in (304)
order by id;

SELECT * 
FROM profile_type_definition t
WHERE 
	t.id in (SELECT custom_type_id FROM data_type WHERE id in (304))
	OR t.id in (SELECT profile_type_definition_id FROM profile_attribute WHERE data_type_id in (304))
order by id;

update profile_attribute
set type_definition_id = 488
where data_type_id = 487

-----
-----
-----

select 
	p.id as p_Id, right(p.namespace, 25) as p_Namespace, p.owner_id as p_Owner, t.name as t_Name,
	a.id as a_id,
	a.name as a_name,
	d.name as d_name,
	d.owner_id as d_owner_id,
	a.*,
	d.*,
	
--	pParent.id as parent_p_Id, right(pParent.namespace, 25) as parent_p_Namespace, pParent.owner_id as parent_p_Owner, tParent.name as parent_t_Name, 
	u.email_address, t.* 
from profile_attribute a
join data_type d on d.id = a.data_type_id
join profile_type_definition t on t.id = a.profile_type_definition_id
join profile p on p.id = t.profile_id
join public.user u on u.id = p.owner_id
where 
	p.id = 32 --AND pParent.id <> p.id
	and d.id = 304
	--AND d.owner_id <> p.owner_id
order by t.id, a.id, d.id
;

select 
	p.id as p_Id, right(p.namespace, 25) as p_Namespace, p.owner_id as p_Owner, t.name as t_Name,
	a.name,
	d.name,
	d.owner_id,
	a.*,
	d.*,
	
--	pParent.id as parent_p_Id, right(pParent.namespace, 25) as parent_p_Namespace, pParent.owner_id as parent_p_Owner, tParent.name as parent_t_Name, 
	u.email_address, t.* 
from profile_attribute a
join data_type d on d.id = a.data_type_id
join profile_type_definition t on t.id = a.profile_type_definition_id
join profile p on p.id = t.profile_id
join public.user u on u.id = p.owner_id
where 
	p.id = 49 --AND pParent.id <> p.id
	and d.id = 487
	--AND d.owner_id <> p.owner_id
order by t.id, a.id, d.id
;

select 
	p.id as p_Id, right(p.namespace, 25) as p_Namespace, p.owner_id as p_Owner, 
	d.name,
	d.owner_id,
	a.*,
	d.*,
	
--	pParent.id as parent_p_Id, right(pParent.namespace, 25) as parent_p_Namespace, pParent.owner_id as parent_p_Owner, tParent.name as parent_t_Name, 
	u.email_address, t.* 
from data_type d
--join profile_type_definition t on t.id = a.profile_type_definition_id
join profile p on p.id = t.profile_id
join public.user u on u.id = p.owner_id
where 
	p.id = 49 --AND pParent.id <> p.id
	and d.id = 487
	--AND d.owner_id <> p.owner_id
order by t.id, a.id, d.id


/*
select 
	u.display_name,
	p.id as p_Id, right(p.namespace, 25) as p_Namespace, p.owner_id as p_Owner, t.name as t_Name,
	pParent.id as parent_p_Id, right(pParent.namespace, 25) as parent_p_Namespace, pParent.owner_id as parent_p_Owner, tParent.name as parent_t_Name, 
	u.email_address, t.* 
from profile_type_definition t
join profile p on p.id = t.profile_id
join profile_type_definition tParent on tParent.id = t.parent_id
join profile pParent on pParent.id = tParent.profile_id
join public.user u on u.id = p.owner_id
where pParent.id = 32 AND pParent.id <> p.id
*/
