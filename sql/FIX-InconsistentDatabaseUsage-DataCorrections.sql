--Type definitions - ISA95PropertyDataType
SELECT t.owner_id, u.display_name, u.email_address, t.profile_id, p.namespace, t.name, t.parent_id, t.*
FROM public.profile_type_definition t
JOIN public.user u on u.id = t.owner_id
JOIN public.profile p on p.id = t.profile_id
WHERE (name like 'ISA95PropertyDataType%' OR name like 'ISA95ParameterDataType')
	and t.owner_id not in (7, 11, 20, 92, 95, 102, 242, 198, 199, 222)
	--and t.owner_id in (222)
;

--Attributes - ISA95PropertyDataType, ISA95ParameterDataType
SELECT t.owner_id, u.display_name, t.profile_id, p.namespace, t.name as typedef_name, a.name, a.data_type_id
	, * -- a.* 
FROM public.profile_attribute a
JOIN public.data_type d on d.id = a.data_type_id
JOIN public.profile_type_definition t on t.id = a.profile_type_definition_id
JOIN public.profile p on p.id = t.profile_id
JOIN public.user u on u.id = t.owner_id
WHERE 
	--t.name like 'ISA95MaterialDataType' and
	a.name in ('Properties','Subproperties', 'Subparameters')
	--and t.owner_id IS NOT null
	--and opc_browse_name like '%ISA95-JOBCONTROL%'
	--and t.owner_id in (198,199,222,223)
	and t.owner_id not in (20, 92, 102, 242, 198, 199)
order by t.owner_id, t.name, a.name
;
	
--Data type - ISA95PropertyDataType, ISA95ParameterDataType
SELECT t.owner_id, t.profile_id, custom_type_id, t.name, dt.owner_id as dt_owner_id, dt.* 
FROM public.data_type dt
JOIN public.profile_type_definition t on t.id = dt.custom_type_id
WHERE dt.name in ('ISA95PropertyDataType', 'ISA95ParameterDataType')
	and t.owner_id not in (20)
	--and t.owner_id in (11, 199, 222, 223)
--ORDER BY dt.name, t.owner_id, t.profile_id
ORDER BY t.owner_id, t.profile_id, dt.name
;

/*
Steps to correct data. We need to re-assign the profile_type_definition which points to 
	data type ISA95PropertyDataType, ISA95ParameterDataType 
	Each user should have its own profile type definition pointing to a unique instance of this data type. 

	For each owner:
	1. Insert 2 record(s) into DataType row - ISA95PropertyDataType, ISA95ParameterDataType
	2. Assign custom_type_id to ISA95PropertyDataType, ISA95ParameterDataType for owner 
	3. Update "Properties" attributes to newly created data_type_id for owner
	3a. Update "Subproperties" attributes to newly created data_type_id for owner
	3b. Update "Subparameters" attributes to newly created data_type_id for owner

	Impacted Users: 
		1st user (no issue - leave): 7, 11
		Corrected: 20 (SC), 242 (PY), 92, 95, 102, 198, 199, 222
		Open: 

		"http://opcfoundation.org/UA/ISA95-JOBCONTROL" - 
			(4142, 4144)
		"http://opcfoundation.org/UA/ISA95-JOBCONTROL_V2/" - (5161, 5163)
	Check: 
		"http://opcfoundation.org/UA/TMC/v2/"
		
*/
--v1 version
INSERT INTO public.data_type(
	custom_type_id, code, name, description, display_order, use_min_max, use_eng_unit, is_numeric, is_active)
SELECT t.id, 'http://opcfoundation.org/UA/ISA95-JOBCONTROL.i=3002', 'ISA95PropertyDataType', null, 9998, false, false, false, true
	FROM public.profile_type_definition t
	WHERE name like 'ISA95PropertyDataType%' and 
		browse_name like 'http://opcfoundation.org/UA/ISA95-JOBCONTROL;%'
		and owner_id = 222
UNION SELECT t.id, 'http://opcfoundation.org/UA/ISA95-JOBCONTROL.i=3003', 'ISA95ParameterDataType', null, 9998, false, false, false, true
	FROM public.profile_type_definition t
	WHERE name like 'ISA95ParameterDataType%' and 
		browse_name like 'http://opcfoundation.org/UA/ISA95-JOBCONTROL;%' 
		and owner_id = 222
;

--v2 version
INSERT INTO public.data_type(
	custom_type_id, code, name, description, display_order, use_min_max, use_eng_unit, is_numeric, is_active)
SELECT t.id, 'http://opcfoundation.org/UA/ISA95-JOBCONTROL_V2/.i=3002', 'ISA95PropertyDataType', null, 9998, false, false, false, true
	FROM public.profile_type_definition t
	WHERE name like 'ISA95PropertyDataType%' and 
		browse_name like 'http://opcfoundation.org/UA/ISA95-JOBCONTROL_V2/;%'
		and owner_id = 222
UNION SELECT t.id, 'http://opcfoundation.org/UA/ISA95-JOBCONTROL_V2/.i=3003', 'ISA95ParameterDataType', null, 9998, false, false, false, true
	FROM public.profile_type_definition t
	WHERE name like 'ISA95ParameterDataType%' and 
		browse_name like 'http://opcfoundation.org/UA/ISA95-JOBCONTROL_V2/;%' 
		and owner_id = 222
;

--verify
SELECT * 
FROM public.data_type 
WHERE name in ('ISA95PropertyDataType', 'ISA95ParameterDataType') 
	and display_order = 9998
ORDER BY ID desc 
--Limit 1
;

--now take newly created data_type and update data_type_id for incorrect owners
--v1 - ISA95ParameterDataType
UPDATE public.profile_attribute
SET data_type_id = (SELECT ID FROM public.data_type 
	WHERE name like 'ISA95ParameterDataType' and code = 'http://opcfoundation.org/UA/ISA95-JOBCONTROL.i=3003' 
	ORDER BY ID desc Limit 1)
WHERE 
	data_type_id IN (4144)
	and id in (
		SELECT a.ID 
		FROM public.profile_attribute a 
		JOIN public.profile_type_definition t on t.id = a.profile_type_definition_id  
		WHERE 
			a.name in ('Subparameters')
			and owner_id in (222)
	)
;

--v2 - ISA95ParameterDataType
UPDATE public.profile_attribute
SET data_type_id = (SELECT ID FROM public.data_type 
	WHERE name like 'ISA95ParameterDataType' and code = 'http://opcfoundation.org/UA/ISA95-JOBCONTROL_V2/.i=3003' 
	ORDER BY ID desc Limit 1)
WHERE 
	data_type_id IN (5163)
	and id in (
		SELECT a.ID 
		FROM public.profile_attribute a 
		JOIN public.profile_type_definition t on t.id = a.profile_type_definition_id  
		WHERE 
			a.name in ('Subparameters')
			and owner_id in (222)
	)
;

--v1 - ISA95PropertyDataType
UPDATE public.profile_attribute
SET data_type_id = (SELECT ID FROM public.data_type 
	WHERE name like 'ISA95PropertyDataType' and code = 'http://opcfoundation.org/UA/ISA95-JOBCONTROL.i=3002' 
	ORDER BY ID desc Limit 1)
WHERE 
	data_type_id IN (4142)
	and id in (
		SELECT a.ID 
		FROM public.profile_attribute a 
		JOIN public.profile_type_definition t on t.id = a.profile_type_definition_id  
		WHERE 
			a.name in ('Properties', 'Subproperties')
			and owner_id in (222)
	)
;

--v2 - ISA95PropertyDataType
UPDATE public.profile_attribute
SET data_type_id = (SELECT ID FROM public.data_type 
	WHERE name like 'ISA95PropertyDataType' and code = 'http://opcfoundation.org/UA/ISA95-JOBCONTROL_V2/.i=3002' 
	ORDER BY ID desc Limit 1)
WHERE 
	data_type_id IN (5161)
	and id in (
		SELECT a.ID 
		FROM public.profile_attribute a 
		JOIN public.profile_type_definition t on t.id = a.profile_type_definition_id  
		WHERE 
			a.name in ('Properties', 'Subproperties')
			and owner_id in (222)
	)
;

